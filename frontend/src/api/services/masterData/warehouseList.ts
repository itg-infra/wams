import axiosProvider from "../../providers/axiosProvider";
import type { WarehouseListResponse } from "../../../types/warehouseList.type";
import type { FetchWarehouseParams } from "../../../types/warehouseList.type";

const WAREHOUSE_ENDPOINTS = {
    list: "api/v1/warehouses",
} as const;

export const warehouseService = {
    getList: async (params?: FetchWarehouseParams): Promise<WarehouseListResponse> => {
        const { data } = await axiosProvider.get<WarehouseListResponse>(
            WAREHOUSE_ENDPOINTS.list,
            {
                params: {
                    page: params?.page ?? 1,
                    limit: params?.limit ?? 20,
                    ...(params?.search ? { search: params.search } : {}),
                },
            }
        );
        return data;
    },
};