"""
PULSE Clinical Decision Support System (CDSS)
Microservice: FastAPI High-Performance Inference & Explainability Engine
Module: ml.main
"""

from contextlib import asynccontextmanager
import logging
import time
from typing import AsyncGenerator

from fastapi import FastAPI, Request, status
from fastapi.exceptions import RequestValidationError
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import JSONResponse

from ml.routers.health import router as health_router
from ml.routers.predict import router as predict_router
from ml.services.inference_service import get_inference_service

# Configure structured logging
logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(name)s: %(message)s",
)
logger = logging.getLogger("pulse.api")


# ---------------------------------------------------------------------------
# Lifespan Context Manager (Startup / Shutdown Artifact Lifecycle)
# ---------------------------------------------------------------------------
@asynccontextmanager
async def lifespan(app: FastAPI) -> AsyncGenerator[None, None]:
    """
    Manages microservice lifecycle. Loads all machine learning artifacts once
    into application state during startup to guarantee sub-30ms inference requests.
    """
    logger.info("Initializing PULSE CDSS Inference Microservice...")
    try:
        service = get_inference_service()
        service.load_artifacts()
        app.state.inference_service = service
        logger.info("PULSE CDSS artifacts successfully loaded and ready for inference.")
    except Exception as e:
        logger.error(f"Failed to load ML artifacts during startup: {e}", exc_info=True)
        # Service continues running in degraded state to report health failures
    yield
    logger.info("Shutting down PULSE CDSS Inference Microservice...")
    if hasattr(app.state, "inference_service"):
        app.state.inference_service = None



# ---------------------------------------------------------------------------
# FastAPI Application Factory & OpenAPI Metadata
# ---------------------------------------------------------------------------
tags_metadata = [
    {
        "name": "Clinical Inference & Explainability",
        "description": (
            "Core CDSS inference pipeline. Accepts patient physiological indicators, "
            "validates physiological constraints, computes calibrated posterior probability, "
            "estimates 95% confidence intervals, and calculates local SHAP feature attributions."
        ),
    },
    {
        "name": "Diagnostics & Health",
        "description": "Liveness probes, readiness checks, and model metadata endpoints.",
    },
]

app = FastAPI(
    title="PULSE - Clinical Decision Support System (CDSS) API",
    description=(
        "Production-grade Machine Learning & Explainable AI (XAI) inference microservice "
        "for Cardiovascular Risk Stratification. Provides calibrated posterior probabilities, "
        "strict physiological boundary validation, and real-time SHAP feature attributions."
    ),
    version="1.0.0",
    openapi_tags=tags_metadata,
    lifespan=lifespan,
    docs_url="/docs",
    redoc_url="/redoc",
    openapi_url="/openapi.json",
)

# ---------------------------------------------------------------------------
# Middleware Configuration
# ---------------------------------------------------------------------------
app.add_middleware(
    CORSMiddleware,
    allow_origins=[
        "http://localhost:3000",
        "https://localhost:5001",
        "http://localhost:5000",
        "http://127.0.0.1:3000",
        "http://127.0.0.1:5001",
        "http://127.0.0.1:5000",
    ],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)


@app.middleware("http")
async def add_process_time_header(request: Request, call_next):
    """Measures and appends total request processing latency to headers."""
    start_time = time.perf_counter()
    response = await call_next(request)
    process_time_ms = (time.perf_counter() - start_time) * 1000.0
    response.headers["X-Process-Time-Ms"] = f"{process_time_ms:.2f}"
    return response


# ---------------------------------------------------------------------------
# Custom Exception Handlers (RFC 7807 ProblemDetails / Actionable Validation)
# ---------------------------------------------------------------------------
@app.exception_handler(RequestValidationError)
async def validation_exception_handler(request: Request, exc: RequestValidationError):
    """
    Translates Pydantic validation errors into RFC 7807-compatible structured problem
    details with clear, actionable clinical error messages.
    """
    formatted_errors = []
    for err in exc.errors():
        msg = err.get("msg", "Validation error")
        # Strip internal 'Value error, ' prefix if present
        if msg.startswith("Value error, "):
            msg = msg[len("Value error, "):]

        loc_parts = [str(part) for part in err.get("loc", [])]
        formatted_errors.append({
            "loc": loc_parts,
            "msg": msg,
            "type": err.get("type", "value_error"),
            "input": err.get("input"),
        })

    return JSONResponse(
        status_code=422,
        content={
            "title": "Validation Error",
            "status": 422,
            "detail": "Patient clinical indicators failed physiological boundary validation.",
            "errors": formatted_errors,
        },
    )


@app.exception_handler(ValueError)
async def value_error_handler(request: Request, exc: ValueError):
    """Catches clinical value errors and translates them to HTTP 422."""
    return JSONResponse(
        status_code=422,
        content={
            "title": "Clinical Parameter Error",
            "status": 422,
            "detail": str(exc),
            "errors": [{"loc": ["body"], "msg": str(exc), "type": "value_error", "input": None}],
        },
    )


# ---------------------------------------------------------------------------
# Route Registration
# ---------------------------------------------------------------------------
app.include_router(health_router)
app.include_router(predict_router)


@app.get(
    "/",
    tags=["Diagnostics & Health"],
    summary="API Root Information",
    description="Returns service name, active version, and documentation pointers.",
)
def root():
    """Service landing endpoint."""
    return {
        "service": "PULSE Clinical Decision Support System (CDSS) API",
        "version": "1.0.0",
        "documentation": "/docs",
        "openapi_spec": "/openapi.json",
        "health": "/health",
        "predict": "/api/v1/predict",
    }


if __name__ == "__main__":
    import uvicorn

    uvicorn.run("ml.main:app", host="0.0.0.0", port=8000, reload=True)
