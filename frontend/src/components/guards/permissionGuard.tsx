import type { ReactNode } from "react";
import { useAuthStore } from "../../store/authStore";

interface PermissionGuardProps {
    permission?: string | string[];
    role?: string | string[];
    fallback?: ReactNode;
    children: ReactNode;
    mode?: "any" | "all";
}

export default function PermissionGuard({
    permission,
    role,
    fallback = null,
    children,
    mode = "any",
}: PermissionGuardProps) {
    const { hasPermission, hasRole } = useAuthStore();

    // ===== ROLE CHECK =====
    const roles = role
        ? Array.isArray(role)
            ? role
            : [role]
        : [];

    const roleAllowed =
        roles.length === 0
            ? true
            : mode === "all"
                ? roles.every((r) => hasRole(r))
                : roles.some((r) => hasRole(r));

    // ===== PERMISSION CHECK =====
    const permissions = permission
        ? Array.isArray(permission)
            ? permission
            : [permission]
        : [];

    const permissionAllowed =
        permissions.length === 0
            ? true
            : mode === "all"
                ? permissions.every((p) => {
                    const [module, resource, action] = p.split(".");
                    if (!module || !resource || !action) return false;
                    return hasPermission(module, resource, action);
                })
                : permissions.some((p) => {
                    const [module, resource, action] = p.split(".");
                    if (!module || !resource || !action) return false;
                    return hasPermission(module, resource, action);
                });

    // ===== FINAL =====
    const allowed = roleAllowed && permissionAllowed;

    return <>{allowed ? children : fallback}</>;
}