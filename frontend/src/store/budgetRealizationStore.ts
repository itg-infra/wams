import { create } from "zustand";
import type { RfbaRowItem, RfbaRowUpdatePayload } from "../types/budgetRealization.type";
import { mapTemplateItemToRfbaRow, createEmptyRfbaRow } from "../types/budgetRealization.type";
import type { BudgetTemplateDetailItem } from "../types/budgetTemplate.type";

interface BudgetRealizationStoreState {
    // State
    templateId: string | null;
    templateNumericId: number | null;
    rows: RfbaRowItem[];
    isDirty: boolean;
    grandTotal: number;

    // Submit state
    isSubmitting: boolean;
    submitError: string | null;

    // Actions
    initFromTemplate: (template: BudgetTemplateDetailItem) => void;
    initFromPlan: (rows: RfbaRowItem[]) => void; // ← tambah ini
    updateRow: (rowId: string, payload: RfbaRowUpdatePayload) => void;
    addRow: () => void;
    removeRow: (rowId: string) => void;
    getRowById: (rowId: string) => RfbaRowItem | undefined;
    resetForm: () => void;
    clearSubmitError: () => void;
}

function computeGrandTotal(rows: RfbaRowItem[]): number {
    return rows.reduce((sum, row) => {
        const cost = row.unitCost ?? 0;
        const count = row.unitCount ?? 0;
        return sum + cost * count;
    }, 0);
}

const initialState = {
    templateId: null,
     templateNumericId: null,
    rows: [] as RfbaRowItem[],
    isDirty: false,
    grandTotal: 0,
};

function normalizeCoaName(data: any): string {
  return data.coaName ?? data.acctName ?? data.accName ?? "";
}

export const useBudgetRealizationStore = create<BudgetRealizationStoreState>(
  (set, get) => ({
    ...initialState,
    isSubmitting: false,
    submitError: null,

    initFromTemplate: (template: BudgetTemplateDetailItem) => {
      const rows = template.items
        .slice()
        .sort((a, b) => a.sortOrder - b.sortOrder)
        // .map(mapTemplateItemToRfbaRow);
        .map((item) => {
          const mapped = mapTemplateItemToRfbaRow(item);

          return {
            ...mapped,
            coaName: normalizeCoaName(item),
          };
        });

      set({
        templateId: template.id,
        templateNumericId: template.templateNumericId,
        rows,
        isDirty: false,
        grandTotal: computeGrandTotal(rows),
      });
    },

    // ← implementasi baru untuk Edit mode
    initFromPlan: (rows: RfbaRowItem[]) => {
      set({
        rows,
        isDirty: false,
        grandTotal: computeGrandTotal(rows),
      });
    },

    updateRow: (rowId: string, payload: RfbaRowUpdatePayload) => {
      set((state) => {
        const rows = state.rows.map((row) =>
          row.id === rowId
            ? {
                ...row,
                ...payload,
                coaName: normalizeCoaName({
                  ...row,
                  ...payload,
                }),
              }
            : row,
        );

        return {
          rows,
          isDirty: true,
          grandTotal: computeGrandTotal(rows),
        };
      });
    },

    addRow: () => {
      set((state) => {
        const nextOrder = state.rows.length + 1;
        const newRow = createEmptyRfbaRow(nextOrder);
        const rows = [...state.rows, newRow];
        return {
          rows,
          isDirty: true,
          grandTotal: computeGrandTotal(rows),
        };
      });
    },

    removeRow: (rowId: string) => {
      set((state) => {
        const rows = state.rows.filter((r) => r.id !== rowId);
        return {
          rows,
          isDirty: true,
          grandTotal: computeGrandTotal(rows),
        };
      });
    },

    getRowById: (rowId: string) => {
      return get().rows.find((row) => row.id === rowId);
    },

    resetForm: () => {
      set({ ...initialState });
    },

    clearSubmitError: () => set({ submitError: null }),
  }),
);