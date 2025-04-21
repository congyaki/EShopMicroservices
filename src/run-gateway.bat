@echo off
echo Select API Gateway:
echo 1. Ocelot
echo 2. YARP
echo 3. Kong
echo 4. Traefik
echo 5. NGINX
set /p choice=Enter your choice (1-5): 

if "%choice%"=="1" docker-compose --profile ocelot up -d
if "%choice%"=="2" docker-compose --profile yarp up -d
if "%choice%"=="3" docker-compose --profile kong up -d
if "%choice%"=="4" docker-compose --profile traefik up -d
if "%choice%"=="5" docker-compose --profile nginx up -d

echo.
echo API Gateway started successfully.