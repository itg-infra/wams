import { create } from "zustand";
import type { BudgetPlanResponse } from "../types/detailBudgetPlan.type";
import { budgetPlanService } from "../api/services/budgeting/budgetPlan/detailBudgetPlanService";

interface BudgetPlanDetailStoreState {
    detail: BudgetPlanResponse | null;
    isLoading: boolean;
    error: string | null;

    fetchDetail: (id: string) => Promise<void>;
    clearDetail: () => void;
}

export const useBudgetPlanDetailStore = create<BudgetPlanDetailStoreState>((set) => ({
    detail: null,
    isLoading: false,
    error: null,

    fetchDetail: async (id: string) => {
        set({
            isLoading: true,
            error: null,
        });

        try {
            const response = await budgetPlanService.getBudgetPlanDetail(id);

            set({
                detail: response,
                isLoading: false,
            });
        } catch (error) {
            set({
                isLoading: false,
                error: error instanceof Error ? error.message : "Failed to fetch budget plan detail",
            });
        }
    },

    clearDetail: () =>
        set({
            detail: null,
            error: null,
            isLoading: false,
        }),
}));