import { useCallback } from "react";
import { useNavigate } from "react-router-dom";
import { useGenerateApStore } from "../../store/formGenerateApStore";
import type {
  CreatePurchaseOrderPayload,
  CreateWorkOrderPayload,
} from "../../api/services/budgeting/accountPayable/formGenerateApService";
import type { BudgetPlanDetailItem } from "../../types/budgetPlanDetial.type";

export const useGenerateApController = () => {
  const navigate = useNavigate();

  const {
    availableItems,
    availableItemsLoading,
    fetchAvailableItems,
    clearAvailableItems,
    submitLoading,
    submitGeneratePO,
    draftLoading,
    draftGeneratePO,
    generateApLoading,
    createWorkOrder,
    error,
    successMessage,
    setError,
    setSuccessMessage,
  } = useGenerateApStore();

  // ✅ Return data dari fetchAvailableItems
  const handleFetchAvailableItems = useCallback(
    async (
      vendorShadowId: number,
      budgetPlanId: number,
      accountPayableId?: number,
    ): Promise<BudgetPlanDetailItem[]> => {
      return await fetchAvailableItems(
        vendorShadowId,
        budgetPlanId,
        accountPayableId,
      );
    },
    [fetchAvailableItems],
  );

  const handleClearAvailableItems = useCallback(() => {
    clearAvailableItems();
  }, [clearAvailableItems]);

  const handleSubmitGeneratePO = useCallback(
    async (payload: CreatePurchaseOrderPayload) => {
      const success = await submitGeneratePO(payload);
      if (success) {
        navigate("/purchase-orders");
      }
      return success;
    },
    [submitGeneratePO, navigate],
  );

  const handleDraftGeneratePO = useCallback(
    async (payload: CreatePurchaseOrderPayload) => {
      return await draftGeneratePO(payload);
    },
    [draftGeneratePO],
  );

  const handleGenerateAp = useCallback(
    async (payload: CreateWorkOrderPayload) => {
      const success = await createWorkOrder(payload);
      if (success) {
        navigate("/account-payables");
      }
      return success;
    },
    [createWorkOrder, navigate],
  );

  const clearError = useCallback(() => setError(null), [setError]);
  const clearSuccessMessage = useCallback(
    () => setSuccessMessage(null),
    [setSuccessMessage],
  );

  return {
    availableItems,
    availableItemsLoading,
    fetchAvailableItems: handleFetchAvailableItems,
    clearAvailableItems: handleClearAvailableItems,
    submitLoading,
    submitGeneratePO: handleSubmitGeneratePO,
    draftLoading,
    draftGeneratePO: handleDraftGeneratePO,
    generateApLoading,
    createWorkOrder: handleGenerateAp,
    error,
    successMessage,
    clearError,
    clearSuccessMessage,
    setError,
    setSuccessMessage,
  };
};
