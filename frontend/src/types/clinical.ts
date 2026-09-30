export enum EncounterType {
  OutpatientScreening = 1,
  EmergencyTriage = 2,
  InpatientConsult = 3,
  CardiologyFollowUp = 4
}

export enum EncounterStatus {
  InProgress = 1,
  AssessmentComplete = 2,
  Signed = 3,
  Cancelled = 4
}

export enum ChestPainType {
  TypicalAngina = 1,
  AtypicalAngina = 2,
  NonAnginal = 3,
  Asymptomatic = 4
}

export enum RestingEcgType {
  Normal = 0,
  SttAbnormality = 1,
  LeftVentricularHypertrophy = 2
}

export enum SlopeType {
  Upsloping = 1,
  Flat = 2,
  Downsloping = 3
}

export enum ThalType {
  Normal = 3,
  FixedDefect = 6,
  ReversibleDefect = 7
}

export interface ClinicalIndicatorsInput {
  age: number;
  sex: number; // 0: Female, 1: Male
  chestPainType: number;
  restingBp: number;
  diastolicBp: number;
  serumCholesterol: number;
  fastingBloodSugar: number;
  restingEcg: number;
  maxHeartRate: number;
  exerciseAngina: number;
  oldpeakStDepression: number;
  slope: number;
  fluoroscopyVessels: number;
  thalassemia: number;
}

export interface MlConfidenceInterval {
  lowerBound: number;
  upperBound: number;
  confidenceLevel: number;
  standardError: number;
}

export interface MlFeatureAttribution {
  featureName: string;
  displayName: string;
  clinicalValue: string;
  attributionValue: number;
  direction: 'RISK_INCREASING' | 'RISK_DECREASING';
  rank: number;
}

export interface RiskAssessmentPayload {
  encounterId: string;
  correlationId: string;
  riskProbability: number;
  riskBand: 'Low' | 'Moderate' | 'Elevated' | 'Critical';
  isCadPositive: boolean;
  decisionThreshold: number;
  confidenceInterval: MlConfidenceInterval;
  topRiskDrivers: MlFeatureAttribution[];
  topProtectiveFactors: MlFeatureAttribution[];
  allAttributions: MlFeatureAttribution[];
  explanations: MlFeatureAttribution[];
  clinicalWarnings: string[];
  recommendedAction: string;
  clinicalDisclaimer: string;
  inferenceLatencyMs: number;
}

export interface MakeEncounterApiRequest {
  encounterNumber: string;
  encounterType: EncounterType;
  chiefComplaint: string | null;
  indicators: ClinicalIndicatorsInput;
}

export interface MakeEncounterResult {
  encounterId: string;
  patientId: string;
  clinicianId: string;
  encounterNumber: string;
  encounterDate: string;
  status: EncounterStatus;
  riskAssessment: RiskAssessmentPayload | null;
}

export interface EncounterDetail {
  encounterId: string;
  patientId: string;
  clinicianId: string;
  encounterNumber: string;
  encounterType: EncounterType;
  chiefComplaint: string | null;
  encounterDate: string;
  status: EncounterStatus;
  indicators: ClinicalIndicatorsInput | null;
  riskAssessment: RiskAssessmentPayload | null;
}

export function makeDefaultIndicators(): ClinicalIndicatorsInput {
  return {
    age: 55,
    sex: 1,
    chestPainType: ChestPainType.AtypicalAngina,
    restingBp: 130,
    diastolicBp: 80,
    serumCholesterol: 210,
    fastingBloodSugar: 0,
    restingEcg: RestingEcgType.Normal,
    maxHeartRate: 150,
    exerciseAngina: 0,
    oldpeakStDepression: 1.0,
    slope: SlopeType.Flat,
    fluoroscopyVessels: 0,
    thalassemia: ThalType.Normal
  };
}
