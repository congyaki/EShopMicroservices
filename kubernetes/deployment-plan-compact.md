# Triển khai EShopMicroservices lên Kubernetes - Bản Rút Gọn

## Tổng quan cấu trúc
Hệ thống EShopMicroservices gồm:
- Kong API Gateway làm entrypoint
- 5 microservices: Auth, Catalog, Basket, Discount, Ordering
- Databases: PostgreSQL, Redis, SQL Server, SQLite
- Message broker: RabbitMQ
- Monitoring: Prometheus, Grafana

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

# Triển khai RabbitMQ cho message bus
kubectl apply -f services/shared/rabbitmq.yaml

# Kiểm tra trạng thái
kubectl get configmap,secret -n eshop-microservices
kubectl get pods -n eshop-microservices -l app=rabbitmq
```

### 3. Triển khai các database
```bash
# Triển khai tất cả databases
kubectl apply -f services/auth/auth-postgres.yaml
kubectl apply -f services/catalog/catalog-postgres.yaml
kubectl apply -f services/basket/basket-postgres.yaml
kubectl apply -f services/basket/basket-redis.yaml
kubectl apply -f services/discount/discount-sqlite-pvc.yaml
kubectl apply -f services/ordering/ordering-sqlserver.yaml

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
# Triển khai Prometheus và Grafana
kubectl apply -f monitoring/prometheus-rbac.yaml
kubectl apply -f monitoring/prometheus.yaml
kubectl apply -f monitoring/grafana-dashboard-provider.yaml
kubectl apply -f monitoring/grafana-dashboards.yaml
kubectl apply -f monitoring/grafana.yaml
kubectl apply -f monitoring/monitoring-ingress.yaml

# Kiểm tra trạng thái
kubectl get pods,svc -n monitoring
kubectl wait --for=condition=ready pod -l app=prometheus -n monitoring --timeout=120s
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

### Khởi động lại service
```bash
kubectl rollout restart deployment/<deployment-name> -n eshop-microservices
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
