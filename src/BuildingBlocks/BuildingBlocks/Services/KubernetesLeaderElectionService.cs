using k8s;
using k8s.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BuildingBlocks.Services
{
    /// <summary>
    /// Implementation of Leader Election sử dụng Kubernetes Lease API
    /// </summary>
    public class KubernetesLeaderElectionService : ILeaderElectionService
    {
        private readonly IKubernetes _kubernetesClient;
        private readonly string _serviceName;
        private readonly string _serviceId;
        private readonly string _namespace;
        private readonly ILogger<KubernetesLeaderElectionService> _logger;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private bool? _isLeader = null;
        private readonly TimeSpan _leaseDuration = TimeSpan.FromSeconds(15);
        private CancellationTokenSource? _leaderRenewalCts;

        public KubernetesLeaderElectionService(
            IKubernetes kubernetesClient,
            string serviceName,
            string serviceId,
            string @namespace,
            ILogger<KubernetesLeaderElectionService> logger)
        {
            _kubernetesClient = kubernetesClient;
            _serviceName = serviceName;
            _serviceId = serviceId;
            _namespace = @namespace;
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

                string leaseName = $"{_serviceName}-leader";

                // Kiểm tra xem lease đã tồn tại chưa
                V1Lease? existingLease = null;
                try
                {
                    existingLease = await _kubernetesClient.CoordinationV1.ReadNamespacedLeaseAsync(
                        leaseName, 
                        _namespace);
                }
                catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    // Lease không tồn tại, chúng ta sẽ tạo mới
                    _logger.LogInformation("Lease {LeaseName} không tồn tại, sẽ tạo mới", leaseName);
                }

                if (existingLease != null)
                {
                    // Kiểm tra xem lease hiện tại còn hiệu lực và có thuộc về instance khác không
                    if (existingLease.Spec.HolderIdentity != null && 
                        existingLease.Spec.HolderIdentity != _serviceId)
                    {
                        // Lease thuộc về instance khác, kiểm tra xem nó đã hết hạn chưa
                        var leaseRenewTime = existingLease.Spec.RenewTime;
                        if (leaseRenewTime.HasValue && 
                            (DateTime.UtcNow - leaseRenewTime.Value) < _leaseDuration)
                        {
                            // Lease còn hiệu lực và thuộc về instance khác
                            _isLeader = false;
                            return false;
                        }
                        
                        // Lease đã hết hạn, chúng ta có thể chiếm lấy
                        _logger.LogInformation("Lease {LeaseName} đã hết hạn, sẽ chiếm lấy", leaseName);
                    }
                }

                // Tạo hoặc cập nhật lease
                var lease = new V1Lease
                {
                    Metadata = new V1ObjectMeta
                    {
                        Name = leaseName,
                        NamespaceProperty = _namespace
                    },
                    Spec = new V1LeaseSpec
                    {
                        HolderIdentity = _serviceId,
                        LeaseDurationSeconds = (int)_leaseDuration.TotalSeconds,
                        AcquireTime = DateTime.UtcNow,
                        RenewTime = DateTime.UtcNow
                    }
                };

                try
                {
                    if (existingLease == null)
                    {
                        // Tạo lease mới
                        await _kubernetesClient.CoordinationV1.CreateNamespacedLeaseAsync(
                            lease, 
                            _namespace);
                    }
                    else
                    {
                        // Cập nhật lease hiện tại
                        await _kubernetesClient.CoordinationV1.ReplaceNamespacedLeaseAsync(
                            lease, 
                            leaseName, 
                            _namespace);
                    }

                    _isLeader = true;
                    _logger.LogInformation("Instance {ServiceId} đã được bầu làm leader cho {ServiceName}", 
                        _serviceId, _serviceName);

                    // Khởi động task gia hạn lease
                    StartLeaseRenewal(leaseName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi tạo/cập nhật lease {LeaseName}", leaseName);
                    _isLeader = false;
                }

                return _isLeader.Value;
            }
            finally
            {
                _lock.Release();
            }
        }

        private void StartLeaseRenewal(string leaseName)
        {
            // Cancel previous renewal task if any
            _leaderRenewalCts?.Cancel();
            _leaderRenewalCts = new CancellationTokenSource();
            
            var token = _leaderRenewalCts.Token;
            
            // Gia hạn lease định kỳ để duy trì vai trò leader
            _ = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested && _isLeader == true)
                {
                    try
                    {
                        // Đợi một khoảng thời gian
                        await Task.Delay(TimeSpan.FromSeconds(_leaseDuration.TotalSeconds / 3), token);
                        
                        if (token.IsCancellationRequested)
                            break;

                        // Đọc lease hiện tại
                        var currentLease = await _kubernetesClient.CoordinationV1.ReadNamespacedLeaseAsync(
                            leaseName, 
                            _namespace, 
                            cancellationToken: token);

                        // Kiểm tra xem chúng ta vẫn còn là leader
                        if (currentLease.Spec.HolderIdentity != _serviceId)
                        {
                            _logger.LogWarning("Instance {ServiceId} không còn là leader cho {ServiceName}",
                                _serviceId, _serviceName);
                            _isLeader = false;
                            break;
                        }

                        // Cập nhật thời gian gia hạn
                        currentLease.Spec.RenewTime = DateTime.UtcNow;
                        
                        // Gia hạn lease
                        await _kubernetesClient.CoordinationV1.ReplaceNamespacedLeaseAsync(
                            currentLease, 
                            leaseName, 
                            _namespace, 
                            cancellationToken: token);
                        
                        _logger.LogDebug("Instance {ServiceId} đã gia hạn leader lease cho {ServiceName}",
                            _serviceId, _serviceName);
                    }
                    catch (TaskCanceledException)
                    {
                        // Bị hủy bởi token, thoát khỏi vòng lặp
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Lỗi khi gia hạn leader lease");
                        
                        // Nếu không thể gia hạn, không còn là leader nữa
                        _isLeader = false;
                        break;
                    }
                }
            }, token);
        }
    }
}