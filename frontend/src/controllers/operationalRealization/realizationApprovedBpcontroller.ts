import { useCallback, useEffect, useMemo, useState } from "react";
import { useRealizationApprovedBpstore } from "../../store/realizaitonListApprovedBpStore";
import type {
  RealizationApprovedBpApiItem,
  RealizationApprovedBpSortValue,
  RealizationApprovedBudgetPlansQueryParams,
} from "../../types/realizationApprovedBp.type";
import { useNavigate } from "react-router-dom";
import { useWarehouseStore } from "../../store/warehouseStore";

export function useRealizationApprovedBpController(
  initialParams?: RealizationApprovedBudgetPlansQueryParams,
) {
  const store = useRealizationApprovedBpstore();
  const [searchInput, setSearchInput] = useState("");
  const [sortLabel, setSortLabel] = useState("Latest");

  const navigate = useNavigate();

  const sortLabels = useMemo(
    () => ({
      latest: "Latest",
      oldest: "Oldest",
      name_asc: "Name A-Z",
      name_desc: "Name Z-A",
    }),
    [],
  );
  const selectedWarehouse = useWarehouseStore(
    (state) => state.selectedWarehouse,
  );

  useEffect(() => {
    store.fetchPlans(initialParams);
  }, [selectedWarehouse?.id]);

  const handleFetchAvailableItems = useCallback(
    async (
      vendorShadowId: number,
      budgetPlanId: number,
      purchaseOrderId?: number,
    ) => {
      // Teruskan error ke form agar UI dapat menampilkan status gagal
      return useRealizationApprovedBpstore.getState().fetchAvailableItems(
        vendorShadowId,
        budgetPlanId,
        purchaseOrderId,
      );
    },
    [],
  );

  const handleSearchChange = useCallback((value: string) => {
    setSearchInput(value);
    const store = useRealizationApprovedBpstore.getState();
    store.setSearch(value);
  }, []);

  const handleSortChange = useCallback(
    (value: RealizationApprovedBpSortValue) => {
      setSortLabel(sortLabels[value] || "Latest");
      const store = useRealizationApprovedBpstore.getState();
      store.setSortBy(value);
    },
    [sortLabels],
  );

  const handlePrevPage = useCallback(() => {
    const store = useRealizationApprovedBpstore.getState();
    if (store.currentPage > 1) {
      store.setPage(store.currentPage - 1);
    }
  }, []);

  const handleCreateWO = useCallback(
    (item: RealizationApprovedBpApiItem) => {
      console.log("isLocked:", item.isLocked); // cek nilai ini
      if (item.isLocked) return;

      navigate(`/work-orders/create/${item.budgetPlanId}`, {
        state: {
          budgetPlan: item,
        },
      });
    },
    [navigate],
  );

  const handleNextPage = useCallback(() => {
    const store = useRealizationApprovedBpstore.getState();
    const totalPages = Math.ceil(store.total / store.limit);
    if (store.currentPage < totalPages) {
      store.setPage(store.currentPage + 1);
    }
  }, []);

  const transformedPlans = store.plans.map((plan) => ({
    ...plan,
    id: plan.budgetPlanId,
    templateId: plan.templateCode,
    templateName: plan.activityTypeName,
    date: plan.docDate,
  }));

  const totalPages = Math.ceil(store.total / store.limit);
  const from =
    store.total === 0 ? 0 : (store.currentPage - 1) * store.limit + 1;
  const to = Math.min(store.currentPage * store.limit, store.total);

  return {
    realizationApprovedBp: transformedPlans,
    isLoading: store.isLoading,
    error: store.error,
    searchInput,
    sortLabel,
    page: store.currentPage,
    totalPages,
    total: store.total,
    from,
    to,
    lastUpdated: store.lastUpdated,

    handleSearchChange,
    handleSortChange,
    handlePrevPage,
    handleNextPage,
    handleCreateWO,

    availableItems: store.availableItems,
    availableItemsLoading: store.availableItemsLoading,
    fetchAvailableItems: handleFetchAvailableItems,

    // Jika diperlukan akses langsung ke store actions
    fetchPlans: store.fetchPlans,
    setPage: store.setPage,
    setLimit: store.setLimit,
    setSearch: store.setSearch,
    setSortBy: store.setSortBy,
    refresh: () => store.fetchPlans(),
    reset: store.reset,
  };
}
