import { create } from "zustand";
import type {
  TransportOrder,
  TransportOrderMeta,
} from "../types/transport.types";
import {
  transportOrderService,
  type GetTransportOrdersParams,
} from "../services/transportService";

interface TransportOrderState {
  transportOrders: TransportOrder[];
  meta: TransportOrderMeta | null;
  isLoading: boolean;
  error: string | null;

  fetchTransportOrders: (
    params?: GetTransportOrdersParams,
    append?: boolean,
  ) => Promise<void>;
  reset: () => void;
}

export const useTransportOrderStore = create<TransportOrderState>(
  (set, get) => ({
    transportOrders: [],
    meta: null,
    isLoading: false,
    error: null,

    fetchTransportOrders: async (
      params?: GetTransportOrdersParams,
      append = false,
    ) => {
      set({ isLoading: true, error: null });
      try {
        const response = await transportOrderService.getTransportOrders(params);
        set({
          transportOrders: append
            ? [...get().transportOrders, ...response.data]
            : response.data,
          meta: response.meta,
          isLoading: false,
        });
      } catch (error) {
        set({
          error: `Gagal memuat transport orders ${error}`,
          isLoading: false,
        });
      }
    },

    reset: () => set({ transportOrders: [], meta: null, error: null }),
  }),
);
