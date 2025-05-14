#!/bin/bash

# This script adds Prometheus annotations to all microservices for metrics collection

# Add annotations to Catalog API
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
      },
      "spec": {
        "containers": [
          {
            "name": "catalog-api",
            "readinessProbe": {
              "httpGet": {
                "path": "/metrics-probe",
                "port": 80
              },
              "initialDelaySeconds": 15,
              "periodSeconds": 10
            },
            "livenessProbe": {
              "httpGet": {
                "path": "/health",
                "port": 80
              },
              "initialDelaySeconds": 30,
              "periodSeconds": 30
            }
          }
        ]
      }
    }
  }
}'

# Add annotations to Basket API
kubectl patch deployment basket-api -n eshop-microservices -p '
{
  "spec": {
    "template": {
      "metadata": {
        "annotations": {
          "prometheus.io/scrape": "true",
          "prometheus.io/port": "80",
          "prometheus.io/path": "/metrics"
        }
      },
      "spec": {
        "containers": [
          {
            "name": "basket-api",
            "readinessProbe": {
              "httpGet": {
                "path": "/metrics-probe",
                "port": 80
              },
              "initialDelaySeconds": 15,
              "periodSeconds": 10
            },
            "livenessProbe": {
              "httpGet": {
                "path": "/health",
                "port": 80
              },
              "initialDelaySeconds": 30,
              "periodSeconds": 30
            }
          }
        ]
      }
    }
  }
}'

# Add annotations to Auth API
kubectl patch deployment auth-api -n eshop-microservices -p '
{
  "spec": {
    "template": {
      "metadata": {
        "annotations": {
          "prometheus.io/scrape": "true",
          "prometheus.io/port": "80",
          "prometheus.io/path": "/metrics"
        }
      },
      "spec": {
        "containers": [
          {
            "name": "auth-api",
            "readinessProbe": {
              "httpGet": {
                "path": "/metrics-probe",
                "port": 80
              },
              "initialDelaySeconds": 15,
              "periodSeconds": 10
            },
            "livenessProbe": {
              "httpGet": {
                "path": "/health",
                "port": 80
              },
              "initialDelaySeconds": 30,
              "periodSeconds": 30
            }
          }
        ]
      }
    }
  }
}'

# Add annotations to Ordering API
kubectl patch deployment ordering-api -n eshop-microservices -p '
{
  "spec": {
    "template": {
      "metadata": {
        "annotations": {
          "prometheus.io/scrape": "true",
          "prometheus.io/port": "80",
          "prometheus.io/path": "/metrics"
        }
      },
      "spec": {
        "containers": [
          {
            "name": "ordering-api",
            "readinessProbe": {
              "httpGet": {
                "path": "/metrics-probe",
                "port": 80
              },
              "initialDelaySeconds": 15,
              "periodSeconds": 10
            },
            "livenessProbe": {
              "httpGet": {
                "path": "/health",
                "port": 80
              },
              "initialDelaySeconds": 30,
              "periodSeconds": 30
            }
          }
        ]
      }
    }
  }
}'

# Add annotations to Discount gRPC
kubectl patch deployment discount-grpc -n eshop-microservices -p '
{
  "spec": {
    "template": {
      "metadata": {
        "annotations": {
          "prometheus.io/scrape": "true",
          "prometheus.io/port": "80",
          "prometheus.io/path": "/metrics"
        }
      },
      "spec": {
        "containers": [
          {
            "name": "discount-grpc",
            "readinessProbe": {
              "httpGet": {
                "path": "/metrics-probe",
                "port": 80
              },
              "initialDelaySeconds": 15,
              "periodSeconds": 10
            },
            "livenessProbe": {
              "httpGet": {
                "path": "/health",
                "port": 80
              },
              "initialDelaySeconds": 30,
              "periodSeconds": 30
            }
          }
        ]
      }
    }
  }
}'

# Cập nhật labels cho services để Prometheus tìm được chúng
kubectl patch service catalog-api -n eshop-microservices -p '{"metadata":{"labels":{"app":"catalog-api","metrics":"prometheus"}}}'
kubectl patch service basket-api -n eshop-microservices -p '{"metadata":{"labels":{"app":"basket-api","metrics":"prometheus"}}}'
kubectl patch service auth-api -n eshop-microservices -p '{"metadata":{"labels":{"app":"auth-api","metrics":"prometheus"}}}'
kubectl patch service ordering-api -n eshop-microservices -p '{"metadata":{"labels":{"app":"ordering-api","metrics":"prometheus"}}}'
kubectl patch service discount-grpc -n eshop-microservices -p '{"metadata":{"labels":{"app":"discount-grpc","metrics":"prometheus"}}}'

echo "Prometheus annotations added to all microservices"
echo "You can verify with: kubectl get pods -n eshop-microservices -o jsonpath='{.items[*].metadata.annotations}'" 