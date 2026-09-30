import { createSlice, createAsyncThunk, PayloadAction } from '@reduxjs/toolkit';
import {
  ClinicalIndicatorsInput,
  EncounterType,
  EncounterDetail,
  MakeEncounterApiRequest,
  MakeEncounterResult,
  makeDefaultIndicators
} from '@/types/clinical';
import { ProblemDetails } from '@/types/api';
import { axiosClient } from '@/api/axiosClient';
import { API_ENDPOINTS } from '@/api/endpoints';
import { setActiveAssessment } from './riskAssessmentSlice';
import { addNotification } from './notificationSlice';

export interface EncounterState {
  activePatientId: string;
  activePatientName: string;
  activePatientMrn: string;
  encounterNumber: string;
  encounterType: EncounterType;
  chiefComplaint: string;
  indicators: ClinicalIndicatorsInput;
  isDirty: boolean;
  isSubmitting: boolean;
  isLoadingHistory: boolean;
  submissionError: ProblemDetails | null;
  patientEncounters: EncounterDetail[];
  selectedEncounter: EncounterDetail | null;
}

export function makeEncounterNumber(): string {
  const madeRandom = Math.floor(1000 + Math.random() * 9000);
  const madeTimestamp = Date.now().toString().slice(-4);
  return `ENC-${madeRandom}-${madeTimestamp}`;
}

export function makeInitialEncounterState(): EncounterState {
  return {
    activePatientId: 'e5a31779-7dfb-4ba5-8b30-b3fa0c8c0f75',
    activePatientName: 'James Holden',
    activePatientMrn: 'MRN-CARD-100293',
    encounterNumber: makeEncounterNumber(),
    encounterType: EncounterType.OutpatientScreening,
    chiefComplaint: 'Routine cardiovascular evaluation with risk assessment.',
    indicators: makeDefaultIndicators(),
    isDirty: false,
    isSubmitting: false,
    isLoadingHistory: false,
    submissionError: null,
    patientEncounters: [],
    selectedEncounter: null
  };
}

export const makeEncounterAsync = createAsyncThunk<
  MakeEncounterResult,
  { patientId: string; requestData: MakeEncounterApiRequest },
  { rejectValue: ProblemDetails }
>('encounter/makeEncounter', async ({ patientId, requestData }, { dispatch, rejectWithValue }) => {
  try {
    const madeResponse = await axiosClient.post<MakeEncounterResult>(
      API_ENDPOINTS.MAKE_ENCOUNTER(patientId),
      requestData
    );

    // Sync risk assessment payload into riskAssessmentSlice
    if (madeResponse.data.riskAssessment) {
      dispatch(setActiveAssessment(madeResponse.data.riskAssessment));
    }

    // Trigger success notification
    dispatch(
      addNotification({
        type: 'success',
        title: 'Encounter Evaluated Successfully',
        message: `Encounter #${madeResponse.data.encounterNumber} completed with risk score: ${(
          (madeResponse.data.riskAssessment?.riskProbability ?? 0) * 100
        ).toFixed(1)}% (${madeResponse.data.riskAssessment?.riskBand ?? 'Pending'}).`
      })
    );

    return madeResponse.data;
  } catch (error) {
    const madeProblem = error as ProblemDetails;
    dispatch(
      addNotification({
        type: 'error',
        title: madeProblem.title || 'Encounter Evaluation Failed',
        message: madeProblem.detail || 'The CDSS backend encountered an issue.',
        details: madeProblem.errors ? Object.values(madeProblem.errors).flat() : undefined
      })
    );
    return rejectWithValue(madeProblem);
  }
});

export const fetchPatientEncountersAsync = createAsyncThunk<
  EncounterDetail[],
  { patientId: string },
  { rejectValue: ProblemDetails }
>('encounter/fetchPatientEncounters', async ({ patientId }, { dispatch, rejectWithValue }) => {
  try {
    const madeResponse = await axiosClient.get<EncounterDetail[]>(
      API_ENDPOINTS.GET_PATIENT_ENCOUNTERS(patientId)
    );
    return madeResponse.data;
  } catch (error) {
    const madeProblem = error as ProblemDetails;
    dispatch(
      addNotification({
        type: 'warning',
        title: 'Could Not Load Patient Encounters',
        message: madeProblem.detail || 'Historical encounters are temporarily unavailable.'
      })
    );
    return rejectWithValue(madeProblem);
  }
});

export const fetchEncounterByIdAsync = createAsyncThunk<
  EncounterDetail,
  { patientId: string; encounterId: string },
  { rejectValue: ProblemDetails }
>('encounter/fetchEncounterById', async ({ patientId, encounterId }, { dispatch, rejectWithValue }) => {
  try {
    const madeResponse = await axiosClient.get<EncounterDetail>(
      API_ENDPOINTS.GET_ENCOUNTER_BY_ID(patientId, encounterId)
    );

    if (madeResponse.data.riskAssessment) {
      dispatch(setActiveAssessment(madeResponse.data.riskAssessment));
    }

    return madeResponse.data;
  } catch (error) {
    const madeProblem = error as ProblemDetails;
    return rejectWithValue(madeProblem);
  }
});

export const encounterSlice = createSlice({
  name: 'encounter',
  initialState: makeInitialEncounterState(),
  reducers: {
    setActivePatient: (
      state,
      action: PayloadAction<{ id: string; name: string; mrn: string }>
    ) => {
      state.activePatientId = action.payload.id;
      state.activePatientName = action.payload.name;
      state.activePatientMrn = action.payload.mrn;
      state.patientEncounters = [];
      state.selectedEncounter = null;
    },
    updateIndicatorField: <K extends keyof ClinicalIndicatorsInput>(
      state: EncounterState,
      action: PayloadAction<{ field: K; value: ClinicalIndicatorsInput[K] }>
    ) => {
      state.indicators[action.payload.field] = action.payload.value;
      state.isDirty = true;
    },
    updateIndicatorsBatch: (
      state,
      action: PayloadAction<Partial<ClinicalIndicatorsInput>>
    ) => {
      state.indicators = { ...state.indicators, ...action.payload };
      state.isDirty = true;
    },
    updateMetadataField: (
      state,
      action: PayloadAction<{
        encounterNumber?: string;
        encounterType?: EncounterType;
        chiefComplaint?: string;
      }>
    ) => {
      if (action.payload.encounterNumber !== undefined) {
        state.encounterNumber = action.payload.encounterNumber;
      }
      if (action.payload.encounterType !== undefined) {
        state.encounterType = action.payload.encounterType;
      }
      if (action.payload.chiefComplaint !== undefined) {
        state.chiefComplaint = action.payload.chiefComplaint;
      }
      state.isDirty = true;
    },
    prefillFromPastEncounter: (state, action: PayloadAction<EncounterDetail>) => {
      const madePast = action.payload;
      if (madePast.indicators) {
        state.indicators = { ...madePast.indicators };
      }
      state.encounterType = madePast.encounterType;
      state.chiefComplaint = `Follow-up following encounter #${madePast.encounterNumber}: ${madePast.chiefComplaint ?? 'Re-evaluation'}`;
      state.encounterNumber = makeEncounterNumber();
      state.isDirty = true;
      state.selectedEncounter = madePast;
    },
    resetEncounterForm: (state) => {
      state.indicators = makeDefaultIndicators();
      state.encounterNumber = makeEncounterNumber();
      state.encounterType = EncounterType.OutpatientScreening;
      state.chiefComplaint = 'Routine cardiovascular evaluation with risk assessment.';
      state.isDirty = false;
      state.submissionError = null;
      state.selectedEncounter = null;
    },
    clearSelectedEncounter: (state) => {
      state.selectedEncounter = null;
    }
  },
  extraReducers: (builder) => {
    builder
      // makeEncounterAsync
      .addCase(makeEncounterAsync.pending, (state) => {
        state.isSubmitting = true;
        state.submissionError = null;
      })
      .addCase(makeEncounterAsync.fulfilled, (state, action) => {
        state.isSubmitting = false;
        state.isDirty = false;
        state.encounterNumber = makeEncounterNumber();
        // Add to local history list
        const madeSummary: EncounterDetail = {
          encounterId: action.payload.encounterId,
          patientId: action.payload.patientId,
          clinicianId: action.payload.clinicianId,
          encounterNumber: action.payload.encounterNumber,
          encounterType: action.meta.arg.requestData.encounterType,
          chiefComplaint: action.meta.arg.requestData.chiefComplaint,
          encounterDate: action.payload.encounterDate,
          status: action.payload.status,
          indicators: action.meta.arg.requestData.indicators,
          riskAssessment: action.payload.riskAssessment
        };
        state.patientEncounters.unshift(madeSummary);
        state.selectedEncounter = madeSummary;
      })
      .addCase(makeEncounterAsync.rejected, (state, action) => {
        state.isSubmitting = false;
        state.submissionError = action.payload || {
          title: 'Request Failed',
          detail: 'Failed to complete clinical encounter.'
        };
      })
      // fetchPatientEncountersAsync
      .addCase(fetchPatientEncountersAsync.pending, (state) => {
        state.isLoadingHistory = true;
      })
      .addCase(fetchPatientEncountersAsync.fulfilled, (state, action) => {
        state.isLoadingHistory = false;
        state.patientEncounters = action.payload;
      })
      .addCase(fetchPatientEncountersAsync.rejected, (state) => {
        state.isLoadingHistory = false;
      })
      // fetchEncounterByIdAsync
      .addCase(fetchEncounterByIdAsync.fulfilled, (state, action) => {
        state.selectedEncounter = action.payload;
      });
  }
});

export const {
  setActivePatient,
  updateIndicatorField,
  updateIndicatorsBatch,
  updateMetadataField,
  prefillFromPastEncounter,
  resetEncounterForm,
  clearSelectedEncounter
} = encounterSlice.actions;

export default encounterSlice.reducer;
