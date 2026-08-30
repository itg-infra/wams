import axiosProvider from "../../providers/axiosProvider";
import type { UserListParams, UserListResponse, CreateUserPayload, CreateUserResponse, UpdateUserPayload, UpdateUserResponse, DeleteUserResponse, UserDetailResponse } from "../../../types/users.types";

const USER_ENDPOINTS = {
    list: "api/v1/users",
    create: "api/v1/users",
    byId: (id: number) => `api/v1/users/${id}`,
} as const;

export const userService = {
    getUsers: async (params: UserListParams = {}): Promise<UserListResponse> => {
        const { data } = await axiosProvider.get<UserListResponse>(
            USER_ENDPOINTS.list,
            {
                params: {
                    page: params.page ?? 1,
                    limit: params.limit ?? 20,
                    search: params.search ?? "",
                    is_active: params.is_active ?? "",
                },
            }
        );
        return data;
    },

    createUser: async (payload: CreateUserPayload): Promise<CreateUserResponse> => {
        const { data } = await axiosProvider.post<CreateUserResponse>(
            USER_ENDPOINTS.create,
            payload
        );
        return data;
    },

    updateUser: async (id: number, payload: UpdateUserPayload): Promise<UpdateUserResponse> => {
        const { data } = await axiosProvider.put<UpdateUserResponse>(
            USER_ENDPOINTS.byId(id),
            payload
        );
        return data;
    },

    deleteUser: async (id: number): Promise<DeleteUserResponse> => {
        const { data } = await axiosProvider.delete<DeleteUserResponse>(
            USER_ENDPOINTS.byId(id)
        );
        return data;
    },

    getUserById: async (id: number): Promise<UserDetailResponse> => {
        const { data } = await axiosProvider.get<UserDetailResponse>(
            USER_ENDPOINTS.byId(id)
        );
        return data;
    },

    assignRole: async (userId: number, roleId: number) => {
        const { data } = await axiosProvider.post(
            `api/v1/users/${userId}/roles/${roleId}`,
        );
        return data;
    },

    removeRole: async (userId: number, roleId: number) => {
        const { data } = await axiosProvider.delete(
            `api/v1/users/${userId}/roles/${roleId}`
        );
        return data;
    },

    assignWarehouse: async (userId: number, warehouseId: number) => {
        const { data } = await axiosProvider.post(
            `api/v1/users/${userId}/warehouses/${warehouseId}`
        );
        return data;
    },

    removeWarehouse: async (userId: number, warehouseId: number) => {
        const { data } = await axiosProvider.delete(
            `api/v1/users/${userId}/warehouses/${warehouseId}`
        );
        return data;
    },
};