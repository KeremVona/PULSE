import { z } from 'zod';
import { ClinicalIndicatorsInput, EncounterType } from '@/types/clinical';

export interface PhysiologicalWarning {
  field: keyof ClinicalIndicatorsInput | 'general';
  severity: 'warning' | 'critical';
  title: string;
  message: string;
}

export function makeClinicalIndicatorsSchema() {
  return z.object({
    age: z
      .number()
      .int('Age must be an integer')
      .min(18, 'Patient age must be at least 18 years for adult cardiovascular risk model')
      .max(110, 'Patient age cannot exceed 110 years'),

    sex: z
      .number()
      .int()
      .min(0, 'Biological sex must be 0 (Female) or 1 (Male)')
      .max(1, 'Biological sex must be 0 (Female) or 1 (Male)'),

    chestPainType: z
      .number()
      .int()
      .min(1, 'Select a valid chest pain classification')
      .max(4, 'Select a valid chest pain classification'),

    restingBp: z
      .number()
      .int('Systolic BP must be an integer')
      .min(60, 'Resting systolic blood pressure must be at least 60 mmHg')
      .max(260, 'Resting systolic blood pressure cannot exceed 260 mmHg'),

    diastolicBp: z
      .number()
      .int('Diastolic BP must be an integer')
      .min(40, 'Resting diastolic blood pressure must be at least 40 mmHg')
      .max(140, 'Resting diastolic blood pressure cannot exceed 140 mmHg'),

    serumCholesterol: z
      .number()
      .int('Serum cholesterol must be an integer')
      .min(80, 'Serum cholesterol must be at least 80 mg/dL')
      .max(600, 'Serum cholesterol cannot exceed 600 mg/dL'),

    fastingBloodSugar: z
      .number()
      .int()
      .min(0, 'Fasting blood sugar flag must be 0 (<= 120 mg/dL) or 1 (> 120 mg/dL)')
      .max(1, 'Fasting blood sugar flag must be 0 (<= 120 mg/dL) or 1 (> 120 mg/dL)'),

    restingEcg: z
      .number()
      .int()
      .min(0, 'Select a valid resting ECG finding')
      .max(2, 'Select a valid resting ECG finding'),

    maxHeartRate: z
      .number()
      .int('Max heart rate must be an integer')
      .min(40, 'Max achieved heart rate must be at least 40 bpm')
      .max(220, 'Max achieved heart rate cannot exceed 220 bpm'),

    exerciseAngina: z
      .number()
      .int()
      .min(0, 'Exercise-induced angina must be 0 (Absent) or 1 (Present)')
      .max(1, 'Exercise-induced angina must be 0 (Absent) or 1 (Present)'),

    oldpeakStDepression: z
      .number()
      .min(0.0, 'ST depression cannot be negative')
      .max(6.5, 'ST depression cannot exceed 6.5 mm'),

    slope: z
      .number()
      .int()
      .min(1, 'Select a valid ST segment slope')
      .max(3, 'Select a valid ST segment slope'),

    fluoroscopyVessels: z
      .number()
      .int()
      .min(0, 'Major vessels colored by fluoroscopy must be between 0 and 3')
      .max(3, 'Major vessels colored by fluoroscopy must be between 0 and 3'),

    thalassemia: z
      .number()
      .int()
      .refine((val) => [3, 6, 7].includes(val), {
        message: 'Thalassemia defect code must be 3 (Normal), 6 (Fixed), or 7 (Reversible)'
      })
  });
}

export function makeEncounterFormSchema() {
  return z.object({
    encounterNumber: z
      .string()
      .min(3, 'Encounter number must be at least 3 characters')
      .max(64, 'Encounter number cannot exceed 64 characters'),

    encounterType: z.nativeEnum(EncounterType),

    chiefComplaint: z
      .string()
      .max(1000, 'Chief complaint cannot exceed 1000 characters')
      .nullable()
      .optional(),

    indicators: makeClinicalIndicatorsSchema()
  });
}

export type EncounterFormData = z.infer<ReturnType<typeof makeEncounterFormSchema>>;

export function makePhysiologicalWarnings(
  indicators: Partial<ClinicalIndicatorsInput>
): PhysiologicalWarning[] {
  const madeWarnings: PhysiologicalWarning[] = [];

  // Blood pressure warnings
  if (indicators.restingBp && indicators.restingBp >= 180) {
    madeWarnings.push({
      field: 'restingBp',
      severity: 'critical',
      title: 'Hypertensive Crisis Alert',
      message: `Systolic BP of ${indicators.restingBp} mmHg exceeds emergency threshold (>= 180 mmHg). Consider immediate triage.`
    });
  } else if (indicators.restingBp && indicators.restingBp >= 140) {
    madeWarnings.push({
      field: 'restingBp',
      severity: 'warning',
      title: 'Stage 2 Hypertension',
      message: `Systolic BP of ${indicators.restingBp} mmHg indicates Stage 2 hypertension according to AHA guidelines.`
    });
  }

  if (indicators.diastolicBp && indicators.diastolicBp >= 120) {
    madeWarnings.push({
      field: 'diastolicBp',
      severity: 'critical',
      title: 'Severe Diastolic Hypertension',
      message: `Diastolic BP of ${indicators.diastolicBp} mmHg presents acute cardiovascular strain.`
    });
  }

  // ST Depression (oldpeak) ischemia warnings
  if (indicators.oldpeakStDepression !== undefined && indicators.oldpeakStDepression >= 2.5) {
    madeWarnings.push({
      field: 'oldpeakStDepression',
      severity: 'critical',
      title: 'Severe Exertional Ischemia',
      message: `ST segment depression of ${indicators.oldpeakStDepression.toFixed(1)} mm indicates high probability of critical myocardial ischemia.`
    });
  } else if (indicators.oldpeakStDepression !== undefined && indicators.oldpeakStDepression >= 1.5) {
    madeWarnings.push({
      field: 'oldpeakStDepression',
      severity: 'warning',
      title: 'Substantial ST Depression',
      message: `ST depression of ${indicators.oldpeakStDepression.toFixed(1)} mm reflects clinically meaningful myocardial stress response.`
    });
  }

  // Cholesterol risk warnings
  if (indicators.serumCholesterol && indicators.serumCholesterol >= 300) {
    madeWarnings.push({
      field: 'serumCholesterol',
      severity: 'critical',
      title: 'Severe Hypercholesterolemia',
      message: `Total cholesterol of ${indicators.serumCholesterol} mg/dL substantially elevates atherogenic plaque risk.`
    });
  } else if (indicators.serumCholesterol && indicators.serumCholesterol >= 240) {
    madeWarnings.push({
      field: 'serumCholesterol',
      severity: 'warning',
      title: 'High Serum Cholesterol',
      message: `Total cholesterol of ${indicators.serumCholesterol} mg/dL is categorized as high risk (> 240 mg/dL).`
    });
  }

  // Multi-vessel occlusion warning
  if (indicators.fluoroscopyVessels && indicators.fluoroscopyVessels >= 2) {
    madeWarnings.push({
      field: 'fluoroscopyVessels',
      severity: 'critical',
      title: 'Multi-Vessel Occlusion Indicator',
      message: `${indicators.fluoroscopyVessels} major vessels colored by fluoroscopy suggests multi-vessel coronary artery disease.`
    });
  }

  // Exercise angina + High ST slope
  if (indicators.exerciseAngina === 1 && indicators.slope === 3) {
    madeWarnings.push({
      field: 'exerciseAngina',
      severity: 'warning',
      title: 'Downsloping ST Segment with Angina',
      message: 'Exercise-induced angina accompanied by downsloping ST segment significantly increases CAD probability.'
    });
  }

  return madeWarnings;
}
