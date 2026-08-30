import type { WorkOrderDetailResponse } from "../../../types/detailWo.type";
import axiosProvider from "../../providers/axiosProvider";

export const workOrderService = {
  getDetail: async (id: number): Promise<WorkOrderDetailResponse> => {
    const response = await axiosProvider.get<WorkOrderDetailResponse>(
      `/api/v1/work-orders/${id}`,
    );

    return response.data;
  },
};
