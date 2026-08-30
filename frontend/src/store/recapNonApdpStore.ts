import { create } from "zustand";
import type {
  BudgetPlanListItem,
  PaginationMeta,
  PurchaseOrderDetailNonApdp,
  RecapNonAPDPListParams,
} from "../types/recapNonApdp.type";
import RecapNonAPDPService from "../api/services/finance/recapNonApdpService";

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

interface RecapNonAPDPState {
  // ---- list state ----
  list: BudgetPlanListItem[];
  meta: PaginationMeta | null;
  //   filters: FinanceReportFilters;
  isListLoading: boolean;
  listError: string | null;

  // ---- detail state ----
  detail: PurchaseOrderDetailNonApdp | null;
  isDetailLoading: boolean;
  detailError: string | null;

  // ---- list actions ----
  /** Fetch list. Param yang tidak dikirim akan fallback ke state (page/limit/filters) saat ini. */
  fetchList: (params?: RecapNonAPDPListParams) => Promise<void>;
  //   setFilters: (filters: Partial<FinanceReportFilters>) => void;
  setPage: (page: number) => void;
  setLimit: (limit: number) => void;
  //   resetFilters: () => void;

  // ---- detail actions ----
  fetchDetail: (budgetPlanId: number | string) => Promise<void>;
  resetDetail: () => void;
}

export const useRecapNonApdpStore = create<RecapNonAPDPState>((set, get) => ({
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
    const merged: RecapNonAPDPListParams = {
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
      const res = await RecapNonAPDPService.getList(merged);
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
      const res = await RecapNonAPDPService.getDetail(poId);
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
