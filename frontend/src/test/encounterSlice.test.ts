import { describe, it, expect } from 'vitest';
import { makeAppStore } from '@/store';
import {
  updateIndicatorField,
  updateMetadataField,
  prefillFromPastEncounter,
  resetEncounterForm,
  makeInitialEncounterState
} from '@/store/slices/encounterSlice';
import {
  EncounterType,
  EncounterStatus,
  ChestPainType,
  RestingEcgType,
  SlopeType,
  ThalType,
  EncounterDetail
} from '@/types/clinical';

describe('encounterSlice State Management', () => {
  it('should initialize with default physiological indicators and dirty false', () => {
    const madeInitialState = makeInitialEncounterState();

    expect(madeInitialState.isDirty).toBe(false);
    expect(madeInitialState.isSubmitting).toBe(false);
    expect(madeInitialState.indicators.age).toBe(55);
    expect(madeInitialState.indicators.restingBp).toBe(130);
    expect(madeInitialState.encounterType).toBe(EncounterType.OutpatientScreening);
  });

  it('should update indicator field and mark dirty true', () => {
    const madeStore = makeAppStore();

    madeStore.dispatch(updateIndicatorField({ field: 'restingBp', value: 165 }));
    const madeState = madeStore.getState().encounter;

    expect(madeState.indicators.restingBp).toBe(165);
    expect(madeState.isDirty).toBe(true);
  });

  it('should update metadata fields (encounter number, chief complaint)', () => {
    const madeStore = makeAppStore();

    madeStore.dispatch(
      updateMetadataField({
        encounterNumber: 'ENC-TEST-999',
        chiefComplaint: 'Severe exertional angina radiating to jaw'
      })
    );
    const madeState = madeStore.getState().encounter;

    expect(madeState.encounterNumber).toBe('ENC-TEST-999');
    expect(madeState.chiefComplaint).toBe('Severe exertional angina radiating to jaw');
    expect(madeState.isDirty).toBe(true);
  });

  it('should prefill encounter form from a past encounter record', () => {
    const madeStore = makeAppStore();

    const madePastEncounter: EncounterDetail = {
      encounterId: 'enc-historical-01',
      patientId: 'patient-test-01',
      clinicianId: 'clinician-test-01',
      encounterNumber: 'ENC-HIST-01',
      encounterType: EncounterType.EmergencyTriage,
      chiefComplaint: 'Acute chest pain onset 2 hours ago',
      encounterDate: '2026-09-28T10:00:00Z',
      status: EncounterStatus.AssessmentComplete,
      indicators: {
        age: 62,
        sex: 1,
        chestPainType: ChestPainType.TypicalAngina,
        restingBp: 175,
        diastolicBp: 105,
        serumCholesterol: 285,
        fastingBloodSugar: 1,
        restingEcg: RestingEcgType.LeftVentricularHypertrophy,
        maxHeartRate: 135,
        exerciseAngina: 1,
        oldpeakStDepression: 3.2,
        slope: SlopeType.Downsloping,
        fluoroscopyVessels: 2,
        thalassemia: ThalType.ReversibleDefect
      },
      riskAssessment: null
    };

    madeStore.dispatch(prefillFromPastEncounter(madePastEncounter));
    const madeState = madeStore.getState().encounter;

    expect(madeState.indicators.age).toBe(62);
    expect(madeState.indicators.restingBp).toBe(175);
    expect(madeState.indicators.oldpeakStDepression).toBe(3.2);
    expect(madeState.encounterType).toBe(EncounterType.EmergencyTriage);
    expect(madeState.chiefComplaint).toContain('Follow-up following encounter #ENC-HIST-01');
    expect(madeState.isDirty).toBe(true);
    expect(madeState.selectedEncounter).toEqual(madePastEncounter);
  });

  it('should reset form to defaults and mark dirty false', () => {
    const madeStore = makeAppStore();

    madeStore.dispatch(updateIndicatorField({ field: 'serumCholesterol', value: 390 }));
    expect(madeStore.getState().encounter.isDirty).toBe(true);

    madeStore.dispatch(resetEncounterForm());
    const madeState = madeStore.getState().encounter;

    expect(madeState.indicators.serumCholesterol).toBe(210);
    expect(madeState.isDirty).toBe(false);
    expect(madeState.submissionError).toBeNull();
  });
});
