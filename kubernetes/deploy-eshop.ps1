# Triển khai EShopMicroservices lên Kubernetes
# Script tự động hóa quy trình triển khai
# Nếu có bất kỳ lỗi nào xảy ra, script sẽ dừng lại hoàn toàn

# Đảm bảo script dừng lại ngay khi có lỗi
$ErrorActionPreference = "Stop"

# Hàm hiển thị thông báo
function Write-Step {
    param (
        [string]$Message
    )
    Write-Host "`n============================================================" -ForegroundColor Cyan
    Write-Host $Message -ForegroundColor Cyan
    Write-Host "============================================================" -ForegroundColor Cyan
}

# Hàm kiểm tra thành công
function Test-LastExitCode {
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Lỗi: Lệnh không thành công với exit code $LASTEXITCODE. Dừng quá trình triển khai." -ForegroundColor Red
        exit $LASTEXITCODE
    }
}

# Kiểm tra kubectl đã được cài đặt
try {
    $kubectlVersion = kubectl version --client -o json
    Test-LastExitCode
    Write-Host "✓ Kubectl đã được cài đặt: $(($kubectlVersion | ConvertFrom-Json).clientVersion.gitVersion)" -ForegroundColor Green
}
catch {
    Write-Host "Lỗi: kubectl chưa được cài đặt hoặc không tìm thấy trong PATH." -ForegroundColor Red
    Write-Host "Vui lòng cài đặt kubectl và thêm vào biến môi trường PATH, sau đó chạy lại script."
    exit 1
}

# Kiểm tra kết nối đến Kubernetes cluster
try {
    Write-Host "Kiểm tra kết nối đến Kubernetes cluster..." -ForegroundColor Yellow
    kubectl get nodes | Out-Null
    Test-LastExitCode
    Write-Host "✓ Kết nối thành công đến Kubernetes cluster" -ForegroundColor Green
}
catch {
    Write-Host "Lỗi: Không thể kết nối đến Kubernetes cluster." -ForegroundColor Red
    Write-Host "Vui lòng kiểm tra cấu hình kubeconfig của bạn và đảm bảo cluster đang hoạt động."
    exit 1
}

# Bước 1: Tạo namespaces
Write-Step "BƯỚC 1: Tạo các namespaces cần thiết"
kubectl apply -f namespaces.yaml
Test-LastExitCode

# Kiểm tra namespaces đã được tạo
$namespaces = kubectl get namespaces -o jsonpath="{.items[*].metadata.name}"
if ($namespaces -match "eshop-microservices" -and $namespaces -match "gateway" -and $namespaces -match "monitoring") {
    Write-Host "✓ Các namespaces đã được tạo thành công" -ForegroundColor Green
}
else {
    Write-Host "Lỗi: Không thể tạo đầy đủ các namespaces cần thiết." -ForegroundColor Red
    exit 1
}

# Bước 2: Triển khai shared resources
Write-Step "BƯỚC 2: Triển khai shared resources"
kubectl apply -f services/shared/configmap.yaml
Test-LastExitCode
kubectl apply -f services/shared/secret.yaml
Test-LastExitCode
kubectl apply -f services/shared/rabbitmq.yaml
Test-LastExitCode

Write-Host "Đợi RabbitMQ khởi động..." -ForegroundColor Yellow
Start-Sleep -Seconds 10 # Đợi một chút để pods được tạo

# Kiểm tra RabbitMQ pods
$rmqPods = kubectl get pods -n eshop-microservices -l app=rabbitmq -o jsonpath='{.items[*].metadata.name}'
if ($rmqPods) {
    Write-Host "Đợi RabbitMQ pod sẵn sàng..." -ForegroundColor Yellow
    kubectl wait --for=condition=ready pod -l app=rabbitmq -n eshop-microservices --timeout=180s
    Test-LastExitCode
    Write-Host "✓ RabbitMQ đã sẵn sàng" -ForegroundColor Green
}
else {
    Write-Host "Lỗi: Không tìm thấy RabbitMQ pod." -ForegroundColor Red
    exit 1
}

# Bước 3: Triển khai các database
Write-Step "BƯỚC 3: Triển khai các database"
kubectl apply -f services/auth/auth-postgres.yaml
Test-LastExitCode
kubectl apply -f services/catalog/catalog-postgres.yaml
Test-LastExitCode
kubectl apply -f services/basket/basket-postgres.yaml
Test-LastExitCode
kubectl apply -f services/basket/basket-redis.yaml
Test-LastExitCode
kubectl apply -f services/discount/discount-sqlite-pvc.yaml
Test-LastExitCode
kubectl apply -f services/ordering/ordering-sqlserver.yaml
Test-LastExitCode

Write-Host "Đợi các database sẵn sàng..." -ForegroundColor Yellow
Start-Sleep -Seconds 15 # Đợi một chút để pods được tạo

# Kiểm tra và đợi các database pods sẵn sàng
Write-Host "Đợi Auth Postgres sẵn sàng..." -ForegroundColor Yellow
kubectl wait --for=condition=ready pod -l app=auth-postgres -n eshop-microservices --timeout=180s
Test-LastExitCode

Write-Host "Đợi Catalog Postgres sẵn sàng..." -ForegroundColor Yellow
kubectl wait --for=condition=ready pod -l app=catalog-postgres -n eshop-microservices --timeout=180s
Test-LastExitCode

Write-Host "Đợi Basket Redis sẵn sàng..." -ForegroundColor Yellow
kubectl wait --for=condition=ready pod -l app=basket-redis -n eshop-microservices --timeout=180s
Test-LastExitCode

Write-Host "Đợi Ordering SQL Server sẵn sàng..." -ForegroundColor Yellow
kubectl wait --for=condition=ready pod -l app=ordering-sqlserver -n eshop-microservices --timeout=180s
Test-LastExitCode

Write-Host "✓ Tất cả database đã sẵn sàng" -ForegroundColor Green

# Bước 4: Triển khai Database Migration
Write-Step "BƯỚC 4: Triển khai Database Migration"
kubectl apply -f migrations/db-migrations-config.yaml
Test-LastExitCode
kubectl apply -f migrations/database-migrator-job.yaml
Test-LastExitCode

Write-Host "Đợi job migration hoàn thành..." -ForegroundColor Yellow
kubectl wait --for=condition=complete job/database-migrator-job -n eshop-microservices --timeout=300s
if ($LASTEXITCODE -ne 0) {
    Write-Host "Lỗi: Job database-migrator-job không hoàn thành trong thời gian chờ." -ForegroundColor Red
    Write-Host "Kiểm tra logs:"
    kubectl logs job/database-migrator-job -n eshop-microservices
    exit 1
}

# Kiểm tra job migration có thất bại không
$jobStatus = kubectl get job database-migrator-job -n eshop-microservices -o jsonpath='{.status.failed}'
if ($jobStatus -eq "1") {
    Write-Host "Lỗi: Job database-migrator-job đã thất bại." -ForegroundColor Red
    Write-Host "Kiểm tra logs:"
    kubectl logs job/database-migrator-job -n eshop-microservices
    exit 1
}

Write-Host "✓ Database migration đã hoàn thành" -ForegroundColor Green

# Bước 5: Triển khai các microservices
Write-Step "BƯỚC 5: Triển khai các microservices"

# Triển khai Discount service (gRPC) trước
Write-Host "Triển khai Discount gRPC service..." -ForegroundColor Yellow
kubectl apply -f services/discount/discount-grpc.yaml
Test-LastExitCode

Write-Host "Đợi Discount gRPC sẵn sàng..." -ForegroundColor Yellow
kubectl wait --for=condition=available deployment/discount-grpc -n eshop-microservices --timeout=180s
Test-LastExitCode

# Triển khai các service khác
Write-Host "Triển khai Auth API..." -ForegroundColor Yellow
kubectl apply -f services/auth/auth-api.yaml
Test-LastExitCode

Write-Host "Triển khai Catalog API..." -ForegroundColor Yellow
kubectl apply -f services/catalog/catalog-api.yaml
Test-LastExitCode

Write-Host "Triển khai Basket API..." -ForegroundColor Yellow
kubectl apply -f services/basket/basket-api.yaml
Test-LastExitCode

Write-Host "Triển khai Ordering API..." -ForegroundColor Yellow
kubectl apply -f services/ordering/ordering-api.yaml
Test-LastExitCode

# Đợi tất cả pods sẵn sàng
Write-Host "Đợi tất cả microservices sẵn sàng..." -ForegroundColor Yellow
kubectl wait --for=condition=available deployment/auth-api -n eshop-microservices --timeout=180s
Test-LastExitCode
kubectl wait --for=condition=available deployment/catalog-api -n eshop-microservices --timeout=180s
Test-LastExitCode
kubectl wait --for=condition=available deployment/basket-api -n eshop-microservices --timeout=180s
Test-LastExitCode
kubectl wait --for=condition=available deployment/ordering-api -n eshop-microservices --timeout=180s
Test-LastExitCode

Write-Host "✓ Tất cả microservices đã sẵn sàng" -ForegroundColor Green

# Bước 6: Cấu hình Network Policies và HPA
Write-Step "BƯỚC 6: Cấu hình Network Policies và HPA"
kubectl apply -f services/shared/network-policies.yaml
Test-LastExitCode
kubectl apply -f services/shared/hpa.yaml
Test-LastExitCode

Write-Host "✓ Network Policies và HPA đã được cấu hình" -ForegroundColor Green

# Bước 7: Triển khai Kong API Gateway
Write-Step "BƯỚC 7: Triển khai Kong API Gateway"

# Kiểm tra Helm đã cài đặt chưa
try {
    $helmVersion = helm version
    Test-LastExitCode
    Write-Host "✓ Helm đã được cài đặt" -ForegroundColor Green
}
catch {
    Write-Host "Cài đặt Helm..." -ForegroundColor Yellow
    if (!(Test-Path -Path "$env:ProgramData\chocolatey\choco.exe")) {
        Set-ExecutionPolicy Bypass -Scope Process -Force
        [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072
        Invoke-Expression ((New-Object System.Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'))
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Lỗi: Không thể cài đặt Chocolatey." -ForegroundColor Red
            exit 1
        }
        choco install kubernetes-helm -y
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Lỗi: Không thể cài đặt Helm." -ForegroundColor Red
            exit 1
        }
    }
    else {
        choco install kubernetes-helm -y
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Lỗi: Không thể cài đặt Helm." -ForegroundColor Red
            exit 1
        }
    }
    # Refresh PATH
    $env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")
}

# Thêm Kong Helm repository
Write-Host "Thêm Kong Helm repository..." -ForegroundColor Yellow
helm repo add kong https://charts.konghq.com
Test-LastExitCode
helm repo update
Test-LastExitCode

# Cài đặt Kong API Gateway
Write-Host "Cài đặt Kong API Gateway..." -ForegroundColor Yellow
helm install kong kong/kong -n gateway -f infrastructure/kong/values.yaml --set ingressController.installCRDs=false
Test-LastExitCode

Write-Host "Đợi Kong API Gateway sẵn sàng..." -ForegroundColor Yellow
Start-Sleep -Seconds 15 # Đợi một chút để pods được tạo
kubectl wait --for=condition=ready pod -l app=kong -n gateway --timeout=300s
Test-LastExitCode

# Triển khai Kong plugins và Ingress rules
Write-Host "Cấu hình Kong plugins và Ingress..." -ForegroundColor Yellow
kubectl apply -f infrastructure/kong/kong-plugins.yaml
Test-LastExitCode
kubectl apply -f infrastructure/kong/kong-ingress.yaml
Test-LastExitCode

Write-Host "✓ Kong API Gateway đã sẵn sàng" -ForegroundColor Green

# Bước 8: Triển khai monitoring
Write-Step "BƯỚC 8: Triển khai monitoring"
kubectl apply -f monitoring/prometheus-rbac.yaml
Test-LastExitCode
kubectl apply -f monitoring/prometheus.yaml
Test-LastExitCode
kubectl apply -f monitoring/grafana-dashboard-provider.yaml
Test-LastExitCode
kubectl apply -f monitoring/grafana-dashboards.yaml
Test-LastExitCode
kubectl apply -f monitoring/grafana.yaml
Test-LastExitCode
kubectl apply -f monitoring/monitoring-ingress.yaml
Test-LastExitCode

Write-Host "Đợi Prometheus sẵn sàng..." -ForegroundColor Yellow
kubectl wait --for=condition=ready pod -l app=prometheus -n monitoring --timeout=180s
Test-LastExitCode

Write-Host "Đợi Grafana sẵn sàng..." -ForegroundColor Yellow
kubectl wait --for=condition=ready pod -l app=grafana -n monitoring --timeout=180s
Test-LastExitCode

Write-Host "✓ Hệ thống monitoring đã sẵn sàng" -ForegroundColor Green

# Hiển thị thông tin để truy cập hệ thống
Write-Step "HOÀN THÀNH: Hệ thống EShopMicroservices đã được triển khai thành công!"

Write-Host "Lấy địa chỉ API Gateway..." -ForegroundColor Yellow
$GATEWAY_IP = kubectl get -o jsonpath="{.status.loadBalancer.ingress[0].ip}" service -n gateway kong-kong-proxy

if ($GATEWAY_IP) {
    Write-Host "API Gateway URL: http://$GATEWAY_IP" -ForegroundColor Green
    Write-Host ""
    Write-Host "Các endpoints:"
    Write-Host "- Auth Service: http://$GATEWAY_IP/auth-service/swagger/index.html"
    Write-Host "- Catalog Service: http://$GATEWAY_IP/catalog-service/swagger/index.html"
    Write-Host "- Basket Service: http://$GATEWAY_IP/basket-service/swagger/index.html"
    Write-Host "- Ordering Service: http://$GATEWAY_IP/ordering-service/swagger/index.html"
    Write-Host ""
    Write-Host "Monitoring:"
    Write-Host "- Prometheus: http://$GATEWAY_IP/prometheus"
    Write-Host "- Grafana: http://$GATEWAY_IP/grafana (username: admin, password: admin)"
}
else {
    Write-Host "Không thể lấy địa chỉ IP của API Gateway. Có thể dịch vụ LoadBalancer chưa cấp IP." -ForegroundColor Yellow
    Write-Host "Hãy kiểm tra thủ công bằng lệnh: kubectl get service -n gateway kong-kong-proxy" -ForegroundColor Yellow
}

Write-Host "`nĐể dọn dẹp toàn bộ hệ thống, chạy script: .\cleanup-eshop.ps1" -ForegroundColor Cyan
