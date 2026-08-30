import { useCallback, useEffect } from "react";
import { useRecapNonApdpStore } from "../../store/recapNonApdpStore";
import type { RecapNonAPDPListParams } from "../../types/recapNonApdp.type";
/**
 * Controller untuk halaman list finance report.
 * Otomatis fetch saat mount, menyediakan helper pagination & filter.
 */
export function useRecapNonApdpListController(
  initialParams?: RecapNonAPDPListParams,
) {
  const {
    list,
    meta,
    isListLoading,
    listError,
    fetchList,
    setPage,
    setLimit,
  } = useRecapNonApdpStore();

  useEffect(() => {
    fetchList(initialParams);
  }, []);

  /** Terapkan filter baru (activityTypeCode, warehouseCode, dateFrom, dateTo) lalu fetch ulang dari page 1 */
//   const applyFilters = useCallback(
//     (newFilters: Partial<FinanceReportListParams>) => {
//       setFilters(newFilters);
//       fetchList({ page: 1, ...filters, ...newFilters });
//     },
//     [fetchList, filters, setFilters],
//   );

  const goToPage = useCallback((page: number) => setPage(page), [setPage]);
  const changeLimit = useCallback(
    (limit: number) => setLimit(limit),
    [setLimit],
  );
  const refresh = useCallback(() => fetchList(), [fetchList]);
//   const clearFilters = useCallback(() => resetFilters(), [resetFilters]);

  return {
    data: list,
    meta,
    // filters,
    isLoading: isListLoading,
    error: listError,
    // applyFilters,
    goToPage,
    changeLimit,
    refresh,
    // clearFilters,
  };
}

/**
 * Controller untuk halaman detail finance report.
 * Fetch otomatis ketika budgetPlanId berubah, reset saat unmount.
 */
export function useFinanceReportDetailController(
  poId?: number | string,
) {
  const { detail, isDetailLoading, detailError, fetchDetail, resetDetail } =
    useRecapNonApdpStore();

  useEffect(() => {
    if (
      poId !== undefined &&
      poId !== null &&
      poId !== ""
    ) {
      fetchDetail(poId);
    }
    return () => {
      resetDetail();
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [poId]);

  const refresh = useCallback(() => {
    if (
      poId !== undefined &&
      poId !== null &&
      poId !== ""
    ) {
      fetchDetail(poId);
    }
  }, [poId, fetchDetail]);

  return {
    data: detail,
    isLoading: isDetailLoading,
    error: detailError,
    refresh,
  };
}
