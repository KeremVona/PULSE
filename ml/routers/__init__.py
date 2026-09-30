"""
PULSE Clinical Decision Support System (CDSS)
Routers package.
"""

from ml.routers.health import router as health_router
from ml.routers.predict import router as predict_router

__all__ = ["health_router", "predict_router"]
