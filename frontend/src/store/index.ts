import { configureStore, combineReducers } from '@reduxjs/toolkit';
import authReducer from './slices/authSlice';
import encounterReducer from './slices/encounterSlice';
import riskAssessmentReducer from './slices/riskAssessmentSlice';
import notificationReducer from './slices/notificationSlice';

export const rootReducer = combineReducers({
  auth: authReducer,
  encounter: encounterReducer,
  riskAssessment: riskAssessmentReducer,
  notification: notificationReducer
});

export type RootState = ReturnType<typeof rootReducer>;

export function makeAppStore(preloadedState?: Partial<RootState>) {
  return configureStore({
    reducer: rootReducer,
    preloadedState,
    middleware: (getDefaultMiddleware) =>
      getDefaultMiddleware({
        serializableCheck: {
          // Allow Date or non-serializable elements in special diagnostic contexts if needed
          ignoredActions: []
        }
      })
  });
}

export const store = makeAppStore();
export type AppStore = ReturnType<typeof makeAppStore>;
export type AppDispatch = AppStore['dispatch'];
