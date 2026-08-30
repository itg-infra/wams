import { create } from "zustand";
import { devtools, persist } from "zustand/middleware";
import { authService } from "../api/services/auth/authService";
import type {
    AuthState,
    EmployeeLoginPayload,
    SuperAdminLoginPayload,
    TokenData,
} from "../types/auth.types";
function saveTokens(data: TokenData) {
    localStorage.setItem('token', data.accessToken);
    localStorage.setItem('refreshToken', data.refreshToken);
}

function clearTokens() {
    localStorage.removeItem('token');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('auth-storage'); 
}

export const useAuthStore = create<AuthState>()(
    devtools(
        persist(
            (set, get) => ({
                user: null,
                tokens: null,
                isAuthenticated: false,
                isLoading: false,
                isLogoutLoading: false,
                error: null,
                errorCode: null,

                hasRole: (role: string) => {
                    const user = get().user;
                    return user?.roles.includes(role) ?? false;
                },

                hasPermission: (module: string, resource: string, action: string) => {
                    const user = get().user;
                    if (!user) return false;

                    const map = user.permissionMap;

                    console.log("USER:", user);
                    console.log("PERMISSION MAP:", user?.permissionMap);

                    return (
                        map?.["*"]?.["*"]?.includes("*") ||

                        
                        map?.[module]?.["*"]?.includes("*") ||

                        
                        map?.[module]?.[resource]?.includes("*") ||

                        
                        map?.[module]?.[resource]?.includes(action)
                    ) ?? false;
                },
                hasWarehouseAccess: (warehouseId: number) => {
                    const user = get().user;
                    if (!user) return false;

                    if (user.hasGlobalAccess) return true;

                    return user.warehouses.some(w => w.id === warehouseId);
                },

                // ── Login Super Admin ──────────────────────────────────────
                loginAsSuperAdmin: async (payload: SuperAdminLoginPayload) => {
                    set({ isLoading: true, error: null, errorCode: null });
                    try {
                        const response = await authService.loginAsSuperAdmin(payload);

                        if (!response.success || !response.data) {
                            set({
                                isLoading: false,
                                error: response.message ?? "Login failed.",
                                errorCode: response.error?.code ?? null,
                                isAuthenticated: false,
                            });
                            return;
                        }

                        saveTokens(response.data);
                        const me = await authService.getMe();

                        if (!me.id) {
                            throw new Error("Failed to fetch user data");
                        }


                        set({
                            tokens: response.data,
                            user: me,
                            isAuthenticated: true,
                            isLoading: false,
                            error: null,
                            errorCode: null,
                        });
                    } catch (err: unknown) {
                        const axiosError = err as {
                            response?: { data?: { message?: string; error?: { code?: string } } };
                        };
                        set({
                            isLoading: false,
                            error: axiosError?.response?.data?.message ?? "Login failed. Please try again.",
                            errorCode: axiosError?.response?.data?.error?.code ?? null,
                            isAuthenticated: false,
                        });
                        throw err;
                    }
                },

                // ── Login Employee ─────────────────────────────────────────
                loginAsEmployee: async (payload: EmployeeLoginPayload) => {
                    set({ isLoading: true, error: null, errorCode: null });
                    try {
                        const response = await authService.loginAsEmployee(payload);

                        if (!response.success || !response.data) {
                            set({
                                isLoading: false,
                                error: response.message ?? "Login failed.",
                                errorCode: response.error?.code ?? null,
                                isAuthenticated: false,
                            });
                            return;
                        }

                        saveTokens(response.data);
                        const me = await authService.getMe();

                        if (!me.id) {
                            throw new Error("Failed to fetch user data");
                        }


                        set({
                            tokens: response.data,
                            user: me,
                            isAuthenticated: true,
                            isLoading: false,
                            error: null,
                            errorCode: null,
                        });
                    } catch (err: unknown) {
                        const axiosError = err as {
                            response?: { data?: { message?: string; error?: { code?: string } } };
                        };
                        set({
                            isLoading: false,
                            error: axiosError?.response?.data?.message ?? "Login failed. Please try again.",
                            errorCode: axiosError?.response?.data?.error?.code ?? null,
                            isAuthenticated: false,
                        });
                        throw err;
                    }
                },

                // ── Logout ─────────────────────────────────────────────────
                logout: async () => {
                    set({ isLogoutLoading: true });
                    try {
                        await authService.logout();
                    } catch {
                        // Tetap lanjut logout lokal meski endpoint gagal
                        // (misal token sudah expired di BE)
                    } finally {
                        clearTokens();
                        set({
                            user: null,
                            tokens: null,
                            isAuthenticated: false,
                            isLoading: false,
                            isLogoutLoading: false,
                            error: null,
                            errorCode: null,
                        });
                    }
                },

                clearError: () => set({ error: null, errorCode: null }),
            }),
            {
                name: "auth-storage",
                partialize: (state) => ({
                    user: state.user,
                    tokens: state.tokens,
                    isAuthenticated: state.isAuthenticated,
                }),
            }
        ),
        { name: "AuthStore" }
    )
);