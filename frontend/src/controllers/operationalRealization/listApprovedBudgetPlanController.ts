import { useEffect, useState, useCallback, useMemo } from "react";
import { useApprovedBudgetPlansStore } from "../../store/listApprovedBudgetPlanStore";
import type { 
    ApprovedBudgetPlansQueryParams, 
    ApprovedBudgetPlanSortValue,
    ApprovedBudgetPlanItem,
} from "../../types/listApproveBudgetPlan.type";

export function useApprovedBudgetPlansController(initialParams?: ApprovedBudgetPlansQueryParams) {
    const store = useApprovedBudgetPlansStore();
    const [searchInput, setSearchInput] = useState("");
    const [sortLabel, setSortLabel] = useState("Latest");

    // Map sort value to display label - wrapped in useMemo
    const sortLabels = useMemo(() => ({
        latest: "Latest",
        oldest: "Oldest",
        name_asc: "Name A-Z",
        name_desc: "Name Z-A",
    }), []);

    // Initial fetch - only depend on initialParams
    useEffect(() => {
        store.fetchPlans(initialParams);
    }, [initialParams]);

    const handleSearchChange = useCallback((value: string) => {
        setSearchInput(value);
        const store = useApprovedBudgetPlansStore.getState();
        store.setSearch(value);
    }, []);

    const handleSortChange = useCallback((value: ApprovedBudgetPlanSortValue) => {
        setSortLabel(sortLabels[value] || "Latest");
        const store = useApprovedBudgetPlansStore.getState();
        store.setSortBy(value);
    }, [sortLabels]);

    const handlePrevPage = useCallback(() => {
        const store = useApprovedBudgetPlansStore.getState();
        if (store.currentPage > 1) {
            store.setPage(store.currentPage - 1);
        }
    }, []);

    const handleNextPage = useCallback(() => {
        const store = useApprovedBudgetPlansStore.getState();
        const totalPages = Math.ceil(store.total / store.limit);
        if (store.currentPage < totalPages) {
            store.setPage(store.currentPage + 1);
        }
    }, []);

    const handleCreateWO = useCallback((item: ApprovedBudgetPlanItem) => {
        // TODO: Navigate to create work order screen
        console.log("Create WO for:", item);
    }, []);

    // Transform data untuk UI
    const transformedPlans = store.plans.map((plan) => ({
        ...plan,
        id: plan.budgetPlanId,
        templateId: plan.templateCode,
        templateName: plan.activityTypeName,
        location: plan.warehouseCode,
        date: plan.docDate,
    }));

    const totalPages = Math.ceil(store.total / store.limit);
    const from = store.total === 0 ? 0 : (store.currentPage - 1) * store.limit + 1;
    const to = Math.min(store.currentPage * store.limit, store.total);

    return {
        // State untuk UI
        approvedBudgetPlans: transformedPlans,
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

        // Handlers untuk UI
        handleSearchChange,
        handleSortChange,
        handlePrevPage,
        handleNextPage,
        handleCreateWO,

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