import { Navigate, Outlet } from "react-router-dom";
import { useAuthStore } from "../store/authStore";

interface ProtectedRouteProps {
    roles?: string[];
}

export default function ProtectedRoute({ roles }: ProtectedRouteProps) {
    const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
    const user = useAuthStore((s) => s.user);

    if (!isAuthenticated) {
        return <Navigate to="/login" replace />;
    }

    if (roles && !roles.some((r) => user?.roles?.includes(r))) {
        return <Navigate to="/forbidden" replace />;
    }

    return <Outlet />;
}