#!/bin/bash
# deploy.sh

# Set variables for your Docker registry (if using)
# export DOCKER_REGISTRY=your-registry/

# Check if Kubernetes is running in Docker Desktop
kubectl cluster-info &> /dev/null
if [ $? -ne 0 ]; then
  echo "Error: Kubernetes does not appear to be running in Docker Desktop."
  echo "Please enable Kubernetes in Docker Desktop settings and try again."
  exit 1
fi

echo "Building Docker images..."
# Build all your Docker images
docker-compose build

echo "Creating Kubernetes resources..."
kubectl apply -f namespace.yaml
kubectl apply -f configmap.yaml
kubectl apply -f persistent-volumes.yaml
kubectl apply -f databases.yaml
kubectl apply -f infrastructure.yaml
kubectl apply -f microservices.yaml
kubectl apply -f gateways.yaml

echo "Deployment complete! Resources are being provisioned..."
echo "You can check status with: kubectl get pods -n ecommerce-dev"
echo "Access the application via the Ocelot API Gateway LoadBalancer service"
