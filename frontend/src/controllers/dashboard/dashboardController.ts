import { useState } from "react";
import { useAuthController } from "../auth/authController";
import { PAGE_META } from "../../config/navigation.config";

export type ActivePage =
    | "dashboard"
    | "master.user"
    | "master.role"
    | "master.product"
    | "master.warehouse"
    | "master.vendor"
    | "master.ratecord"
    | "master.bl"
    | "master.coa"
    | "budgeting"
    | "budgeting.template"
    | "budgeting.plan"
    | "budgeting.po"
    | "budgeting.ap"
    | "quality"
    | "operational"
    | "operational.approved-bp"
    | "operational.work-order"
    | "operational.recap-work-order"
    | "finance"
    | "reports"
    | string;

export function useDashboardController() {
    const { displayName, isLogoutLoading, handleLogout } = useAuthController();

    const [activePage, setActivePage] = useState<ActivePage>("dashboard");
    const [showLogoutDialog, setShowLogoutDialog] = useState(false);

    // ── Computed ──────────────────────────────────────────────────────────
    const meta = PAGE_META[activePage] ?? { title: activePage, subtitle: "" };
    const headerTitle = activePage === "dashboard" ? `Welcome, ${displayName}` : meta.title;
    const initials = displayName
        .split(" ").map((n) => n[0]).join("").toUpperCase().slice(0, 2);

    // ── Handlers ──────────────────────────────────────────────────────────
    const handleNavigate = (id: string) => setActivePage(id);
    const handleOpenLogout = () => setShowLogoutDialog(true);
    const handleCancelLogout = () => setShowLogoutDialog(false);
    const handleConfirmLogout = () => handleLogout(() => setShowLogoutDialog(false));

    return {
        // State
        activePage,
        showLogoutDialog,
        isLogoutLoading,

        // Computed
        displayName,
        headerTitle,
        initials,
        meta,

        // Handlers
        handleNavigate,
        handleOpenLogout,
        handleCancelLogout,
        handleConfirmLogout,
    };
}