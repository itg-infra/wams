import { create } from "zustand";
import { devtools } from "zustand/middleware";
import { roleService } from "../api/services/masterData/roleService";
import type {
    CreateRolePayload,
    Role,
    RoleListParams,
    RoleState,
    UpdateRolePayload,
} from "../types/role.type";

function extractErrorMessage(err: unknown, fallback: string): string {
    const e = err as {
        response?: {
            status?: number;
            data?: { message?: string; error?: { code?: string } };
        };
    };

    const status = e?.response?.status;
    const message = e?.response?.data?.message;
    const code = e?.response?.data?.error?.code;

    if (status === 409 || code === "CONFLICT") {
        return message ?? "Role already exists.";
    }

    return message ?? fallback;
}

interface RoleStoreState extends RoleState {
    // Create
    isCreating: boolean;
    createError: string | null;
    createSuccess: string | null;
    createRole: (payload: CreateRolePayload) => Promise<boolean>;
    clearCreateError: () => void;
    clearCreateSuccess: () => void;

    selectedRole: Role | null;
    isFetchingDetail: boolean,
    detailError: string | null,
    fetchRoleDetail: (id: number) => Promise<void>;


    // Update
    isUpdating: boolean;
    updateError: string | null;
    updateSuccess: string | null;
    updateRole: (id: number, payload: UpdateRolePayload) => Promise<boolean>;
    clearUpdateError: () => void;
    clearUpdateSuccess: () => void;

    // Delete
    isDeleting: boolean;
    deleteError: string | null;
    deleteSuccess: string | null;
    deleteRole: (id: number) => Promise<boolean>;
    clearDeleteError: () => void;
    clearDeleteSuccess: () => void;

    // assign / delete permission
    isAssigning: boolean;
    isDeletingPermission: boolean;

    assignPermissionToRole: (roleId: number, permissionId: number) => Promise<void>;
    deletePermissionFromRole: (roleId: number, permissionId: number) => Promise<void>;
}

export const useRoleStore = create<RoleStoreState>()(
    devtools(
        (set, get) => ({
            // ── List ───────────────────────────────────────────────────────
            roles: [],
            meta: null,
            isLoading: false,
            error: null,
            params: { page: 1, limit: 10, search: "" },


            selectedRole: null,

            isFetchingDetail: false,
            detailError: null,
            fetchRoleDetail: async (id) => {
                set({ isFetchingDetail: true, detailError: null });

                try {
                    const res = await roleService.getRoleDetail(id);

                    if (!res.success) {
                        set({
                            isFetchingDetail: false,
                            detailError: res.message ?? "Failed to fetch role detail",
                        });
                        return;
                    }

                    set({
                        selectedRole: res.data,
                        isFetchingDetail: false,
                    });
                } catch (err: unknown) {
                    set({
                        isFetchingDetail: false,
                        detailError: extractErrorMessage(err, "Failed to fetch role detail"),
                    });
                }
            },


            fetchRoles: async (params?: RoleListParams) => {
                const mergedParams = { ...get().params, ...params };
                set({ isLoading: true, error: null, params: mergedParams });

                try {
                    const response = await roleService.getRoles(mergedParams);

                    if (!response.success) {
                        set({
                            isLoading: false,
                            error: response.message ?? "Failed to load roles.",
                        });
                        return;
                    }

                    set({
                        roles: response.data,
                        meta: response.meta,
                        isLoading: false,
                    });
                } catch (err: unknown) {
                    const e = err as { response?: { data?: { message?: string } } };
                    set({
                        isLoading: false,
                        error: e?.response?.data?.message ?? "Failed to load roles.",
                    });
                }
            },

            setParams: (params) =>
                set((s) => ({ params: { ...s.params, ...params } })),

            clearError: () => set({ error: null }),

            // ── Create ─────────────────────────────────────────────────────
            isCreating: false,
            createError: null,
            createSuccess: null,

            createRole: async (payload) => {
                set({ isCreating: true, createError: null, createSuccess: null });

                try {
                    const response = await roleService.createRole(payload);

                    if (!response.success) {
                        set({
                            isCreating: false,
                            createError: response.message ?? "Failed to create role.",
                        });
                        return false;
                    }

                    set({
                        isCreating: false,
                        createSuccess: response.message ?? "Role created successfully.",
                    });

                    await get().fetchRoles({ page: 1 });
                    return true;
                } catch (err: unknown) {
                    set({
                        isCreating: false,
                        createError: extractErrorMessage(err, "Failed to create role."),
                    });
                    return false;
                }
            },

            clearCreateError: () => set({ createError: null }),
            clearCreateSuccess: () => set({ createSuccess: null }),

            // ── Update ─────────────────────────────────────────────────────
            isUpdating: false,
            updateError: null,
            updateSuccess: null,

            updateRole: async (id, payload) => {
                set({ isUpdating: true, updateError: null, updateSuccess: null });

                try {
                    const response = await roleService.updateRole(id, payload);

                    if (!response.success) {
                        set({
                            isUpdating: false,
                            updateError: response.message ?? "Failed to update role.",
                        });
                        return false;
                    }

                    set({
                        isUpdating: false,
                        updateSuccess: response.message ?? "Role updated successfully.",
                    });

                    await get().fetchRoles({ page: get().meta?.page ?? 1 });
                    return true;
                } catch (err: unknown) {
                    set({
                        isUpdating: false,
                        updateError: extractErrorMessage(err, "Failed to update role."),
                    });
                    return false;
                }
            },

            clearUpdateError: () => set({ updateError: null }),
            clearUpdateSuccess: () => set({ updateSuccess: null }),

            // ── Delete ─────────────────────────────────────────────────────
            isDeleting: false,
            deleteError: null,
            deleteSuccess: null,

            deleteRole: async (id) => {
                set({ isDeleting: true, deleteError: null, deleteSuccess: null });

                try {
                    const response = await roleService.deleteRole(id);

                    if (!response.success) {
                        set({
                            isDeleting: false,
                            deleteError: response.message ?? "Failed to delete role.",
                        });
                        return false;
                    }

                    set({
                        isDeleting: false,
                        deleteSuccess: response.message ?? "Role deleted successfully.",
                    });

                    const currentMeta = get().meta;
                    const currentPage = currentMeta?.page ?? 1;
                    const currentTotal = currentMeta?.total ?? 0;

                    const nextPage =
                        currentTotal - 1 <= (currentPage - 1) * (currentMeta?.limit ?? 10) && currentPage > 1
                            ? currentPage - 1
                            : currentPage;

                    await get().fetchRoles({ page: nextPage });
                    return true;
                } catch (err: unknown) {
                    set({
                        isDeleting: false,
                        deleteError: extractErrorMessage(err, "Failed to delete role."),
                    });
                    return false;
                }
            },

            clearDeleteError: () => set({ deleteError: null }),
            clearDeleteSuccess: () => set({ deleteSuccess: null }),

            isAssigning: false,
            isDeletingPermission: false,

            assignPermissionToRole: async (roleId, permissionId) => {
                set({ isAssigning: true });

                try {
                    await roleService.assignPermission(roleId, permissionId);

                    await get().fetchRoleDetail(roleId);
                } catch (err) {
                    console.error("Assign permission failed", err);
                } finally {
                    set({ isAssigning: false });
                }
            },

            deletePermissionFromRole: async (roleId, permissionId) => {
                set({ isDeletingPermission: true });

                try {
                    await roleService.deletePermission(roleId, permissionId);

                    await get().fetchRoleDetail(roleId);
                } catch (err) {
                    console.error("Delete permission failed", err);
                } finally {
                    set({ isDeletingPermission: false });
                }
            },
        }),
        { name: "RoleStore" }
    )
);