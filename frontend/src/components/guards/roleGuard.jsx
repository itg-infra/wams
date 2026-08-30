import { useAuthStore } from "../../store/authStore";

export function RoleGuard({ roles = [], fallback = null, children }) {
    const hasRole = useAuthStore((s) => s.hasRole);

    const allowed = Array.isArray(roles)
        ? roles.some((r) => hasRole(r))
        : hasRole(roles);

    return allowed ? children : fallback;
}