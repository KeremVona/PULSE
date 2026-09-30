# ==============================================================================
# PULSE Clinical Decision Support System (CDSS)
# Module: ml.Dockerfile
# Architecture: Multi-Stage Lightweight Hardened Container (< 500 MB)
# Security: Non-Root Execution (UID 10001:GID 10001), Minimal Attack Surface
# ==============================================================================

# ------------------------------------------------------------------------------
# Stage 1: Build & Dependency Wheel Compilation
# ------------------------------------------------------------------------------
FROM python:3.12-slim-bookworm AS builder

# Prevent python from writing pyc files and buffering stdout/stderr
ENV PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1

WORKDIR /build

# Install minimal build tools for C-extension compilation
RUN apt-get update && apt-get install -y --no-install-recommends \
    build-essential \
    && rm -rf /var/lib/apt/lists/*

# Make isolated production virtual environment
RUN python -m venv /opt/venv
ENV PATH="/opt/venv/bin:$PATH"

# Install production dependencies
COPY ml/requirements.txt requirements.txt
RUN pip install --no-cache-dir --upgrade pip && \
    pip install --no-cache-dir -r requirements.txt

# Prune unnecessary documentation, test suites, and caches from installed packages
RUN find /opt/venv -type d -name "tests" -exec rm -rf {} + 2>/dev/null || true && \
    find /opt/venv -type d -name "__pycache__" -exec rm -rf {} + 2>/dev/null || true && \
    find /opt/venv -type f -name "*.pyc" -delete 2>/dev/null || true

# ------------------------------------------------------------------------------
# Stage 2: Minimal Non-Root Runtime Container
# ------------------------------------------------------------------------------
FROM python:3.12-slim-bookworm AS runtime

# Install libgomp1 for OpenMP acceleration in LightGBM runtime
RUN apt-get update && apt-get install -y --no-install-recommends \
    libgomp1 \
    && rm -rf /var/lib/apt/lists/*

# Security Hardening: Enforce unprivileged user execution (Least Privilege Principle)
# Make a dedicated system group and user with no shell login
RUN groupadd -g 10001 pulsegroup && \
    useradd -u 10001 -g pulsegroup -s /sbin/nologin -M pulseuser

# Runtime environment settings
ENV PATH="/opt/venv/bin:$PATH" \
    PYTHONUNBUFFERED=1 \
    PYTHONDONTWRITEBYTECODE=1 \
    PYTHONPATH=/app \
    PORT=8000

WORKDIR /app

# Copy isolated virtual environment from builder stage with strict ownership
COPY --from=builder --chown=pulseuser:pulsegroup /opt/venv /opt/venv

# Copy only the ML microservice code and serialized model artifacts
COPY --chown=pulseuser:pulsegroup ml/main.py /app/ml/main.py
COPY --chown=pulseuser:pulsegroup ml/schemas /app/ml/schemas
COPY --chown=pulseuser:pulsegroup ml/routers /app/ml/routers
COPY --chown=pulseuser:pulsegroup ml/services /app/ml/services
COPY --chown=pulseuser:pulsegroup ml/src /app/ml/src
COPY --chown=pulseuser:pulsegroup ml/artifacts /app/ml/artifacts

# Drop privileges to unprivileged non-root user
USER 10001:10001

# Expose internal microservice HTTP port
EXPOSE 8000

# Container Healthcheck:
# Verifies container health and readiness within 5 seconds of startup
# Uses native python urllib to eliminate the need for curl/wget binaries
HEALTHCHECK --interval=10s --timeout=3s --start-period=5s --retries=3 \
    CMD python -c "import urllib.request, sys; sys.exit(0 if urllib.request.urlopen('http://localhost:8000/health', timeout=2).getcode() == 200 else 1)"

# Start high-performance production server
CMD ["uvicorn", "ml.main:app", "--host", "0.0.0.0", "--port", "8000", "--workers", "1"]
