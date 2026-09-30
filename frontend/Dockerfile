# ==============================================================================
# PULSE Clinical Decision Support System (CDSS)
# Module: frontend.Dockerfile (React 19 + Tailwind CSS v4 + Nginx)
# Architecture: Multi-Stage Production SPA Delivery Container
# ==============================================================================

# ------------------------------------------------------------------------------
# Stage 1: Build Frontend Single-Page Application
# ------------------------------------------------------------------------------
FROM node:22-alpine AS build
WORKDIR /app

# Copy dependency manifests for layer caching
COPY frontend/package.json frontend/package-lock.json ./

# Install exact production & build dependencies
RUN npm ci

# Copy source code and build config
COPY frontend/ ./

# Compile production bundles via TypeScript compiler and Vite
RUN npm run build

# ------------------------------------------------------------------------------
# Stage 2: Production Nginx Runtime
# ------------------------------------------------------------------------------
FROM nginx:1.27-alpine AS runtime

# Copy compiled web assets from builder stage
COPY --from=build /app/dist /usr/share/nginx/html

# Copy optimized reverse proxy configuration
COPY frontend/nginx.conf /etc/nginx/conf.d/default.conf

# Expose HTTP port
EXPOSE 80

# Container Healthcheck
HEALTHCHECK --interval=10s --timeout=3s --start-period=5s --retries=3 \
    CMD wget -q -O /dev/null http://127.0.0.1:80/ || exit 1

# Start Nginx server in foreground mode
CMD ["nginx", "-g", "daemon off;"]
