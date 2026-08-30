import { create } from 'zustand';
import type { ApprovedBudgetPlan, SortField } from '../types/listGeneratePo.type';
import { approvedBudgetPlanService } from '../api/services/budgeting/purchaseOrders/listGeneratePoService';

interface ApprovedBudgetPlanState {
  // ── Data ──────────────────────────────────────────────────────────────────
  budgetPlans: ApprovedBudgetPlan[];
  filteredPlans: ApprovedBudgetPlan[];

  // ── UI State ──────────────────────────────────────────────────────────────
  isLoading: boolean;
  error: string | null;
  lastUpdated: Date | null;

  // ── Filter / Sort ─────────────────────────────────────────────────────────
  searchQuery: string;
  sortField: SortField | null;
  sortDirection: "asc" | "desc";

  // ── Pagination ────────────────────────────────────────────────────────────
  currentPage: number;
  pageSize: number;
  totalItems: number;

  // ── Actions ───────────────────────────────────────────────────────────────
  fetchBudgetPlans: () => Promise<void>;
  setSearchQuery: (query: string) => void;
  setSortField: (field: SortField, direction?: "asc" | "desc") => void;
  setSortDirection: (direction: "asc" | "desc") => void;
  setCurrentPage: (page: number) => void;
  setPageSize: (size: number) => void;
  reset: () => void;
}

const applyFilterAndSort = (
    plans: ApprovedBudgetPlan[],
    query: string,
    sortField: SortField | null,
    sortDirection: 'asc' | 'desc'
): ApprovedBudgetPlan[] => {
    let result = [...plans];

    // ── Filter ────────────────────────────────────────────────────────────────
    if (query.trim()) {
        const lower = query.toLowerCase();
        result = result.filter(
          (p) =>
            (p.budgetPlanCode.toLowerCase().includes(lower) ||
              p.vendorName.toLowerCase().includes(lower) ||
              p.remark.toLowerCase().includes(lower) ||
              p.makerName.toLowerCase().includes(lower) ||
              (p.approvalName?.toLowerCase().includes(lower) ?? false) ||
              p.purchaseOrders?.some((po) =>
                po.code.toLowerCase().includes(lower),
              )) ??
            false,
        );
    }

    // ── Sort ──────────────────────────────────────────────────────────────────
    if (sortField) {
        result.sort((a, b) => {
            const aVal = a[sortField] ?? '';
            const bVal = b[sortField] ?? '';
            const cmp = String(aVal).localeCompare(String(bVal));
            return sortDirection === 'asc' ? cmp : -cmp;
        });
    }

    return result;
};

export const useApprovedBudgetPlanStore = create<ApprovedBudgetPlanState>(
  (set, get) => ({
    // ── Initial State ─────────────────────────────────────────────────────────
    budgetPlans: [],
    filteredPlans: [],
    isLoading: false,
    error: null,
    lastUpdated: null,
    searchQuery: "",
    sortField: null,
    sortDirection: "asc",
    currentPage: 1,
    pageSize: 10,
    totalItems: 0,

    // ── Actions ───────────────────────────────────────────────────────────────
    fetchBudgetPlans: async () => {
      set({ isLoading: true, error: null });
      try {
        const response =
          await approvedBudgetPlanService.getApprovedBudgetPlans();
        const { searchQuery, sortField, sortDirection } = get();
        const filtered = applyFilterAndSort(
          response.data,
          searchQuery,
          sortField,
          sortDirection,
        );
        set({
          budgetPlans: response.data,
          filteredPlans: filtered,
          totalItems: filtered.length,
          isLoading: false,
          lastUpdated: new Date(),
        });
      } catch (err: unknown) {
        const message =
          err instanceof Error ? err.message : "Failed to fetch budget plans";
        set({ error: message, isLoading: false });
      }
    },

    setSearchQuery: (query) => {
      const { budgetPlans, sortField, sortDirection } = get();
      const filtered = applyFilterAndSort(
        budgetPlans,
        query,
        sortField,
        sortDirection,
      );
      set({
        searchQuery: query,
        filteredPlans: filtered,
        totalItems: filtered.length,
        currentPage: 1,
      });
    },

    setSortField: (field, direction) => {
      const { budgetPlans, searchQuery, sortField, sortDirection } = get();
      const newDir =
        direction ??
        (sortField === field && sortDirection === "asc" ? "desc" : "asc");
      const filtered = applyFilterAndSort(
        budgetPlans,
        searchQuery,
        field,
        newDir,
      );
      set({
        sortField: field,
        sortDirection: newDir,
        filteredPlans: filtered,
      });
    },

    setSortDirection: (direction) => {
      const { budgetPlans, searchQuery, sortField } = get();
      const filtered = applyFilterAndSort(
        budgetPlans,
        searchQuery,
        sortField,
        direction,
      );
      set({
        sortDirection: direction,
        filteredPlans: filtered,
      });
    },

    setCurrentPage: (page) => set({ currentPage: page }),

    setPageSize: (size) => set({ pageSize: size, currentPage: 1 }),

    reset: () =>
      set({
        budgetPlans: [],
        filteredPlans: [],
        isLoading: false,
        error: null,
        lastUpdated: null,
        searchQuery: "",
        sortField: null,
        sortDirection: "asc",
        currentPage: 1,
        pageSize: 10,
        totalItems: 0,
      }),
  }),
);