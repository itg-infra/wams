import axiosProvider from "../../providers/axiosProvider";
import type {
    ApiResponse,
    EmployeeLoginPayload,
    LoginResponse,
    LogoutResponse,
    RefreshResponse,
    SuperAdminLoginPayload,
} from "../../../types/auth.types";
import type { MeResponseData } from "../../../types/me.type";

const AUTH_ENDPOINTS = {
    login: "api/v1/auth/login",
    logout: "api/v1/auth/logout",
    refresh: "api/v1/auth/refresh",
    me: "api/v1/auth/me",
} as const;

export const authService = {
    // ── Login Super Admin ──────────────────────────────────────────────────
    loginAsSuperAdmin: async (
        payload: SuperAdminLoginPayload
    ): Promise<LoginResponse> => {
        const { data } = await axiosProvider.post<LoginResponse>(
            AUTH_ENDPOINTS.login,
            {
                email: payload.email,
                password: payload.password,
                companyId: payload.companyId,
            }
        );
        return data;
    },

    // ── Login Employee ─────────────────────────────────────────────────────
    loginAsEmployee: async (
        payload: EmployeeLoginPayload
    ): Promise<LoginResponse> => {
        const { data } = await axiosProvider.post<LoginResponse>(
            AUTH_ENDPOINTS.login,
            {
                email: payload.email,
                password: payload.password,
            }
        );
        return data;
    },

    // ── Logout ────────────────────────────────────────────────────────────
    // access_token otomatis di-attach oleh Axios interceptor
    logout: async (): Promise<LogoutResponse> => {
        const refreshToken = localStorage.getItem("refreshToken");

        const { data } = await axiosProvider.post<LogoutResponse>(
            AUTH_ENDPOINTS.logout,
            {
                "refreshToken": `${refreshToken}`
            }
        );
        return data;
    },

    // ── Refresh Token ─────────────────────────────────────────────────────
    refresh: async (refreshToken: string): Promise<RefreshResponse> => {
        const { data } = await axiosProvider.post<RefreshResponse>(
            AUTH_ENDPOINTS.refresh,
            { refreshToken }
        );
        return data;
    },

    getMe: async (): Promise<MeResponseData> => {
        const { data } = await axiosProvider.get<ApiResponse<MeResponseData>>(
            AUTH_ENDPOINTS.me
        );

        console.log(`me info data: ${data.data.fullname}`)

        return data.data;
    },
    

    
};