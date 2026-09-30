import { describe, it, expect } from 'vitest';
import { makeCorrelationId, makeNormalizedError, makeAxiosClient } from '@/api/axiosClient';
import { AxiosError, AxiosResponse, InternalAxiosRequestConfig } from 'axios';
import { ProblemDetails } from '@/types/api';

describe('Axios Client & Resilient Correlation Context', () => {
  it('should generate valid non-empty correlation IDs', () => {
    const madeId1 = makeCorrelationId();
    const madeId2 = makeCorrelationId();

    expect(madeId1).toBeDefined();
    expect(madeId2).toBeDefined();
    expect(madeId1).not.toBe(madeId2);
  });

  it('should normalize RFC 7807 ProblemDetails from Axios error response', () => {
    const madeProblemData: ProblemDetails = {
      title: 'Physiological Boundary Violation',
      status: 400,
      detail: 'Resting BP is outside physiological limits [60, 260].',
      errors: {
        'Indicators.RestingBp': ['Value must be between 60 and 260 mmHg.']
      },
      traceId: 'trace-test-123'
    };

    const madeAxiosError = new AxiosError<ProblemDetails>(
      'Request failed with status code 400',
      'ERR_BAD_REQUEST',
      {} as InternalAxiosRequestConfig,
      {},
      {
        data: madeProblemData,
        status: 400,
        statusText: 'Bad Request',
        headers: {},
        config: {} as InternalAxiosRequestConfig
      } as AxiosResponse<ProblemDetails>
    );

    const madeNormalized = makeNormalizedError(madeAxiosError);

    expect(madeNormalized.status).toBe(400);
    expect(madeNormalized.title).toBe('Physiological Boundary Violation');
    expect(madeNormalized.detail).toBe('Resting BP is outside physiological limits [60, 260].');
    expect(madeNormalized.errors).toBeDefined();
    expect(madeNormalized.errors!['Indicators.RestingBp']).toContain(
      'Value must be between 60 and 260 mmHg.'
    );
  });

  it('should normalize fallback error when no response body exists', () => {
    const madeNetworkError = new AxiosError(
      'Network Error',
      'ERR_NETWORK',
      {} as InternalAxiosRequestConfig
    );

    const madeNormalized = makeNormalizedError(madeNetworkError);

    expect(madeNormalized.title).toBe('Network Communication Error');
    expect(madeNormalized.detail).toBe('Network Error');
  });

  it('should construct configured axios instance with defaults', () => {
    const madeClient = makeAxiosClient('http://localhost:5000');

    expect(madeClient.defaults.baseURL).toBe('http://localhost:5000');
    expect(madeClient.defaults.timeout).toBe(10000);
    expect(madeClient.defaults.headers['Content-Type']).toBe('application/json');
  });
});
