import type { PurchaseOrder } from "../../types/listGeneratePo.type";

// Hanya PO Draft yang aman dipakai kembali sebagai edit-flow
export function findEditablePurchaseOrder(
  purchaseOrders: PurchaseOrder[] | undefined,
): PurchaseOrder | undefined {
  return purchaseOrders?.find((purchaseOrder) => purchaseOrder.status === "Draft");
}

// Abaikan ID route yang kosong, bukan angka, atau bukan ID positif
export function normalizePurchaseOrderId(
  value: number | string | null | undefined,
): number | undefined {
  const id = typeof value === "string" ? Number(value) : value;

  if (typeof id !== "number" || !Number.isInteger(id) || id <= 0) {
    return undefined;
  }

  return id;
}
