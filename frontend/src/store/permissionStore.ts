import { create } from "zustand";
import type { Permission } from "../types/permission.type";
import { permissionService } from "../api/services/masterData/permissionService";

type PermissionState = {
    permissions: Permission[];
    page: number;
    limit: number;
    hasMore: boolean;
    loading: boolean;

    fetchPermissions: () => Promise<void>;
    reset: () => void;

};

export const usePermissionStore = create<PermissionState>((set, get) => ({
    permissions: [],
    page: 1,
    limit: 20,
    hasMore: true,
    loading: false,

    fetchPermissions: async () => {
        const { page, limit, permissions, hasMore, loading } = get();
        if (!hasMore || loading) return;

        set({ loading: true });

        try {
            const res = await permissionService.getPermissions(page, limit);

            const newData = res.data;

            set({
                permissions: [...permissions, ...newData],
                page: page + 1,
                hasMore: newData.length === limit,
            });
        } catch (err) {
            console.error(err);
        } finally {
            set({ loading: false });
        }
    },

    reset: () => {
        set({
            permissions: [],
            page: 1,
            hasMore: true,
        });
    },

  
}));