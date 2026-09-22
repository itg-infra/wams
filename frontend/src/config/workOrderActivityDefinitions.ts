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
  formKind: WorkOrderFormKind;
  detailKey: WorkOrderDetailKey;
};

const DEFINITIONS: Record<string, WorkOrderActivityDefinition> = {
  "K.BONGKAR": {
    formKind: "unloading",
    detailKey: "unloadingItems",
  },
  "K.MUAT": {
    formKind: "loading",
    detailKey: "loadingItems",
  },
  FUMIGASI: {
    formKind: "fumigation",
    detailKey: "fumigation",
  },
  "K.GUDANG": {
    formKind: "storageHandling",
    detailKey: "storage",
  },
  OPNAME: {
    formKind: "storageHandling",
    detailKey: "opname",
  },
  OTHERS: {
    formKind: "storageHandling",
    detailKey: "others",
  },
  QC: {
    formKind: "qc",
    detailKey: "qc",
  },
  ALAT_BERAT: {
    formKind: "heavyEquipment",
    detailKey: "heavyEquipment",
  },
  UNBAGGING: {
    formKind: "unbagging",
    detailKey: "unbagging",
  },
  REBAGGING: {
    formKind: "rebagging",
    detailKey: "rebagging",
  },
};

export function getWorkOrderActivityDefinition(
  code: string,
): WorkOrderActivityDefinition | null {
  return DEFINITIONS[code] ?? null;
}

type WorkOrderActivityLabelInput = {
  activityTypeCode: string;
  activityTypeDisplay?: string | null;
  coaName?: string | null;
};

export function getWorkOrderActivityLabel(
  activity: WorkOrderActivityLabelInput,
): string {
  return activity.activityTypeCode === "OTHERS" && activity.coaName
    ? activity.coaName
    : activity.activityTypeDisplay ?? activity.activityTypeCode;
}
