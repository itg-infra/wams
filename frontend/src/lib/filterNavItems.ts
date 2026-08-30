import type { SidebarItem } from "../components/sidebar/sidebar";

export function filterNavItems(
    items: SidebarItem[],
    hasPermission: (module: string, resource: string, action: string) => boolean
): SidebarItem[] {
    return items
        .map((item) => {
            if (item.permission) {
                const [module, resource, action] = item.permission.split(".");
                if (!hasPermission(module, resource, action)) {
                    return null;
                }
            }

            if (item.children) {
                const filteredChildren = item.children.filter((child) => {
                    if (!child.permission) return true;

                    const [module, resource, action] = child.permission.split(".");
                    return hasPermission(module, resource, action);
                });

                if (filteredChildren.length === 0) return null;

                return {
                    ...item,
                    children: filteredChildren,
                };
            }

            return item;
        })
        .filter(Boolean) as SidebarItem[];
}