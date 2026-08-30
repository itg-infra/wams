import type { SidebarItem } from "../components/sidebar/sidebar";
import { useAuthStore } from "../store/authStore";


/**
 * "report.dashboard.read" -> hasPermission("report", "dashboard", "read")
 */
export function checkPermission(permission?: string): boolean {
  if (!permission) return true; // item tanpa permission dianggap boleh diakses
  const [module, resource, action] = permission.split(".");
  return useAuthStore.getState().hasPermission(module, resource, action);
}


export function getFirstAccessibleRoute(
  items: SidebarItem[],
  excludeIds: string[] = [],
): string | null {
  for (const item of items) {
    if (excludeIds.includes(item.id)) continue;

    const hasChildren = (item.children?.length ?? 0) > 0;

    if (!hasChildren && checkPermission(item.permission)) {
      return item.id;
    }

    if (hasChildren) {
      const found = item.children!.find((child) =>
        checkPermission(child.permission),
      );
      if (found) return found.id;
    }
  }
  return null;
}
