import { create } from "zustand";
import type { BudgetPlanListItem, PaginationMeta, PurchaseOrderDetailApdp, RecapAPDPListParams } from "../types/recapApdp.type";
import RecapAPDPService from "../api/services/finance/recapApdpService";

// export interface FinanceReportFilters {
//   activityTypeCode?: string;
//   warehouseCode?: string;
//   dateFrom?: string;
//   dateTo?: string;
// }

// const DEFAULT_FILTERS: FinanceReportFilters = {
//   activityTypeCode: undefined,
//   warehouseCode: undefined,
//   dateFrom: undefined,
//   dateTo: undefined,
// };

interface RecapAPDPState {
  // ---- list state ----
  list: BudgetPlanListItem[];
  meta: PaginationMeta | null;
  //   filters: FinanceReportFilters;
  isListLoading: boolean;
  listError: string | null;

  // ---- detail state ----
  detail: PurchaseOrderDetailApdp | null;
  isDetailLoading: boolean;
  detailError: string | null;

  // ---- list actions ----
  /** Fetch list. Param yang tidak dikirim akan fallback ke state (page/limit/filters) saat ini. */
  fetchList: (params?: RecapAPDPListParams) => Promise<void>;
  //   setFilters: (filters: Partial<FinanceReportFilters>) => void;
  setPage: (page: number) => void;
  setLimit: (limit: number) => void;
  //   resetFilters: () => void;

  // ---- detail actions ----
  fetchDetail: (budgetPlanId: number | string) => Promise<void>;
  resetDetail: () => void;
}

export const useRecapApdpStore = create<RecapAPDPState>((set, get) => ({
  list: [],
  meta: null,
  //   filters: DEFAULT_FILTERS,
  isListLoading: false,
  listError: null,

  detail: null,
  isDetailLoading: false,
  detailError: null,

  fetchList: async (params) => {
    const state = get();
    const merged: RecapAPDPListParams = {
      page: params?.page ?? state.meta?.page ?? 1,
      limit: params?.limit ?? state.meta?.limit ?? 10,
      //   activityTypeCode:
      //     params?.activityTypeCode ?? state.filters.activityTypeCode,
      //   warehouseCode: params?.warehouseCode ?? state.filters.warehouseCode,
      //   dateFrom: params?.dateFrom ?? state.filters.dateFrom,
      //   dateTo: params?.dateTo ?? state.filters.dateTo,
    };

    set({ isListLoading: true, listError: null });
    try {
      const res = await RecapAPDPService.getList(merged);
      set({
        list: res.data,
        meta: res.meta,
        // filters: {
        //   activityTypeCode: merged.activityTypeCode,
        //   warehouseCode: merged.warehouseCode,
        //   dateFrom: merged.dateFrom,
        //   dateTo: merged.dateTo,
        // },
        isListLoading: false,
      });
    } catch (err: any) {
      set({
        isListLoading: false,
        listError:
          err?.response?.data?.message ??
          err?.message ??
          "Gagal mengambil data finance report",
      });
    }
  },

  //   setFilters: (filters) => {
  //     set((state) => ({ filters: { ...state.filters, ...filters } }));
  //   },

  setPage: (page) => {
    get().fetchList({ page });
  },

  setLimit: (limit) => {
    // reset ke page 1 saat limit berubah, umum untuk UX pagination
    get().fetchList({ page: 1, limit });
  },

  //   resetFilters: () => {
  //     set({ filters: DEFAULT_FILTERS });
  //     get().fetchList({ page: 1, ...DEFAULT_FILTERS });
  //   },

  fetchDetail: async (poId) => {
    set({ isDetailLoading: true, detailError: null });
    try {
      const res = await RecapAPDPService.getDetail(poId);
      set({ detail: res.data, isDetailLoading: false });
    } catch (err: any) {
      set({
        isDetailLoading: false,
        detailError:
          err?.response?.data?.message ??
          err?.message ??
          "Gagal mengambil detail finance report",
      });
    }
  },

  resetDetail: () => {
    set({ detail: null, detailError: null, isDetailLoading: false });
  },
}));
