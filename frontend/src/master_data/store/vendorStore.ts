import { create } from "zustand";
import { vendorService } from "../services/vendorService";
import type { Vendor, VendorQueryParams } from "../types/vendor.type";

interface VendorStoreState {
  vendors: Vendor[];
  selectedVendor: Vendor | null;
  isLoading: boolean;
  isLoadingMore: boolean;
  isDetailLoading: boolean;
  error: string | null;
  detailError: string | null;

  search: string;
  page: number;
  limit: number;
  total: number;
  totalPages: number;
  from: number;
  to: number;
  hasMore: boolean;

  fetchVendors: (
    params?: Partial<VendorQueryParams>,
    options?: { append?: boolean },
  ) => Promise<void>;
  loadMoreVendors: () => Promise<void>;
  fetchVendorDetail: (id: string) => Promise<void>;
  setSearch: (value: string) => void;
  setPage: (value: number) => void;
  resetFilters: () => void;
  clearSelectedVendor: () => void;
}

export const useVendorStore = create<VendorStoreState>((set, get) => ({
  vendors: [],
  selectedVendor: null,
  isLoading: false,
  isLoadingMore: false,
  isDetailLoading: false,
  error: null,
  detailError: null,

  search: "",
  page: 1,
  limit: 10,
  total: 0,
  totalPages: 1,
  from: 0,
  to: 0,
  hasMore: false,

  //   fetchVendors: async (params = {}) => {
  //     const current = get();

  //     const nextSearch = params.search ?? current.search;
  //     const nextPage = params.page ?? current.page;
  //     const nextLimit = params.limit ?? current.limit;

  //     set({
  //       isLoading: true,
  //       error: null,
  //       search: nextSearch,
  //       page: nextPage,
  //       limit: nextLimit,
  //     });

  //     try {
  //       const response = await vendorService.getVendors({
  //         search: nextSearch,
  //         page: nextPage,
  //         limit: nextLimit,
  //       });

  //       set({
  //         vendors: response.data,
  //         total: response.meta.total,
  //         totalPages: response.meta.totalPages,
  //         from: response.meta.from,
  //         to: response.meta.to,
  //         isLoading: false,
  //       });
  //     } catch (error) {
  //       set({
  //         isLoading: false,
  //         error:
  //           error instanceof Error ? error.message : "Failed to fetch vendors",
  //       });
  //     }
  //   },

  fetchVendors: async (params = {}, options = {}) => {
    const current = get();
    const { append = false } = options;

    const nextSearch = params.search ?? current.search;
    const nextPage = params.page ?? current.page;
    const nextLimit = params.limit ?? current.limit;

    set({
      ...(append ? { isLoadingMore: true } : { isLoading: true }),
      error: null,
      search: nextSearch,
      page: nextPage,
      limit: nextLimit,
    });

    try {
      const response = await vendorService.getVendors({
        search: nextSearch,
        page: nextPage,
        limit: nextLimit,
      });

      set({
        vendors: append
          ? [...current.vendors, ...response.data]
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
        error:
          error instanceof Error ? error.message : "Failed to fetch vendors",
      });
    }
  },

  loadMoreVendors: async () => {
    const { isLoading, isLoadingMore, hasMore, page } = get();
    if (isLoading || isLoadingMore || !hasMore) return;

    await get().fetchVendors({ page: page + 1 }, { append: true });
  },

  fetchVendorDetail: async (id: string) => {
    set({
      isDetailLoading: true,
      detailError: null,
    });

    try {
      const response = await vendorService.getVendorDetail(id);

      set({
        selectedVendor: response.data,
        isDetailLoading: false,
      });
    } catch (error) {
      set({
        isDetailLoading: false,
        detailError:
          error instanceof Error
            ? error.message
            : "Failed to fetch vendor detail",
      });
    }
  },

  setSearch: (value) => set({ search: value, page: 1 }),
  setPage: (value) => set({ page: value }),

  resetFilters: () =>
    set({
      search: "",
      page: 1,
    }),

  clearSelectedVendor: () =>
    set({
      selectedVendor: null,
      detailError: null,
    }),
}));