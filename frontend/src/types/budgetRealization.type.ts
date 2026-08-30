import type { SpkItemCodeOption } from "../components/costRows";
import type { BudgetTemplateDetailCostItem } from "./budgetTemplate.type";

export type RfbaRowType = "internal" | "external";

export interface RfbaRowItem {
  // Identity
  id: string;
  itemShadowId: number | null; // null for manually added rows
  sortOrder: number;
  isManual: boolean; // true = user added, false = from template
  activityTypeName: string | null;
  activityTypeId: number | null;

  //
  selectedSpkItemCode?: string | null;
  selectedSpkId?: number | null;
  selectedSpk?: SpkItemCodeOption | null;

  quantity?: number | null;

  // Read-only (from template, editable for manual rows)
  costDetail: string; // Cost ID
  costName: string;
  coa: string;
  coaName: string;

  // User-filled fields
  type: RfbaRowType | null;
  vendorId: string | null;
  vendorCode: string | null;
  vendorName: string | null;
  pphTaxType: pphTaxTypeItem | null;
  ppnTaxType: ppnTaxTypeItem | null;
  costTreatment: string;
  isRfba: boolean | null;
  docExternal: string | null;
  billOfLading: string | null;
  uomId: number | null;
  uomCode: string | null;
  uomName: string | null;
  unitCost: number | null;
  unitCount: number | null;
  costValue: number | null;
  kemasan: string | null
  description: string | null;
}

export interface pphTaxTypeItem {
  id: number;
  code: string;
  rate: number;
}

export interface ppnTaxTypeItem {
  id: number;
  code: string;
  rate: number;
}

export type RfbaRowUpdatePayload = Partial<
  Omit<RfbaRowItem, "id" | "itemShadowId" | "sortOrder" | "isManual">
>;

export function mapTemplateItemToRfbaRow(
  item: BudgetTemplateDetailCostItem,
): RfbaRowItem {
  return {
    id: item.id,
    itemShadowId: item.itemShadowId,
    activityTypeName: item.activityTypeName,
    sortOrder: item.sortOrder,
    activityTypeId: item.activityTypeId,

    isManual: false,
    costDetail: item.costDetail,
    costName: item.costName,
    coa: item.coa,
    coaName: item.coaName,
    type: null,
    vendorId: null,
    vendorCode: null,
    vendorName: null,
    pphTaxType: null,
    ppnTaxType: null,
    costTreatment: "",
    isRfba: null,
    docExternal: null,
    billOfLading: null,
    uomId: null,
    uomCode: null,
    uomName: null,
    unitCost: null,
    unitCount: null,
    costValue: null,
    kemasan: null,
    description: null,
  };
}

export function createEmptyRfbaRow(sortOrder: number): RfbaRowItem {
  return {
    id: `manual-${Date.now()}-${Math.random().toString(36).slice(2)}`,
    itemShadowId: null,
    activityTypeName: null,
    activityTypeId: null,
    sortOrder,
    isManual: true,
    costDetail: "",
    costName: "",
    coa: "",
    coaName: "",
    type: null,
    vendorId: null,
    vendorCode: null,
    vendorName: null,
    pphTaxType: null,
    ppnTaxType: null,
    costTreatment: "",
    isRfba: null,
    docExternal: null,
    billOfLading: null,
    uomId: null,
    uomCode: null,
    uomName: null,
    unitCost: null,
    costValue: null,
    unitCount: null,
    kemasan: null,
    description: null,
  };
}
