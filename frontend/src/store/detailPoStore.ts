import { create } from "zustand";
import { approvedBudgetPlanService } from "../api/services/budgeting/purchaseOrders/listGeneratePoService";
import type { PurchaseOrderDetail } from "../api/services/budgeting/purchaseOrders/detailPoService";

interface DetailPoState {
  detail: PurchaseOrderDetail | null;
  isLoading: boolean;
  error: string | null;

  getDetail: (id: number) => Promise<void>;
  reset: () => void;
}

export const useDetailPoStore = create<DetailPoState>((set) => ({
  detail: null,
  isLoading: false,
  error: null,

  getDetail: async (id: number) => {
    try {
      set({
        isLoading: true,
        error: null,
      });

      const detail = await approvedBudgetPlanService.getPurchaseOrderDetail(id);

      set({
        detail,
        isLoading: false,
      });
    } catch (error: any) {
      set({
        detail: null,
        isLoading: false,
        error:
          error?.response?.data?.message ??
          error?.message ??
          "Failed to load purchase order detail",
      });
    }
  },

  reset: () =>
    set({
      detail: null,
      isLoading: false,
      error: null,
    }),
}));
