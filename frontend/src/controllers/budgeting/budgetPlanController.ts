import { useCallback, useEffect, useRef, useState } from "react";
import { budgetPlanService } from "../../api/services/budgeting/budgetPlan/budgetPlanService";
import { useBudgetPlanStore } from "../../store/budgetPlanStore";
import type {
  BudgetPlanQueryParams,
  BudgetPlanStatus,
  BudgetPlanType,
} from "../../types/budgetPlan.type";
import { useWarehouseStore } from "../../store/warehouseStore";

interface UseBudgetPlanControllerOptions {
  initialStatus?: BudgetPlanStatus | "";
}

export function useBudgetPlanController(
  options?: UseBudgetPlanControllerOptions,
) {
  const {
    plans,
    meta,
    isLoading,
    error,
    params,
    setPlans,
    setMeta,
    setLoading,
    setError,
    setSearch,

    // --- PENYESUAIAN SORTING ---
    setSortBy,
    setSortOrder, // <--- Ekstrak ini dari store

    setPage,
    setStatusFilter,
    setTypeFilter,
    resetParams,
  } = useBudgetPlanStore();

  const selectedWarehouse = useWarehouseStore(
    (state) => state.selectedWarehouse,
  );

  const hasAppliedInitialStatus = useRef(false);

  const fetchPlans = useCallback(
    async (overrideParams?: Partial<BudgetPlanQueryParams>) => {
      setLoading(true);
      setError(null);

      // merge dengan params store, JANGAN gantikan seluruhnya,
      // supaya filter yang sedang aktif (sort/status/type) tidak hilang
      const finalParams = { ...params, ...overrideParams };

      try {
        const result = await budgetPlanService.getBudgetPlans(finalParams);
        setPlans(result.data);
        setMeta(result.meta);
      } catch (err) {
        const message =
          err instanceof Error ? err.message : "Gagal memuat data budget plan.";
        setError(message);
      } finally {
        setLoading(false);
      }
    },
    [params, setError, setLoading, setMeta, setPlans],
  );

  const [searchInput, setSearchInput] = useState(params.search ?? "");

  const fetchPlansRef = useRef(fetchPlans);
  useEffect(() => {
    fetchPlansRef.current = fetchPlans;
  }, [fetchPlans]);

  // Efek khusus: fetch ulang saat warehouse berubah
  useEffect(() => {
    // void fetchPlansRef.current({ page: 1, search: searchInput });

    const initialStatus = options?.initialStatus;
    if (!hasAppliedInitialStatus.current && initialStatus) {
      hasAppliedInitialStatus.current = true;
      setStatusFilter(initialStatus); // sinkronkan ke store juga (untuk UI filter/tab kalau ada)
      void fetchPlansRef.current({
        page: 1,
        search: searchInput,
        status: initialStatus,
      });
      return;
    }

    if (!hasAppliedInitialStatus.current && !initialStatus) {
      hasAppliedInitialStatus.current = true;
      setStatusFilter("");
      void fetchPlansRef.current({ page: 1, search: searchInput, status: "" });
      return;
    }
    void fetchPlansRef.current({ page: 1, search: searchInput });
  }, [selectedWarehouse?.id]); // eslint-disable-line react-hooks/exhaustive-deps

  // Efek khusus: debounce fetch saat user mengetik di search box
  useEffect(() => {
    const handler = setTimeout(() => {
      void fetchPlansRef.current({ search: searchInput, page: 1 });
    }, 400);
    return () => clearTimeout(handler);
  }, [searchInput]);

  const handleSearchChange = (value: string) => {
    setSearchInput(value);
    setSearch(value); // update params.search di store (untuk konsistensi state)
  };

  // --- HANDLER SORT & ORDER ---
  const handleSortChange = (
    value: NonNullable<BudgetPlanQueryParams["sortBy"]>,
  ) => {
    setSortBy(value);
    void fetchPlans({ sortBy: value, page: 1, search: searchInput });
  };

  const handleOrderChange = (value: "asc" | "desc") => {
    setSortOrder(value);
    void fetchPlans({ sortOrder: value, page: 1, search: searchInput });
  };
  // -----------------------------

  const handleStatusFilter = (status: BudgetPlanStatus | "") => {
    setStatusFilter(status);
    void fetchPlans({ status, page: 1, search: searchInput });
  };

  const handleTypeFilter = (type: BudgetPlanType | "") => {
    setTypeFilter(type);
    void fetchPlans({ type, page: 1, search: searchInput });
  };

  const handlePrevPage = () => {
    const currentPage = params.page ?? 1;
    if (currentPage <= 1) return;
    const nextPage = currentPage - 1;
    setPage(nextPage);
    void fetchPlans({ page: nextPage, search: searchInput });
  };

  const handleNextPage = () => {
    const currentPage = params.page ?? 1;
    const totalPages = meta?.totalPages ?? 1;
    if (currentPage >= totalPages) return;
    const nextPage = currentPage + 1;
    setPage(nextPage);
    void fetchPlans({ page: nextPage, search: searchInput });
  };

  const handlePageClick = (targetPage: number) => {
    const currentPage = params.page ?? 1;
    if (targetPage === currentPage) return;
    setPage(targetPage);
    void fetchPlans({ page: targetPage, search: searchInput });
  };

  const handleReset = () => {
    resetParams(); // Ini sudah mereset params di store ke defaultParams ("docDate" & "desc")
    setSearchInput("");

    // Pastikan override fetch mengikuti default yang baru
    void fetchPlans({
      ...params,
      search: "",
      page: 1,
      sortBy: "docDate", // <--- UPDATE: Sebelumnya "latest"
      sortOrder: "desc", // <--- UPDATE: Tambahkan default order
      status: "",
      type: "",
    });
  };

  const handleRefresh = useCallback(() => {
    void fetchPlans();
  }, [fetchPlans]);

  return {
    plans,
    meta,
    isLoading,
    error,
    params,
    page: params.page ?? 1,
    totalPages: meta?.totalPages ?? 1,

    searchInput,
    handleSearchChange,

    // --- EXPORT HANDLER BARU KE UI ---
    handleSortChange,
    handleOrderChange,

    handlePrevPage,
    handleNextPage,
    handlePageClick,
    handleStatusFilter,
    handleTypeFilter,
    handleReset,
    handleRefresh,
  };
}
