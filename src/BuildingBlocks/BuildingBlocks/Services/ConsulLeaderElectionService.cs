using Consul;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BuildingBlocks.Services
{
    public interface ILeaderElectionService
    {
        Task<bool> IsLeaderAsync();
    }

    public class ConsulLeaderElectionService : ILeaderElectionService
    {
        private readonly IConsulClient _consulClient;
        private readonly string _serviceName;
        private readonly string _serviceId;
        private readonly ILogger<ConsulLeaderElectionService> _logger;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private bool? _isLeader = null;
        private string _sessionId = null;

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
                // Check again in case another thread already set it
                if (_isLeader.HasValue)
                {
                    return _isLeader.Value;
                }

                // Leader key trong Consul KV store
                var leaderKey = $"services/{_serviceName}/leader";

                // Tạo session mới nếu chưa có
                if (string.IsNullOrEmpty(_sessionId))
                {
                    var sessionResponse = await _consulClient.Session.Create(new SessionEntry
                    {
                        Name = $"{_serviceName}-{_serviceId}",
                        TTL = TimeSpan.FromSeconds(15),
                        Behavior = SessionBehavior.Delete
                    });

                    _sessionId = sessionResponse.Response;
                    _logger.LogInformation("Created session {SessionId} for leader election", _sessionId);
                }

                // Thử acquire lock
                var acquireResult = await _consulClient.KV.Acquire(new KVPair(leaderKey)
                {
                    Session = _sessionId,
                    Value = System.Text.Encoding.UTF8.GetBytes(_serviceId)
                });

                _isLeader = acquireResult.Response;
                
                if (_isLeader.Value)
                {
                    _logger.LogInformation("Instance {ServiceId} elected as leader for {ServiceName}", 
                        _serviceId, _serviceName);
                    
                    // Renew session periodically to maintain leadership
                    _ = Task.Run(async () =>
                    {
                        while (_isLeader.Value)
                        {
                            try
                            {
                                await Task.Delay(TimeSpan.FromSeconds(10));
                                await _consulClient.Session.Renew(_sessionId);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error renewing leader session");
                                _isLeader = false;
                                break;
                            }
                        }
                    });
                }
                else
                {
                    _logger.LogInformation("Instance {ServiceId} is not the leader for {ServiceName}", 
                        _serviceId, _serviceName);
                }

                return _isLeader.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during leader election");
                return false;
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}