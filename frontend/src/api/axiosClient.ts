import axios, { AxiosInstance, AxiosError, InternalAxiosRequestConfig } from 'axios';
import { ProblemDetails } from '@/types/api';

export function makeCorrelationId(): string {
  if (typeof crypto !== 'undefined' && crypto.randomUUID) {
    return crypto.randomUUID();
  }
  return 'pulse-' + Math.random().toString(36).substring(2, 11) + '-' + Date.now();
}

export function makeNormalizedError(error: unknown): ProblemDetails {
  if (axios.isAxiosError(error)) {
    const madeAxiosError = error as AxiosError<ProblemDetails>;
    if (madeAxiosError.response?.data) {
      const madeData = madeAxiosError.response.data;
      return {
        title: madeData.title || madeAxiosError.message || 'API Request Failed',
        status: madeAxiosError.response.status,
        detail: madeData.detail || madeAxiosError.message,
        errors: madeData.errors,
        traceId: madeData.traceId,
        instance: madeData.instance
      };
    }
    return {
      title: 'Network Communication Error',
      status: madeAxiosError.response?.status || 500,
      detail: madeAxiosError.message || 'Unable to connect to PULSE Clinical Backend.'
    };
  }
  return {
    title: 'Unexpected Error',
    status: 500,
    detail: error instanceof Error ? error.message : 'An unexpected error occurred.'
  };
}

export function makeAxiosClient(baseURL: string = ''): AxiosInstance {
  const madeInstance = axios.create({
    baseURL,
    timeout: 10000,
    headers: {
      'Content-Type': 'application/json',
      'Accept': 'application/json'
    }
  });

  madeInstance.interceptors.request.use((config: InternalAxiosRequestConfig) => {
    const madeToken = typeof window !== 'undefined' ? localStorage.getItem('pulse_auth_token') : null;
    if (madeToken && config.headers) {
      config.headers.Authorization = `Bearer ${madeToken}`;
    }

    if (config.headers && !config.headers['X-Correlation-ID']) {
      config.headers['X-Correlation-ID'] = makeCorrelationId();
    }

    return config;
  });

  madeInstance.interceptors.response.use(
    (response) => response,
    (error: AxiosError) => {
      const madeProblem = makeNormalizedError(error);
      return Promise.reject(madeProblem);
    }
  );

  return madeInstance;
}

export const axiosClient = makeAxiosClient();
