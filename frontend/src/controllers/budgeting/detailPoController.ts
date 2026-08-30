import { useCallback } from "react";
import { useDetailPoStore } from "../../store/detailPoStore";

export const useDetailPoController = () => {
  const { detail, isLoading, error, getDetail, reset } = useDetailPoStore();

  const loadDetail = useCallback(
    async (id: number) => {
      await getDetail(id);
    },
    [getDetail],
  );

  return {
    detail,
    isLoading,
    error,

    loadDetail,
    reset,
  };
};
