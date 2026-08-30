import { useUserStore } from "../../store/userStore";
import { useAuthStore } from "../../store/authStore";
import type { CreateUserPayload, UpdateUserPayload } from "../../types/users.types";

export function useUserController() {
    const {
        users, meta, isLoading, error, params,
        fetchUsers, setParams, clearError,
        isCreating, createError, createUser, clearCreateError,
        isUpdating, updateError, updateUser, clearUpdateError,
        isDeleting, deleteError, deleteUser, clearDeleteError,
    } = useUserStore();
    const currentUser = useAuthStore((s) => s.user);
    const companyId = currentUser?.companyId ? parseInt(currentUser.companyId) : 0;

    const isEmpty = !isLoading && !error && users.length === 0;
    const totalUsers = meta?.total ?? 0;
    const hasNextPage = meta ? meta.page < meta.totalPages : false;
    const hasPrevPage = meta ? meta.page > 1 : false;
    const handleRefresh = () => fetchUsers();

    const handleSearch = (value: string) => {
        setParams({ search: value, page: 1 });
        fetchUsers({ search: value, page: 1 });
    };

    const handleNextPage = () => {
        if (!hasNextPage || !meta) return;
        fetchUsers({ page: meta.page + 1 });
    };

    const handlePrevPage = () => {
        if (!hasPrevPage || !meta) return;
        fetchUsers({ page: meta.page - 1 });
    };

    const handleCreateUser = async (
        payload: CreateUserPayload,
        onSuccess?: () => void,
        onError?: (msg: string) => void
    ) => {
        const ok = await createUser(payload);
        if (ok) {
            onSuccess?.();
        } else {
            onError?.(createError ?? "Failed to create user.");
        }
    };

    const handleUpdateUser = async (
        id: number,
        payload: UpdateUserPayload,
        onSuccess?: () => void,
        onError?: (msg: string) => void
    ) => {
        const ok = await updateUser(id, payload);
        if (ok) {
            onSuccess?.();
        } else {
            onError?.(updateError ?? "Failed to update user.");
        }
    };

    const handleDeleteUser = async (
        id: number,
        onSuccess?: () => void,
        onError?: (msg: string) => void
    ) => {
        const ok = await deleteUser(id);
        if (ok) {
            onSuccess?.();
        } else {
            onError?.(deleteError ?? "Failed to delete user.");
        }
    };

    return {
        users, meta, isLoading, error, params,

        isEmpty, totalUsers, hasNextPage, hasPrevPage, companyId,

        isCreating, createError, clearCreateError,


        isUpdating, updateError, clearUpdateError,

        isDeleting, deleteError, clearDeleteError,

        handleRefresh,
        handleSearch,
        handleNextPage,
        handlePrevPage,
        handleCreateUser,
        handleUpdateUser,
        handleDeleteUser,
        clearError,
        fetchUsers,
    };
}