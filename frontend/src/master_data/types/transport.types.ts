export interface TransportOrder {
  id: number;
  docNo: string;
  type: string;
  cardCode: string;
  cardName: string;
  vehicleNo: string;
  vehicleType: string;
  blNo: string;
  itemCode: string;
  itemName: string;
  quantity: number;
  uoM: string;
  whsCode: string;
  whsName: string;
  docStatus: string;
}
export interface TransportOrderMeta {
  page: number;
  limit: number;
  total: number;
  totalPages: number;
}

// Response mentah dari API
export interface TransportOrderApiResponse {
  success: boolean;
  data: TransportOrder[];
  meta: TransportOrderMeta;
  requestId: string | null;
}

// Response yang dikembalikan service
export interface TransportOrderListResponse {
  data: TransportOrder[];
  meta: TransportOrderMeta;
}
