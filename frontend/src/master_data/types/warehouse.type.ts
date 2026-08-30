export interface WarehouseTypes {
  id: number;
  code: string;
  name: string;
  location: string;
  isActive: boolean;
  firstSeenAt: string;
  syncedAt: string;
}

export interface WarehouseQueryParams {
  search?: string;
  page?: number;
  limit?: number;
  location?: string;
  provinceId?: string;
  append?: boolean;
}

export interface WarehouseListReponse {
  data: WarehouseTypes[];
  meta: {
    page: number;
    limit: number;
    total: number;
    totalPages: number;
    from: number; // NEW
    to: number; // NEW
  };
}

export interface WarehouseDetailResponse {
  data: WarehouseTypes;
}
