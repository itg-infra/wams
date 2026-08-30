import axiosProvider from "../../api/providers/axiosProvider";

import type { WarehouseTypes, WarehouseDetailResponse, WarehouseListReponse, WarehouseQueryParams } from "../types/warehouse.type";

interface WarehouseListApiResponse {
    success: boolean;
    data: Array<{

        id: number;
        code: string;
        name: string;
        location: string;
        isActive: boolean;
        firstSeenAt: string;
        syncedAt: string;
    }>;
    meta: {
        page: number;
        limit: number;
        total: number;
        totalPages: number;
    };
    requestId?: string | null;
}

interface WarehouseDetailApiResponse {
    success: boolean;
    data: {
        id: number;
        code: string;
        name: string;
        location: string;
        isActive: boolean;
        firstSeenAt: string;
        syncedAt: string;
    };
    message?: string;
    requestId?: string | null;
}

function mapItem(warehouse: WarehouseListApiResponse['data'][number]): WarehouseTypes {
    return {
        id: warehouse.id,
        code: warehouse.code,
        name: warehouse.name,
        location: warehouse.location,
        isActive: warehouse.isActive,
        firstSeenAt: warehouse.firstSeenAt,
        syncedAt: warehouse.syncedAt
    }
}

export const warehouseService = {
  async getWarehouse(
    params: WarehouseQueryParams = {},
  ): Promise<WarehouseListReponse> {
    const { search = "", page = 1, limit = 10, location, provinceId } = params;

    const response = await axiosProvider.get<WarehouseListApiResponse>(
      "api/v1/warehouses",
      {
        params: {
          search: search || undefined,
          page,
          limit,
          location: location || undefined,
          provinceId: provinceId || undefined,
        },
      },
    );

    const { data, meta } = response.data;
    const mappedData = data.map(mapItem);

    const from = meta.total === 0 ? 0 : (meta.page - 1) * meta.limit + 1;
    const to =
      meta.total === 0 ? 0 : Math.min(meta.page * meta.limit, meta.total);

    return {
      data: mappedData,
      meta: {
        page: meta.page,
        limit: meta.limit,
        total: meta.total,
        totalPages: meta.totalPages,
        from,
        to,
      },
    };
  },

  async getWarehouseDetail(id: string): Promise<WarehouseDetailResponse> {
    const response = await axiosProvider.get<WarehouseDetailApiResponse>(
      `api/v1/warehouses/${id}`,
    );

    return {
      data: mapItem(response.data.data),
    };
  },
};