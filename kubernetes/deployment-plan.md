# Kế hoạch triển khai EShopMicroservices lên Kubernetes

## Tổng quan cấu trúc
Hệ thống EShopMicroservices gồm các thành phần chính:
- Kong API Gateway làm entrypoint (các lựa chọn thay thế: Nginx, Ocelot, Traefik, YARP)
- 5 microservices: Auth, Catalog, Basket, Discount, Ordering
- Các cơ sở dữ liệu: PostgreSQL (Auth, Catalog), Redis (Basket), SQL Server (Ordering), SQLite (Discount)
- Message broker: RabbitMQ
- Monitoring: Prometheus, Grafana

## Thứ tự triển khai

### 1. Thiết lập môi trường cơ bản
```bash
# Tạo các namespaces cần thiết (eshop-microservices, gateway và monitoring)
kubectl apply -f namespaces.yaml

# Kiểm tra các namespace đã tạo thành công
kubectl get namespaces | grep -E 'eshop-microservices|gateway|monitoring'
```

### 2. Triển khai shared resources
```bash
# Triển khai shared configmap và secrets
kubectl apply -f services/shared/configmap.yaml
kubectl apply -f services/shared/secret.yaml

# Triển khai RabbitMQ cho message bus
kubectl apply -f services/shared/rabbitmq.yaml

# Kiểm tra các resources đã được tạo
kubectl get configmap -n eshop-microservices
kubectl get secret -n eshop-microservices
kubectl get pods -n eshop-microservices -l app=rabbitmq
```

### 3. Triển khai các database
```bash
# Auth database (PostgreSQL)
kubectl apply -f services/auth/auth-postgres.yaml

# Catalog database (PostgreSQL)
kubectl apply -f services/catalog/catalog-postgres.yaml
# Basket database (PostgreSQL)
kubectl apply -f services/basket/basket-postgres.yaml
# Basket database (Redis)
kubectl apply -f services/basket/basket-redis.yaml

# Discount database (SQLite PVC)
kubectl apply -f services/discount/discount-sqlite-pvc.yaml

# Ordering database (SQL Server)
kubectl apply -f services/ordering/ordering-sqlserver.yaml

# Kiểm tra trạng thái của các database pods
kubectl get pods -n eshop-microservices | grep -E 'postgres|redis|sqlserver'

# Đợi các database sẵn sàng
kubectl wait --for=condition=ready pod -l app=auth-postgres -n eshop-microservices --timeout=120s
kubectl wait --for=condition=ready pod -l app=catalog-postgres -n eshop-microservices --timeout=120s
kubectl wait --for=condition=ready pod -l app=basket-redis -n eshop-microservices --timeout=120s
kubectl wait --for=condition=ready pod -l app=ordering-sqlserver -n eshop-microservices --timeout=120s
```

### 3.1. Triển khai Database Migration và Seed Data
```bash
# Tạo ConfigMap chứa các script SQL để migration và seed data
kubectl apply -f migrations/db-migrations-config.yaml

# Triển khai Job để thực thi migration và seed data
kubectl apply -f migrations/database-migrator-job.yaml

# Kiểm tra trạng thái của job
kubectl get jobs -n eshop-microservices

# Đợi Job hoàn thành
kubectl wait --for=condition=complete job/database-migrator-job -n eshop-microservices --timeout=300s

# Kiểm tra logs của job để xác nhận migration đã thành công
kubectl logs job/database-migrator-job -n eshop-microservices
```

Tất cả migration và seed data được thực hiện thông qua Kubernetes Job có tên database-migrator-job. Job này sẽ:
1. Sử dụng ConfigMap chứa các script SQL
2. Cài đặt các công cụ cần thiết (PostgreSQL client, SQL Server tools, SQLite)
3. Thực thi migration và seed data cho tất cả database
4. Chỉ chạy một lần duy nhất khi triển khai hệ thống

### 4. Triển khai các microservices
```bash
# Discount Service (gRPC) - nên triển khai trước vì Basket service phụ thuộc vào nó
kubectl apply -f services/discount/discount-grpc.yaml

# Đợi Discount service sẵn sàng
kubectl wait --for=condition=available deployment/discount-grpc -n eshop-microservices --timeout=120s

# Auth Service
kubectl apply -f services/auth/auth-api.yaml

# Catalog Service 
kubectl apply -f services/catalog/catalog-api.yaml

# Basket Service (phụ thuộc vào Discount service qua gRPC)
kubectl apply -f services/basket/basket-api.yaml

# Ordering Service (phụ thuộc vào message broker và các service khác)
kubectl apply -f services/ordering/ordering-api.yaml

# Kiểm tra tất cả pods đã chạy
kubectl get pods -n eshop-microservices
kubectl get services -n eshop-microservices
```

### 5. Triển khai Network Policies và HPA
```bash
# Triển khai Network Policies
kubectl apply -f services/shared/network-policies.yaml

# Triển khai Horizontal Pod Autoscalers
kubectl apply -f services/shared/hpa.yaml

# Kiểm tra các policies và HPA
kubectl get networkpolicies -n eshop-microservices
kubectl get hpa -n eshop-microservices
```

### 6. Triển khai Kong API Gateway
```bash
# Namespace gateway đã được tạo trong bước 1 (namespaces.yaml)

# Cài đặt Helm (nếu chưa có)
# Windows (PowerShell):
if (!(Test-Path -Path "$env:ProgramData\chocolatey\choco.exe")) {
    Set-ExecutionPolicy Bypass -Scope Process -Force
    [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072
    Invoke-Expression ((New-Object System.Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'))
    choco install kubernetes-helm -y
}

# Xác nhận Helm đã được cài đặt
helm version

# Thêm Kong Helm repository
helm repo add kong https://charts.konghq.com
helm repo update

# Cài đặt Kong API Gateway
helm install kong kong/kong -n gateway -f infrastructure/kong/values.yaml --set ingressController.installCRDs=false

# Kiểm tra trạng thái của các pods Kong
kubectl get pods -n gateway

# Đợi pods Kong khởi động hoàn tất
kubectl wait --for=condition=ready pod -l app=kong -n gateway --timeout=180s

# Triển khai cấu hình Kong plugins
kubectl apply -f infrastructure/kong/kong-plugins.yaml

# Triển khai Kong Ingress rules
kubectl apply -f infrastructure/kong/kong-ingress.yaml

# Kiểm tra các Ingress đã được tạo
kubectl get ingress -n eshop-microservices
```

### 7. Triển khai hệ thống giám sát
```bash
# Namespace monitoring đã được tạo trong bước 1 (namespaces.yaml)

# Triển khai RBAC cho Prometheus
kubectl apply -f monitoring/prometheus-rbac.yaml

# Triển khai Prometheus
kubectl apply -f monitoring/prometheus.yaml

# Triển khai Grafana Dashboard Provider
kubectl apply -f monitoring/grafana-dashboard-provider.yaml

# Triển khai Grafana Dashboards
kubectl apply -f monitoring/grafana-dashboards.yaml

# Triển khai Grafana
kubectl apply -f monitoring/grafana.yaml

# Triển khai Ingress cho monitoring
kubectl apply -f monitoring/monitoring-ingress.yaml

# Kiểm tra trạng thái
kubectl get pods -n monitoring
kubectl get svc -n monitoring

# Đợi pods đã sẵn sàng
kubectl wait --for=condition=ready pod -l app=prometheus -n monitoring --timeout=120s
kubectl wait --for=condition=ready pod -l app=grafana -n monitoring --timeout=120s
```

## Kiểm tra hệ thống

```bash
# Kiểm tra tất cả pods
kubectl get pods --all-namespaces | grep -E 'eshop-microservices|gateway|monitoring'

# Kiểm tra các service
kubectl get svc -n eshop-microservices
kubectl get svc -n gateway
kubectl get svc -n monitoring

# Kiểm tra API Gateway endpoint
$GATEWAY_IP=$(kubectl get -o jsonpath="{.status.loadBalancer.ingress[0].ip}" service -n gateway kong-kong-proxy)
echo "API Gateway URL: http://$GATEWAY_IP"

# Test các endpoints
curl -v "http://$GATEWAY_IP/auth-service/swagger/index.html"
curl -v "http://$GATEWAY_IP/catalog-service/swagger/index.html"
curl -v "http://$GATEWAY_IP/basket-service/swagger/index.html"
curl -v "http://$GATEWAY_IP/ordering-service/swagger/index.html"

# Kiểm tra endpoints monitoring
echo "Prometheus URL: http://$GATEWAY_IP/prometheus"
echo "Grafana URL: http://$GATEWAY_IP/grafana (username: admin, password: admin)"
```

## Xác nhận kết quả Database Migration và Seed Data

Sau khi migration job hoàn thành, bạn có thể kiểm tra dữ liệu trong các database:

```bash
# Kiểm tra Auth database
kubectl exec -it $(kubectl get pods -n eshop-microservices -l app=auth-postgres -o name | head -n 1) -n eshop-microservices -- psql -U auth_user -d AuthDb -c "SELECT * FROM \"Users\" LIMIT 5;"

# Kiểm tra Catalog database
kubectl exec -it $(kubectl get pods -n eshop-microservices -l app=catalog-postgres -o name | head -n 1) -n eshop-microservices -- psql -U catalog_user -d CatalogDb -c "SELECT COUNT(*) FROM \"Products\";"

# Kiểm tra Ordering database
kubectl exec -it $(kubectl get pods -n eshop-microservices -l app=ordering-sqlserver -o name | head -n 1) -n eshop-microservices -- /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P Ordering_P@ssw0rd123 -Q "SELECT * FROM Customers"

# Kiểm tra Discount database (khó hơn vì là SQLite trong container)
kubectl exec -it $(kubectl get pods -n eshop-microservices -l app=discount-grpc -o name | head -n 1) -n eshop-microservices -- sqlite3 /app/data/discountdb "SELECT * FROM Coupons;"
```

## Cấu hình Monitoring cho Microservices

Để thu thập metrics từ các microservices .NET, cần thực hiện các bước sau:

### 1. Thêm các annotations vào microservices
Đảm bảo mỗi microservice có các annotations sau để Prometheus có thể phát hiện và thu thập metrics:

```bash
# Ví dụ cập nhật Catalog API
kubectl patch deployment catalog-api -n eshop-microservices -p '
{
  "spec": {
    "template": {
      "metadata": {
        "annotations": {
          "prometheus.io/scrape": "true",
          "prometheus.io/port": "80",
          "prometheus.io/path": "/metrics"
        }
      }
    }
  }
}'

# Tương tự cho các services khác
kubectl patch deployment auth-api -n eshop-microservices -p '{"spec":{"template":{"metadata":{"annotations":{"prometheus.io/scrape":"true","prometheus.io/port":"80","prometheus.io/path":"/metrics"}}}}}'
kubectl patch deployment basket-api -n eshop-microservices -p '{"spec":{"template":{"metadata":{"annotations":{"prometheus.io/scrape":"true","prometheus.io/port":"80","prometheus.io/path":"/metrics"}}}}}'
kubectl patch deployment ordering-api -n eshop-microservices -p '{"spec":{"template":{"metadata":{"annotations":{"prometheus.io/scrape":"true","prometheus.io/port":"80","prometheus.io/path":"/metrics"}}}}}'
kubectl patch deployment discount-grpc -n eshop-microservices -p '{"spec":{"template":{"metadata":{"annotations":{"prometheus.io/scrape":"true","prometheus.io/port":"80","prometheus.io/path":"/metrics"}}}}}'
```

### 2. Kiểm tra Monitoring Dashboard

Sau khi triển khai, bạn có thể:

1. Truy cập Grafana đã được cấu hình sẵn tại http://<gateway-ip>/grafana
   - Username: admin
   - Password: admin

2. Truy cập các dashboard mặc định đã được cấu hình:
   - Kubernetes Pods Dashboard - Hiển thị metrics về các pods trong namespace eshop-microservices

3. Thêm các dashboard mới cho .NET:
   - Trong giao diện Grafana, chọn "Import" và nhập Dashboard ID: 10915 cho ASP.NET Core Dashboard
   - Hoặc nhập Dashboard ID: 10427 cho .NET Core Dashboard

### 3. Kiểm tra Alerting (nếu có)
Kiểm tra các alerts đã được cấu hình trong Prometheus:
```bash
# Xem danh sách các alerts được cấu hình
kubectl port-forward svc/prometheus -n monitoring 9090:9090
# Sau đó truy cập http://localhost:9090/alerts trên trình duyệt
```

## Troubleshooting

### Vấn đề với ContainerCreating
Khi các pod bị kẹt ở trạng thái ContainerCreating, kiểm tra chi tiết:
```bash
kubectl describe pod <pod-name> -n eshop-microservices
```

Các lỗi phổ biến và cách giải quyết:

1. **ConfigMap không tồn tại**: `MountVolume.SetUp failed for volume "xxx-config" : configmap "xxx-config" not found`
   ```bash
   # Kiểm tra và triển khai ConfigMap thiếu
   kubectl get configmap -n eshop-microservices
   kubectl apply -f services/shared/configmap.yaml
   ```

2. **Secret không tồn tại**: `Secret "xxx" not found`
   ```bash
   kubectl get secrets -n eshop-microservices
   kubectl apply -f services/shared/secret.yaml
   ```

3. **PersistentVolumeClaim không tồn tại**: `persistentvolumeclaim "discount-sqlite-data" not found`
   ```bash
   kubectl apply -f services/discount/discount-sqlite-pvc.yaml
   kubectl get pvc -n eshop-microservices
   ```

4. **ReadinessProbe gRPC errors**: Lỗi với readinessProbe trong discount-grpc.yaml
   ```bash
   # Sửa readinessProbe thành dạng đơn giản hơn
   kubectl edit deployment discount-grpc -n eshop-microservices
   
   # Thay readinessProbe thành tcpSocket:
   readinessProbe:
     tcpSocket:
       port: 80
     initialDelaySeconds: 15
     periodSeconds: 10
   ```

5. **Database chưa sẵn sàng**: Service không thể kết nối đến database
   ```bash
   # Kiểm tra trạng thái database
   kubectl get pods -n eshop-microservices | grep -E 'postgres|redis|sqlserver'
   kubectl logs <database-pod-name> -n eshop-microservices
   
   # Đảm bảo database đã sẵn sàng trước khi triển khai service
   kubectl wait --for=condition=ready pod -l app=auth-postgres -n eshop-microservices
   ```

### Vấn đề khi Migration Job gặp lỗi

Nếu Job migration không hoàn thành hoặc gặp lỗi:

```bash
# Kiểm tra trạng thái job
kubectl get jobs -n eshop-microservices

# Xem logs chi tiết
kubectl logs job/database-migrator-job -n eshop-microservices

# Nếu job bị lỗi, xóa và tạo lại
kubectl delete job database-migrator-job -n eshop-microservices
kubectl apply -f migrations/database-migrator-job.yaml

# Nếu job bị mắc kẹt trong trạng thái ContainerCreating, xem mô tả pod
kubectl describe pod -l job-name=database-migrator-job -n eshop-microservices
```

Các vấn đề thường gặp:
1. **ConfigMap không tồn tại**: Đảm bảo ConfigMap db-migrations-config đã được tạo
2. **Container không thể cài đặt công cụ**: Kiểm tra kết nối mạng của pod
3. **Lỗi kết nối database**: Đảm bảo các database đang chạy và sẵn sàng

### Vấn đề với kết nối giữa các service
Kiểm tra DNS và network policies:
```bash
# Kiểm tra kết nối giữa các service
kubectl exec -it <pod-name> -n eshop-microservices -- curl <service-name>:<port>/health

# Kiểm tra cấu hình service
kubectl describe service <service-name> -n eshop-microservices

# Kiểm tra logs của service
kubectl logs <pod-name> -n eshop-microservices
```

### Vấn đề với Prometheus và Grafana

Nếu hệ thống monitoring không hoạt động đúng:

```bash
# Kiểm tra trạng thái các pods monitoring
kubectl get pods -n monitoring
kubectl describe pod -l app=prometheus -n monitoring
kubectl describe pod -l app=grafana -n monitoring

# Kiểm tra logs
kubectl logs -l app=prometheus -n monitoring
kubectl logs -l app=grafana -n monitoring

# Kiểm tra quyền truy cập của ServiceAccount Prometheus
kubectl auth can-i get pods --as=system:serviceaccount:monitoring:prometheus -n eshop-microservices

# Kiểm tra ConfigMaps
kubectl get configmap -n monitoring
kubectl describe configmap prometheus-config -n monitoring
kubectl describe configmap grafana-datasources -n monitoring
kubectl describe configmap grafana-dashboard-provider -n monitoring
kubectl describe configmap grafana-dashboards -n monitoring

# Kiểm tra kết nối từ Prometheus đến các targets
kubectl port-forward svc/prometheus -n monitoring 9090:9090
# Sau đó truy cập http://localhost:9090/targets trên trình duyệt để xem targets nào không hoạt động
```

### Các lệnh debug hữu ích
```bash
# Khởi động lại một deployment
kubectl rollout restart deployment/<deployment-name> -n eshop-microservices

# Kiểm tra event logs
kubectl get events -n eshop-microservices --sort-by='.lastTimestamp'

# Kiểm tra lỗi trong các pods
kubectl get pods -n eshop-microservices -o wide | grep -v Running

# Truy cập shell của container để debug
kubectl exec -it <pod-name> -n eshop-microservices -- /bin/bash
```

## Dọn dẹp hệ thống

```bash
# Xóa API Gateway
helm uninstall kong -n gateway
kubectl delete namespace gateway

kubectl delete kongplugin --all -n eshop-microservices
kubectl delete kongconsumer --all -n eshop-microservices
kubectl delete secret microservice-clients-jwt -n eshop-microservices
kubectl delete secret microservice-clients-jwt-credential -n eshop-microservices
kubectl delete ingress --all -n eshop-microservices

# Xóa monitoring
kubectl delete -f monitoring/grafana.yaml
kubectl delete -f monitoring/prometheus.yaml
kubectl delete -f monitoring/prometheus-rbac.yaml
kubectl delete -f monitoring/grafana-dashboard-provider.yaml
kubectl delete -f monitoring/grafana-dashboards.yaml
kubectl delete -f monitoring/monitoring-ingress.yaml
kubectl delete namespace monitoring

# Xóa microservices và databases
kubectl delete -f services/ordering/
kubectl delete -f services/basket/
kubectl delete -f services/catalog/
kubectl delete -f services/auth/
kubectl delete -f services/discount/
kubectl delete -f services/shared/

# Xóa migration job và configmap
kubectl delete -f migrations/
kubectl delete -f namespaces.yaml