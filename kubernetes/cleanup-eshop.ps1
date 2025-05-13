# Script dọn dẹp hệ thống EShopMicroservices
# Sẽ xóa toàn bộ tài nguyên đã triển khai trên Kubernetes

# Đảm bảo script dừng lại ngay khi có lỗi
$ErrorActionPreference = "Stop"

# Hàm hiển thị thông báo
function Write-Step {
    param (
        [string]$Message
    )
    Write-Host "`n============================================================" -ForegroundColor Yellow
    Write-Host $Message -ForegroundColor Yellow
    Write-Host "============================================================" -ForegroundColor Yellow
}

# Hàm xử lý lỗi
function Handle-Error {
    param (
        [string]$Step
    )
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Cảnh báo: Không thể xóa tài nguyên trong bước '$Step'. Tiếp tục xử lý..." -ForegroundColor Red
    }
}

Write-Host "BẮT ĐẦU QUÁ TRÌNH DỌN DẸP EShopMicroservices..." -ForegroundColor Red
$confirmation = Read-Host "Bạn có chắc chắn muốn xóa toàn bộ tài nguyên EShopMicroservices không? (y/n)"
if ($confirmation -ne 'y') {
    Write-Host "Hủy quá trình dọn dẹp."
    exit 0
}

# Xóa API Gateway
Write-Step "1. Xóa Kong API Gateway"
helm uninstall kong -n gateway
Handle-Error "Kong Helm Uninstall"
kubectl delete namespace gateway
Handle-Error "Gateway Namespace"

# Xóa Kong resources khác
Write-Step "2. Xóa Kong resources trong namespace eshop-microservices"
kubectl delete ingress --all -n eshop-microservices
Handle-Error "Kong Ingress"
kubectl delete kongplugin --all -n eshop-microservices
Handle-Error "Kong Plugins"
kubectl delete kongconsumer --all -n eshop-microservices
Handle-Error "Kong Consumers"
kubectl delete secret microservice-clients-jwt -n eshop-microservices
Handle-Error "JWT Secret"
kubectl delete secret microservice-clients-jwt-credential -n eshop-microservices
Handle-Error "JWT Credential Secret"

# Xóa monitoring
Write-Step "3. Xóa hệ thống monitoring"
kubectl delete -f monitoring/monitoring-ingress.yaml
Handle-Error "Monitoring Ingress"
kubectl delete -f monitoring/grafana.yaml
Handle-Error "Grafana"
kubectl delete -f monitoring/grafana-dashboards.yaml
Handle-Error "Grafana Dashboards"
kubectl delete -f monitoring/grafana-dashboard-provider.yaml
Handle-Error "Grafana Dashboard Provider"
kubectl delete -f monitoring/prometheus.yaml
Handle-Error "Prometheus"
kubectl delete -f monitoring/prometheus-rbac.yaml
Handle-Error "Prometheus RBAC"

kubectl delete namespace monitoring
Handle-Error "Monitoring Namespace"

# Xóa microservices và database
Write-Step "4. Xóa các microservices"
kubectl delete -f services/ordering/ordering-api.yaml
Handle-Error "Ordering API"
kubectl delete -f services/basket/basket-api.yaml
Handle-Error "Basket API"
kubectl delete -f services/catalog/catalog-api.yaml
Handle-Error "Catalog API"
kubectl delete -f services/auth/auth-api.yaml
Handle-Error "Auth API"
kubectl delete -f services/discount/discount-grpc.yaml
Handle-Error "Discount gRPC"

# Xóa Network Policies và HPA
kubectl delete -f services/shared/network-policies.yaml
Handle-Error "Network Policies"
kubectl delete -f services/shared/hpa.yaml
Handle-Error "HPA"

# Xóa job migration
Write-Step "5. Xóa migration job"
kubectl delete -f migrations/database-migrator-job.yaml
Handle-Error "Database Migrator Job"
kubectl delete -f migrations/db-migrations-config.yaml
Handle-Error "DB Migrations Config"

# Xóa database
Write-Step "6. Xóa các database"
kubectl delete -f services/ordering/ordering-sqlserver.yaml
Handle-Error "SQL Server"
kubectl delete -f services/discount/discount-sqlite-pvc.yaml
Handle-Error "SQLite PVC"
kubectl delete -f services/basket/basket-redis.yaml
Handle-Error "Redis"
kubectl delete -f services/basket/basket-postgres.yaml
Handle-Error "Basket Postgres"
kubectl delete -f services/catalog/catalog-postgres.yaml
Handle-Error "Catalog Postgres"
kubectl delete -f services/auth/auth-postgres.yaml
Handle-Error "Auth Postgres"

# Xóa shared resources
Write-Step "7. Xóa shared resources"
kubectl delete -f services/shared/rabbitmq.yaml
Handle-Error "RabbitMQ"
kubectl delete -f services/shared/secret.yaml
Handle-Error "Secrets"
kubectl delete -f services/shared/configmap.yaml
Handle-Error "ConfigMaps"

# Xóa namespace
Write-Step "8. Xóa namespace eshop-microservices"
kubectl delete -f namespaces.yaml
Handle-Error "Namespaces"

Write-Host "`n✓ QUÁ TRÌNH DỌN DẸP ĐÃ HOÀN THÀNH!" -ForegroundColor Green
Write-Host "Tất cả tài nguyên EShopMicroservices đã được xóa khỏi Kubernetes cluster." -ForegroundColor Green
