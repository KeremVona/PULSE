import { createSlice, createAsyncThunk, PayloadAction } from '@reduxjs/toolkit';
import { AuthState, TokenRequestDto, AuthTokenResponseDto, UserProfile, ClinicalRoles } from '@/types/auth';
import { axiosClient } from '@/api/axiosClient';
import { API_ENDPOINTS } from '@/api/endpoints';
import { ProblemDetails } from '@/types/api';

export function makeInitialAuthState(): AuthState {
  const madeToken = typeof window !== 'undefined' ? localStorage.getItem('pulse_auth_token') : null;
  const madeUserRaw = typeof window !== 'undefined' ? localStorage.getItem('pulse_user_profile') : null;
  let madeUser: UserProfile | null = null;

  if (madeUserRaw) {
    try {
      madeUser = JSON.parse(madeUserRaw) as UserProfile;
    } catch {
      madeUser = null;
    }
  }

  // Fallback default physician profile for immediate clinical evaluation workflow
  if (!madeUser && !madeToken) {
    madeUser = {
      id: 'd3b07384-d113-40e1-bb69-52e6f4a86b3a',
      username: 'dr.elena.vance',
      fullName: 'Dr. Elena Vance, MD (Cardiology)',
      roles: [ClinicalRoles.Physician]
    };
  }

  return {
    token: madeToken,
    user: madeUser,
    isAuthenticated: Boolean(madeToken),
    isLoading: false,
    error: null
  };
}

export const makeTokenAsync = createAsyncThunk<
  AuthTokenResponseDto,
  TokenRequestDto,
  { rejectValue: ProblemDetails }
>('auth/makeToken', async (requestData, { rejectWithValue }) => {
  try {
    const madeResponse = await axiosClient.post<AuthTokenResponseDto>(
      API_ENDPOINTS.AUTH_TOKEN,
      requestData
    );
    if (typeof window !== 'undefined') {
      localStorage.setItem('pulse_auth_token', madeResponse.data.accessToken);
      localStorage.setItem(
        'pulse_user_profile',
        JSON.stringify({
          id: requestData.userId,
          username: madeResponse.data.username,
          fullName: madeResponse.data.fullName,
          roles: madeResponse.data.roles
        })
      );
    }
    return madeResponse.data;
  } catch (error) {
    return rejectWithValue(error as ProblemDetails);
  }
});

export const authSlice = createSlice({
  name: 'auth',
  initialState: makeInitialAuthState(),
  reducers: {
    setCredentials: (
      state,
      action: PayloadAction<{ user: UserProfile; token: string }>
    ) => {
      state.user = action.payload.user;
      state.token = action.payload.token;
      state.isAuthenticated = true;
      state.error = null;
      if (typeof window !== 'undefined') {
        localStorage.setItem('pulse_auth_token', action.payload.token);
        localStorage.setItem('pulse_user_profile', JSON.stringify(action.payload.user));
      }
    },
    logout: (state) => {
      state.user = null;
      state.token = null;
      state.isAuthenticated = false;
      state.error = null;
      if (typeof window !== 'undefined') {
        localStorage.removeItem('pulse_auth_token');
        localStorage.removeItem('pulse_user_profile');
      }
    }
  },
  extraReducers: (builder) => {
    builder
      .addCase(makeTokenAsync.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(makeTokenAsync.fulfilled, (state, action) => {
        state.isLoading = false;
        state.isAuthenticated = true;
        state.token = action.payload.accessToken;
        state.user = {
          id: action.meta.arg.userId,
          username: action.payload.username,
          fullName: action.payload.fullName,
          roles: action.payload.roles as UserProfile['roles']
        };
      })
      .addCase(makeTokenAsync.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload?.detail || 'Failed to authenticate with PULSE API.';
      });
  }
});

export const { setCredentials, logout } = authSlice.actions;
export default authSlice.reducer;
