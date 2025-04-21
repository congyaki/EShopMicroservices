# EShopMicroservices

## Container Ports

### Database Services (5000-5999)
| Service | Container Name | Port Mapping |
|---------|---------------|--------------|
| Catalog Database | catalogdb | 5001:5432 |
| Basket Database | basketdb | 5002:5432 |
| Auth Database | authdb | 5003:5432 |
| Order Database | orderdb | 5004:1433 |

### Infrastructure Services (7000-7999)
| Service | Container Name | Port Mapping |
|---------|---------------|--------------|
| Redis Cache | distributedcache | 7001:6379 |
| RabbitMQ | messagebroker | 7002:5672, 7003:15672 |
| Consul | consul | 7500:8500, 7600:8600/udp |

### API Services
#### Catalog API (8000-8009, 9000-9009)
| Service | Container Name | Port Mapping |
|---------|---------------|--------------|
| Catalog API 1 | catalog-api-1 | 8001:80, 9001:443 |
| Catalog API 2 | catalog-api-2 | 8002:80, 9002:443 |
| Catalog API 3 | catalog-api-3 | 8003:80, 9003:443 |

#### Basket API (8010-8019, 9010-9019)
| Service | Container Name | Port Mapping |
|---------|---------------|--------------|
| Basket API 1 | basket-api-1 | 8011:80, 9011:443 |

#### Discount gRPC (8020-8029, 9020-9029)
| Service | Container Name | Port Mapping |
|---------|---------------|--------------|
| Discount gRPC | discount.grpc | 8021:80, 9021:443 |

#### Ordering API (8030-8039, 9030-9039)
| Service | Container Name | Port Mapping |
|---------|---------------|--------------|
| Ordering API 1 | ordering-api-1 | 8031:80, 9031:443 |

#### Auth API (8040-8049, 9040-9049)
| Service | Container Name | Port Mapping |
|---------|---------------|--------------|
| Auth API 1 | auth-api-1 | 8041:80, 9041:443 |

### API Gateways (8080-8099, 9080-9099)
| Service | Container Name | Port Mapping |
|---------|---------------|--------------|
| YARP API Gateway | yarpapigateway | 8080:80 |
| Ocelot API Gateway | ocelotapigateway | 8090:80, 9090:443 |