# Kế hoạch triển khai EShopMicroservices lên Kubernetes

## Tổng quan cấu trúc
Hệ thống EShopMicroservices gồm các thành phần chính:
- Kong API Gateway làm entrypoint
- 4 microservices: Auth, Catalog, Basket, Ordering
- Các cơ sở dữ liệu: PostgreSQL, Redis, SQL Server
- Message broker: RabbitMQ
- Monitoring: Prometheus, Grafana
- Backup system

## Thứ tự triển khai

### 1. Thiết lập môi trường cơ bản
```bash
# Tạo namespace
kubectl apply -f namespace.yaml

# Triển khai các resource dùng chung
kubectl apply -f services/shared/
```

### 2. Triển khai các database và message broker
```bash
# Auth database
kubectl apply -f services/auth/auth-postgres.yaml

# Catalog database
kubectl apply -f services/catalog/catalog-postgres.yaml

# Basket Redis
kubectl apply -f services/basket/basket-redis.yaml

# Ordering database
kubectl apply -f services/ordering/ordering-sqlserver.yaml
```

### 3. Triển khai các microservices
```bash
# Auth Service
kubectl apply -f services/auth/auth-api.yaml

# Catalog Service
kubectl apply -f services/catalog/catalog-api.yaml

# Basket Service
kubectl apply -f services/basket/basket-api.yaml

# Ordering Service
kubectl apply -f services/ordering/ordering-api.yaml
```

### 4. Triển khai Kong API Gateway
```bash
# Tạo namespace cho Kong
kubectl create namespace kong

# Cài đặt Kong với Helm
helm repo add kong https://charts.konghq.com
helm repo update
helm install kong kong/kong -n kong -f infrastructure/kong/values.yaml

# Triển khai cấu hình Kong
kubectl apply -f infrastructure/kong/kong-plugins.yaml
kubectl apply -f infrastructure/kong/kong-ingress.yaml
```

### 5. Triển khai hệ thống giám sát
```bash
# Tạo namespace
kubectl create namespace monitoring

# Triển khai Prometheus
kubectl apply -f monitoring/prometheus.yaml

# Triển khai Grafana
kubectl apply -f monitoring/grafana.yaml
```

### 6. Thiết lập hệ thống backup
```bash
# Triển khai các job sao lưu tự động
kubectl apply -f backup/database-backup.yaml
```

## Kiểm tra hệ thống

```bash
# Kiểm tra các pod
kubectl get pods -n eshop-microservices

# Kiểm tra các service
kubectl get svc -n eshop-microservices

# Kiểm tra database đã sẵn sàng
kubectl get pvc -n eshop-microservices

# Kiểm tra Kong API Gateway
kubectl get pods -n kong
kubectl get svc -n kong

# Kiểm tra URL của Kong Gateway
export KONG_PROXY_IP=$(kubectl get -o jsonpath="{.status.loadBalancer.ingress[0].ip}" service -n kong kong-proxy)
echo "Kong Gateway URL: http://$KONG_PROXY_IP"
```

## Dọn dẹp hệ thống

```bash
# Xóa Kong API Gateway
helm uninstall kong -n kong

# Xóa microservices
kubectl delete -f services/auth/
kubectl delete -f services/catalog/
kubectl delete -f services/basket/
kubectl delete -f services/ordering/
kubectl delete -f services/shared/

# Xóa monitoring
kubectl delete -f monitoring/

# Xóa backup
kubectl delete -f backup/

# Xóa namespace
kubectl delete -f namespace.yaml
kubectl delete namespace kong
kubectl delete namespace monitoring
```