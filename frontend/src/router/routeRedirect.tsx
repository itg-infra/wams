import { Navigate, Outlet } from "react-router-dom";
import { useAuthStore } from "../store/authStore";

interface PermissionRouteProps {
  module: string;
  resource: string;
  action: string;
}

export default function PermissionRoute({
  module,
  resource,
  action,
}: PermissionRouteProps) {
  const hasPermission = useAuthStore((s) => s.hasPermission);

  if (!hasPermission(module, resource, action)) {
    return <Navigate to="/"/>;
  }

  return <Outlet />;
}
