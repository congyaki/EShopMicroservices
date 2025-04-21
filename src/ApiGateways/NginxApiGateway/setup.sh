#!/bin/sh

# Wait for NGINX to be available
echo "Waiting for NGINX to be available..."
until curl -s http://nginx:80/nginx_status > /dev/null; do
  sleep 2
done

echo "NGINX is available. Configuration is active."

# We can add dynamic configuration updates here if needed
# For example, updating upstream server weights based on health checks

# Keep the container running to maintain volume mount
tail -f /dev/null

