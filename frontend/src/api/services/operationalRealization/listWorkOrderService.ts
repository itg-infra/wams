import axios from "axios";

import type {
  WorkOrderDetailResponse,
  WorkOrderListResponse,
  WorkOrderQueryParams,
} from "../../../types/workOrder.type";

import type { ApiErrorResponse } from "../../../types/api.type";

import axiosProvider from "../../providers/axiosProvider";

// ======================================================
// GET LIST
// ======================================================

export const getWorkOrders = async (params: WorkOrderQueryParams) => {
  try {
    const response = await axiosProvider.get<WorkOrderListResponse>(
      "/api/v1/work-orders",
      {
        params,
        withWarehouseId : true
      },
    );

    return response.data;
  } catch (error) {
    if (axios.isAxiosError<ApiErrorResponse>(error)) {
      throw new Error(
        error.response?.data?.message || "Failed to fetch work orders",
      );
    }

    throw new Error("Unexpected error occurred");
  }
};

// ======================================================
// GET DETAIL
// ======================================================

export const getWorkOrderDetail = async (id: number) => {
  try {
    const response = await axiosProvider.get<WorkOrderDetailResponse>(
      `/api/v1/work-orders/${id}`,
    );

    return response.data;
  } catch (error) {
    if (axios.isAxiosError<ApiErrorResponse>(error)) {
      throw new Error(
        error.response?.data?.message || "Failed to fetch work order detail",
      );
    }

    throw new Error("Unexpected error occurred");
  }
};

// ======================================================
// DELETE
// ======================================================

export async function deleteWorkOrder(id: number): Promise<void> {
  try {
    await axiosProvider.delete(`/api/v1/work-orders/${id}`);
  } catch (error) {
    if (axios.isAxiosError<ApiErrorResponse>(error)) {
      throw new Error(
        error.response?.data?.message || "Failed to delete work order",
      );
    }

    throw new Error("Unexpected error occurred");
  }
}
