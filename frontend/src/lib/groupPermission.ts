import type { Permission } from "../types/permission.type";

export function groupByModule(data: Permission[]) {
    return data.reduce<Record<string, Permission[]>>((acc, item) => {
        if (!acc[item.module]) {
            acc[item.module] = [];
        }
        acc[item.module].push(item);
        return acc;
    }, {});
}