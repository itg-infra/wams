import axiosProvider from "../../api/providers/axiosProvider";
import type {
  TransportOrder,
  TransportOrderApiResponse,
  TransportOrderListResponse,
} from "../types/transport.types";

const mapItem = (item: TransportOrder): TransportOrder => ({
  id: item.id,
  docNo: item.docNo,
  type: item.type,
  cardCode: item.cardCode,
  cardName: item.cardName,
  vehicleNo: item.vehicleNo,
  vehicleType: item.vehicleType,
  blNo: item.blNo,
  itemCode: item.itemCode,
  itemName: item.itemName,
  quantity: item.quantity,
  uoM: item.uoM,
  whsCode: item.whsCode,
  whsName: item.whsName,
  docStatus: item.docStatus,
});

export interface GetTransportOrdersParams {
  budgetPlanId?: number;
  page?: number;
  limit?: number;
}

export const transportOrderService = {
  async getTransportOrders(
    params?: GetTransportOrdersParams,
  ): Promise<TransportOrderListResponse> {
    const response = await axiosProvider.get<TransportOrderApiResponse>(
      "api/v1/transport-orders",
      {
        params: {
          budgetPlanId: params?.budgetPlanId,
          page: params?.page,
          limit: params?.limit,
        },
      },
    );

    const { data, meta } = response.data;

    return {
      data: data.map(mapItem),
      meta,
    };
  },
};
