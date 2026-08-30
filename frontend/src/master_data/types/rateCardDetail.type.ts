// ─── existing types (tambahkan ke file rateCard.type.ts yang sudah ada) ───────

export interface RateCardDetailVendor {
    id: number;
    cardCode: string;
    cardName: string;
}

export interface RateCardDetailItem {
    id: number;
    itemCode: string;
    itemName: string;
    acctCode: string;
    acctName: string;
}

export interface RateCardDetailUom {
    id: number;
    code: string;
    name: string;
    isActive: boolean;
}

export interface RateCardDetailLine {
  id: number;
  item: RateCardDetailItem;
  uom: RateCardDetailUom;
  costValue: number;
  ppnTaxType: ppnTaxTypeItem;
  pphTaxType: pphTaxTypeItem;
  costTreatment: null;
}

export interface ppnTaxTypeItem{
    id: number;
    code: string;
    rate: number;
}

export interface pphTaxTypeItem {
  id: number;
  code: string;
  rate: number;
}

export interface RateCardDetail {
    id: number;
    vendor: RateCardDetailVendor;
    status: string;
    items: RateCardDetailLine[];
    createdAt: string;
    submittedAt: string | null;
}

export interface RateCardDetailApiResponse {
    success: boolean;
    data: RateCardDetail;
    message: string;
    requestId: string | null;
}