import { useCallback } from "react";
import { getHistoryActivities } from "../../api/services/dashboard/historyActivityService";
import { useHistoryActivityStore } from "../../store/historyActivityStore";

export const useHistoryActivityController = () => {
  const {
    activities,
    meta,
    isLoadingHistory,
    error,
    page,
    limit,
    setActivities,
    setLoading,
    setError,
    setPage,
  } = useHistoryActivityStore();

  const fetchActivities = useCallback(
    async (currentPage = page) => {
      try {
        setLoading(true);
        setError(null);

        const response = await getHistoryActivities({
          page: currentPage,
          limit,
        });

        setActivities(response.data, response.meta);
      } catch (err: any) {
        setError(err?.response?.data?.message ?? "Failed to load activities.");
      } finally {
        setLoading(false);
      }
    },
    [page, limit],
  );

  const handlePageChange = async (newPage: number) => {
    if (!meta) return;

    if (newPage < 1 || newPage > meta.totalPages) return;

    setPage(newPage);

    await fetchActivities(newPage);
  };

  return {
    activities,
    meta,
    isLoadingHistory,
    error,
    page,

    fetchActivities,
    handlePageChange,
  };
};
