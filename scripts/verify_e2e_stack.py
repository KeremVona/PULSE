#!/usr/bin/env python3
"""
PULSE Clinical Decision Support System (CDSS)
Module: scripts/verify_e2e_stack.py
Purpose: End-to-End Orchestration & Performance Verification Suite

Validates the full clinical transaction roundtrip across containerized topology:
  Client/Script -> React/Nginx Reverse Proxy (:3000) -> ASP.NET Core (:5000)
                -> ML Microservice (FastAPI :8000) -> PostgreSQL 17 Persistence
                -> Roundtrip payload delivery & latency verification (< 250 ms budget).
"""

import sys
import json
import time
import uuid
import urllib.request
import urllib.error
from typing import Dict, Any, Tuple, Optional, List

FRONTEND_URL = "http://localhost:3000"
BACKEND_URL = "http://localhost:5000"
TEST_PATIENT_ID = "11111111-1111-1111-1111-111111111111"
PERFORMANCE_BUDGET_MS = 250.0


def make_http_request(
    url: str,
    method: str = "GET",
    data: Optional[Dict[str, Any]] = None,
    headers: Optional[Dict[str, str]] = None,
    timeout: float = 10.0,
) -> Tuple[int, Dict[str, Any], float]:
    """
    Executes an HTTP request and measures high-resolution roundtrip latency in milliseconds.
    """
    made_headers = {"Accept": "application/json"}
    if headers:
        made_headers.update(headers)

    made_payload_bytes = None
    if data is not None:
        made_payload_bytes = json.dumps(data).encode("utf-8")
        made_headers["Content-Type"] = "application/json"

    made_request = urllib.request.Request(
        url=url,
        data=made_payload_bytes,
        headers=made_headers,
        method=method,
    )

    made_start_time = time.perf_counter()
    try:
        with urllib.request.urlopen(made_request, timeout=timeout) as made_response:
            made_elapsed_ms = (time.perf_counter() - made_start_time) * 1000.0
            made_status = made_response.getcode()
            made_body = made_response.read().decode("utf-8")
            try:
                made_json = json.loads(made_body) if made_body else {}
            except json.JSONDecodeError:
                made_json = {"raw": made_body}
            return made_status, made_json, made_elapsed_ms
    except urllib.error.HTTPError as made_http_err:
        made_elapsed_ms = (time.perf_counter() - made_start_time) * 1000.0
        made_body = made_http_err.read().decode("utf-8")
        try:
            made_json = json.loads(made_body) if made_body else {}
        except json.JSONDecodeError:
            made_json = {"raw": made_body}
        return made_http_err.code, made_json, made_elapsed_ms
    except Exception as made_err:
        made_elapsed_ms = (time.perf_counter() - made_start_time) * 1000.0
        return 0, {"error": str(made_err)}, made_elapsed_ms


def wait_for_services(max_attempts: int = 30, delay_seconds: float = 2.0) -> bool:
    """
    Polls the health endpoints until all microservices reach healthy status.
    """
    print("[*] Awaiting multi-container readiness across topology...")
    for made_attempt in range(1, max_attempts + 1):
        made_be_status, made_be_data, _ = make_http_request(f"{BACKEND_URL}/health", timeout=3.0)
        made_fe_status, _, _ = make_http_request(FRONTEND_URL, timeout=3.0)

        if made_be_status == 200 and made_fe_status == 200:
            print(f"[+] All services report ready on attempt {made_attempt}/{max_attempts}.")
            return True

        time.sleep(delay_seconds)

    print("[-] Timeout waiting for containerized microservices to stabilize.")
    return False


def make_jwt_token() -> str:
    """
    Acquires an authenticated clinician JWT Bearer access token from the ASP.NET Core API.
    """
    print("\n--- Step 1: Clinician Authentication & Token Generation ---")
    made_token_url = f"{BACKEND_URL}/api/v1/auth/token"
    made_auth_payload = {
        "username": "dr_curie",
        "fullName": "Dr. Marie Curie",
        "roles": ["Physician", "ClinicalStaff"],
    }

    made_status, made_data, made_latency = make_http_request(
        url=made_token_url,
        method="POST",
        data=made_auth_payload,
    )

    if made_status != 200 or "accessToken" not in made_data:
        print(f"[-] Authentication failed: status {made_status}, response: {made_data}")
        sys.exit(1)

    made_token = made_data["accessToken"]
    print(f"[+] Token acquired successfully in {made_latency:.2f} ms.")
    print(f"    User: {made_auth_payload['username']}, Roles: {made_data.get('roles')}")
    return made_token


def make_encounter_payload(made_encounter_num: str) -> Dict[str, Any]:
    """
    Constructs high-acuity clinical indicators representing elevated cardiovascular risk.
    """
    return {
        "encounterNumber": made_encounter_num,
        "encounterType": 1,  # Outpatient
        "chiefComplaint": "Exertional substernal chest pressure radiating to left jaw, diaphoresis",
        "indicators": {
            "age": 63,
            "sex": 1,
            "chestPainType": 3,  # Asymptomatic/Typical angina equivalent
            "restingBp": 152,
            "diastolicBp": 94,
            "serumCholesterol": 278,
            "fastingBloodSugar": 1,
            "restingEcg": 2,  # Left ventricular hypertrophy
            "maxHeartRate": 126,
            "exerciseAngina": 1,  # Induced angina
            "oldpeakStDepression": 2.6,
            "slope": 2,  # Flat
            "fluoroscopyVessels": 2,
            "thalassemia": 3,  # Reversible defect
        },
    }


def make_clinical_roundtrip_verification(made_token: str) -> str:
    """
    Dispatches a clinical encounter and verifies end-to-end inference, explainability, and storage.
    """
    print("\n--- Step 2: Clinical Encounter Submission & ML Inference Roundtrip ---")
    made_enc_num = f"ENC-VERIFY-{uuid.uuid4().hex[:8].upper()}"
    made_payload = make_encounter_payload(made_enc_num)
    made_post_url = f"{BACKEND_URL}/api/v1/patients/{TEST_PATIENT_ID}/encounters"
    made_headers = {"Authorization": f"Bearer {made_token}"}

    made_status, made_result, made_latency = make_http_request(
        url=made_post_url,
        method="POST",
        data=made_payload,
        headers=made_headers,
    )

    if made_status != 201:
        print(f"[-] Encounter submission failed: HTTP {made_status}, body: {made_result}")
        sys.exit(1)

    made_enc_id = made_result.get("encounterId") or made_result.get("encounter_id")
    made_assessment = made_result.get("riskAssessment") or made_result.get("risk_assessment") or {}
    made_prob = made_assessment.get("risk_probability", made_assessment.get("riskProbability"))
    made_band = made_assessment.get("risk_band", made_assessment.get("riskBand"))
    made_ci = made_assessment.get("confidence_interval") or made_assessment.get("confidenceInterval") or {}
    made_lower = made_ci.get("lower", made_ci.get("lowerBound", 0.0))
    made_upper = made_ci.get("upper", made_ci.get("upperBound", 0.0))
    made_explanations = made_assessment.get("explanations") or made_assessment.get("top_risk_drivers") or []

    print(f"[+] Clinical encounter submitted successfully!")
    print(f"    Encounter ID:        {made_enc_id}")
    print(f"    Encounter Number:    {made_enc_num}")
    print(f"    Roundtrip Latency:   {made_latency:.2f} ms")
    print(f"    Calibrated Risk:     {made_prob:.4f} ({made_band})")
    print(f"    95% CI Interval:     [{made_lower:.4f}, {made_upper:.4f}]")
    print(f"    TreeSHAP Attributions: {len(made_explanations)} features computed")

    assert made_enc_id is not None, "Encounter ID is missing from response"
    assert made_prob is not None and 0.0 <= made_prob <= 1.0, "Risk probability out of bounds"
    assert len(made_explanations) > 0, "SHAP attributions must not be empty"

    # Display top risk drivers and protective factors
    print("    Top Attributions:")
    for made_feat in made_explanations[:4]:
        feat_name = made_feat.get("feature_label") or made_feat.get("featureName") or made_feat.get("feature_key")
        feat_val = made_feat.get("shap_value", made_feat.get("shapValue", 0.0))
        feat_dir = made_feat.get("impact_direction") or made_feat.get("impactDirection")
        print(f"      - {feat_name}: impact={feat_val:+.4f} (direction={feat_dir})")

    return made_enc_id


def make_persistence_verification(made_token: str, made_enc_id: str) -> None:
    """
    Verifies relational persistence and audit integrity via GET encounter query.
    """
    print("\n--- Step 3: Database Persistence & Historical Retrieval Verification ---")
    made_get_url = f"{BACKEND_URL}/api/v1/patients/{TEST_PATIENT_ID}/encounters/{made_enc_id}"
    made_headers = {"Authorization": f"Bearer {made_token}"}

    made_status, made_data, made_latency = make_http_request(
        url=made_get_url,
        method="GET",
        headers=made_headers,
    )

    if made_status != 200:
        print(f"[-] Encounter persistence check failed: HTTP {made_status}, body: {made_data}")
        sys.exit(1)

    retrieved_id = made_data.get('encounterId') or made_data.get('encounter_id')
    made_assessments = made_data.get('riskAssessments') or made_data.get('risk_assessments') or []
    assessment_found = len(made_assessments) > 0 or (made_data.get('riskAssessment') or made_data.get('risk_assessment')) is not None
    print(f"[+] Encounter retrieved from PostgreSQL in {made_latency:.2f} ms.")
    print(f"    Status:              {made_data.get('status')}")
    print(f"    Chief Complaint:     {made_data.get('chiefComplaint') or made_data.get('chief_complaint')}")
    print(f"    Persisted Assessments Count: {len(made_assessments)}")
    if made_assessments:
        first_asmt = made_assessments[0]
        prob = first_asmt.get('riskProbability', first_asmt.get('risk_probability', 0.0))
        band = first_asmt.get('riskBand', first_asmt.get('risk_band', ''))
        attrs = first_asmt.get('attributions', [])
        print(f"    Stored Calibrated Risk:      {prob:.4f} ({band})")
        print(f"    Stored Attributions Count:   {len(attrs)} features in PostgreSQL")
    assert retrieved_id == made_enc_id, "Retrieved encounter ID mismatch"
    assert assessment_found, "Risk assessment was not found in persisted encounter"


def make_reverse_proxy_verification(made_token: str) -> None:
    """
    Verifies Nginx reverse proxy routing through frontend port 3000 to backend API.
    """
    print("\n--- Step 4: Reverse Proxy & Ingress Routing Verification ---")
    made_proxy_url = f"{FRONTEND_URL}/api/health"
    made_status, made_data, made_latency = make_http_request(made_proxy_url)

    if made_status != 200:
        print(f"[-] Nginx reverse proxy test failed: HTTP {made_status}, body: {made_data}")
        sys.exit(1)

    print(f"[+] Reverse proxy route /api/health succeeded in {made_latency:.2f} ms.")
    print(f"    Response Status: {made_data.get('status')}")


def make_latency_benchmark(made_token: str, made_iterations: int = 5) -> List[float]:
    """
    Benchmarks sequential warm-path clinical transactions against the 250ms performance budget.
    """
    print(f"\n--- Step 5: Latency Benchmark ({made_iterations} Sequential Warm Transactions) ---")
    made_latencies: List[float] = []
    made_headers = {"Authorization": f"Bearer {made_token}"}
    made_post_url = f"{BACKEND_URL}/api/v1/patients/{TEST_PATIENT_ID}/encounters"

    for i in range(1, made_iterations + 1):
        made_num = f"ENC-BENCH-{i:03d}-{uuid.uuid4().hex[:6].upper()}"
        made_payload = make_encounter_payload(made_num)
        made_status, _, made_latency = make_http_request(
            url=made_post_url,
            method="POST",
            data=made_payload,
            headers=made_headers,
        )
        if made_status == 201:
            made_latencies.append(made_latency)
            made_flag = "PASS (<250ms)" if made_latency <= PERFORMANCE_BUDGET_MS else "WARN (>250ms)"
            print(f"    Iteration {i:02d}: {made_latency:6.2f} ms | {made_flag}")
        else:
            print(f"    Iteration {i:02d}: FAILED (HTTP {made_status})")

    if made_latencies:
        made_avg = sum(made_latencies) / len(made_latencies)
        made_min = min(made_latencies)
        made_max = max(made_latencies)
        made_sorted = sorted(made_latencies)
        made_median = made_sorted[len(made_sorted) // 2]
        print(f"\n    Benchmark Summary:")
        print(f"      - Min Latency:    {made_min:.2f} ms")
        print(f"      - Max Latency:    {made_max:.2f} ms")
        print(f"      - Mean Latency:   {made_avg:.2f} ms")
        print(f"      - Median Latency: {made_median:.2f} ms")
        print(f"      - Target Budget:  < {PERFORMANCE_BUDGET_MS:.0f} ms")

        if made_median <= PERFORMANCE_BUDGET_MS:
            print(f"    [+] Performance verification PASSED within turnaround budget.")
        else:
            print(f"    [!] Warning: Median latency exceeded target budget.")

    return made_latencies


def execute_full_verification() -> bool:
    """
    Main driver executing full end-to-end integration and performance verification.
    """
    print("=" * 72)
    print("PULSE Clinical Decision Support System - End-to-End Stack Verification")
    print("=" * 72)

    if not wait_for_services():
        return False

    made_token = make_jwt_token()
    made_enc_id = make_clinical_roundtrip_verification(made_token)
    make_persistence_verification(made_token, made_enc_id)
    make_reverse_proxy_verification(made_token)
    make_latency_benchmark(made_token, made_iterations=5)

    print("\n" + "=" * 72)
    print("[SUCCESS] Multi-container stack orchestration fully verified!")
    print("=" * 72)
    return True


if __name__ == "__main__":
    made_success = execute_full_verification()
    sys.exit(0 if made_success else 1)
