FROM nginx:1.25-alpine

# Install curl for health checks and setup
RUN apk add --no-cache curl

WORKDIR /etc/nginx

# Copy NGINX configuration
COPY ["ApiGateways/NginxApiGateway/nginx.conf", "/etc/nginx/nginx.conf"]

EXPOSE 80

# Use CMD instead of ENTRYPOINT to allow docker-compose to override it
CMD ["nginx", "-g", "daemon off;"]

