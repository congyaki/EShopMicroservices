# Hướng dẫn giám sát microservices

## Cấu hình Monitoring Stack
Hệ thống giám sát gồm ba thành phần chính:

1. **Prometheus**: Thu thập metrics và xử lý cảnh báo
2. **AlertManager**: Quản lý việc thông báo các cảnh báo  
3. **Grafana**: Hiển thị dashboard trực quan hóa metrics

## Các loại cảnh báo

### Các cảnh báo thiết lập sẵn:

1. **ServiceDown**: Kích hoạt khi một service thiết yếu không hoạt động (thời gian: 15 giây)
   ```
   up{job="essential-services"} == 0 or absent(up{job="essential-services"})
   ```

2. **HighCPUUsage**: Kích hoạt khi CPU sử dụng > 80% (thời gian: 5 phút)
   ```
   process_cpu_seconds_total / rate(process_cpu_seconds_total[5m]) > 0.8
   ```

3. **HighMemoryUsage**: Kích hoạt khi bộ nhớ sử dụng > 80% (thời gian: 5 phút)
   ```
   sum(container_memory_usage_bytes) / sum(container_memory_max_usage_bytes) > 0.8
   ```

4. **HostOutOfDiskSpace**: Kích hoạt khi dung lượng đĩa còn < 10% (thời gian: 5 phút)
   ```
   (node_filesystem_avail_bytes / node_filesystem_size_bytes * 100) < 10
   ```

5. **HighLatency**: Kích hoạt khi thời gian phản hồi > 1 giây (thời gian: 5 phút)
   ```
   histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m])) > 1
   ```

6. **APIEndpointDown**: Kích hoạt khi endpoint API không phản hồi (thời gian: 1 phút)
   ```
   probe_success{job=~"eshop-services"} == 0
   ```

### Giám sát các dịch vụ thiết yếu

Prometheus được cấu hình để luôn giám sát các dịch vụ thiết yếu, ngay cả khi chúng bị scale xuống 0 replica:

```yaml
- job_name: 'essential-services'
  static_configs:
  - targets: 
    - 'basket-api.eshop-microservices.svc.cluster.local:80'
    - 'catalog-api.eshop-microservices.svc.cluster.local:80'
    - 'auth-api.eshop-microservices.svc.cluster.local:80'
    - 'ordering-api.eshop-microservices.svc.cluster.local:80'
    - 'discount-grpc.eshop-microservices.svc.cluster.local:80'
```

## Truy cập các UI giám sát

Các UI giám sát có thể được truy cập qua Kong API Gateway:

- **Prometheus**: http://{GATEWAY_IP}/prometheus
- **AlertManager**: http://{GATEWAY_IP}/alertmanager
- **Grafana**: http://{GATEWAY_IP}/grafana (username: admin, password: admin)

## Kiểm tra và thử nghiệm hệ thống cảnh báo

### Cách thử nghiệm kích hoạt cảnh báo:

1. Tạo tình huống cảnh báo bằng cách scale down một service thiết yếu:
   ```bash
   kubectl scale deployment basket-api --replicas=0 -n eshop-microservices
   ```

2. Kiểm tra xem cảnh báo đã được kích hoạt chưa (sau khoảng 15 giây):
   ```bash
   kubectl port-forward -n monitoring svc/prometheus 9090:9090
   ```
   Sau đó mở trình duyệt tại http://localhost:9090/alerts

3. Khôi phục service:
   ```bash
   kubectl scale deployment basket-api --replicas=1 -n eshop-microservices
   ```

## Các metrics quan trọng cần giám sát

1. **CPU và Memory sử dụng**:
   - Tỷ lệ CPU sử dụng
   - Tỷ lệ Memory sử dụng
   - Giới hạn CPU/Memory

2. **Latency và Throughput**:
   - Response time (p50, p95, p99)
   - Requests per second
   - Error rate

3. **Database**:
   - Connection pool usage
   - Query execution time
   - Transaction rate

4. **Message broker (RabbitMQ)**:
   - Queue depth
   - Message processing rate
   - Consumer lag
