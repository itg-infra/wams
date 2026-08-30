import axiosProvider from "../../providers/axiosProvider";

import type {
  ApproveRecapResponse,
  RecapWorkOrderDetailResponse,
  RecapWorkOrderListResponse,
  RejectRecapPayload,
  RejectRecapResponse,
} from "../../../types/recapWo.type";

const BASE_URL = "/api/v1/recap-work-orders";

/** Columns the API can order by — see RecapWorkOrderRepository.SortColumns. */
export type RecapWorkOrderSortBy = "status" | "docDate" | "createdAt";

export interface RecapWorkOrderListQuery {
  page?: number;
  limit?: number;
  sortBy?: RecapWorkOrderSortBy;
  sortOrder?: "asc" | "desc";
}

export const recapWorkOrderService = {
  async getRecapWorkOrders(
    query: RecapWorkOrderListQuery = {},
  ): Promise<RecapWorkOrderListResponse> {
    const params = new URLSearchParams({
      page: String(query.page ?? 1),
      limit: String(query.limit ?? 20),
    });

    // The endpoint is paginated, so ordering has to be resolved server-side.
    if (query.sortBy) {
      params.set("sortBy", query.sortBy);
      params.set("sortOrder", query.sortOrder ?? "asc");
    }

    const response = await axiosProvider.get<RecapWorkOrderListResponse>(
      `${BASE_URL}?${params.toString()}`,
      {
        withWarehouseId: true,
      },
    );

    return response.data;
  },

  async getRecapWorkOrderDetail(
    id: number,
  ): Promise<RecapWorkOrderDetailResponse> {
    const response = await axiosProvider.get<RecapWorkOrderDetailResponse>(
      `${BASE_URL}/${id}`,
    );

    return response.data;
  },

  async approveRecapWorkOrder(id: number): Promise<ApproveRecapResponse> {
    const response = await axiosProvider.post<ApproveRecapResponse>(
      `${BASE_URL}/${id}/approve`,
    );

    return response.data;
  },

  async rejectRecapWorkOrder(
    id: number,
    payload: RejectRecapPayload,
  ): Promise<RejectRecapResponse> {
    const response = await axiosProvider.post<RejectRecapResponse>(
      `${BASE_URL}/${id}/reject`,
      payload,
    );

    return response.data;
  },
};
