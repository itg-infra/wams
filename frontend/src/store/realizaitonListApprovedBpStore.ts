import { create } from "zustand";
import { realizationApprovedBpService, } from "../api/services/operationalRealization/realizationListBpService";
import type {
  RealizationApprovedBpItem,
  RealizationApprovedBudgetPlansQueryParams,
} from "../types/realizationApprovedBp.type";
import { budgetPlanService } from "../api/services/budgeting/budgetPlan/detailBudgetPlanService";
import type { BudgetPlanResponse } from "../types/detailBudgetPlan.type";
import type { BudgetPlanDetailItem } from "../types/budgetPlanDetial.type";

interface RealizationBpStoreState {
  plans: RealizationApprovedBpItem[];
  isLoading: boolean;
  error: string | null;

  detail: BudgetPlanResponse | null;

  availableItems: BudgetPlanDetailItem[];
  // availableItems: AvailableItem[];

  availableItemsLoading: boolean;

  // fetchAvailableItems: (
  //   vendorShadowId: number,
  //   budgetPlanId: number,
  // ) => Promise<AvailableItem[]>;

  fetchAvailableItems: (
    vendorShadowId: number,
    budgetPlanId: number,
    purchaseOrderId?: number,
  ) => Promise<BudgetPlanDetailItem[]>;

  fetchDetail: (id: string) => Promise<void>;
  clearDetail: () => void;

  currentPage: number;
  limit: number;
  total: number;
  lastUpdated: string;
  sortBy: RealizationApprovedBudgetPlansQueryParams["sortBy"];
  search: string;

  fetchPlans: (
    params?: RealizationApprovedBudgetPlansQueryParams,
  ) => Promise<void>;
  setPage: (page: number) => void;
  setLimit: (limit: number) => void;
  setSearch: (search: string) => void;
  setSortBy: (
    sortBy: RealizationApprovedBudgetPlansQueryParams["sortBy"],
  ) => void;
  reset: () => void;
}

const initialState = {
  plans: [] as RealizationApprovedBpItem[],
  detail: null as BudgetPlanResponse | null,
  isLoading: false,
  error: null,
  currentPage: 1,
  limit: 20,
  total: 0,
  lastUpdated: "-",
  sortBy: "latest" as const,
  search: "",
};

export const useRealizationApprovedBpstore = create<RealizationBpStoreState>(
  (set, get) => ({
    ...initialState,

    availableItems: [],
    availableItemsLoading: false,

    // fetchAvailableItems: async (
    //   vendorShadowId: number,
    //   budgetPlanId: number,
    // ) => {
    //   set({ availableItemsLoading: true });

    //   try {
    //     const response = await realizationApprovedBpService.fetchAvailableItems(
    //       vendorShadowId,
    //       budgetPlanId,
    //     );

    //     set({
    //       availableItems: response.data ?? [],
    //       availableItemsLoading: false,
    //     });

    //     return response.data ?? [];
    //   } catch (error) {
    //     set({
    //       availableItems: [],
    //       availableItemsLoading: false,
    //     });

    //     throw error;
    //   }
    // },

    fetchAvailableItems: async (
      vendorShadowId: number,
      budgetPlanId: number,
      purchaseOrderId?: number,
    ) => {
      set({ availableItemsLoading: true });

      try {
        const response = await realizationApprovedBpService.fetchAvailableItems(
          vendorShadowId,
          budgetPlanId,
          purchaseOrderId,
        );

        set({
          availableItems: response ?? [],
          availableItemsLoading: false,
        });

        return response?? [];
      } catch (error) {
        set({
          availableItems: [],
          availableItemsLoading: false,
        });

        throw error;
      }
    },

    fetchPlans: async (params?: RealizationApprovedBudgetPlansQueryParams) => {
      set({ isLoading: true, error: null });

      try {
        const state = get();
        const response =
          await realizationApprovedBpService.getRealizationApprovedBp({
            search: params?.search ?? state.search,
            sortBy: params?.sortBy ?? state.sortBy,
            page: params?.page ?? state.currentPage,
            limit: params?.limit ?? state.limit,
          });

        set({
          plans: response.data,
          total: response.meta.total,
          lastUpdated: response.meta.lastUpdated,
          currentPage: params?.page ?? state.currentPage,
          isLoading: false,
        });
      } catch (err) {
        const errorMessage =
          err instanceof Error
            ? err.message
            : "Failed to fetch approved budget plans";
        set({ error: errorMessage, isLoading: false });
        console.error("Error fetching approved budget plans:", err);
      }
    },

    fetchDetail: async (id: string) => {
      set({
        isLoading: true,
        error: null,
      });

      try {
        const response = await budgetPlanService.getBudgetPlanDetail(id);

        set({
          detail: response,
          isLoading: false,
        });
      } catch (error) {
        set({
          isLoading: false,
          error:
            error instanceof Error
              ? error.message
              : "Failed to fetch budget plan detail",
        });
      }
    },

    clearDetail: () =>
      set({
        detail: null,
        error: null,
        isLoading: false,
      }),

    setPage: (page: number) => {
      set({ currentPage: page });
      get().fetchPlans({ page });
    },

    setLimit: (limit: number) => {
      set({ limit, currentPage: 1 });
      get().fetchPlans({ limit, page: 1 });
    },

    setSearch: (search: string) => {
      set({ search, currentPage: 1 });
      get().fetchPlans({ search, page: 1 });
    },

    setSortBy: (
      sortBy: RealizationApprovedBudgetPlansQueryParams["sortBy"],
    ) => {
      set({ sortBy, currentPage: 1 });
      get().fetchPlans({ sortBy, page: 1 });
    },

    reset: () => {
      set({ ...initialState });
    },
  }),
);
