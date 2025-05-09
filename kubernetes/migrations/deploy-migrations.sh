#!/bin/bash
set -e

echo "Starting EShopMicroservices database migration deployment..."

# 1. Tạo namespace (nếu chưa tồn tại)
kubectl apply -f ../namespace.yaml

# 2. Tạo ConfigMaps và Secrets cần thiết
kubectl apply -f ../services/shared/configmap.yaml
kubectl apply -f ../services/shared/secret.yaml

# 3. Tạo ConfigMap cho migration scripts
kubectl apply -f db-migrations-config.yaml

# 4. Triển khai databases
echo "Deploying databases..."
kubectl apply -f ../services/auth/auth-postgres.yaml
kubectl apply -f ../services/catalog/catalog-postgres.yaml
kubectl apply -f ../services/ordering/ordering-sqlserver.yaml
kubectl apply -f ../services/discount/discount-sqlite-pvc.yaml

# 5. Chờ các database khởi động
echo "Waiting for databases to be ready..."
kubectl wait --for=condition=ready pod -l app=auth-postgres -n eshop-microservices --timeout=120s
kubectl wait --for=condition=ready pod -l app=catalog-postgres -n eshop-microservices --timeout=120s
kubectl wait --for=condition=ready pod -l app=ordering-sqlserver -n eshop-microservices --timeout=180s

# 6. Chạy Job migration
echo "Running database migration job..."
kubectl apply -f database-migrator-job.yaml

# 7. Theo dõi trạng thái của job
echo "Monitoring migration job status..."
kubectl wait --for=condition=complete job/database-migrator-job -n eshop-microservices --timeout=300s

echo "Database migration completed successfully!"
echo "You can now deploy your microservices."
echo "To check migration logs, run: kubectl logs job/database-migrator-job -n eshop-microservices"