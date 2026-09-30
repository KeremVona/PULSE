import { describe, it, expect } from 'vitest';
import {
  makeClinicalIndicatorsSchema,
  makeEncounterFormSchema,
  makePhysiologicalWarnings
} from '@/validation/encounterSchema';
import {
  EncounterType,
  makeDefaultIndicators
} from '@/types/clinical';

describe('encounterSchema Physiological Boundary Validation', () => {
  it('should accept valid default clinical indicators', () => {
    const madeSchema = makeClinicalIndicatorsSchema();
    const madeIndicators = makeDefaultIndicators();

    const madeResult = madeSchema.safeParse(madeIndicators);
    expect(madeResult.success).toBe(true);
  });

  describe('Blood Pressure Boundaries', () => {
    it('should reject systolic BP below 60 mmHg and above 260 mmHg', () => {
      const madeSchema = makeClinicalIndicatorsSchema();
      const madeBase = makeDefaultIndicators();

      const madeLowResult = madeSchema.safeParse({ ...madeBase, restingBp: 59 });
      expect(madeLowResult.success).toBe(false);
      if (!madeLowResult.success) {
        expect(madeLowResult.error.issues[0].message).toContain('at least 60 mmHg');
      }

      const madeHighResult = madeSchema.safeParse({ ...madeBase, restingBp: 261 });
      expect(madeHighResult.success).toBe(false);
      if (!madeHighResult.success) {
        expect(madeHighResult.error.issues[0].message).toContain('cannot exceed 260 mmHg');
      }
    });

    it('should reject diastolic BP below 40 mmHg and above 140 mmHg', () => {
      const madeSchema = makeClinicalIndicatorsSchema();
      const madeBase = makeDefaultIndicators();

      const madeLowResult = madeSchema.safeParse({ ...madeBase, diastolicBp: 35 });
      expect(madeLowResult.success).toBe(false);

      const madeHighResult = madeSchema.safeParse({ ...madeBase, diastolicBp: 145 });
      expect(madeHighResult.success).toBe(false);
    });
  });

  describe('Cholesterol & Age Boundaries', () => {
    it('should reject serum cholesterol below 80 mg/dL and above 600 mg/dL', () => {
      const madeSchema = makeClinicalIndicatorsSchema();
      const madeBase = makeDefaultIndicators();

      expect(madeSchema.safeParse({ ...madeBase, serumCholesterol: 75 }).success).toBe(false);
      expect(madeSchema.safeParse({ ...madeBase, serumCholesterol: 605 }).success).toBe(false);
    });

    it('should reject pediatric ages below 18 and supercentenarians above 110', () => {
      const madeSchema = makeClinicalIndicatorsSchema();
      const madeBase = makeDefaultIndicators();

      expect(madeSchema.safeParse({ ...madeBase, age: 17 }).success).toBe(false);
      expect(madeSchema.safeParse({ ...madeBase, age: 111 }).success).toBe(false);
    });
  });

  describe('ST Depression & Exercise Vitals Boundaries', () => {
    it('should reject negative ST depression and values above 6.5 mm', () => {
      const madeSchema = makeClinicalIndicatorsSchema();
      const madeBase = makeDefaultIndicators();

      expect(madeSchema.safeParse({ ...madeBase, oldpeakStDepression: -0.1 }).success).toBe(false);
      expect(madeSchema.safeParse({ ...madeBase, oldpeakStDepression: 6.6 }).success).toBe(false);
      expect(madeSchema.safeParse({ ...madeBase, oldpeakStDepression: 2.5 }).success).toBe(true);
    });

    it('should reject heart rate below 40 bpm and above 220 bpm', () => {
      const madeSchema = makeClinicalIndicatorsSchema();
      const madeBase = makeDefaultIndicators();

      expect(madeSchema.safeParse({ ...madeBase, maxHeartRate: 38 }).success).toBe(false);
      expect(madeSchema.safeParse({ ...madeBase, maxHeartRate: 225 }).success).toBe(false);
    });
  });

  describe('Categorical & Diagnostic Indicators', () => {
    it('should enforce thalassemia defect codes 3, 6, or 7', () => {
      const madeSchema = makeClinicalIndicatorsSchema();
      const madeBase = makeDefaultIndicators();

      expect(madeSchema.safeParse({ ...madeBase, thalassemia: 3 }).success).toBe(true);
      expect(madeSchema.safeParse({ ...madeBase, thalassemia: 6 }).success).toBe(true);
      expect(madeSchema.safeParse({ ...madeBase, thalassemia: 7 }).success).toBe(true);
      expect(madeSchema.safeParse({ ...madeBase, thalassemia: 2 }).success).toBe(false);
      expect(madeSchema.safeParse({ ...madeBase, thalassemia: 4 }).success).toBe(false);
    });

    it('should reject fluoroscopy major vessels outside 0-3 range', () => {
      const madeSchema = makeClinicalIndicatorsSchema();
      const madeBase = makeDefaultIndicators();

      expect(madeSchema.safeParse({ ...madeBase, fluoroscopyVessels: 4 }).success).toBe(false);
      expect(madeSchema.safeParse({ ...madeBase, fluoroscopyVessels: -1 }).success).toBe(false);
      expect(madeSchema.safeParse({ ...madeBase, fluoroscopyVessels: 3 }).success).toBe(true);
    });
  });

  describe('Full Encounter Form Schema', () => {
    it('should reject empty or short encounter numbers', () => {
      const madeFormSchema = makeEncounterFormSchema();

      const madeInvalid = madeFormSchema.safeParse({
        encounterNumber: 'EN',
        encounterType: EncounterType.OutpatientScreening,
        chiefComplaint: 'Routine check',
        indicators: makeDefaultIndicators()
      });

      expect(madeInvalid.success).toBe(false);
    });

    it('should validate full encounter form with complete payload', () => {
      const madeFormSchema = makeEncounterFormSchema();

      const madeValid = madeFormSchema.safeParse({
        encounterNumber: 'ENC-2026-001',
        encounterType: EncounterType.CardiologyFollowUp,
        chiefComplaint: 'Exertional dyspnea and jaw pain on climbing stairs',
        indicators: makeDefaultIndicators()
      });

      expect(madeValid.success).toBe(true);
    });
  });

  describe('Real-time Physiological Warning Rules', () => {
    it('should detect hypertensive crisis when systolic BP >= 180 mmHg', () => {
      const madeWarnings = makePhysiologicalWarnings({ restingBp: 185 });
      const foundCrisis = madeWarnings.find((w) => w.title === 'Hypertensive Crisis Alert');

      expect(foundCrisis).toBeDefined();
      expect(foundCrisis?.severity).toBe('critical');
    });

    it('should detect Stage 2 hypertension when systolic BP between 140 and 179 mmHg', () => {
      const madeWarnings = makePhysiologicalWarnings({ restingBp: 155 });
      const foundHtn = madeWarnings.find((w) => w.title === 'Stage 2 Hypertension');

      expect(foundHtn).toBeDefined();
      expect(foundHtn?.severity).toBe('warning');
    });

    it('should detect severe exertional ischemia when ST depression >= 2.5 mm', () => {
      const madeWarnings = makePhysiologicalWarnings({ oldpeakStDepression: 2.8 });
      const foundIschemia = madeWarnings.find((w) => w.title === 'Severe Exertional Ischemia');

      expect(foundIschemia).toBeDefined();
      expect(foundIschemia?.severity).toBe('critical');
    });

    it('should detect multi-vessel disease when fluoroscopy vessels >= 2', () => {
      const madeWarnings = makePhysiologicalWarnings({ fluoroscopyVessels: 2 });
      const foundVessels = madeWarnings.find((w) => w.title === 'Multi-Vessel Occlusion Indicator');

      expect(foundVessels).toBeDefined();
      expect(foundVessels?.severity).toBe('critical');
    });

    it('should detect severe hypercholesterolemia when serum cholesterol >= 300 mg/dL', () => {
      const madeWarnings = makePhysiologicalWarnings({ serumCholesterol: 320 });
      const foundChol = madeWarnings.find((w) => w.title === 'Severe Hypercholesterolemia');

      expect(foundChol).toBeDefined();
      expect(foundChol?.severity).toBe('critical');
    });
  });
});
