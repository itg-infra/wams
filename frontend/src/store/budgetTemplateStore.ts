import { create } from "zustand";
import { budgetTemplateService } from "../api/services/budgeting/budgetTemplate/budgetTemplateService";
import type {
  BudgetTemplateItem,
  BudgetTemplateQueryParams,
} from "../types/budgetTemplate.type";

interface BudgetTemplateStoreState {
  templates: BudgetTemplateItem[];
  isLoading: boolean;
  error: string | null;

  search: string;
  page: number;
  limit: number;
  total: number;
  totalPages: number;
  from: number;
  to: number;
  lastUpdated: string;

  // --- MENGGUNAKAN NAMA API ---
  sortBy: string | null;
  sortOrder: "asc" | "desc";

  fetchTemplates: (
    params?: Partial<BudgetTemplateQueryParams>,
  ) => Promise<void>;
  setSearch: (value: string) => void;

  // --- MENGGUNAKAN NAMA API ---
  setSortBy: (field: string) => void;
  setSortOrder: (order: "asc" | "desc") => void;

  setPage: (value: number) => void;
  resetFilters: () => void;

  isApprovedBudgetTemplate: boolean;
  approvedBudgetTemplate: (id: string) => Promise<void>;
}

export const useBudgetTemplateStore = create<BudgetTemplateStoreState>(
  (set, get) => ({
    templates: [],
    isLoading: false,
    error: null,

    search: "",
    page: 1,
    limit: 20,
    total: 0,
    totalPages: 1,
    from: 0,
    to: 0,
    lastUpdated: "-",

    sortBy: null,
    sortOrder: "desc",

    isApprovedBudgetTemplate: false,

    fetchTemplates: async (params = {}) => {
      const current = get();

      const nextSearch = params.search ?? current.search;
      const nextPage = params.page ?? current.page;
      const nextLimit = params.limit ?? current.limit;
      const nextSortBy =
        params.sortBy !== undefined ? params.sortBy : current.sortBy;
      const nextSortOrder = params.sortOrder ?? current.sortOrder;

      set({
        isLoading: true,
        error: null,
        search: nextSearch,
        page: nextPage,
        limit: nextLimit,
        sortBy: nextSortBy,
        sortOrder: nextSortOrder,
      });

      try {
        const response = await budgetTemplateService.getBudgetTemplates({
          search: nextSearch,
          page: nextPage,
          limit: nextLimit,
          sortBy: nextSortBy,
          sortOrder: nextSortOrder,
        });

        set({
          templates: response.data,
          total: response.meta.total,
          totalPages: response.meta.totalPages,
          from: response.meta.from,
          to: response.meta.to,
          lastUpdated: response.meta.lastUpdated,
          isLoading: false,
        });
      } catch (error) {
        set({
          isLoading: false,
          error:
            error instanceof Error
              ? error.message
              : "Failed to fetch templates",
        });
      }
    },

    setSearch: (value) => {
      set({ search: value, page: 1 });
      get().fetchTemplates();
    },

    setSortBy: (field) => {
      set({ sortBy: field, page: 1 });
      get().fetchTemplates();
    },

    setSortOrder: (order) => {
      set({ sortOrder: order, page: 1 });
      get().fetchTemplates();
    },

    setPage: (value) => {
      set({ page: value });
      get().fetchTemplates();
    },

    resetFilters: () => {
      set({ search: "", sortBy: null, sortOrder: "desc", page: 1 });
      get().fetchTemplates();
    },

    approvedBudgetTemplate: async (id: string) => {
      set({ isApprovedBudgetTemplate: true });
      try {
        await budgetTemplateService.budgetTemplateApproved(id);
        await get().fetchTemplates();
      } catch (error) {
        console.error("Approved budget template failed", error);
      } finally {
        set({ isApprovedBudgetTemplate: false });
      }
    },
  }),
);
