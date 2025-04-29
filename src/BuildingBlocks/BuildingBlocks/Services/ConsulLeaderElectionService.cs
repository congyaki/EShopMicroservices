using Consul;
using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BuildingBlocks.Services
{
    /// <summary>
    /// Implementation của Leader Election sử dụng Consul cho môi trường Development
    /// </summary>
    public class ConsulLeaderElectionService : ILeaderElectionService
    {
        private readonly IConsulClient _consulClient;
        private readonly string _serviceName;
        private readonly string _serviceId;
        private readonly ILogger<ConsulLeaderElectionService> _logger;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private bool? _isLeader = null;
        private readonly TimeSpan _sessionTTL = TimeSpan.FromSeconds(15);
        private string? _sessionId;
        private CancellationTokenSource? _sessionRenewalCts;

        public ConsulLeaderElectionService(
            IConsulClient consulClient,
            string serviceName,
            string serviceId,
            ILogger<ConsulLeaderElectionService> logger)
        {
            _consulClient = consulClient;
            _serviceName = serviceName;
            _serviceId = serviceId;
            _logger = logger;
        }

        public async Task<bool> IsLeaderAsync()
        {
            // Nếu đã xác định là leader, trả về kết quả ngay
            if (_isLeader.HasValue)
            {
                return _isLeader.Value;
            }

            await _lock.WaitAsync();
            try
            {
                // Kiểm tra lại trong trường hợp một thread khác đã đặt giá trị
                if (_isLeader.HasValue)
                {
                    return _isLeader.Value;
                }

                string leaderKey = $"service/{_serviceName}/leader";

                // Tạo session nếu chưa tồn tại
                if (string.IsNullOrEmpty(_sessionId))
                {
                    _sessionId = await CreateSessionAsync();
                    if (string.IsNullOrEmpty(_sessionId))
                    {
                        _isLeader = false;
                        return false;
                    }
                }

                // Thử lấy lock với session hiện tại
                var lockAcquired = await AcquireLockAsync(leaderKey, _sessionId);

                _isLeader = lockAcquired;

                if (lockAcquired)
                {
                    _logger.LogInformation("Instance {ServiceId} đã được bầu làm leader cho {ServiceName}",
                        _serviceId, _serviceName);

                    // Bắt đầu task gia hạn session
                    StartSessionRenewal(_sessionId);
                }
                else
                {
                    _logger.LogInformation("Instance {ServiceId} không phải là leader cho {ServiceName}",
                        _serviceId, _serviceName);
                }

                return lockAcquired;
            }
            finally
            {
                _lock.Release();
            }
        }

        private async Task<string> CreateSessionAsync()
        {
            try
            {
                var sessionEntry = new SessionEntry
                {
                    Name = $"{_serviceName}-leader-election",
                    TTL = _sessionTTL,
                    Behavior = SessionBehavior.Release // Tự động giải phóng locks khi session hết hạn
                };

                var sessionResponse = await _consulClient.Session.Create(sessionEntry);
                var sessionId = sessionResponse.Response;

                _logger.LogInformation("Đã tạo session mới cho leader election: {SessionId}", sessionId);

                return sessionId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo Consul session");
                return null;
            }
        }

        private async Task<bool> AcquireLockAsync(string key, string sessionId)
        {
            try
            {
                var kvPair = new KVPair(key)
                {
                    Value = Encoding.UTF8.GetBytes(_serviceId),
                    Session = sessionId
                };

                var acquireResult = await _consulClient.KV.Acquire(kvPair);
                return acquireResult.Response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy Consul lock với key {Key}", key);
                return false;
            }
        }

        private void StartSessionRenewal(string sessionId)
        {
            // Hủy task gia hạn cũ nếu có
            _sessionRenewalCts?.Cancel();
            _sessionRenewalCts = new CancellationTokenSource();

            var token = _sessionRenewalCts.Token;

            // Gia hạn session định kỳ để duy trì vai trò leader
            _ = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested && _isLeader == true)
                {
                    try
                    {
                        // Đợi một khoảng thời gian
                        await Task.Delay(TimeSpan.FromSeconds(_sessionTTL.TotalSeconds / 3), token);

                        if (token.IsCancellationRequested)
                            break;

                        // Gia hạn session
                        var renewResult = await _consulClient.Session.Renew(sessionId);
                        if (renewResult.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            _logger.LogDebug("Đã gia hạn Consul session {SessionId}", sessionId);
                        }
                        else
                        {
                            // Session đã hết hạn, tạo mới và thử lấy lock lại
                            _logger.LogWarning("Session {SessionId} đã hết hạn, tạo mới", sessionId);
                            _isLeader = null; // Đặt lại trạng thái để thử lấy lock lại ở lần gọi IsLeaderAsync tiếp theo
                            break;
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        // Bị hủy bởi token, thoát khỏi vòng lặp
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Lỗi khi gia hạn Consul session");

                        // Nếu không thể gia hạn, không còn là leader nữa
                        _isLeader = false;
                        break;
                    }
                }
            }, token);
        }
    }
}