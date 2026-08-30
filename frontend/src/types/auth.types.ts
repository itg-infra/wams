import type { User } from "./me.type";

export interface ApiError {
    code: string;
}

export interface ApiResponse<T = null> {
    success: boolean;
    message: string;
    requestId?: string;
    error?: ApiError;
    data: T;
}

// ─── Login Role ───────────────────────────────────────────────────────────────
export type LoginRole = "super_admin" | "employee";

// ─── Request Payloads ─────────────────────────────────────────────────────────
export interface SuperAdminLoginPayload {
    email: string;
    password: string;
    companyId: number;
}

export interface EmployeeLoginPayload {
    email: string;
    password: string;
}

// ─── Token Data (login & refresh response sama strukturnya) ───────────────────
export interface TokenData {
    accessToken: string;
    refreshToken: string;
    expiresIn: number;
    tokenType: string;
}

// ─── Responses ────────────────────────────────────────────────────────────────
export type LoginResponse = ApiResponse<TokenData>;
export type RefreshResponse = ApiResponse<TokenData>;
export type LogoutResponse = ApiResponse<null>;

// ─── Auth Tokens (disimpan di store) ─────────────────────────────────────────
export type AuthTokens = TokenData;

export interface AuthUserReponse {
    success: boolean;
    data: AuthUser;
}

// ─── Auth User (di-decode dari JWT) ──────────────────────────────────────────
export interface AuthUser {
    sub: string;
    email: string;
    fullname: string;
    permissions: string[];
    roles: string[];  
    company_id: string;
}

// ─── Auth Store State ─────────────────────────────────────────────────────────
export interface AuthState {
    user: User | null;

    tokens: AuthTokens | null;
    isAuthenticated: boolean;
    isLoading: boolean;
    isLogoutLoading: boolean;
    error: string | null;
    errorCode: string | null;

    hasPermission: (module: string, resource: string, action: string) => boolean;
    hasRole: (role: string) => boolean;

    // Actions
    loginAsSuperAdmin: (payload: SuperAdminLoginPayload) => Promise<void>;
    loginAsEmployee: (payload: EmployeeLoginPayload) => Promise<void>;
    logout: () => Promise<void>;
    clearError: () => void;
}