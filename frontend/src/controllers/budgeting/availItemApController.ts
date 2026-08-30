// controllers/useAvailableItemController.ts

import { useEffect } from "react";
import { useAvailableItemStore } from "../../store/availItemApStore";

export const useAvailableItemController = (
  vendorShadowId: number | null,
  accountPayableId?: number,
) => {
  const {
    items,
    isLoading,
    error,
    selectedRowIds,
    fetchAvailableItems,
    setItems,
    toggleCheck,
  } = useAvailableItemStore();

  useEffect(() => {
    if (vendorShadowId && !accountPayableId) {
      fetchAvailableItems(vendorShadowId);
    }
  }, [vendorShadowId, accountPayableId, fetchAvailableItems]);

  const grandTotal = items.reduce((sum, item) => sum + item.budgetPlanTotal, 0);

  return {
    items,
    isLoading,
    error,
    selectedRowIds,
    setItems,
    toggleCheck,
    grandTotal,
  };
};
