import { useCallback, useEffect } from "react";
import type { RecapAPDPListParams } from "../../types/recapApdp.type";
import { useRecapApdpStore } from "../../store/recapApdpStore";
/**
 * Controller untuk halaman list finance report.
 * Otomatis fetch saat mount, menyediakan helper pagination & filter.
 */
export function useRecapApdpListController(
  initialParams?: RecapAPDPListParams,
) {
  const {
    list,
    meta,
    isListLoading,
    listError,
    fetchList,
    setPage,
    setLimit,
  } = useRecapApdpStore();

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
    useRecapApdpStore();

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
