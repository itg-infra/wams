import type { GetPermissionsResponse } from "../../../types/permission.type";
import axiosProvider from "../../providers/axiosProvider";

export const permissionService = {
    async getPermissions(page: number, limit: number) {
        const res = await axiosProvider.get<GetPermissionsResponse>(
            `/api/v1/permissions?page=${page}&limit=${limit}`
        );
        return res.data;
    },

    async assignPermission(roleId: number, permissionId: number) {
        return axiosProvider.post(`/api/v1/roles/${roleId}/permissions`, {
            permissionId,
        });
    },

    async deletePermission(roleId: number, permissionId: number) {
        return axiosProvider.delete(
            `/api/v1/roles/${roleId}/permissions/${permissionId}`
        );
    },
};