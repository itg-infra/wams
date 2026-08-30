import { create } from "zustand";

import type {
  RecapWorkOrderDetail,
  RecapWorkOrderItem,
} from "../types/recapWo.type";

import {
  recapWorkOrderService,
  type RecapWorkOrderListQuery,
} from "../api/services/operationalRealization/recapWoService";

interface RecapWorkOrderState {
  recapWorkOrders: RecapWorkOrderItem[];
  selectedRecapWorkOrder: RecapWorkOrderDetail | null;

  loading: boolean;
  detailLoading: boolean;
  actionLoading: boolean;

  /** Query currently applied to the list (page/limit/sort). */
  params: RecapWorkOrderListQuery;

  fetchRecapWorkOrders: (params?: RecapWorkOrderListQuery) => Promise<void>;

  fetchRecapWorkOrderDetail: (id: number) => Promise<void>;

  approveRecapWorkOrder: (id: number) => Promise<void>;

  rejectRecapWorkOrder: (id: number, reason: string) => Promise<void>;
}

export const useRecapWorkOrderStore = create<RecapWorkOrderState>((set, get) => ({
  recapWorkOrders: [],
  selectedRecapWorkOrder: null,

  loading: false,
  detailLoading: false,
  actionLoading: false,

  params: {},

  fetchRecapWorkOrders: async (params) => {
    try {
      const mergedParams = { ...get().params, ...params };

      set({ loading: true, params: mergedParams });

      const response =
        await recapWorkOrderService.getRecapWorkOrders(mergedParams);

      set({
        recapWorkOrders: response.data,
      });
    } finally {
      set({ loading: false });
    }
  },

  fetchRecapWorkOrderDetail: async (id: number) => {
    try {
      set({ detailLoading: true });

      const response = await recapWorkOrderService.getRecapWorkOrderDetail(id);

      set({
        selectedRecapWorkOrder: response.data,
      });
    } finally {
      set({ detailLoading: false });
    }
  },

  approveRecapWorkOrder: async (id: number) => {
    try {
      set({ actionLoading: true });

      await recapWorkOrderService.approveRecapWorkOrder(id);
    } finally {
      set({ actionLoading: false });
    }
  },

  rejectRecapWorkOrder: async (id: number, reason: string) => {
    try {
      set({ actionLoading: true });

      await recapWorkOrderService.rejectRecapWorkOrder(id, { reason });
    } finally {
      set({ actionLoading: false });
    }
  },
}));
