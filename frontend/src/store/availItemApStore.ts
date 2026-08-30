// stores/useAvailableItemStore.ts

import { create } from "zustand";
import type { AvailableItem } from "../types/avaiItemAp.type";
import { availableItemService } from "../api/services/budgeting/accountPayable/availItemApService";

interface AvailableItemState {
  items: AvailableItem[];
  isLoading: boolean;
  error: string | null;
  selectedRowIds: number[];

  fetchAvailableItems: (vendorShadowId: number) => Promise<void>;
  setItems: (items: AvailableItem[], selectedRowIds?: number[]) => void;
  toggleCheck: (id: number) => void;
  resetSelectedRows: () => void;
  clearItems: () => void;
}

export const useAvailableItemStore = create<AvailableItemState>((set, get) => ({
  items: [],
  isLoading: false,
  error: null,
  selectedRowIds: [],

  fetchAvailableItems: async (vendorShadowId: number) => {
    set({ isLoading: true, error: null });
    try {
      const res = await availableItemService.getAvailableItems(vendorShadowId);
      set({
        items: res.data,
        isLoading: false,
        selectedRowIds: [], // reset selection tiap vendor ganti
      });
    } catch (err) {
      set({
        error:
          err instanceof Error
            ? err.message
            : "Gagal mengambil available items",
        isLoading: false,
        items: [],
      });
    }
  },

  setItems: (items, selectedRowIds = []) =>
    set({ items, selectedRowIds, isLoading: false, error: null }),

  toggleCheck: (id: number) => {
    const { selectedRowIds } = get();
    set({
      selectedRowIds: selectedRowIds.includes(id)
        ? selectedRowIds.filter((rowId) => rowId !== id)
        : [...selectedRowIds, id],
    });
  },

  resetSelectedRows: () => set({ selectedRowIds: [] }),
  clearItems: () => set({ items: [], selectedRowIds: [] }),
}));
