import { useEffect } from "react";
import { useBudgetTemplateFormStore } from "../../store/budgetTemplateFormStore";

export function useBudgetTemplateFormController() {
    const {
        form,
        templateNameOptions,
        warehouseOptions,
        costDetailOptions,
        coaOptions,
        isLoading,
        isSubmitting,
        error,
        fetchForm,
        updateField,
        updateItemField,
        addItem,
        removeItem,
        applyWarehouse,
        applyCoaAutoFill,
        submitForm,
        draftForm,
    } = useBudgetTemplateFormStore();

    useEffect(() => {
        fetchForm();
    }, [fetchForm]);

    const handleTemplateNameChange = (value: string) => {
        updateField("templateName", value);
    };

    const handleWarehouseCodeChange = (value: string) => {
        applyWarehouse(value);
    };

    const handleCostDetailChange = (itemId: string, value: string) => {
        updateItemField(itemId, "costDetail", value);
    };

    const handleCostNameChange = (itemId: string, value: string) => {
        updateItemField(itemId, "costName", value);
    };

    const handleCoaChange = (itemId: string, value: string) => {
        applyCoaAutoFill(itemId, value);
    };

    const handleCoaNameChange = (itemId: string, value: string) => {
        updateItemField(itemId, "coaName", value);
    };

    const handleAddItem = () => {
        addItem();
    };

    const handleRemoveItem = (itemId: string) => {
        removeItem(itemId);
    };

    const handleSubmit = async () => {
        await submitForm();
    };

    const handleDraft = async () => {
        await draftForm();
    };

    return {
        form,
        templateNameOptions,
        warehouseOptions,
        costDetailOptions,
        coaOptions,
        isLoading,
        isSubmitting,
        error,

        handleTemplateNameChange,
        handleWarehouseCodeChange,
        handleCostDetailChange,
        handleCostNameChange,
        handleCoaChange,
        handleCoaNameChange,
        handleAddItem,
        handleRemoveItem,
        handleSubmit,
        handleDraft,
    };
}