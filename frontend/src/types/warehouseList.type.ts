// ─── Warehouse Model ──────────────────────────────────────────────────────────
export interface Warehouse {
    id: number;
    code: string;
    name: string;
    location: string;
    isActive: boolean;
    firstSeenAt: string;
    syncedAt: string;
}

// ─── API Response ─────────────────────────────────────────────────────────────
export interface WarehouseMeta {
    page: number;
    limit: number;
    total: number;
    totalPages: number;
}

export interface WarehouseListResponse {
    success: boolean;
    data: Warehouse[];
    meta: WarehouseMeta;
    requestId: string;
}

// ─── Store State ──────────────────────────────────────────────────────────────
export interface WarehouseState {
    warehouses: Warehouse[];
    selectedWarehouse: Warehouse | null;
    isLoading: boolean;
    error: string | null;
    meta: WarehouseMeta | null;
    getWarehouseId: () => number | null;

    // Params
    page: number;
    search: string;
    limit: number;

    // Actions
    fetchWarehouses: (params?: FetchWarehouseParams) => Promise<void>;
    setSelectedWarehouse: (warehouse: Warehouse) => void;
    setSearch: (search: string) => void;
    setPage: (page: number) => void;
    clearError: () => void;
}

export interface FetchWarehouseParams {
    page?: number;
    limit?: number;
    search?: string;
}