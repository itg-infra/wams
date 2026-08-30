// stores/useBudgetPlanStore.ts

import { create } from "zustand";
import type {
    BudgetPlanItem,
    BudgetPlanMeta,
    BudgetPlanQueryParams,
    BudgetPlanStatus,
    BudgetPlanType,
} from "../types/budgetPlan.type";

interface BudgetPlanState {
  // Data
  plans: BudgetPlanItem[];
  meta: BudgetPlanMeta | null;

  search: string;
  page: number;
  totalPages: number;

  // UI State
  isLoading: boolean;
  error: string | null;

  // Query params
  params: BudgetPlanQueryParams;

  // Actions
  setPlans: (plans: BudgetPlanItem[]) => void;
  setMeta: (meta: BudgetPlanMeta) => void;
  setLoading: (loading: boolean) => void;
  setError: (error: string | null) => void;
  setSearch: (search: string) => void;
  setSortBy: (
    sortBy: "budgetNo" | "templateCode" | "location" | "docDate",
  ) => void;
  setSortOrder: (sortOrder: "asc" | "desc") => void; // Tambahan baru
  setPage: (page: number) => void;
  setStatusFilter: (status: BudgetPlanStatus | "") => void;
  setTypeFilter: (type: BudgetPlanType | "") => void;
  resetParams: () => void;
}

const defaultParams: BudgetPlanQueryParams = {
  search: "",
  // Nilai default diubah dari "latest" menjadi field spesifik ("docDate")
  sortBy: "docDate",
  // Tambahan nilai default untuk order
  sortOrder: "desc",
  page: 1,
  limit: 20,
  status: "",
  type: "",
};

export const useBudgetPlanStore = create<BudgetPlanState>((set) => ({
  plans: [],
  meta: null,
  isLoading: false,
  error: null,
  page: 1,
  totalPages: 1,
  search: "",
  params: { ...defaultParams },

  setPlans: (plans) => set({ plans }),
  setMeta: (meta) => set({ meta }),
  setLoading: (isLoading) => set({ isLoading }),
  setError: (error) => set({ error }),

  setSearch: (search) =>
    set((state) => ({
      params: { ...state.params, search, page: 1 },
    })),

  setSortBy: (sortBy) =>
    set((state) => ({
      params: { ...state.params, sortBy, page: 1 },
    })),

  setSortOrder: (sortOrder) =>
    set((state) => ({
      params: { ...state.params, sortOrder, page: 1 },
    })),

  setPage: (page) =>
    set((state) => ({
      params: { ...state.params, page },
    })),

  setStatusFilter: (status) =>
    set((state) => ({
      params: { ...state.params, status, page: 1 },
    })),

  setTypeFilter: (type) =>
    set((state) => ({
      params: { ...state.params, type, page: 1 },
    })),

  resetParams: () => set({ params: { ...defaultParams } }),
}));