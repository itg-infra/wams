import { useNavigate } from "react-router-dom";
import { useAuthStore } from "../../store/authStore";
import type {
    EmployeeLoginPayload,
    SuperAdminLoginPayload,
} from "../../types/auth.types";

export function useAuthController() {
    const navigate = useNavigate();

    const {
        user,
        tokens,
        isAuthenticated,
        isLoading,
        isLogoutLoading,
        error,
        errorCode,
        loginAsSuperAdmin,
        loginAsEmployee,
        logout,
        clearError,
    } = useAuthStore();

    const isSuperAdmin = user?.roles.includes("SUPER_ADMIN");

    // check access warehouse
    // const canAccessAllWarehouse = user?.hasGlobalAccess
    
    const isEmployee = !isSuperAdmin && !!user;
    const displayName = user?.fullname ?? user?.email ?? "User";

    // ── Handlers ──────────────────────────────────────────────────────────────

    const handleSuperAdminLogin = async (
        payload: SuperAdminLoginPayload,
        onSuccess?: () => void,
        onError?: (message: string, code?: string | null) => void
    ) => {
        try {
            await loginAsSuperAdmin(payload);
            if (useAuthStore.getState().isAuthenticated) {
                onSuccess?.();
            } else {
                const { error: err, errorCode: code } = useAuthStore.getState();
                onError?.(err ?? "Login failed.", code);
            }
        } catch {
            const { error: err, errorCode: code } = useAuthStore.getState();
            onError?.(err ?? "Login failed.", code);
        }
    };

    const handleEmployeeLogin = async (
        payload: EmployeeLoginPayload,
        onSuccess?: () => void,
        onError?: (message: string, code?: string | null) => void
    ) => {
        try {
            await loginAsEmployee(payload);
            if (useAuthStore.getState().isAuthenticated) {
                onSuccess?.();
            } else {
                const { error: err, errorCode: code } = useAuthStore.getState();
                onError?.(err ?? "Login failed.", code);
            }
        } catch {
            const { error: err, errorCode: code } = useAuthStore.getState();
            onError?.(err ?? "Login failed.", code);
        }
    };

    const handleLogout = async (
        onSuccess?: () => void,
        onError?: (message: string) => void
    ) => {
        try {
            await logout();
            onSuccess?.();
            navigate("/login", { replace: true });
        } catch {
            onError?.("Logout failed. Please try again.");
        }
    };

    return {
        user,
        tokens,
        isAuthenticated,
        isLoading,
        isLogoutLoading,
        error,
        errorCode,

        isSuperAdmin,
        isEmployee,
        displayName,

        handleSuperAdminLogin,
        handleEmployeeLogin,
        handleLogout,
        clearError,
    };
}