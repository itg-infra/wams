// ─── Role ─────────────────────────────────────────────────────────────────────
export interface UserRole {
    roleId: number;
    roleName: string;
    displayName: string;
}

// ─── Warehouse ────────────────────────────────────────────────────────────────
export interface UserWarehouse {
    warehouseId: number;
    name: string;
}

// ─── User ─────────────────────────────────────────────────────────────────────
export interface User {
    id: number;
    email: string;
    fullname: string;
    employeeId: string | null;
    isActive: boolean;
    createdAt: string;
    roles: UserRole[];
    warehouses: UserWarehouse[];
}

// ─── Meta (pagination) ────────────────────────────────────────────────────────
export interface PaginationMeta {
    page: number;
    limit: number;
    total: number;
    totalPages: number;
}

// ─── Response ─────────────────────────────────────────────────────────────────
export interface UserListData {
    data: User[];
    meta: PaginationMeta;
}export interface UserListResponse {
    success: boolean;
    message?: string;
    data: User[];
    meta: PaginationMeta;
    requestId?: string;
}

// ─── Query Params ─────────────────────────────────────────────────────────────
export interface UserListParams {
    page?: number;
    limit?: number;
    search?: string;
    is_active?: boolean | "";
}

// ─── User Store State ─────────────────────────────────────────────────────────
export interface UserState {
    users: User[];
    meta: PaginationMeta | null;
    isLoading: boolean;
    error: string | null;
    params: UserListParams;

    // Actions
    fetchUsers: (params?: UserListParams) => Promise<void>;
    setParams: (params: Partial<UserListParams>) => void;
    clearError: () => void;
}

export interface CreateUserPayload {
    email: string;
    password: string;
    fullname: string;
    employeeId: string;
}

export interface CreateUserResponse {
    success: boolean;
    message?: string;
    data?: User;
    requestId?: string;
}

export interface UpdateUserPayload {
    email?: string;
    fullname?: string;
    employeeId?: string;
    password?: string;
}

export interface UpdateUserResponse {
    success: boolean;
    message?: string;
    data?: User;
    requestId?: string;
}

// ─── Delete User ──────────────────────────────────────────────────────────────
export interface DeleteUserResponse {
    success: boolean;
    message?: string;
    data: null;
    requestId?: string;
}

// Detail User
export interface UserDetailResponse {
    success: boolean;
    message?: string;
    data: User;
    requestId?: string;
}