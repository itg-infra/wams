// RequirePermission.tsx
import { Navigate, Outlet } from "react-router-dom";
import { useAuthStore } from "../store/authStore";
import { checkPermission } from "./getAccessFirstRoute";

interface RequirePermissionProps {
  permission: string;
}

export function RequirePermission({ permission }: RequirePermissionProps) {
  const user = useAuthStore((s) => s.user);
  const hasRole = useAuthStore((s) => s.hasRole);

  if (!user) return <Navigate to="/login" replace />;

  // super admin bebas akses semua
  if (hasRole("SUPER_ADMIN")) return <Outlet />;

  if (!checkPermission(permission)) {
    return <Navigate to="/unauthorized" replace />;
  }

  return <Outlet />;
}
