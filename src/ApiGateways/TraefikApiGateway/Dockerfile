FROM alpine:3.19

RUN apk add --no-cache curl jq

WORKDIR /config

COPY ["ApiGateways/TraefikApiGateway/traefik-config.yaml", "/config/traefik-config.yaml"]
COPY ["ApiGateways/TraefikApiGateway/setup.sh", "/setup.sh"]

RUN chmod +x /setup.sh

CMD ["/setup.sh"]