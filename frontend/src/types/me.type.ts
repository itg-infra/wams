import type { ApiResponse } from "./auth.types";

export interface MeResponseData {
  id: number;
  email: string;
  fullname: string;
  isActive: boolean;
  hasGlobalAccess: boolean;
  companyId: string;
  companyName: string;
  companyCode: string;
  roles: string[];
  permissions: string[];
  permissionMap: Record<string, Record<string, string[]>>;
  warehouses: {
    id: number;
    code: string;
    name: string;
    location: string;
    isPrimary: boolean;
  }[];
  createdAt: string;
}

export type GetMeResponse = ApiResponse<MeResponseData>;

export interface Warehouse {
    id: number;
    code: string;
    name: string;
    location: string;
    isPrimary: boolean;
}

export type PermissionMap = Record<
    string,
    Record<string, string[]>
>;

export interface User {
    id: number;
    companyId: string,
    email: string;
    fullname: string;
    isActive: boolean;
    hasGlobalAccess: boolean;
    roles: string[];
    permissions: string[];
    permissionMap: PermissionMap;
    warehouses: Warehouse[];
    createdAt: string;
}

export const mapUser = (data: MeResponseData): User => ({
    id: data.id,
    companyId: String(data.companyId),
    email: data.email,
    fullname: data.fullname,
    isActive: data.isActive,
    hasGlobalAccess: data.hasGlobalAccess,
    roles: data.roles,
    permissions: data.permissions,
    permissionMap: data.permissionMap,
    warehouses: data.warehouses.map((w) => ({
        id: w.id,
        code: w.code,
        name: w.name,
        location: w.location,
        isPrimary: w.isPrimary,
    })),
    createdAt: data.createdAt,
});