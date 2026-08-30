import { create } from "zustand";
import { workOrderController } from "../controllers/operationalRealization/detailWoController";
import type { WorkOrderDetail } from "../types/detailWo.type";

interface WorkOrderState {
  data: WorkOrderDetail | null;
  isLoading: boolean;
  error: string | null;

  getDetail: (id: number) => Promise<void>;
  reset: () => void;
}

export const useWorkOrderStore = create<WorkOrderState>((set) => ({
  data: null,
  isLoading: false,
  error: null,

  getDetail: async (id: number) => {
    try {
      set({
        isLoading: true,
        error: null,
      });

      const response = await workOrderController.getDetail(id);

      set({
        data: response.data,
        isLoading: false,
      });
    } catch (error: any) {
      set({
        isLoading: false,
        error:
          error?.response?.data?.message ?? "Failed to get work order detail",
      });
    }
  },

  reset: () =>
    set({
      data: null,
      error: null,
      isLoading: false,
    }),
}));
