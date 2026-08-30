import { Navigate } from "react-router-dom";
import { useSidebar } from "../hook/useSideBar";

export default function PermissionRedirect() {
  const sidebarItems = useSidebar();

  if (sidebarItems.length === 0) {
    return <Navigate to="/dashboard" replace />;
  }

  return <Navigate to={sidebarItems[0].id} replace />;
}
