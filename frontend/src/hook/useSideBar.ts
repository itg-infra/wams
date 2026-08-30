import { useMemo } from "react";
import { useAuthStore } from "../store/authStore";
import { NAV_ITEMS } from "../config/navigation.config";
import { filterNavItems } from "../lib/filterNavItems";

export function useSidebar() {
    const hasPermission = useAuthStore((s) => s.hasPermission);

    const items = useMemo(() => {
        return filterNavItems(NAV_ITEMS, hasPermission);
    }, [hasPermission]);

    return items;
}