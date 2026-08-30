import { useCallback, useEffect } from "react";

import { useRecapWorkOrderStore } from "../../store/recapWoStore";
import type { RecapWorkOrderSortBy } from "../../api/services/operationalRealization/recapWoService";
import { useWarehouseStore } from "../../store/warehouseStore";

export const useRecapWorkOrderController = () => {
  const {
    recapWorkOrders,

    params,

    selectedRecapWorkOrder,

    loading,

    detailLoading,

    actionLoading,

    fetchRecapWorkOrders,

    fetchRecapWorkOrderDetail,

    approveRecapWorkOrder,

    rejectRecapWorkOrder,
  } = useRecapWorkOrderStore();

  const selectedWarehouse = useWarehouseStore(
          (state) => state.selectedWarehouse,
        );

  useEffect(()=>{
    fetchRecapWorkOrders();
  },[selectedWarehouse?.id])

  // ======================================================
  // LIST
  // ======================================================

  const handleFetchRecapWorkOrders = useCallback(async () => {
    await fetchRecapWorkOrders();
  }, [fetchRecapWorkOrders]);

  /**
   * The endpoint is paginated, so ordering is sent to the server — sorting the
   * rows already fetched would only reorder the page currently on screen.
   */
  const handleSortChange = useCallback(
    async (sortBy: RecapWorkOrderSortBy, sortOrder: "asc" | "desc") => {
      await fetchRecapWorkOrders({ sortBy, sortOrder, page: 1 });
    },
    [fetchRecapWorkOrders],
  );

  // ======================================================
  // DETAIL
  // ======================================================

  const handleGetDetail = useCallback(
    async (id: number) => {
      await fetchRecapWorkOrderDetail(id);
    },
    [fetchRecapWorkOrderDetail],
  );

  // ======================================================
  // APPROVE
  // ======================================================

  const handleApprove = useCallback(
    async (id: number) => {
      await approveRecapWorkOrder(id);

      // refresh list setelah approve
      await fetchRecapWorkOrders();
    },
    [approveRecapWorkOrder, fetchRecapWorkOrders],
  );

  // ======================================================
  // REJECT
  // ======================================================

  const handleReject = useCallback(
    async (id: number, reason: string) => {
      await rejectRecapWorkOrder(id, reason);

      // refresh list setelah reject
      await fetchRecapWorkOrders();
    },
    [rejectRecapWorkOrder, fetchRecapWorkOrders],
  );

  return {
    // ======================================================
    // STATES
    // ======================================================

    recapWorkOrders,

    params,

    selectedRecapWorkOrder,

    loading,

    detailLoading,

    actionLoading,

    // ======================================================
    // ACTIONS
    // ======================================================

    fetchRecapWorkOrders,

    fetchRecapWorkOrderDetail,

    approveRecapWorkOrder,

    rejectRecapWorkOrder,

    // ======================================================
    // HANDLERS
    // ======================================================

    handleFetchRecapWorkOrders,

    handleSortChange,

    handleGetDetail,

    handleApprove,

    handleReject,
  };
};
