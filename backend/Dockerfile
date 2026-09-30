# ==============================================================================
# PULSE Clinical Decision Support System (CDSS)
# Module: backend.Dockerfile (.NET 10 Enterprise Orchestration Layer)
# Architecture: Multi-Stage Minimal Container with Healthcheck
# Security: Non-Root Execution (UID 10001:GID 10001)
# ==============================================================================

# ------------------------------------------------------------------------------
# Stage 1: Build & Publish
# ------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files for optimized layer caching during restore
COPY backend/src/HeartCDSS.Domain/HeartCDSS.Domain.csproj HeartCDSS.Domain/
COPY backend/src/HeartCDSS.Application/HeartCDSS.Application.csproj HeartCDSS.Application/
COPY backend/src/HeartCDSS.Infrastructure/HeartCDSS.Infrastructure.csproj HeartCDSS.Infrastructure/
COPY backend/src/HeartCDSS.Api/HeartCDSS.Api.csproj HeartCDSS.Api/

# Restore dependencies
RUN dotnet restore HeartCDSS.Api/HeartCDSS.Api.csproj

# Copy remaining source code
COPY backend/src/ .

# Build and publish release binaries
WORKDIR /src/HeartCDSS.Api
RUN dotnet publish HeartCDSS.Api.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# ------------------------------------------------------------------------------
# Stage 2: Runtime Container
# ------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Install curl for reliable container healthchecks
RUN apt-get update && apt-get install -y --no-install-recommends \
    curl \
    && rm -rf /var/lib/apt/lists/*

# Security Hardening: Make dedicated non-root user and group
RUN groupadd -g 10001 pulsegroup && \
    useradd -u 10001 -g pulsegroup -s /sbin/nologin -M pulseuser

# Runtime environment settings
ENV ASPNETCORE_URLS=http://+:5000 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    PORT=5000

# Copy published binaries with non-root ownership
COPY --from=build --chown=pulseuser:pulsegroup /app/publish /app

# Drop privileges to non-root user
USER 10001:10001

# Expose internal service HTTP port
EXPOSE 5000

# Container Healthcheck
HEALTHCHECK --interval=10s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:5000/api/health || exit 1

# Start ASP.NET Core orchestration service
ENTRYPOINT ["dotnet", "HeartCDSS.Api.dll"]
