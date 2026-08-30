// api/services/budgetPlanService.ts
import type { BudgetPlanResponse } from "../../../../types/detailBudgetPlan.type";
import axiosProvider from "../../../providers/axiosProvider";

export const budgetPlanService = {
    async getBudgetPlanDetail(id: string): Promise<BudgetPlanResponse> {
        const response = await axiosProvider.get<{ data: BudgetPlanResponse }>(
            `api/v1/budget-plans/${id}`
        );

        return response.data.data;
    },
};