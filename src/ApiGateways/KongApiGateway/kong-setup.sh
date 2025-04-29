#!/bin/sh

# Wait for Kong to be available
echo "Waiting for Kong Admin API to be available..."
until curl -s http://kong:8001 > /dev/null; do
  sleep 2
done

echo "Kong Admin API is available. Setting up routes and services..."

# Create services for each microservice

# Auth Service
curl -s -X POST http://kong:8001/services \
  --data name=auth-service \
  --data url=http://auth-api-1:80

# Catalog Service with multiple instances (load balanced)
curl -s -X POST http://kong:8001/services \
  --data name=catalog-service \
  --data url=http://catalog-api-1:80

# Basket Service
curl -s -X POST http://kong:8001/services \
  --data name=basket-service \
  --data url=http://basket-api-1:80

# Ordering Service
curl -s -X POST http://kong:8001/services \
  --data name=ordering-service \
  --data url=http://ordering-api-1:80

# Create routes for each service
# Auth Service Route
curl -s -X POST http://kong:8001/services/auth-service/routes \
  --data name=auth-route \
  --data "paths[]=/auth-service" \
  --data "paths[]=/auth-service/(?P<path>.*)" \
  --data "strip_path=true"

# Catalog Service Route
curl -s -X POST http://kong:8001/services/catalog-service/routes \
  --data name=catalog-route \
  --data "paths[]=/catalog-service" \
  --data "paths[]=/catalog-service/(?P<path>.*)" \
  --data "strip_path=true"

# Basket Service Route
curl -s -X POST http://kong:8001/services/basket-service/routes \
  --data name=basket-route \
  --data "paths[]=/basket-service" \
  --data "paths[]=/basket-service/(?P<path>.*)" \
  --data "strip_path=true"

# Ordering Service Route
curl -s -X POST http://kong:8001/services/ordering-service/routes \
  --data name=ordering-route \
  --data "paths[]=/ordering-service" \
  --data "paths[]=/ordering-service/(?P<path>.*)" \
  --data "strip_path=true"

# Configure load balancing for catalog service (3 instances)
curl -s -X POST http://kong:8001/upstreams \
  --data name=catalog-upstream \
  --data algorithm=round-robin

curl -s -X POST http://kong:8001/upstreams/catalog-upstream/targets \
  --data target=catalog-api-1:80 \
  --data weight=100

curl -s -X POST http://kong:8001/upstreams/catalog-upstream/targets \
  --data target=catalog-api-2:80 \
  --data weight=100

curl -s -X POST http://kong:8001/upstreams/catalog-upstream/targets \
  --data target=catalog-api-3:80 \
  --data weight=100

# Update catalog service to use the upstream
curl -s -X PATCH http://kong:8001/services/catalog-service \
  --data host=catalog-upstream

# Add rate limiting plugin globally - standardized across all gateways
curl -s -X POST http://kong:8001/plugins \
  --data name=rate-limiting \
  --data config.second=50 \
  --data config.policy=local

# Add JWT plugin
echo "Setting up JWT authentication..."

# Add JWT plugin for JWT validation
curl -s -X POST http://kong:8001/plugins \
  --data name=jwt \
  --data config.claims_to_verify=exp

# Create a consumer for the microservices
curl -s -X POST http://kong:8001/consumers \
  --data username=microservice-clients

# Create a JWT credential with the same parameters as in Auth service
curl -s -X POST http://kong:8001/consumers/microservice-clients/jwt \
  --data algorithm=HS256 \
  --data key=authservice \
  --data secret=727410b36cdc4b5c8ac91011dd2083db8381aa7b07d245b787800a5e263f525a

# Exclude JWT authentication for Auth Service (login and register endpoints)
curl -s -X POST http://kong:8001/services/auth-service/plugins \
  --data name=request-transformer \
  --data config.remove.headers=Authorization

# Disable JWT for Auth service
curl -s -X POST http://kong:8001/services/auth-service/plugins \
  --data name=jwt \
  --data config.enabled=false

echo "Kong API Gateway configuration completed with JWT authentication!"