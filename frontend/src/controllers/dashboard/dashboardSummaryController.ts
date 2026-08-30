import { useEffect } from "react";
import { useDashboardSummaryStore } from "../../store/dashboardSummaryStore";

export function useDashboardSummaryController() {
  const summary = useDashboardSummaryStore((state) => state.summary);
  const loading = useDashboardSummaryStore((state) => state.loading);
  const error = useDashboardSummaryStore((state) => state.error);
  const getSummary = useDashboardSummaryStore((state) => state.getSummary);

  useEffect(() => {
    getSummary();
  }, [getSummary]);

  return {
    summary,
    loading,
    error,
  };
}
