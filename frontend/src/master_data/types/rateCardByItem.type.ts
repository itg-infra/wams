export interface RateCardByItemVendor {
  vendorShadowId: number;
  vendorCode: string;
  vendorName: string;
  uomMasterId: number;
  uomCode: string;
  uomName: string;
  costValue: number;
  ppnTaxType: ppnTaxTypeItem;
  pphTaxType: pphTaxTypeItem
}

export interface ppnTaxTypeItem{
    id: number;
    code: string;
    rate: number;
    costTreatment: string;
}

export interface pphTaxTypeItem {
  id: number;
  code: string;
  rate: number;
  costTreatment: string;
}

export interface RateCardByItemResponse {
    success: boolean;
    data: RateCardByItemVendor[];
    message: string;
    requestId: string;
}