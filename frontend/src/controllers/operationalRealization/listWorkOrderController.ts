// ======================================================
// src/controllers/workOrderController.ts
// ======================================================

import { useCallback, useEffect } from "react";
import { deleteWorkOrder } from "../../api/services/operationalRealization/listWorkOrderService";
import { useWorkOrderStore } from "../../store/workOrderStore";

import type { WorkOrderQueryParams } from "../../types/workOrder.type";
import { confirmationDialog } from "../../components/confirmationDelet";
import { useWarehouseStore } from "../../store/warehouseStore";

export const useWorkOrderController = () => {
  const {
    workOrders,

    total,

    totalPages,

    page,

    limit,

    isLoading,

    error,

    params,

    fetchWorkOrders,

    setParams,

    selectedWorkOrder,

    isDetailLoading,

    fetchWorkOrderDetail,

    clearSelectedWorkOrder,

    deleteLoadingId,
    setDeleteLoadingId,

    errorMessage,
    setErrorMessage,
  } = useWorkOrderStore();

  const selectedWarehouse = useWarehouseStore(
    (state) => state.selectedWarehouse,
  );

  useEffect(() => {
    fetchWorkOrders();
  }, [selectedWarehouse?.id]);

  // delete
  const handleDeleteWorkOrder = useCallback(
    async (id: number) => {
      const confirmed = await confirmationDialog({
        title: "Delete Work Order?",
        text: "This action cannot be undone.",
        confirmText: "Delete",
      });

      if (!confirmed) {
        return;
      }

      try {
        setErrorMessage(null);

        setDeleteLoadingId(id);

        await deleteWorkOrder(id);

        await fetchWorkOrders();
      } catch (error) {
        if (error instanceof Error) {
          setErrorMessage(error.message);
        } else {
          setErrorMessage("Failed to delete work order");
        }
      } finally {
        setDeleteLoadingId(null);
      }
    },
    [fetchWorkOrders, setDeleteLoadingId, setErrorMessage],
  );

  const clearError = () => {
    setErrorMessage("");
  };

  // ======================================================
  // LIST
  // ======================================================

  const handleSearch = async (search: string) => {
    await fetchWorkOrders({
      search,
      page: 1,
    });
  };

  const handleFilter = async (filters: Partial<WorkOrderQueryParams>) => {
    await fetchWorkOrders({
      ...filters,
      page: 1,
    });
  };

  const handlePageChange = async (nextPage: number) => {
    await fetchWorkOrders({
      page: nextPage,
    });
  };

  const handleSort = async (
    sortBy: "status" | "startDate" | "createdAt",

    sortOrder: "asc" | "desc",
  ) => {
    await fetchWorkOrders({
      sortBy,
      sortOrder,
      page: 1,
    });
  };

  // ======================================================
  // DETAIL
  // ======================================================

  const handleGetDetail = async (id: number) => {
    await fetchWorkOrderDetail(id);
  };

  return {
    // ======================================================
    // LIST
    // ======================================================

    workOrders,

    total,

    totalPages,

    page,

    limit,

    isLoading,

    error,

    params,

    fetchWorkOrders,

    setParams,

    handleSearch,

    handleFilter,

    handlePageChange,

    handleSort,

    // ======================================================
    // DETAIL
    // ======================================================

    selectedWorkOrder,

    isDetailLoading,

    fetchWorkOrderDetail,

    clearSelectedWorkOrder,

    handleGetDetail,

    handleDeleteWorkOrder,

    deleteLoadingId,

    errorMessage,

    clearError,
  };
};
