import { create } from "zustand";
import type {
    ApprovedBudgetPlanItem,
    ApprovedBudgetPlansQueryParams,
} from "../types/listApproveBudgetPlan.type";
import { approvedBudgetPlanService } from "../api/services/operationalRealization/listApprovedBudgetPlanService";

interface ApprovedBudgetPlansStoreState {
    // State
    plans: ApprovedBudgetPlanItem[];
    isLoading: boolean;
    error: string | null;
    
    // Pagination & Sorting
    currentPage: number;
    limit: number;
    total: number;
    lastUpdated: string;
    sortBy: ApprovedBudgetPlansQueryParams["sortBy"];
    search: string;

    // Actions
    fetchPlans: (params?: ApprovedBudgetPlansQueryParams) => Promise<void>;
    setPage: (page: number) => void;
    setLimit: (limit: number) => void;
    setSearch: (search: string) => void;
    setSortBy: (sortBy: ApprovedBudgetPlansQueryParams["sortBy"]) => void;
    reset: () => void;
}

const initialState = {
    plans: [] as ApprovedBudgetPlanItem[],
    isLoading: false,
    error: null,
    currentPage: 1,
    limit: 20,
    total: 0,
    lastUpdated: "-",
    sortBy: "latest" as const,
    search: "",
};

export const useApprovedBudgetPlansStore = create<ApprovedBudgetPlansStoreState>((set, get) => ({
    ...initialState,

    fetchPlans: async (params?: ApprovedBudgetPlansQueryParams) => {
        set({ isLoading: true, error: null });

        try {
            const state = get();
            const response = await approvedBudgetPlanService.getApprovedBudgetPlans({
                search: params?.search ?? state.search,
                sortBy: params?.sortBy ?? state.sortBy,
                page: params?.page ?? state.currentPage,
                limit: params?.limit ?? state.limit,
            });

            set({
                plans: response.data,
                total: response.meta.total,
                lastUpdated: response.meta.lastUpdated,
                currentPage: params?.page ?? state.currentPage,
                isLoading: false,
            });
        } catch (err) {
            const errorMessage = err instanceof Error ? err.message : "Failed to fetch approved budget plans";
            set({ error: errorMessage, isLoading: false });
            console.error("Error fetching approved budget plans:", err);
        }
    },

    setPage: (page: number) => {
        set({ currentPage: page });
        get().fetchPlans({ page });
    },

    setLimit: (limit: number) => {
        set({ limit, currentPage: 1 });
        get().fetchPlans({ limit, page: 1 });
    },

    setSearch: (search: string) => {
        set({ search, currentPage: 1 });
        get().fetchPlans({ search, page: 1 });
    },

    setSortBy: (sortBy: ApprovedBudgetPlansQueryParams["sortBy"]) => {
        set({ sortBy, currentPage: 1 });
        get().fetchPlans({ sortBy, page: 1 });
    },

    reset: () => {
        set({ ...initialState });
    },
}));