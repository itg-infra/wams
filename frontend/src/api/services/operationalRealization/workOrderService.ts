// services/workOrder.service.ts

import axiosProvider from "../../providers/axiosProvider";

import type {
  CreateWorkOrderPayload,
  CreateWorkOrderResponse,
} from "../../../types/createWorkOrder.type";
import type { EditWorkOrderPayload } from "../../../types/editWo.type";
import type { WOPICResponse } from "../../../types/woPic";

export const workOrderService = {
  async createWorkOrder(
    id: number,
    payload: CreateWorkOrderPayload,
  ): Promise<CreateWorkOrderResponse> {
    const response = await axiosProvider.post<CreateWorkOrderResponse>(
      `api/v1/work-orders/${id}/submit`,
      payload,
      {
        withWarehouseId: true,
      },
    );

    return response.data;
  },

  async draftWorkOrder(
    id: number,
    payload: CreateWorkOrderPayload,
  ): Promise<CreateWorkOrderResponse> {
    const response = await axiosProvider.put<CreateWorkOrderResponse>(
      `api/v1/work-orders/${id}`,
      payload,
      {
        withWarehouseId: true,
      },
    );

    return response.data;
  },

  async editWorkOrder(
    id: number,
    payload: EditWorkOrderPayload,
  ): Promise<CreateWorkOrderResponse> {
    const response = await axiosProvider.put<CreateWorkOrderResponse>(
      `api/v1/work-orders/${id}`,
      payload,
      {
        withWarehouseId: true,
      },
    );

    return response.data;
  },

  async submitDraftWorkOrder(id: number): Promise<CreateWorkOrderResponse> {
    const response = await axiosProvider.post<CreateWorkOrderResponse>(
      `api/v1/work-orders/${id}/submit`,
      {
        withWarehouseId: true,
      },
    );

    return response.data;
  },

  async woPic(woID: string | number): Promise<WOPICResponse> {
   const response = await axiosProvider.get<WOPICResponse>(
     `/api/v1/work-orders/${woID}/pic`,
   );
   return response.data;
  },
};
