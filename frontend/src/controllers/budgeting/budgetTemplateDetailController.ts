import { useEffect } from "react";
import { useBudgetTemplateDetailStore } from "../../store/budgetTemplateDetailStore";

export function useBudgetTemplateDetailController(id: string) {
    const {
        detail,
        isLoading,
        error,
        fetchDetail,
        clearDetail,
        approvedBudgetTemplate,
        isApproved,
    } = useBudgetTemplateDetailStore();

    useEffect(() => {
        if (!id) return;

        void fetchDetail(id);

        return () => {
            clearDetail();
        };
    }, [id, fetchDetail, clearDetail]);

    return {
        detail,
        isLoading,
        error,
        approvedBudgetTemplate,
        isApproved,
    };
}