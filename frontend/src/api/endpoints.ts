export const API_ENDPOINTS = {
  AUTH_TOKEN: '/api/v1/auth/token',
  HEALTH: '/api/health',
  MAKE_ENCOUNTER: (patientId: string) => `/api/v1/patients/${patientId}/encounters`,
  GET_ENCOUNTER_BY_ID: (patientId: string, encounterId: string) => `/api/v1/patients/${patientId}/encounters/${encounterId}`,
  GET_PATIENT_ENCOUNTERS: (patientId: string) => `/api/v1/patients/${patientId}/encounters`
} as const;
