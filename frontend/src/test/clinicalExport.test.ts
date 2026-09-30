import { describe, it, expect } from 'vitest';
import { makeClinicalNoteMarkdown } from '@/utils/clinicalExport';
import { makeSampleRiskAssessment } from '@/utils/riskFixtures';
import { makeDefaultIndicators } from '@/types/clinical';

describe('clinicalExport Utility', () => {
  it('should format full clinical SOAP note with patient, indicators, probability, and drivers', () => {
    const madeAssessment = makeSampleRiskAssessment('Elevated');
    const madeIndicators = makeDefaultIndicators();

    const note = makeClinicalNoteMarkdown({
      patientName: 'Alexander Wright',
      mrn: 'MRN-PULSE-2026-0881',
      encounterNumber: 'ENC-2026-004',
      encounterDate: '2026-09-30T10:00:00Z',
      clinicianName: 'Dr. Elena Vance, MD',
      clinicianRole: 'Cardiology Attending',
      chiefComplaint: 'Substernal chest tightness radiating to left arm',
      indicators: madeIndicators,
      assessment: madeAssessment
    });

    expect(note).toContain('PULSE CARDIOVASCULAR CDSS — CLINICAL ENCOUNTER SUMMARY NOTE');
    expect(note).toContain('PATIENT NAME:      Alexander Wright');
    expect(note).toContain('MRN:               MRN-PULSE-2026-0881');
    expect(note).toContain('ENCOUNTER NUMBER:  ENC-2026-004');
    expect(note).toContain('Calibrated CAD Probability:  74.2%');
    expect(note).toContain('Risk Stratification Tier:    Elevated Risk');
    expect(note).toContain('95% Bootstrap CI:            [64.8% – 83.6%]');
    expect(note).toContain('Decision Threshold:          50%');
    expect(note).toContain('ST Depression (Oldpeak)');
    expect(note).toContain('Fluoroscopy Major Vessels');
    expect(note).toContain('Recommend urgent non-invasive stress imaging (echocardiogram/SPECT)');
    expect(note).toContain('Attesting Clinician: Dr. Elena Vance, MD');
  });
});
