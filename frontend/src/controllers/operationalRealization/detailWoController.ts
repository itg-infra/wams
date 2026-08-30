import { workOrderService } from "../../api/services/operationalRealization/detailWoService";


export const workOrderController = {
  getDetail: async (id: number) => {
    return await workOrderService.getDetail(id);
  },
};
