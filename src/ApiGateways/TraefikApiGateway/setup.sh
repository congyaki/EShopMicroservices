#!/bin/sh

# Wait for Traefik to be up and running
echo "Waiting for Traefik to be available..."
until curl -s http://traefik:8080/api/overview > /dev/null; do
  sleep 2
done

echo "Traefik is available. Setting up configuration..."

# Copy config file to the volume directory
cp /config/traefik-config.yaml /config/dynamic_config.yaml

echo "Traefik configuration successfully deployed!"

# Keep the container running to maintain volume mount
tail -f /dev/null