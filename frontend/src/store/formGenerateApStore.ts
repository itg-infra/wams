import { create } from "zustand";
import type { BudgetPlanDetailItem } from "../types/budgetPlanDetial.type";
import {
  generateApServices,
  type CreatePurchaseOrderPayload,
  type CreateWorkOrderPayload,
} from "../api/services/budgeting/accountPayable/formGenerateApService";

interface GenerateApStore {
  availableItems: BudgetPlanDetailItem[];
  availableItemsLoading: boolean;
  // ✅ Return BudgetPlanDetailItem[] bukan void
  fetchAvailableItems: (
    vendorShadowId: number,
    budgetPlanId: number,
    accountPayableId?: number,
  ) => Promise<BudgetPlanDetailItem[]>;
  clearAvailableItems: () => void;

  submitLoading: boolean;
  submitGeneratePO: (payload: CreatePurchaseOrderPayload) => Promise<boolean>;

  draftLoading: boolean;
  draftGeneratePO: (payload: CreatePurchaseOrderPayload) => Promise<boolean>;

  generateApLoading: boolean;
  createWorkOrder: (payload: CreateWorkOrderPayload) => Promise<boolean>;

  error: string | null;
  successMessage: string | null;
  setError: (value: string | null) => void;
  setSuccessMessage: (value: string | null) => void;
}

export const useGenerateApStore = create<GenerateApStore>((set) => ({
  availableItems: [],
  availableItemsLoading: false,

  // ✅ Sekarang return data-nya
  fetchAvailableItems: async (vendorShadowId, budgetPlanId, accountPayableId) => {
    try {
      set({ availableItemsLoading: true, error: null });

      const data = await generateApServices.fetchAvailableItems(
        vendorShadowId,
        budgetPlanId,
        accountPayableId,
      );

      set({ availableItems: data });

      return data; // ✅ return data
    } catch (error) {
      set({
        error:
          error instanceof Error
            ? error.message
            : "Failed fetch available items",
      });
      return []; // ✅ return empty array on error
    } finally {
      set({ availableItemsLoading: false });
    }
  },

  clearAvailableItems: () => set({ availableItems: [] }),

  submitLoading: false,
  submitGeneratePO: async (payload) => {
    try {
      set({ submitLoading: true, error: null, successMessage: null });
      const response = await generateApServices.submitGeneratePO(payload);
      set({ successMessage: response.message ?? "Generate PO success" });
      return true;
    } catch (error) {
      set({
        error:
          error instanceof Error ? error.message : "Failed submit generate PO",
      });
      return false;
    } finally {
      set({ submitLoading: false });
    }
  },

  draftLoading: false,
  draftGeneratePO: async (payload) => {
    try {
      set({ draftLoading: true, error: null, successMessage: null });
      const response = await generateApServices.draftGeneratePO(payload);
      set({ successMessage: response.message ?? "Draft saved successfully" });
      return true;
    } catch (error) {
      set({
        error:
          error instanceof Error ? error.message : "Failed draft generate PO",
      });
      return false;
    } finally {
      set({ draftLoading: false });
    }
  },

  generateApLoading: false,
  createWorkOrder: async (payload) => {
    try {
      set({ generateApLoading: true, error: null, successMessage: null });
      const response = await generateApServices.createWorkOrder(payload);
      set({ successMessage: response.message ?? "Generate AP success" });
      return true;
    } catch (error) {
      set({
        error: error instanceof Error ? error.message : "Failed generate AP",
      });
      return false;
    } finally {
      set({ generateApLoading: false });
    }
  },

  error: null,
  successMessage: null,
  setError: (value) => set({ error: value }),
  setSuccessMessage: (value) => set({ successMessage: value }),
}));
