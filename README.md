# PULSE — Clinical Decision Support System (CDSS)

> **Cardiovascular Risk Stratification & Explainable AI (XAI)**
> An enterprise-grade clinical decision support platform combining calibrated machine learning, local SHAP attributions, resilient microservice architecture, and ergonomic doctor-facing interfaces.

---

## 🩺 Overview

PULSE (Predictive Utility for Laboratory and Symptom Evaluation) is an explainable Clinical Decision Support System (CDSS) tailored for clinical risk screening using physiological indicators from the UCI Heart Disease dataset. In acute cardiovascular medicine, diagnostic cost is asymmetric: missing an active coronary condition (a **False Negative**) carries severe clinical consequences, whereas a **False Positive** triggers safe, non-invasive secondary testing.

Heart addresses this challenge by:

1. **Calibrated Posterior Risks**: Moving beyond raw classification scores to true empirical probabilities using Isotonic Regression and Platt Scaling.
2. **Asymmetric Cost Optimization**: Tuning decision thresholds against the $F_2$ metric to strictly prioritize clinical **Sensitivity / Recall ($\ge 0.92$)**.
3. **Local Explainability (XAI)**: Leveraging SHAP `TreeExplainer` per patient to provide clinicians with clear positive (risk-increasing) and protective (risk-reducing) physiological drivers.
4. **Resilient Microservices**: Decoupling CPU-intensive ML inference (Python FastAPI) from enterprise orchestration, audit logging, and RBAC (ASP.NET Core 10 Web API).

---

## 🏛️ System Architecture

```text
+-----------------------------------------------------------------------------------+
| FRONTEND: React 19 + TypeScript (Tailwind CSS, Redux Toolkit, Axios)               |
+-----------------------------------------------------------------------------------+
                                         |  HTTPS / REST (JWT Bearer)
                                         v
+-----------------------------------------------------------------------------------+
| CORE API GATEWAY: ASP.NET Core Web API (.NET 10)                                  |
| - Authentication & RBAC (Physician / Clinical Staff)                              |
| - FluentValidation (Physiological Boundary Checking)                              |
| - Audit Logging & Record Versioning                                               |
+-----------------------------------------------------------------------------------+
             |                                                  |
             | Internal REST / Resilient HTTP                   | EF Core Migrations
             v                                                  v
+-----------------------------+               +-------------------------------------+
| ML INFERENCE ENGINE         |               | DATABASE: PostgreSQL                |
| - Python + FastAPI          |               | - Patients & Encounter Records      |
| - LightGBM + Scikit-Learn   |               | - Clinical Indicator Snapshots      |
| - SHAP TreeExplainer        |               | - Prediction History & Audit Traces |
+-----------------------------+               +-------------------------------------+
```

---

## 📁 Repository Structure

```text
Heart/
├── backend/            # ASP.NET Core 10 Clean Architecture Web API
├── frontend/           # React 19 + TypeScript + Tailwind CSS clinician portal
├── ml/                 # Python 3.12 ML training pipeline & FastAPI inference service
│   ├── artifacts/      # Serialized models and scalers (.joblib)
│   └── data/           # Dataset storage and preprocessed sets
├── .gitignore          # Environment, Python, .NET, and Node exclusions
├── LICENSE             # MIT License
└── README.md           # Project documentation
```

## ⚠️ Clinical Decision Support Notice

> **IMPORTANT DISCLAIMER**  
> _This software is an experimental adjunct screening tool and clinical decision support prototype. It does not provide medical diagnosis or replace professional medical evaluation, clinical judgment, diagnostic testing, or biomarker panels (e.g., troponin, echocardiogram). Always consult qualified clinical healthcare professionals._

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
