import { useEffect } from "react";
import { useBudgetPlanDetailStore } from "../../store/detailBudgetPlanStore";

export function useBudgetPlanDetailController(
    id: string,
    onNavigate?: (pageId: string, payload?: Record<string, string>) => void
) {
    const { detail, isLoading, error, fetchDetail, clearDetail } = useBudgetPlanDetailStore();

    useEffect(() => {
        if (!id) return;
        void fetchDetail(id);

        return () => {
            clearDetail();
        };
    }, [clearDetail, fetchDetail, id]);

    const handleBack = () => {
        onNavigate?.("budgeting.plan.list");
    };

    const handleEdit = () => {
        onNavigate?.("budgeting.plan.edit", { id: String(detail?.id) });
    };

    const isApproved = detail?.status === "Approved";
    const isRejected = detail?.status === "Rejected";
    const isDraft = detail?.status === "Draft";
    const isSubmitted = detail?.status === "Submitted";
    const isPartialApproved = detail?.status === "PartialApproved";

    const sortedItems = detail?.items
        ? [...detail.items].sort((a, b) => a.sortOrder - b.sortOrder)
        : [];

    return {
        detail,
        isLoading,
        error,
        sortedItems,

        isApproved,
        isRejected,
        isDraft,
        isSubmitted,
        isPartialApproved,

        handleBack,
        handleEdit,
    };
}