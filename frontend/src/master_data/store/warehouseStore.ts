import { create } from "zustand";
import type {
  WarehouseQueryParams,
  WarehouseTypes,
} from "../types/warehouse.type";
import { warehouseService } from "../services/warehouseService";

interface WarehousesStoreState {
  warehousesTypes: WarehouseTypes[];
  selectedItem: WarehouseTypes | null;
  isLoading: boolean;
  isLoadingMore: boolean; // NEW: loading khusus saat "load more" (append), beda dari isLoading initial
  isDetailLoading: boolean;
  error: string | null;
  detailError: string | null;

  search: string;
  page: number;
  limit: number;
  location: string;
  provinceId: string;
  total: number;
  totalPages: number;
  from: number;
  to: number;
  hasMore: boolean; // NEW

  fetchWarehouse: (params?: Partial<WarehouseQueryParams>) => Promise<void>;
  fetchWarehouseDetail: (id: string) => Promise<void>;
  setSearch: (value: string) => void;
  setPage: (value: number) => void;
  resetFilters: () => void;
  clearSelectedWarehouse: () => void;
}

export const useWarehouseStore = create<WarehousesStoreState>((set, get) => ({
  warehousesTypes: [],
  selectedItem: null,
  isLoading: false,
  isLoadingMore: false,
  isDetailLoading: false,
  error: null,
  detailError: null,

  provinceId: "",
  search: "",
  location: "",
  page: 1,
  limit: 10,
  total: 0,
  totalPages: 1,
  from: 0,
  to: 0,
  hasMore: false,

  fetchWarehouse: async (params: WarehouseQueryParams = {}) => {
    const current = get();

    const nextSearch = params.search ?? current.search;
    const nextPage = params.page ?? current.page;
    const nextLimit = params.limit ?? current.limit;
    const nextLocation = params.location ?? current.location;
    const nextProvince = params.provinceId ?? current.provinceId;

    const append = params.append ?? false;

    set({
      // saat append (load more), jangan pakai isLoading global
      // supaya list lama tetap tampil & tidak flicker
      isLoading: append ? current.isLoading : true,
      isLoadingMore: append,
      error: null,
      search: nextSearch,
      page: nextPage,
      limit: nextLimit,
      location: nextLocation,
      provinceId: nextProvince,
    });

    try {
      const response = await warehouseService.getWarehouse({
        search: nextSearch,
        page: nextPage,
        limit: nextLimit,
        location: nextLocation,
        provinceId: nextProvince,
      });

      set({
        warehousesTypes: append
          ? [...current.warehousesTypes, ...response.data]
          : response.data,
        total: response.meta.total,
        totalPages: response.meta.totalPages,
        from: response.meta.from,
        to: response.meta.to,
        hasMore: response.meta.page < response.meta.totalPages,
        isLoading: false,
        isLoadingMore: false,
      });
    } catch (error) {
      set({
        isLoading: false,
        isLoadingMore: false,
        error: error instanceof Error ? error.message : "Failed to fetch items",
      });
    }
  },

  fetchWarehouseDetail: async (id: string) => {
    set({ isDetailLoading: true, detailError: null });

    try {
      const response = await warehouseService.getWarehouseDetail(id);
      set({ selectedItem: response.data, isDetailLoading: false });
    } catch (error) {
      set({
        isDetailLoading: false,
        detailError:
          error instanceof Error
            ? error.message
            : "Failed to fetch item detail",
      });
    }
  },

  setSearch: (value) => set({ search: value, page: 1 }),
  setPage: (value) => set({ page: value }),
  resetFilters: () => set({ search: "", page: 1 }),
  clearSelectedWarehouse: () => set({ selectedItem: null, detailError: null }),
}));