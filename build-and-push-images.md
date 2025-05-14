# Hướng Dẫn Build và Push Docker Images cho EShopMicroservices

Tài liệu này cung cấp các bước để build Docker images cho các microservices trong dự án và đẩy lên Docker Hub để triển khai trên Kubernetes.

## Chuẩn bị

1. Đảm bảo bạn đã cài đặt Docker trên máy của mình
2. Đăng nhập vào Docker Hub từ terminal:

```bash
docker login -u duccongdo
```

## Build và Push Images cho Các Microservices

Dưới đây là các lệnh để build và push từng microservice. Các image được đặt tên giống với cấu hình trong Kubernetes.

### 1. Catalog API

```bash
# Di chuyển đến thư mục gốc của dự án
cd e:\WorkSpace\c_sharp\EShopMicroservices\src

# Build image
docker build -t duccongdo/catalogapi:2 -f Services/Catalog/Catalog.API/Dockerfile .

# Push image lên Docker Hub
docker push duccongdo/catalogapi:2
```

### 2. Auth API

```bash
# Build image
docker build -t duccongdo/authapi:2 -f Services/Auth/Auth.API/Dockerfile .

# Push image lên Docker Hub
docker push duccongdo/authapi:2
```

### 3. Basket API

```bash
# Build image
docker build -t duccongdo/basketapi:3 -f Services/Basket/Basket.API/Dockerfile .

# Push image lên Docker Hub
docker push duccongdo/basketapi:3
```

### 4. Discount gRPC

```bash
# Build image
docker build -t duccongdo/discountgrpc:3 -f Services/Discount/Discount.gRPC/Dockerfile .

# Push image lên Docker Hub
docker push duccongdo/discountgrpc:3
```

### 5. Ordering API

```bash
# Build image
docker build -t duccongdo/orderingapi:3 -f Services/Ordering/Ordering.API/Dockerfile .

# Push image lên Docker Hub
docker push duccongdo/orderingapi:3
```

## API Gateways

Nếu bạn cần triển khai API Gateway, dưới đây là các lệnh cho từng loại gateway:

### YARP API Gateway

```bash
# Build image
docker build -t duccongdo/yarp:latest -f ApiGateways/YarpApiGateway/Dockerfile .

# Push image lên Docker Hub
docker push duccongdo/yarp:latest
```

### Ocelot API Gateway

```bash
# Build image
docker build -t duccongdo/ocelot:latest -f ApiGateways/OcelotApiGateway/Dockerfile .

# Push image lên Docker Hub
docker push duccongdo/ocelot:latest
```

### Kong API Gateway Setup

```bash
# Build image
docker build -t duccongdo/kong-setup:latest -f ApiGateways/KongApiGateway/Dockerfile .

# Push image lên Docker Hub
docker push duccongdo/kong-setup:latest
```

### Nginx API Gateway

```bash
# Build image
docker build -t duccongdo/nginx-gateway:latest -f ApiGateways/NginxApiGateway/Dockerfile .

# Push image lên Docker Hub
docker push duccongdo/nginx-gateway:latest
```

### Traefik API Gateway Setup

```bash
# Build image
docker build -t duccongdo/traefik-setup:latest -f ApiGateways/TraefikApiGateway/Dockerfile .

# Push image lên Docker Hub
docker push duccongdo/traefik-setup:latest
```

## Build và Push Tất Cả Images Một Lúc

Nếu muốn build và push tất cả các images cùng một lúc, bạn có thể sử dụng script sau:

```bash
#!/bin/bash
# Đặt username Docker Hub của bạn
DOCKER_USERNAME=duccongdo

# Mảng chứa tên các services và đường dẫn tới Dockerfile tương ứng
declare -A services=(
  ["catalogapi"]="Services/Catalog/Catalog.API/Dockerfile"
  ["authapi"]="Services/Auth/Auth.API/Dockerfile"
  ["basketapi"]="Services/Basket/Basket.API/Dockerfile"
  ["discountgrpc"]="Services/Discount/Discount.gRPC/Dockerfile"
  ["orderingapi"]="Services/Ordering/Ordering.API/Dockerfile"
  ["yarp"]="ApiGateways/YarpApiGateway/Dockerfile"
  ["ocelot"]="ApiGateways/OcelotApiGateway/Dockerfile"
  ["kong-setup"]="ApiGateways/KongApiGateway/Dockerfile"
  ["nginx-gateway"]="ApiGateways/NginxApiGateway/Dockerfile"
  ["traefik-setup"]="ApiGateways/TraefikApiGateway/Dockerfile"
)

# Di chuyển đến thư mục gốc của dự án
cd e:/WorkSpace/c_sharp/EShopMicroservices/src

# Build và push từng image
for service in "${!services[@]}"; do
  dockerfile=${services[$service]}
  echo "Building $service from $dockerfile..."
  docker build -t $DOCKER_USERNAME/$service:latest -f $dockerfile .
  
  echo "Pushing $service to Docker Hub..."
  docker push $DOCKER_USERNAME/$service:latest
done

echo "Hoàn thành build và push tất cả images!"
```

## Kiểm Tra Images đã Push

Để kiểm tra các images đã được push lên Docker Hub, bạn có thể chạy:

```bash
docker images | grep duccongdo
```

## Ghi Chú Quan Trọng

1. Đảm bảo rằng tên image trong các file cấu hình Kubernetes phải khớp với tên image bạn push lên Docker Hub.
2. Các images third-party như postgres, redis, rabbitmq không cần phải build vì chúng đã có sẵn trên Docker Hub.
3. Nếu bạn muốn sử dụng phiên bản cụ thể thay vì "latest", hãy thay thế tag "latest" bằng số phiên bản tương ứng.

## Triển Khai lên Kubernetes

Sau khi đã push tất cả images lên Docker Hub, bạn có thể triển khai lên Kubernetes bằng cách:

```bash
kubectl apply -f kubernetes/namespace.yaml
kubectl apply -f kubernetes/services/shared/
kubectl apply -f kubernetes/services/auth/
kubectl apply -f kubernetes/services/catalog/
kubectl apply -f kubernetes/services/basket/
kubectl apply -f kubernetes/services/discount/
kubectl apply -f kubernetes/services/ordering/
kubectl apply -f kubernetes/infrastructure/
```

Bạn có thể kiểm tra trạng thái các pods và services bằng lệnh:

```bash
kubectl get pods -n eshop
kubectl get services -n eshop
```