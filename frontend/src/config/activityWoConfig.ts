import type { WorkOrderRow } from "./workOrderRowConfig";

export type FieldType = "text" | "number" | "boolean" | "select";

export type LayoutType =
  | "table"
  | "grid"
  | "inline"
  | "checklist"
  | "others"
  | "qc";

export type FieldConfig = {
  label: string;
  key: keyof WorkOrderRow;
  type: FieldType;
  readOnly?: boolean;

  // optional ui props
  colSpan?: number;
  placeholder?: string;

  variant?: "radio" | "checkbox"; // gaya toggle untuk boolean
  group?: "tools"; // untuk mengelompokkan ke section "Tools"
  unit?: string; // suffix unit di sebelah input, mis. "Kg"
  options?: { label: string; value: string }[];
};

export type ActivityConfig = {
  layout: LayoutType;
  columns?: number;
  fields: FieldConfig[];
};

export const ACTIVITY_CONFIG_EDIT: Record<string, ActivityConfig> = {
  // ================= K.BONGKAR =================

  Unloading: {
    layout: "table",

    fields: [
      {
        label: "BL Number",
        key: "blNumber",
        type: "text",
      },
      {
        label: "Product Name",
        key: "productName",
        type: "text",
      },
      {
        label: "Qty",
        key: "quantity",
        type: "number",
      },
      {
        label: "UoM",
        key: "uomCode",
        type: "text",
      },
      {
        label: "No. Vehicle",
        key: "noVehicle",
        type: "text",
      },
      {
        label: "No. Container",
        key: "noContainer",
        type: "text",
      },
      {
        label: "No. Seal",
        key: "noSeal",
        type: "text",
      },
      {
        label: "Gross Weight",
        key: "grossWeight",
        type: "number",
      },
      {
        label: "Final Weight",
        key: "finalWeight",
        type: "number",
      },
      {
        label: "Nett Weight",
        key: "nettWeight",
        type: "number",
      },
      {
        label: "Total Bag",
        key: "totalBag",
        type: "number",
      },
      {
        label: "Unit Weight",
        key: "unitWeight",
        type: "number",
      },
    ],
  },

  // ================= K.MUAT =================

  Loading: {
    layout: "table",

    fields: [
      {
        label: "BL Number",
        key: "blNumber",
        type: "text",
      },
      {
        label: "Product Name",
        key: "productName",
        type: "text",
      },
      {
        label: "Qty",
        key: "quantity",
        type: "number",
      },
      {
        label: "UoM",
        key: "uomCode",
        type: "text",
      },
      {
        label: "No. Vehicle",
        key: "noVehicle",
        type: "text",
      },
      {
        label: "No. Container",
        key: "noContainer",
        type: "text",
      },
      {
        label: "No. Seal",
        key: "noSeal",
        type: "text",
      },
      {
        label: "Gross Weight",
        key: "grossWeight",
        type: "number",
      },
      {
        label: "Final Weight",
        key: "finalWeight",
        type: "number",
      },
      {
        label: "Nett Weight",
        key: "nettWeight",
        type: "number",
      },
      {
        label: "Total Bag",
        key: "totalBag",
        type: "number",
      },
      {
        label: "Unit Weight",
        key: "unitWeight",
        type: "number",
      },
    ],
  },

  // ================= FUMIGASI =================

  Fumigation: {
    layout: "grid",
    columns: 2,

    fields: [
      {
        label: "Fumi Id",
        key: "fumiId",
        type: "text",
      },
      {
        label: "Total Duration",
        key: "totalDuration",
        type: "text",
      },
      {
        label: "BL Number",
        key: "blNumber",
        type: "text",
      },
      {
        label: "Mv Name",
        key: "mvName",
        type: "text",
      },
      {
        label: "Initial Temperature",
        key: "initialTemperature",
        type: "number",
      },
      {
        label: "Final Temperature",
        key: "finalTemperature",
        type: "number",
      },
      {
        label: "Fumigation Type",
        key: "fumigationType",
        type: "text",
      },
      {
        label: "Methyl Bromide Dosage",
        key: "methylBromideDosage",
        type: "number",
      },
      {
        label: "Sulphur Fluoride Dosage",
        key: "sulphurFluorideDosage",
        type: "number",
      },
      {
        label: "Phosphine Dosage",
        key: "phosphineDosage",
        type: "number",
      },
      {
        label: "Result",
        key: "result",
        type: "text",
        colSpan: 2,
      },
    ],
  },

  // ================= QC =================

  QC: {
    layout: "inline",
    columns: 3,

    fields: [
      {
        label: "Moisture %",
        key: "moisturePercent",
        type: "number",
        unit: "%",
      },
      {
        label: "Jamur %",
        key: "jamurPercent",
        type: "number",
        unit: "%",
      },
      {
        label: "Bau %",
        key: "bauPercent",
        type: "number",
        unit: "%",
      },
      {
        label: "Quality Status",
        key: "qualityStatus",
        type: "text",
        colSpan: 3,
      },
    ],
  },

  // ================= K.GUDANG =================

  Storage: {
    layout: "checklist",
    columns: 3,

    fields: [
      {
        label: "Pindah Stapel",
        key: "hasPindahStapel",
        type: "boolean",
      },
      {
        label: "Pembersihan",
        key: "hasPembersihan",
        type: "boolean",
      },
      {
        label: "Perapihan",
        key: "hasPerapihan",
        type: "boolean",
      },
      {
        label: "Volume Weight",
        key: "volumeWeight",
        type: "number",
      },
      {
        label: "Worker On Duty",
        key: "workerOnDuty",
        type: "number",
      },
      {
        label: "Mask",
        key: "hasMask",
        type: "boolean",
      },
      {
        label: "Safety Glasses",
        key: "hasSafetyGlasses",
        type: "boolean",
      },
      {
        label: "Hand Gloves",
        key: "hasHandGloves",
        type: "boolean",
      },
      {
        label: "Helmet",
        key: "hasHelmet",
        type: "boolean",
      },
      {
        label: "Safety Shoes",
        key: "hasSafetyShoes",
        type: "boolean",
      },
      {
        label: "Safety Vest",
        key: "hasSafetyVest",
        type: "boolean",
      },
    ],
  },

  // ================= UNBAGGING =================

  Unbagging: {
    layout: "grid",
    columns: 2,

    fields: [
      {
        label: "No. Vehicle",
        key: "noVehicle",
        type: "text",
      },
      {
        label: "No. Container",
        key: "noContainer",
        type: "text",
      },
      {
        label: "No. Seal",
        key: "noSeal",
        type: "text",
      },
      {
        label: "Initial Weight",
        key: "initialWeight",
        type: "number",
      },
      {
        label: "Total Bag",
        key: "totalBag",
        type: "number",
      },
      {
        label: "Final Weight",
        key: "finalWeight",
        type: "number",
      },
      {
        label: "Unit Weight",
        key: "unitWeight",
        type: "number",
      },
      {
        label: "Total Weight",
        key: "totalWeight",
        type: "number",
      },
    ],
  },

  // ================= ALAT BERAT =================

  "Heavy Equipment": {
    layout: "grid",
    columns: 2,

    fields: [
      {
        label: "BL Number",
        key: "blNumber",
        type: "text",
      },
      {
        label: "Start Time",
        key: "startTime",
        type: "text",
      },
      {
        label: "End Time",
        key: "endTime",
        type: "text",
      },
      {
        label: "Standby Duration 1",
        key: "standbyDuration1",
        type: "text",
      },
      {
        label: "Standby Duration 2",
        key: "standbyDuration2",
        type: "text",
      },
      {
        label: "Minimum Duration",
        key: "minimumDuration",
        type: "text",
      },
      {
        label: "Cost Per Hour",
        key: "costPerHour",
        type: "number",
      },
      {
        label: "Total Cost",
        key: "totalCost",
        type: "number",
      },
    ],
  },

  // ================= REBAGGING =================

  Rebagging: {
    layout: "grid",
    columns: 2,

    fields: [
      {
        label: "Receiver",
        key: "receiver",
        type: "text",
        colSpan: 2,
      },
      {
        label: "No. Vehicle",
        key: "noVehicle",
        type: "text",
      },
      {
        label: "No. Container",
        key: "noContainer",
        type: "text",
      },
      {
        label: "No. Seal",
        key: "noSeal",
        type: "text",
      },
      {
        label: "Initial Weight",
        key: "initialWeight",
        type: "number",
      },
      {
        label: "Final Weight",
        key: "finalWeight",
        type: "number",
      },
      {
        label: "Total Weight",
        key: "totalWeight",
        type: "number",
      },
    ],
  },
};

export const STORAGE_HANDLING_CONFIG: ActivityConfig = ACTIVITY_CONFIG_EDIT.Storage;

const STORAGE_OTHER_CONFIG: ActivityConfig = {
  layout: "others",
  fields: STORAGE_HANDLING_CONFIG.fields.map((field) => {
    if (
      field.key === "hasPindahStapel" ||
      field.key === "hasPembersihan" ||
      field.key === "hasPerapihan"
    ) {
      return { ...field, variant: "radio" as const };
    }

    if (
      field.key === "hasMask" ||
      field.key === "hasSafetyGlasses" ||
      field.key === "hasHandGloves" ||
      field.key === "hasHelmet" ||
      field.key === "hasSafetyShoes" ||
      field.key === "hasSafetyVest"
    ) {
      return { ...field, group: "tools" as const };
    }

    if (field.key === "volumeWeight") {
      return { ...field, unit: "Kg" };
    }

    return field;
  }),
};

export const ACTIVITY_CONFIG: Record<string, ActivityConfig> = {
  // ================= K.BONGKAR =================

  "K.BONGKAR": {
    layout: "table",

    fields: [
      {
        label: "BL Number",
        key: "blNumber",
        type: "text",
      },
      {
        label: "Product Name",
        key: "productName",
        type: "text",
      },
      {
        label: "Qty",
        key: "quantity",
        type: "number",
      },
      {
        label: "UoM",
        key: "uomCode",
        type: "text",
      },
      {
        label: "No. Vehicle",
        key: "noVehicle",
        type: "text",
      },
      {
        label: "No. Container",
        key: "noContainer",
        type: "text",
      },
      {
        label: "No. Seal",
        key: "noSeal",
        type: "text",
      },
      {
        label: "Gross Weight",
        key: "grossWeight",
        type: "number",
      },
      {
        label: "Final Weight",
        key: "finalWeight",
        type: "number",
      },
      {
        label: "Nett Weight",
        key: "nettWeight",
        type: "number",
      },
      {
        label: "Total Bag",
        key: "totalBag",
        type: "number",
      },
      {
        label: "Unit Weight",
        key: "unitWeight",
        type: "number",
      },
    ],
  },

  // ================= K.MUAT =================

  "K.MUAT": {
    layout: "table",

    fields: [
      {
        label: "BL Number",
        key: "blNumber",
        type: "text",
      },
      {
        label: "Product Name",
        key: "productName",
        type: "text",
      },
      {
        label: "Qty",
        key: "quantity",
        type: "number",
      },
      {
        label: "UoM",
        key: "uomCode",
        type: "text",
      },
      {
        label: "No. Vehicle",
        key: "noVehicle",
        type: "text",
      },
      {
        label: "No. Container",
        key: "noContainer",
        type: "text",
      },
      {
        label: "No. Seal",
        key: "noSeal",
        type: "text",
      },
      {
        label: "Gross Weight",
        key: "grossWeight",
        type: "number",
      },
      {
        label: "Final Weight",
        key: "finalWeight",
        type: "number",
      },
      {
        label: "Nett Weight",
        key: "nettWeight",
        type: "number",
      },
      {
        label: "Total Bag",
        key: "totalBag",
        type: "number",
      },
      {
        label: "Unit Weight",
        key: "unitWeight",
        type: "number",
      },
    ],
  },

  // ================= FUMIGASI =================

  FUMIGASI: {
    layout: "grid",
    columns: 2,

    fields: [
      {
        label: "Fumi Id",
        key: "fumiId",
        type: "text",
      },
      {
        label: "Total Duration",
        key: "totalDuration",
        type: "text",
      },
      {
        label: "BL Number",
        key: "blNumber",
        type: "text",
      },
      {
        label: "Mv Name",
        key: "mvName",
        type: "text",
      },
      {
        label: "Initial Temperature",
        key: "initialTemperature",
        type: "number",
      },
      {
        label: "Final Temperature",
        key: "finalTemperature",
        type: "number",
      },
      {
        label: "Fumigation Type",
        key: "fumigationType",
        type: "text",
      },
      {
        label: "Methyl Bromide Dosage",
        key: "methylBromideDosage",
        type: "number",
      },
      {
        label: "Sulphur Fluoride Dosage",
        key: "sulphurFluorideDosage",
        type: "number",
      },
      {
        label: "Phosphine Dosage",
        key: "phosphineDosage",
        type: "number",
      },
      {
        label: "Result",
        key: "result",
        type: "text",
        colSpan: 2,
      },
    ],
  },

  // ================= QC =================

  QC: {
    layout: "inline",
    columns: 3,

    fields: [
      {
        label: "Moisture %",
        key: "moisturePercent",
        type: "number",
        unit: "%",
      },
      {
        label: "Jamur %",
        key: "jamurPercent",
        type: "number",
        unit: "%",
      },
      {
        label: "Bau %",
        key: "bauPercent",
        type: "number",
        unit: "%",
      },
      {
        label: "Quality Status",
        key: "qualityStatus",
        type: "text",
        colSpan: 3,
      },
    ],
  },

  // ================= K.GUDANG =================

  "K.GUDANG": STORAGE_HANDLING_CONFIG,
  OPNAME: STORAGE_OTHER_CONFIG,

  // ================= Others =================

  // OTHERS: {
  //   layout: "checklist",
  //   columns: 3,

  //   fields: [
  //     {
  //       label: "Pindah Stapel",
  //       key: "hasPindahStapel",
  //       type: "boolean",
  //     },
  //     {
  //       label: "Pembersihan",
  //       key: "hasPembersihan",
  //       type: "boolean",
  //     },
  //     {
  //       label: "Perapihan",
  //       key: "hasPerapihan",
  //       type: "boolean",
  //     },
  //     {
  //       label: "Volume Weight",
  //       key: "volumeWeight",
  //       type: "number",
  //     },
  //     {
  //       label: "Worker On Duty",
  //       key: "workerOnDuty",
  //       type: "number",
  //     },
  //     {
  //       label: "Mask",
  //       key: "hasMask",
  //       type: "boolean",
  //     },
  //     {
  //       label: "Safety Glasses",
  //       key: "hasSafetyGlasses",
  //       type: "boolean",
  //     },
  //     {
  //       label: "Hand Gloves",
  //       key: "hasHandGloves",
  //       type: "boolean",
  //     },
  //     {
  //       label: "Helmet",
  //       key: "hasHelmet",
  //       type: "boolean",
  //     },
  //     {
  //       label: "Safety Shoes",
  //       key: "hasSafetyShoes",
  //       type: "boolean",
  //     },
  //     {
  //       label: "Safety Vest",
  //       key: "hasSafetyVest",
  //       type: "boolean",
  //     },
  //   ],
  // },

  OTHERS: STORAGE_OTHER_CONFIG,

  // ================= UNBAGGING =================

  UNBAGGING: {
    layout: "grid",
    columns: 2,

    fields: [
      {
        label: "No. Vehicle",
        key: "noVehicle",
        type: "text",
      },
      {
        label: "No. Container",
        key: "noContainer",
        type: "text",
      },
      {
        label: "No. Seal",
        key: "noSeal",
        type: "text",
      },
      {
        label: "Initial Weight",
        key: "initialWeight",
        type: "number",
      },
      {
        label: "Total Bag",
        key: "totalBag",
        type: "number",
      },
      {
        label: "Final Weight",
        key: "finalWeight",
        type: "number",
      },
      {
        label: "Unit Weight",
        key: "unitWeight",
        type: "number",
      },
      {
        label: "Total Weight",
        key: "totalWeight",
        type: "number",
      },
    ],
  },

  // ================= ALAT BERAT =================

  ALAT_BERAT: {
    layout: "grid",
    columns: 2,

    fields: [
      {
        label: "BL Number",
        key: "blNumber",
        type: "text",
      },
      {
        label: "Start Time",
        key: "startTime",
        type: "text",
      },
      {
        label: "End Time",
        key: "endTime",
        type: "text",
      },
      {
        label: "Standby Duration 1",
        key: "standbyDuration1",
        type: "text",
      },
      {
        label: "Standby Duration 2",
        key: "standbyDuration2",
        type: "text",
      },
      {
        label: "Minimum Duration",
        key: "minimumDuration",
        type: "text",
      },
      {
        label: "Cost Per Hour",
        key: "costPerHour",
        type: "number",
      },
      {
        label: "Total Cost",
        key: "totalCost",
        type: "number",
      },
    ],
  },

  // ================= REBAGGING =================

  REBAGGING: {
    layout: "grid",
    columns: 2,

    fields: [
      {
        label: "Receiver",
        key: "receiver",
        type: "text",
        colSpan: 2,
      },
      {
        label: "No. Vehicle",
        key: "noVehicle",
        type: "text",
      },
      {
        label: "No. Container",
        key: "noContainer",
        type: "text",
      },
      {
        label: "No. Seal",
        key: "noSeal",
        type: "text",
      },
      {
        label: "Initial Weight",
        key: "initialWeight",
        type: "number",
      },
      {
        label: "Final Weight",
        key: "finalWeight",
        type: "number",
      },
      {
        label: "Total Weight",
        key: "totalWeight",
        type: "number",
      },
    ],
  },
};
