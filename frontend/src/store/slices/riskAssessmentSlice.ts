import { createSlice, PayloadAction } from '@reduxjs/toolkit';
import { RiskAssessmentPayload, MlFeatureAttribution } from '@/types/clinical';
import { ProblemDetails } from '@/types/api';

export interface RiskAssessmentState {
  activeAssessment: RiskAssessmentPayload | null;
  history: RiskAssessmentPayload[];
  isEvaluating: boolean;
  evaluationError: ProblemDetails | null;
  selectedAttribution: MlFeatureAttribution | null;
}

export function makeInitialRiskAssessmentState(): RiskAssessmentState {
  return {
    activeAssessment: null,
    history: [],
    isEvaluating: false,
    evaluationError: null,
    selectedAttribution: null
  };
}

export const riskAssessmentSlice = createSlice({
  name: 'riskAssessment',
  initialState: makeInitialRiskAssessmentState(),
  reducers: {
    setActiveAssessment: (state, action: PayloadAction<RiskAssessmentPayload | null>) => {
      state.activeAssessment = action.payload;
      if (action.payload) {
        state.history.unshift(action.payload);
      }
      state.evaluationError = null;
    },
    selectAttribution: (state, action: PayloadAction<MlFeatureAttribution | null>) => {
      state.selectedAttribution = action.payload;
    },
    clearActiveAssessment: (state) => {
      state.activeAssessment = null;
      state.selectedAttribution = null;
      state.evaluationError = null;
    }
  }
});

export const { setActiveAssessment, selectAttribution, clearActiveAssessment } = riskAssessmentSlice.actions;
export default riskAssessmentSlice.reducer;
