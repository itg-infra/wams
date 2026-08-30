import axiosProvider from "../../providers/axiosProvider";
import type {
    RoleListParams,
    RoleListResponse,
    CreateRolePayload,
    CreateRoleResponse,
    UpdateRolePayload,
    UpdateRoleResponse,
    DeleteRoleResponse,
    RoleDetailResponse,
} from "../../../types/role.type";

const ROLE_ENDPOINTS = {
    list: "api/v1/roles",
    create: "api/v1/roles",
    update: (id: number) => `api/v1/roles/${id}`,
    detail: (id: number) => `api/v1/roles/${id}`,
} as const;

// ─── Error shape dari API ──────────────────────────────────────────────────────
export interface ApiErrorResponse {
    success: false;
    message: string;
    error: {
        code: string;
        details: null | unknown;
    };
    requestId: string;
}

export const roleService = {

    getRoleDetail: async (id: number): Promise<RoleDetailResponse> => {
        const { data } = await axiosProvider.get<RoleDetailResponse>(
            ROLE_ENDPOINTS.detail(id)
        );
        return data;
    },

    getRoles: async (params: RoleListParams = {}): Promise<RoleListResponse> => {
        const { data } = await axiosProvider.get<RoleListResponse>(
            ROLE_ENDPOINTS.list,
            {
                params: {
                    page: params.page ?? 1,
                    limit: params.limit ?? 20,
                    search: params.search ?? "",
                    // Ordering has to happen server-side: the list is paginated,
                    // so sorting only the rows on screen would be wrong as soon
                    // as there is a second page.
                    ...(params.sortBy
                        ? { sortBy: params.sortBy, sortOrder: params.sortOrder ?? "asc" }
                        : {}),
                },
            }
        );
        return data;
    },

    createRole: async (payload: CreateRolePayload): Promise<CreateRoleResponse> => {
        const { data } = await axiosProvider.post<CreateRoleResponse>(
            ROLE_ENDPOINTS.create,
            payload
        );
        return data;
    },

    updateRole: async (
        id: number,
        payload: UpdateRolePayload
    ): Promise<UpdateRoleResponse> => {
        const { data } = await axiosProvider.put<UpdateRoleResponse>(
            ROLE_ENDPOINTS.update(id),
            payload
        );
        return data;
    },

    deleteRole: async (id: number): Promise<DeleteRoleResponse> => {
        const response = await axiosProvider.delete(`/api/v1/roles/${id}`);
        return response.data;
    },

    assignPermission: async (roleId: number, permissionId: number) => {
        await axiosProvider.post(
            `api/v1/roles/${roleId}/permissions/${permissionId}`,
        );
    },

    deletePermission: async (roleId: number, permissionId: number) => {
        await axiosProvider.delete(
            `api/v1/roles/${roleId}/permissions/${permissionId}`
        );
    },
};