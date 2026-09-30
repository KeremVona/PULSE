export const ClinicalRoles = {
  Physician: 'Physician',
  ClinicalStaff: 'ClinicalStaff',
  Admin: 'Admin',
  Auditor: 'Auditor'
} as const;

export type ClinicalRole = (typeof ClinicalRoles)[keyof typeof ClinicalRoles];

export interface TokenRequestDto {
  userId: string;
  username: string;
  fullName: string;
  roles: ClinicalRole[];
}

export interface AuthTokenResponseDto {
  accessToken: string;
  tokenType: string;
  expiresInSeconds: number;
  issuedAtUtc: string;
  expiresAtUtc: string;
  roles: string[];
  username: string;
  fullName: string;
}

export interface UserProfile {
  id: string;
  username: string;
  fullName: string;
  roles: ClinicalRole[];
}

export interface AuthState {
  token: string | null;
  user: UserProfile | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;
}
