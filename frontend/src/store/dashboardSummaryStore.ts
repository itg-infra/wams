import { create } from "zustand";
import type { DashboardSummary } from "../types/dashboardSummary.type";
import dashboardSummaryService from "../api/services/dashboard/dashboardSummaryService";

interface DashboardSummaryStore {
  summary: DashboardSummary | null;
  loading: boolean;
  error: string | null;

  getSummary: () => Promise<void>;
}

export const useDashboardSummaryStore = create<DashboardSummaryStore>(
  (set) => ({
    summary: null,
    loading: false,
    error: null,

    getSummary: async () => {
      try {
        set({
          loading: true,
          error: null,
        });

        const response = await dashboardSummaryService.getSummary();

        set({
          summary: response.data,
          loading: false,
        });
      } catch (error: any) {
        set({
          loading: false,
          error: error?.message ?? "Failed to load dashboard summary",
        });
      }
    },
  }),
);
