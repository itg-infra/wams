export type WorkOrderDetailKey =
  | "unloadingItems"
  | "loadingItems"
  | "fumigation"
  | "storage"
  | "opname"
  | "others"
  | "qc"
  | "heavyEquipment"
  | "unbagging"
  | "rebagging";

export type WorkOrderFormKind =
  | "unloading"
  | "loading"
  | "fumigation"
  | "storageHandling"
  | "qc"
  | "heavyEquipment"
  | "unbagging"
  | "rebagging";

export type WorkOrderActivityDefinition = {
  label: string;
  formKind: WorkOrderFormKind;
  detailKey: WorkOrderDetailKey;
};

const DEFINITIONS: Record<string, WorkOrderActivityDefinition> = {
  "K.BONGKAR": {
    label: "Unloading",
    formKind: "unloading",
    detailKey: "unloadingItems",
  },
  "K.MUAT": {
    label: "Loading",
    formKind: "loading",
    detailKey: "loadingItems",
  },
  FUMIGASI: {
    label: "Fumigation",
    formKind: "fumigation",
    detailKey: "fumigation",
  },
  "K.GUDANG": {
    label: "Storage & Handling",
    formKind: "storageHandling",
    detailKey: "storage",
  },
  OPNAME: {
    label: "Opname",
    formKind: "storageHandling",
    detailKey: "opname",
  },
  OTHERS: {
    label: "Others",
    formKind: "storageHandling",
    detailKey: "others",
  },
  QC: {
    label: "QC",
    formKind: "qc",
    detailKey: "qc",
  },
  ALAT_BERAT: {
    label: "Heavy Equipment",
    formKind: "heavyEquipment",
    detailKey: "heavyEquipment",
  },
  UNBAGGING: {
    label: "Unbagging",
    formKind: "unbagging",
    detailKey: "unbagging",
  },
  REBAGGING: {
    label: "Rebagging",
    formKind: "rebagging",
    detailKey: "rebagging",
  },
};

export function getWorkOrderActivityDefinition(
  code: string,
): WorkOrderActivityDefinition | null {
  return DEFINITIONS[code] ?? null;
}

export function getWorkOrderActivityLabel(
  code: string,
  coaName?: string | null,
): string {
  if (code === "OTHERS" && coaName) {
    return coaName;
  }

  return getWorkOrderActivityDefinition(code)?.label ?? code;
}
