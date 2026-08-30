import { useTransportOrderStore } from "../store/transportStore";
import type { GetTransportOrdersParams } from "../services/transportService";

export const useTransportOrderController = () => {
  const {
    transportOrders,
    meta,
    isLoading,
    error,
    fetchTransportOrders,
    reset,
  } = useTransportOrderStore();

  const loadTransportOrders = async (
    params?: GetTransportOrdersParams,
    append?: boolean,
  ) => {
    await fetchTransportOrders(params, append);
  };

  return {
    transportOrders,
    meta,
    isLoading,
    error,
    loadTransportOrders,
    reset,
  };
};
