import { useEffect } from "react";
import { useBudgetRealizationStore } from "../../store/budgetRealizationStore";
import { budgetTemplateService } from "../../api/services/budgeting/budgetTemplate/budgetTemplateService";

export function useBudgetRealizationController(templateId?: string) {
    const {
        rows,
        isDirty,
        isSubmitting,
        submitError,
        initFromTemplate,
        resetForm,
        clearSubmitError,
    } = useBudgetRealizationStore();

    useEffect(() => {
        if (!templateId) return;

        let cancelled = false;

        async function load() {
            try {
                const detail = await budgetTemplateService.getBudgetTemplateDetail(templateId!);
                if (!cancelled) {
                    initFromTemplate(detail);
                }
            } catch (err) {
                console.error("Failed to load budget template detail:", err);
            }
        }

        load();

        return () => {
            cancelled = true;
        };
    }, [templateId, initFromTemplate]);

    /**
     * Collect the current row state for submission.
     * Adapt this to match your actual submit API shape.
     */
    function getSubmitPayload() {
        return {
            budgetTemplateId: templateId,
            items: rows.map((row) => ({
                itemShadowId: row.itemShadowId,
                type: row.type,
                vendorId: row.vendorId,
                isRfba: row.isRfba,
                docExternal: row.docExternal,
                billOfLading: row.billOfLading,
                uomId: row.uomId,
                unitCost: row.unitCost,
                unitCount: row.unitCount,
                description: row.description,
            })),
        };
    }

    return {
        rows,
        isDirty,
        isSubmitting,
        submitError,
        resetForm,
        clearSubmitError,
        getSubmitPayload,
    };
}