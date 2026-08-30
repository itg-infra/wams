import { create } from "zustand";

import { getWorkOrderDetail, getWorkOrders } from "../api/services/operationalRealization/listWorkOrderService";

import type {
  WorkOrderDetail,
  WorkOrderItem,
  WorkOrderQueryParams,
} from "../types/workOrder.type";

type WorkOrderStore = {
  workOrders: WorkOrderItem[];

  total: number;

  totalPages: number;

  page: number;

  limit: number;

  isLoading: boolean;

  error: string | null;

  params: WorkOrderQueryParams;

  fetchWorkOrders: (params?: WorkOrderQueryParams) => Promise<void>;

  setParams: (params: Partial<WorkOrderQueryParams>) => void;

  // ======================================================
  // DETAIL
  // ======================================================

  selectedWorkOrder: WorkOrderDetail | null;

  isDetailLoading: boolean;

  fetchWorkOrderDetail: (id: number) => Promise<void>;

  clearSelectedWorkOrder: () => void;

  // delete
  deleteLoadingId: number | null;

  setDeleteLoadingId: (payload: number | null) => void;

  errorMessage: string | null;

  setErrorMessage: (payload: string | null) => void;
};

export const useWorkOrderStore = create<WorkOrderStore>((set, get) => ({
  // ======================================================
  // INITIAL STATE
  // ======================================================

  errorMessage: null,

  workOrders: [],

  total: 0,

  totalPages: 0,

  page: 1,

  limit: 10,

  isLoading: false,

  error: null,

  params: {
    page: 1,
    limit: 10,
    search: "",

    status: undefined,

    budgetPlanId: undefined,

    activityTypeCode: undefined,

    dateFrom: undefined,

    dateTo: undefined,

    sortBy: "createdAt",

    sortOrder: "desc",
  },

  // ======================================================
  // FETCH LIST
  // ======================================================

  fetchWorkOrders: async (params) => {
    try {
      set({
        isLoading: true,
        error: null,
      });

      const mergedParams = {
        ...get().params,
        ...params,
      };

      const response = await getWorkOrders(mergedParams);

      set({
        workOrders: response.data,

        total: response.meta.total,

        totalPages: response.meta.totalPages,

        page: response.meta.page,

        limit: response.meta.limit,

        params: mergedParams,

        isLoading: false,
      });
    } catch (error) {
      set({
        isLoading: false,

        error:
          error instanceof Error
            ? error.message
            : "Failed to fetch work orders",
      });
    }
  },

  // ======================================================
  // SET PARAMS
  // ======================================================

  setParams: (newParams) => {
    set({
      params: {
        ...get().params,
        ...newParams,
      },
    });
  },

  // ======================================================
  // DETAIL
  // ======================================================

  selectedWorkOrder: null,

  isDetailLoading: false,

  fetchWorkOrderDetail: async (id) => {
    try {
      set({
        isDetailLoading: true,
        error: null,
      });

      const response = await getWorkOrderDetail(id);

      set({
        selectedWorkOrder: response.data,

        isDetailLoading: false,
      });
    } catch (error) {
      set({
        isDetailLoading: false,

        error:
          error instanceof Error ? error.message : "Failed to fetch detail",
      });
    }
  },

  clearSelectedWorkOrder: () => {
    set({
      selectedWorkOrder: null,
    });
  },

  // delete
  deleteLoadingId: null,

  setDeleteLoadingId: (payload) =>
    set({
      deleteLoadingId: payload,
    }),

  setErrorMessage: (payload) =>
    set({
      errorMessage: payload,
    }),
}));
