# Triển khai EShopMicroservices lên Kubernetes - Bản Rút Gọn

## Tổng quan cấu trúc
Hệ thống EShopMicroservices gồm:
- Kong API Gateway làm entrypoint
- 5 microservices: Auth, Catalog, Basket, Discount, Ordering
- Databases: PostgreSQL, Redis, SQL Server, SQLite
- Message broker: RabbitMQ
- Monitoring: 
  - Prometheus: Thu thập metrics và xử lý cảnh báo
  - AlertManager: Quản lý các cảnh báo từ Prometheus
  - Grafana: Cung cấp dashboard trực quan hóa metrics

## Các bước triển khai

### 1. Tạo namespaces
```bash
# Tạo các namespaces cần thiết
kubectl apply -f namespaces.yaml

# Kiểm tra các namespace
kubectl get namespaces | grep -E 'eshop-microservices|gateway|monitoring'
```

### 2. Triển khai shared resources
```bash
# Triển khai shared configmap và secrets
kubectl apply -f services/shared/configmap.yaml
kubectl apply -f services/shared/secret.yaml


# Kiểm tra trạng thái
kubectl get configmap,secret -n eshop-microservices
```

### 3. Triển khai các database
```bash
# Triển khai RabbitMQ cho message bus
kubectl apply -f services/shared/rabbitmq.yaml
# Triển khai tất cả databases
kubectl apply -f services/auth/auth-postgres.yaml
kubectl apply -f services/catalog/catalog-postgres.yaml
kubectl apply -f services/basket/basket-postgres.yaml
kubectl apply -f services/basket/basket-redis.yaml
kubectl apply -f services/discount/discount-sqlite-pvc.yaml
kubectl apply -f services/ordering/ordering-sqlserver.yaml

kubectl get pods -n eshop-microservices -l app=rabbitmq

# Kiểm tra trạng thái
kubectl get pods -n eshop-microservices | grep -E 'postgres|redis|sqlserver'

# Đợi databases sẵn sàng
kubectl wait --for=condition=ready pod -l app=auth-postgres -n eshop-microservices --timeout=120s
kubectl wait --for=condition=ready pod -l app=catalog-postgres -n eshop-microservices --timeout=120s
kubectl wait --for=condition=ready pod -l app=basket-redis -n eshop-microservices --timeout=120s
kubectl wait --for=condition=ready pod -l app=ordering-sqlserver -n eshop-microservices --timeout=120s
```

### 4. Triển khai Database Migration
```bash
# Tạo ConfigMap cho migration scripts
kubectl apply -f migrations/db-migrations-config.yaml

# Triển khai migration job
kubectl apply -f migrations/database-migrator-job.yaml

# Kiểm tra trạng thái job
kubectl get jobs -n eshop-microservices
kubectl wait --for=condition=complete job/database-migrator-job -n eshop-microservices --timeout=300s
```

### 5. Triển khai các microservices
```bash
# Triển khai theo thứ tự phụ thuộc
kubectl apply -f services/discount/discount-grpc.yaml
kubectl wait --for=condition=available deployment/discount-grpc -n eshop-microservices --timeout=120s

kubectl apply -f services/auth/auth-api.yaml
kubectl apply -f services/catalog/catalog-api.yaml
kubectl apply -f services/basket/basket-api.yaml
kubectl apply -f services/ordering/ordering-api.yaml

# Kiểm tra trạng thái
kubectl get pods,svc -n eshop-microservices
```

### 6. Cấu hình Network Policies và HPA
```bash
# Triển khai Network Policies và HPA
kubectl apply -f services/shared/network-policies.yaml
kubectl apply -f services/shared/hpa.yaml
```

### 7. Triển khai Kong API Gateway
```bash
# Thêm Helm repo
helm repo add kong https://charts.konghq.com
helm repo update

# Cài đặt Kong
helm install kong kong/kong -n gateway -f infrastructure/kong/values.yaml --set ingressController.installCRDs=false

# Kiểm tra và đợi Kong sẵn sàng
kubectl get pods -n gateway
kubectl wait --for=condition=ready pod -l app=kong -n gateway --timeout=180s

# Triển khai Kong plugins và Ingress rules
kubectl apply -f infrastructure/kong/kong-plugins.yaml
kubectl apply -f infrastructure/kong/kong-ingress.yaml
```

### 8. Triển khai monitoring
```bash
# Tạo RBAC cho Prometheus
kubectl apply -f monitoring/prometheus-rbac.yaml

# Triển khai các quy tắc cảnh báo
kubectl apply -f monitoring/prometheus-alert-rules.yaml   # Quy tắc cảnh báo cho essential services
kubectl apply -f monitoring/alert-rules.yaml              # Các quy tắc cảnh báo khác

# Triển khai AlertManager không cần cấu hình SMTP
kubectl apply -f monitoring/alertmanager.yaml

# Triển khai Prometheus với các job giám sát cơ bản và essential services
kubectl apply -f monitoring/prometheus.yaml

# Triển khai Grafana và các dashboard
kubectl apply -f monitoring/grafana-dashboard-provider.yaml
kubectl apply -f monitoring/grafana-dashboards.yaml
kubectl apply -f monitoring/grafana.yaml

# Cấu hình Ingress để truy cập các UI qua Kong API Gateway
kubectl apply -f monitoring/monitoring-ingress.yaml

# Kiểm tra trạng thái
kubectl get pods,svc -n monitoring
kubectl wait --for=condition=ready pod -l app=prometheus -n monitoring --timeout=120s
kubectl wait --for=condition=ready pod -l app=alertmanager -n monitoring --timeout=120s
kubectl wait --for=condition=ready pod -l app=grafana -n monitoring --timeout=120s
```

## Kiểm tra hệ thống

```bash
# Kiểm tra tất cả pods
kubectl get pods --all-namespaces | grep -E 'eshop-microservices|gateway|monitoring'

# Lấy địa chỉ API Gateway
$GATEWAY_IP=$(kubectl get -o jsonpath="{.status.loadBalancer.ingress[0].ip}" service -n gateway kong-kong-proxy)
echo "API Gateway URL: http://$GATEWAY_IP"

# Test các endpoints
curl -v "http://$GATEWAY_IP/auth-service/swagger/index.html"
curl -v "http://$GATEWAY_IP/catalog-service/swagger/index.html"
curl -v "http://$GATEWAY_IP/basket-service/swagger/index.html"
curl -v "http://$GATEWAY_IP/ordering-service/swagger/index.html"

# Kiểm tra endpoints monitoring
echo "Prometheus URL: http://$GATEWAY_IP/prometheus"
echo "Alertmanager URL: http://$GATEWAY_IP/alertmanager"
echo "Grafana URL: http://$GATEWAY_IP/grafana (username: admin, password: admin)"
```

## Xử lý sự cố cơ bản

### Kiểm tra lỗi Pod
```bash
# Xem chi tiết lỗi pod
kubectl describe pod <pod-name> -n eshop-microservices

# Xem logs
kubectl logs <pod-name> -n eshop-microservices
```

### Các lỗi thường gặp
1. **ConfigMap/Secret thiếu**: Đảm bảo tất cả ConfigMap và Secret đã được tạo
2. **Database chưa sẵn sàng**: Kiểm tra trạng thái database trước khi triển khai service
3. **Lỗi Network**: Kiểm tra Network Policies và kết nối giữa các service
4. **Alert không hoạt động**: Kiểm tra cấu hình AlertManager và kết nối giữa Prometheus và AlertManager
5. **Prometheus không phát hiện service**: Kiểm tra các annotations prometheus.io/scrape trong service và pod

### Khởi động lại service
```bash
kubectl rollout restart deployment/<deployment-name> -n eshop-microservices
```

### Quản lý AlertManager
```bash
# Khởi động lại AlertManager sau khi thay đổi cấu hình
kubectl rollout restart deployment alertmanager -n monitoring

# Xem logs của AlertManager để kiểm tra lỗi
kubectl logs -n monitoring $(kubectl get pods -n monitoring -l app=alertmanager -o name | head -1)

# Kiểm tra các cảnh báo hiện tại trong AlertManager
kubectl port-forward -n monitoring svc/alertmanager 9093:9093
# Sau đó mở trình duyệt tại http://localhost:9093/#/alerts
```

### Kiểm tra và thử nghiệm Alerts
```bash
# Tạo tình huống cảnh báo bằng cách scale down một service thiết yếu
kubectl scale deployment basket-api --replicas=0 -n eshop-microservices

# Kiểm tra xem alert đã được kích hoạt chưa (sau khoảng 15 giây)
kubectl port-forward -n monitoring svc/prometheus 9090:9090
# Sau đó mở trình duyệt tại http://localhost:9090/alerts

# Khôi phục service để hủy cảnh báo
kubectl scale deployment basket-api --replicas=1 -n eshop-microservices
```

## Dọn dẹp hệ thống

```bash
# Xóa API Gateway
helm uninstall kong -n gateway
kubectl delete namespace gateway

# Xóa monitoring
kubectl delete -f monitoring/
kubectl delete namespace monitoring

# Xóa microservices và databases
kubectl delete -f services/
kubectl delete -f migrations/
kubectl delete -f namespaces.yaml
```
