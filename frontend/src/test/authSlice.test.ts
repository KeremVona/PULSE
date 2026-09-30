import { describe, it, expect, beforeEach } from 'vitest';
import { makeAppStore } from '@/store';
import { setCredentials, logout, makeInitialAuthState } from '@/store/slices/authSlice';
import { ClinicalRoles, UserProfile } from '@/types/auth';

describe('authSlice State Management', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it('should initialize with default physician profile in dev mode', () => {
    const madeInitialState = makeInitialAuthState();

    expect(madeInitialState.user).not.toBeNull();
    expect(madeInitialState.user?.roles).toContain(ClinicalRoles.Physician);
    expect(madeInitialState.isLoading).toBe(false);
  });

  it('should set credentials and persist token in localStorage', () => {
    const madeStore = makeAppStore();

    const madeUser: UserProfile = {
      id: 'doc-1234',
      username: 'dr.marcus',
      fullName: 'Dr. Marcus Cole, MD',
      roles: [ClinicalRoles.Physician]
    };
    const madeToken = 'mock-jwt-bearer-token-xyz';

    madeStore.dispatch(setCredentials({ user: madeUser, token: madeToken }));
    const madeState = madeStore.getState().auth;

    expect(madeState.isAuthenticated).toBe(true);
    expect(madeState.token).toBe(madeToken);
    expect(madeState.user?.username).toBe('dr.marcus');
    expect(localStorage.getItem('pulse_auth_token')).toBe(madeToken);
  });

  it('should clear credentials on logout and purge localStorage', () => {
    const madeStore = makeAppStore();

    const madeUser: UserProfile = {
      id: 'doc-1234',
      username: 'dr.marcus',
      fullName: 'Dr. Marcus Cole, MD',
      roles: [ClinicalRoles.Physician]
    };
    madeStore.dispatch(setCredentials({ user: madeUser, token: 'token-abc' }));
    expect(madeStore.getState().auth.isAuthenticated).toBe(true);

    madeStore.dispatch(logout());
    const madeState = madeStore.getState().auth;

    expect(madeState.isAuthenticated).toBe(false);
    expect(madeState.token).toBeNull();
    expect(madeState.user).toBeNull();
    expect(localStorage.getItem('pulse_auth_token')).toBeNull();
  });
});
