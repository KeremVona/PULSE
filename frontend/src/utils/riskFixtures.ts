import {
  RiskAssessmentPayload,
  MlFeatureAttribution,
  MlConfidenceInterval
} from '@/types/clinical';

export function makeSampleFeatureAttributions(): MlFeatureAttribution[] {
  return [
    {
      featureName: 'oldpeak',
      displayName: 'ST Depression (Oldpeak)',
      clinicalValue: '2.8 mm',
      attributionValue: 0.412,
      direction: 'RISK_INCREASING',
      rank: 1
    },
    {
      featureName: 'ca',
      displayName: 'Fluoroscopy Major Vessels',
      clinicalValue: '2 vessels',
      attributionValue: 0.354,
      direction: 'RISK_INCREASING',
      rank: 2
    },
    {
      featureName: 'thal',
      displayName: 'Thalassemia Defect',
      clinicalValue: 'Reversible Defect',
      attributionValue: 0.287,
      direction: 'RISK_INCREASING',
      rank: 3
    },
    {
      featureName: 'cp',
      displayName: 'Chest Pain Classification',
      clinicalValue: 'Asymptomatic (Type 4)',
      attributionValue: 0.245,
      direction: 'RISK_INCREASING',
      rank: 4
    },
    {
      featureName: 'trestbps',
      displayName: 'Resting Blood Pressure',
      clinicalValue: '172 mmHg',
      attributionValue: 0.198,
      direction: 'RISK_INCREASING',
      rank: 5
    },
    {
      featureName: 'exang',
      displayName: 'Exercise-Induced Angina',
      clinicalValue: 'Present',
      attributionValue: 0.176,
      direction: 'RISK_INCREASING',
      rank: 6
    },
    {
      featureName: 'chol',
      displayName: 'Serum Cholesterol',
      clinicalValue: '310 mg/dL',
      attributionValue: 0.143,
      direction: 'RISK_INCREASING',
      rank: 7
    },
    {
      featureName: 'thalach',
      displayName: 'Max Achieved Heart Rate',
      clinicalValue: '115 bpm',
      attributionValue: -0.165,
      direction: 'RISK_DECREASING',
      rank: 8
    },
    {
      featureName: 'age',
      displayName: 'Patient Age',
      clinicalValue: '63 years',
      attributionValue: -0.092,
      direction: 'RISK_DECREASING',
      rank: 9
    },
    {
      featureName: 'restecg',
      displayName: 'Resting ECG Findings',
      clinicalValue: 'Normal (Type 0)',
      attributionValue: -0.045,
      direction: 'RISK_DECREASING',
      rank: 10
    },
    {
      featureName: 'fbs',
      displayName: 'Fasting Blood Sugar',
      clinicalValue: '> 120 mg/dL',
      attributionValue: 0.038,
      direction: 'RISK_INCREASING',
      rank: 11
    },
    {
      featureName: 'slope',
      displayName: 'Peak ST Slope',
      clinicalValue: 'Downsloping (Type 3)',
      attributionValue: 0.031,
      direction: 'RISK_INCREASING',
      rank: 12
    },
    {
      featureName: 'sex',
      displayName: 'Biological Sex',
      clinicalValue: 'Male (1)',
      attributionValue: 0.022,
      direction: 'RISK_INCREASING',
      rank: 13
    }
  ];
}

export function makeSampleRiskAssessment(
  tier: 'Low' | 'Moderate' | 'Elevated' | 'Critical' = 'Elevated'
): RiskAssessmentPayload {
  const madeAttributions = makeSampleFeatureAttributions();

  let madeProbability = 0.742;
  let madeCi: MlConfidenceInterval = {
    lowerBound: 0.648,
    upperBound: 0.836,
    confidenceLevel: 0.95,
    standardError: 0.048
  };
  let madeIsCadPositive = true;

  if (tier === 'Low') {
    madeProbability = 0.125;
    madeCi = {
      lowerBound: 0.082,
      upperBound: 0.168,
      confidenceLevel: 0.95,
      standardError: 0.022
    };
    madeIsCadPositive = false;
  } else if (tier === 'Moderate') {
    madeProbability = 0.384;
    madeCi = {
      lowerBound: 0.312,
      upperBound: 0.456,
      confidenceLevel: 0.95,
      standardError: 0.036
    };
    madeIsCadPositive = false;
  } else if (tier === 'Critical') {
    madeProbability = 0.885;
    madeCi = {
      lowerBound: 0.812,
      upperBound: 0.958,
      confidenceLevel: 0.95,
      standardError: 0.037
    };
    madeIsCadPositive = true;
  }

  const madeRiskDrivers = madeAttributions.filter((a) => a.direction === 'RISK_INCREASING');
  const madeProtective = madeAttributions.filter((a) => a.direction === 'RISK_DECREASING');

  return {
    encounterId: 'enc-sample-881',
    correlationId: 'corr-cdss-9921',
    riskProbability: madeProbability,
    riskBand: tier,
    isCadPositive: madeIsCadPositive,
    decisionThreshold: 0.5,
    confidenceInterval: madeCi,
    topRiskDrivers: madeRiskDrivers.slice(0, 5),
    topProtectiveFactors: madeProtective.slice(0, 3),
    allAttributions: madeAttributions,
    explanations: madeAttributions,
    clinicalWarnings:
      tier === 'Critical' || tier === 'Elevated'
        ? [
            'Severe Exertional Ischemia: ST segment depression of 2.8 mm indicates substantial myocardial stress.',
            'Hypertensive Finding: Systolic BP of 172 mmHg reflects Stage 2 hypertension.'
          ]
        : [],
    recommendedAction:
      tier === 'Critical'
        ? 'Immediate cardiology consult and urgent diagnostic coronary angiography recommended.'
        : tier === 'Elevated'
        ? 'Recommend urgent non-invasive stress imaging (echocardiogram/SPECT) and aggressive medical optimization.'
        : tier === 'Moderate'
        ? 'Outpatient cardiology evaluation with lifestyle modification and secondary prevention pharmacotherapy.'
        : 'Reassure patient; continue routine preventive cardiovascular maintenance.',
    clinicalDisclaimer:
      'PULSE CDSS is an assistive software tool intended solely to support licensed physicians. Output does not replace individualized clinical judgment.',
    inferenceLatencyMs: 13.8
  };
}
