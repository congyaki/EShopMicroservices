FROM curlimages/curl:8.5.0 AS build

USER root
WORKDIR /tmp
COPY ["ApiGateways/KongApiGateway/kong-setup.sh", "/tmp/kong-setup.sh"]
RUN chmod +x /tmp/kong-setup.sh

FROM curlimages/curl:8.5.0

WORKDIR /scripts
COPY --from=build /tmp/kong-setup.sh /scripts/kong-setup.sh

USER curl_user
ENTRYPOINT ["sh", "/scripts/kong-setup.sh"]