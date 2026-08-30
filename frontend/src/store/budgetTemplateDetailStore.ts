import { create } from "zustand";
import { budgetTemplateService } from "../api/services/budgeting/budgetTemplate/budgetTemplateService";
import type { BudgetTemplateDetailItem } from "../types/budgetTemplate.type";

interface BudgetTemplateDetailStoreState {
    detail: BudgetTemplateDetailItem | null;
    isLoading: boolean;
    error: string | null;

    fetchDetail: (id: string) => Promise<void>;
    clearDetail: () => void;

    isApproved: boolean;
    approvedBudgetTemplate: (id?: string) => Promise<void>;
}

export const useBudgetTemplateDetailStore = create<BudgetTemplateDetailStoreState>((set) => ({
    detail: null,
    isLoading: false,
    error: null,

    fetchDetail: async (id: string) => {
        set({
            isLoading: true,
            error: null,
        });

        try {
            const response = await budgetTemplateService.getBudgetTemplateDetail(id);

            set({
                detail: response,
                isLoading: false,
            });
        } catch (error) {
            set({
                isLoading: false,
                error: error instanceof Error ? error.message : "Failed to fetch template detail",
            });
        }
    },

    clearDetail: () =>
        set({
            detail: null,
            error: null,
            isLoading: false,
        }),

    isApproved: false,

    approvedBudgetTemplate: async (id?: string) => {
        set({
            isLoading: true,
            error: null,
        })
        try {
            const response = await budgetTemplateService.budgetTemplateApproved(id);
            console.log(response);
        } catch (error) {
            set({
                isLoading: false,
                error: error instanceof Error ? error.message : "Failed to fetch template detail",
            });
        }
    }
}));