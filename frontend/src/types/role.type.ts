export interface RolePaginationMeta {
    page: number;
    limit: number;
    total: number;
    totalPages: number;
}

// ─── List ─────────────────────────────────────────────────────────────────────
export interface RoleListParams {
    page?: number;
    limit?: number;
    search?: string;
}

export interface RoleListResponse {
    success: boolean;
    message?: string;
    data: Role[];
    meta: RolePaginationMeta;
    requestId?: string;
}

// ─── Create ───────────────────────────────────────────────────────────────────
export interface CreateRolePayload {
    name: string;
    displayName: string;
    description: string;
    permissionIds: number[];
}

export interface CreateRoleResponse {
    success: boolean;
    message?: string;
    data?: Role;
    requestId?: string;
}

// ─── Store State ──────────────────────────────────────────────────────────────
export interface RoleState {
    roles: Role[];
    meta: RolePaginationMeta | null;
    isLoading: boolean;
    error: string | null;
    params: RoleListParams;

    fetchRoles: (params?: RoleListParams) => Promise<void>;
    setParams: (params: Partial<RoleListParams>) => void;
    clearError: () => void;
}

// ─── Permission ───────────────────────────────────────────────────────────────
export interface RolePermission {
    id: number;
    module: string;
    resource: string;
    action: string;
    description: string;
}

// ─── Role ─────────────────────────────────────────────────────────────────────
export interface Role {
    id: number;
    name: string;
    displayName: string | null;
    description: string | null;
    createdAt: string;
    isSystem?: boolean;
    globalAccess?: boolean;
    permissions?: RolePermission[];
}

// ─── Meta (pagination) ────────────────────────────────────────────────────────
export interface RolePaginationMeta {
    page: number;
    limit: number;
    total: number;
    totalPages: number;
}

// ─── List ─────────────────────────────────────────────────────────────────────
/** Columns the API can order by — see RbacRepository.GetAllRolesAsync. */
export type RoleSortBy = "name" | "displayName";

export interface RoleListParams {
    page?: number;
    limit?: number;
    search?: string;
    sortBy?: RoleSortBy;
    sortOrder?: "asc" | "desc";
}

export interface RoleListResponse {
    success: boolean;
    message?: string;
    data: Role[];
    meta: RolePaginationMeta;
    requestId?: string;
}

// ─── Detail ───────────────────────────────────────────────────────────────────
export interface RoleDetailResponse {
    success: boolean;
    message?: string;
    data: Role;
    requestId?: string;
}

// ─── Create ───────────────────────────────────────────────────────────────────
export interface CreateRolePayload {
    name: string;
    displayName: string;
    description: string;
}

export interface CreateRoleResponse {
    success: boolean;
    message?: string;
    data?: Role;
    requestId?: string;
}

// ─── Delete ───────────────────────────────────────────────────────────────────
export interface DeleteRoleResponse {
    success: boolean;
    message?: string;
    data: null;
    requestId?: string;
}

// ─── Store State ──────────────────────────────────────────────────────────────
export interface RoleState {
    roles: Role[];
    meta: RolePaginationMeta | null;
    isLoading: boolean;
    error: string | null;
    params: RoleListParams;

    fetchRoles: (params?: RoleListParams) => Promise<void>;
    setParams: (params: Partial<RoleListParams>) => void;
    clearError: () => void;
}

// ─── Update ───────────────────────────────────────────────────────────────────
export interface UpdateRolePayload {
    name?: string;
    displayName?: string;
    description?: string;
}

export interface UpdateRoleResponse {
    success: boolean;
    message?: string;
    data?: Role;
    requestId?: string;
}