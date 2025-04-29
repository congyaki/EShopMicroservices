# Leader Election trong EShop Microservices

Tài liệu này hướng dẫn cách sử dụng Leader Election trong hệ thống EShop Microservices.

## Giới thiệu

Leader Election là một pattern quan trọng trong hệ thống phân tán, cho phép chỉ định một instance duy nhất (leader) để thực hiện một số tác vụ đặc biệt như:
- Xử lý scheduled jobs
- Đồng bộ dữ liệu
- Migration database
- Cleanup dữ liệu
- Gửi thông báo định kỳ

Chúng ta đã triển khai hai implementation của Leader Election:
1. `ConsulLeaderElectionService` - sử dụng cho môi trường Development với Docker Compose
2. `KubernetesLeaderElectionService` - sử dụng cho môi trường Production với Kubernetes

## Cách sử dụng Leader Election trong mã nguồn

Để sử dụng Leader Election trong code, bạn chỉ cần inject `ILeaderElectionService` và gọi phương thức `IsLeaderAsync()`:

```csharp
public class SomeService
{
    private readonly ILeaderElectionService _leaderElectionService;
    
    public SomeService(ILeaderElectionService leaderElectionService)
    {
        _leaderElectionService = leaderElectionService;
    }
    
    public async Task DoSomethingAsync()
    {
        // Kiểm tra xem instance này có phải là leader không
        if (await _leaderElectionService.IsLeaderAsync())
        {
            // Chỉ thực hiện tác vụ này trên leader
            await PerformLeaderOnlyTaskAsync();
        }
    }
}
```

## Cấu hình môi trường Development (Docker Compose)

Trong môi trường development, chúng ta sử dụng Consul cho Leader Election.

### Khởi động môi trường Development

```bash
cd src
docker-compose up -d
```

Consul UI sẽ có sẵn tại http://localhost:8500.

### Cấu hình trong appsettings.json

```json
{
  "LeaderElection": {
    "Provider": "Consul",
    "ServiceName": "your-service-name"
  },
  
  "Consul": {
    "Address": "http://consul:8500"
  }
}
```

## Cấu hình môi trường Production (Kubernetes)

Trong môi trường production, chúng ta sử dụng Kubernetes Lease API cho Leader Election.

### Triển khai ConfigMap

```bash
kubectl apply -f kubernetes/infrastructure/leader-election/configmap.yaml
```

### Cấu hình trong Deployment

Thêm các biến môi trường vào Deployment:

```yaml
env:
- name: LeaderElection__Provider
  value: "Kubernetes"
- name: LeaderElection__ServiceName
  value: "your-service-name"
- name: LeaderElection__Namespace
  value: "your-namespace"
```

Hoặc mount ConfigMap:

```yaml
volumeMounts:
- name: leader-election-config
  mountPath: /app/leader-election-config
  
volumes:
- name: leader-election-config
  configMap:
    name: leader-election-config
```

## Quyền truy cập cần thiết

### Kubernetes RBAC

Trong môi trường Kubernetes, service account cần có quyền tạo và cập nhật Leases. Đảm bảo bạn có quyền hạn phù hợp bằng cách tạo role và roleBinding:

```yaml
apiVersion: rbac.authorization.k8s.io/v1
kind: Role
metadata:
  name: leader-election-role
  namespace: eshop-microservices
rules:
- apiGroups: ["coordination.k8s.io"]
  resources: ["leases"]
  verbs: ["get", "create", "update", "list", "watch"]

---
apiVersion: rbac.authorization.k8s.io/v1
kind: RoleBinding
metadata:
  name: leader-election-rolebinding
  namespace: eshop-microservices
subjects:
- kind: ServiceAccount
  name: default  # Hoặc tên service account của bạn
  namespace: eshop-microservices
roleRef:
  kind: Role
  name: leader-election-role
  apiGroup: rbac.authorization.k8s.io
```

## Kiểm tra Leader Status

### Trong Consul

Truy cập Consul UI (http://localhost:8500) và kiểm tra key/value store tại đường dẫn `service/{service-name}/leader`.

### Trong Kubernetes

Sử dụng lệnh sau để kiểm tra các lease:

```bash
kubectl get lease -n eshop-microservices
```

Hoặc xem chi tiết một lease cụ thể:

```bash
kubectl describe lease {service-name}-leader -n eshop-microservices
```

## Khắc phục sự cố

### Vấn đề thường gặp với Consul

- Đảm bảo Consul đang chạy và có thể truy cập từ các service
- Kiểm tra log của service để xem các lỗi liên quan đến session

### Vấn đề thường gặp với Kubernetes

- Đảm bảo service account có quyền truy cập vào Kubernetes Lease API
- Kiểm tra log của pod để xem các lỗi liên quan đến permissions
- Sử dụng `kubectl describe lease` để xem trạng thái hiện tại của lease

## Ví dụ tham khảo

Xem `ScheduledTaskService` trong Catalog.API để tham khảo cách triển khai một background service chỉ chạy trên leader.