# syntax=docker/dockerfile:1
# One image serves both the Angular UI (static files in wwwroot) and the .NET API on port 8080.

# ---- 1. Build the Angular app ----
FROM node:24-alpine AS web
WORKDIR /src
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci
COPY frontend/ ./
RUN npx ng build --configuration production

# ---- 2. Build and publish the API ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api
WORKDIR /src
COPY nuget.config ./
COPY backend/VehicleExplorer.Api/VehicleExplorer.Api.csproj backend/VehicleExplorer.Api/
RUN dotnet restore backend/VehicleExplorer.Api/VehicleExplorer.Api.csproj
COPY backend/VehicleExplorer.Api/ backend/VehicleExplorer.Api/
RUN dotnet publish backend/VehicleExplorer.Api/VehicleExplorer.Api.csproj \
    --configuration Release --no-restore --output /app

# ---- 3. Runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app
COPY --from=api /app ./
COPY --from=web /src/dist/vehicle-explorer/browser ./wwwroot

# Non-root user that ships with the official .NET images.
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "VehicleExplorer.Api.dll"]
