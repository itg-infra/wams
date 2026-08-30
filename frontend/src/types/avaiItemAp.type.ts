export interface AvailableItem {
  budgetPlanItemId: number;
  budgetPlanId: number;
  budgetPlanCode: string;
  budgetPlanRemark: string;
  vendorShadowId: number;
  vendorCode: string;
  vendorName: string;
  itemCode: string;
  itemName: string;
  coaCode: string;
  coaName: string;
  uomCode: string;
  uomName: string;
  isRfba: boolean;
  billOfLading: string;
  unitCost: number;
  unitCount: number;
  budgetPlanTotal: number;
  isGenerated: boolean;
  takenByCode: string | null;
  availabilityStatus: string;
}

export interface AvailableItemsResponse {
  success: boolean;
  data: AvailableItem[];
  message: string;
  requestId: string;
}
