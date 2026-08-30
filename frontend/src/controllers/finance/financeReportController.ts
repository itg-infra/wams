import { useCallback, useEffect } from "react";
import { useFinanceReportStore } from "../../store/financeReportStore";
import type {
  FinanceReportListParams,
  FinanceReportSortBy,
} from "../../types/financeReport.type";

/**
 * Controller untuk halaman list finance report.
 * Otomatis fetch saat mount, menyediakan helper pagination & filter.
 */
export function useFinanceReportListController(
  initialParams?: FinanceReportListParams,
) {
  const {
    list,
    meta,
    filters,
    sort,
    isListLoading,
    listError,
    fetchList,
    setFilters,
    setPage,
    setLimit,
    resetFilters,
  } = useFinanceReportStore();

  useEffect(() => {
    fetchList(initialParams);
  }, []);

  /** Terapkan filter baru (activityTypeCode, warehouseCode, dateFrom, dateTo) lalu fetch ulang dari page 1 */
  const applyFilters = useCallback(
    (newFilters: Partial<FinanceReportListParams>) => {
      setFilters(newFilters);
      fetchList({ page: 1, ...filters, ...newFilters });
    },
    [fetchList, filters, setFilters],
  );

  /**
   * The list is paginated by the API, so ordering is sent to the server rather
   * than applied to the page already in hand.
   */
  const handleSortChange = useCallback(
    (sortBy: FinanceReportSortBy, sortOrder: "asc" | "desc") => {
      fetchList({ page: 1, sortBy, sortOrder });
    },
    [fetchList],
  );

  const goToPage = useCallback((page: number) => setPage(page), [setPage]);
  const changeLimit = useCallback(
    (limit: number) => setLimit(limit),
    [setLimit],
  );
  const refresh = useCallback(() => fetchList(), [fetchList]);
  const clearFilters = useCallback(() => resetFilters(), [resetFilters]);

  return {
    data: list,
    meta,
    filters,
    sort,
    isLoading: isListLoading,
    error: listError,
    applyFilters,
    handleSortChange,
    goToPage,
    changeLimit,
    refresh,
    clearFilters,
  };
}

/**
 * Controller untuk halaman detail finance report.
 * Fetch otomatis ketika budgetPlanId berubah, reset saat unmount.
 */
export function useFinanceReportDetailController(
  budgetPlanId?: number | string,
) {
  const { detail, isDetailLoading, detailError, fetchDetail, resetDetail } =
    useFinanceReportStore();

  useEffect(() => {
    if (
      budgetPlanId !== undefined &&
      budgetPlanId !== null &&
      budgetPlanId !== ""
    ) {
      fetchDetail(budgetPlanId);
    }
    return () => {
      resetDetail();
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [budgetPlanId]);

  const refresh = useCallback(() => {
    if (
      budgetPlanId !== undefined &&
      budgetPlanId !== null &&
      budgetPlanId !== ""
    ) {
      fetchDetail(budgetPlanId);
    }
  }, [budgetPlanId, fetchDetail]);

  return {
    data: detail,
    isLoading: isDetailLoading,
    error: detailError,
    refresh,
  };
}
