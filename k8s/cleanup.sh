#!/bin/bash
# cleanup.sh

echo "Cleaning up Kubernetes resources..."
kubectl delete -f gateways.yaml --ignore-not-found
kubectl delete -f microservices.yaml --ignore-not-found
kubectl delete -f infrastructure.yaml --ignore-not-found
kubectl delete -f databases.yaml --ignore-not-found

# Give time for pods to terminate before removing volumes
echo "Waiting for pods to terminate..."
sleep 10

kubectl delete -f persistent-volumes.yaml --ignore-not-found
kubectl delete -f configmap.yaml --ignore-not-found
kubectl delete -f namespace.yaml --ignore-not-found

echo "Cleanup complete!"
