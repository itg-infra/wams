import { create } from "zustand";
import { devtools } from "zustand/middleware";
import { userService } from "../api/services/masterData/userService";
import type {
    CreateUserPayload,
    UpdateUserPayload,
    UserListParams,
    UserState,
} from "../types/users.types";

interface UserStoreState extends UserState {
    // Create
    isCreating: boolean;
    createError: string | null;
    createUser: (payload: CreateUserPayload) => Promise<boolean>;
    clearCreatesuccess: ()=> void;
    clearCreateError: () => void;

    // Update
    isUpdating: boolean;
    updateError: string | null;
    updateUser: (id: number, payload: UpdateUserPayload) => Promise<boolean>;
    clearUpdateError: () => void;

    // Delete
    isDeleting: boolean;
    deleteError: string | null;
    deleteUser: (id: number) => Promise<boolean>;
    clearDeleteError: () => void;

    // state
    isAssigningRole: boolean,
    isRemovingRole: boolean,
    assignRoleToUser: (userId: number, roleId: number) => Promise<void>;
    removeRoleFromUser: (userId: number, roleId: number) => Promise<void>;



    isAssigningWarehouse: boolean,
    isRemovingWarehouse: boolean,
    assignWarehouseToUser: (userId: number, warehouseId: number) => Promise<void>;

    removeWarehouseFromUser: (userId: number, warehouseId: number) => Promise<void>;
}

export const useUserStore = create<UserStoreState>()(
    devtools(
        (set, get) => ({
            // ── List ───────────────────────────────────────────────────────
            users: [],
            meta: null,
            isLoading: false,
            error: null,
            params: { page: 1, limit: 20, search: "", is_active: "" },

            fetchUsers: async (params?: UserListParams) => {
                const mergedParams = { ...get().params, ...params };
                set({ isLoading: true, error: null, params: mergedParams });
                try {
                    const response = await userService.getUsers(mergedParams);
                    if (!response.success) {
                        set({ isLoading: false, error: response.message ?? "Failed to load users." });
                        return;
                    }
                    set({ users: response.data, meta: response.meta, isLoading: false });
                } catch (err: unknown) {
                    const e = err as { response?: { data?: { message?: string } } };
                    set({ isLoading: false, error: e?.response?.data?.message ?? "Failed to load users." });
                }
            },

            setParams: (params) => set((s) => ({ params: { ...s.params, ...params } })),
            clearError: () => set({ error: null }),

            // ── Create ─────────────────────────────────────────────────────
            isCreating: false,
            createError: null,

            createUser: async (payload) => {
                set({ isCreating: true, createError: null });
                try {
                    const response = await userService.createUser(payload);
                    if (!response.success) {
                        set({ isCreating: false, createError: response.message ?? "Failed to create user." });
                        return false;
                    }
                    set({ isCreating: false });
                    await get().fetchUsers({ page: 1 });
                    return true;
                } catch (err: unknown) {
                    const e = err as { response?: { data?: { message?: string } } };
                    set({ isCreating: false, createError: e?.response?.data?.message ?? "Failed to create user." });
                    return false;
                }
            },

            clearCreateError: () => set({ createError: null }),
            clearCreatesuccess: () => set({ createError: null }),

            // ── Update ─────────────────────────────────────────────────────
            isUpdating: false,
            updateError: null,

            updateUser: async (id, payload) => {
                set({ isUpdating: true, updateError: null });
                try {
                    const response = await userService.updateUser(id, payload);
                    if (!response.success) {
                        set({ isUpdating: false, updateError: response.message ?? "Failed to update user." });
                        return false;
                    }
                    set({ isUpdating: false });
                    await get().fetchUsers();
                    return true;
                } catch (err: unknown) {
                    const e = err as { response?: { data?: { message?: string } } };
                    set({ isUpdating: false, updateError: e?.response?.data?.message ?? "Failed to update user." });
                    return false;
                }
            },

            clearUpdateError: () => set({ updateError: null }),

            // ── Delete ─────────────────────────────────────────────────────
            isDeleting: false,
            deleteError: null,

            deleteUser: async (id) => {
                set({ isDeleting: true, deleteError: null });
                try {
                    const response = await userService.deleteUser(id);
                    if (!response.success) {
                        set({ isDeleting: false, deleteError: response.message ?? "Failed to delete user." });
                        return false;
                    }
                    set({ isDeleting: false });
                    await get().fetchUsers();
                    return true;
                } catch (err: unknown) {
                    const e = err as { response?: { data?: { message?: string } } };
                    set({ isDeleting: false, deleteError: e?.response?.data?.message ?? "Failed to delete user." });
                    return false;
                }
            },

            clearDeleteError: () => set({ deleteError: null }),

            // state
            isAssigningRole: false,
            isRemovingRole: false,

            assignRoleToUser: async (userId: number, roleId: number) => {
                set({ isAssigningRole: true });

                try {
                    await userService.assignRole(userId, roleId);

                    await get().fetchUsers();
                } catch (err) {
                    console.error("Assign role failed", err);
                } finally {
                    set({ isAssigningRole: false });
                }
            },

            removeRoleFromUser: async (userId: number, roleId: number) => {
                set({ isRemovingRole: true });

                try {
                    await userService.removeRole(userId, roleId);

                    await get().fetchUsers();
                } catch (err) {
                    console.error("Remove role failed", err);
                } finally {
                    set({ isRemovingRole: false });
                }
            },

            isAssigningWarehouse: false,
            isRemovingWarehouse: false,

            assignWarehouseToUser: async (userId: number, warehouseId: number) => {
                set({ isAssigningWarehouse: true });
                try {
                    await userService.assignWarehouse(userId, warehouseId);
                } catch (err) {
                    console.error("Assign warehouse failed", err);
                } finally {
                    set({ isAssigningWarehouse: false });
                }
            },

            removeWarehouseFromUser: async (userId: number, warehouseId: number) => {
                set({ isRemovingWarehouse: true });
                try {
                    await userService.removeWarehouse(userId, warehouseId);
                } catch (err) {
                    console.error("Remove warehouse failed", err);
                } finally {
                    set({ isRemovingWarehouse: false });
                }
            },
        }),
        { name: "UserStore" }
    )
);