export type WorkOrderRow = {
  id: number;

  source: "budgetPlan" | "transportOrder";

  // ================= K.BONGKAR & K.MUAT =================

  blNumber: string;
  productName: string;

  quantity: number;
  uomCode: string;

  noVehicle: string;
  noContainer: string;
  noSeal: string;

  grossWeight: number;
  finalWeight: number;
  nettWeight: number;

  totalBag: number;
  unitWeight: number;

  // ================= FUMIGASI =================

  fumiId: string;
  totalDuration: string;

  mvName: string;

  initialTemperature: number;
  finalTemperature: number;

  fumigationType: string;

  methylBromideDosage: number | null;
  sulphurFluorideDosage: number | null;
  phosphineDosage: number;

  result: string;

  // ================= QC =================

  moisturePercent: number;
  jamurPercent: number;
  bauPercent: number;

  qualityStatus: string;

  // ================= K.GUDANG =================

  hasPindahStapel: boolean;
  hasPembersihan: boolean;
  hasPerapihan: boolean;

  volumeWeight: number;
  workerOnDuty: number;

  hasMask: boolean;
  hasSafetyGlasses: boolean;
  hasHandGloves: boolean;

  hasHelmet: boolean;
  hasSafetyShoes: boolean;
  hasSafetyVest: boolean;

  // ================= REBAGGING & UNBAGGING =================

  receiver: string;

  initialWeight: number;
  totalWeight: number;

  // ================= Alat berat =================

  startTime: string;
  endTime: string;
  standbyDuration1: string;
  standbyDuration2: string;
  minimumDuration: string;
  costPerHour: number;
  totalCost: number;

  // ================= GLOBAL =================

  isChecked: boolean;
  sortOrder: number;
};

export const createEmptyRow = (id: number): WorkOrderRow => ({
  id,

  source: "budgetPlan",

  // ================= K.BONGKAR & K.MUAT =================
  blNumber: "",
  productName: "",

  quantity: 0,
  uomCode: "",

  noVehicle: "",
  noContainer: "",
  noSeal: "",

  grossWeight: 0,
  finalWeight: 0,
  nettWeight: 0,

  totalBag: 0,
  unitWeight: 0,

  // ================= FUMIGASI =================
  fumiId: "",
  totalDuration: "",

  mvName: "",

  initialTemperature: 0,
  finalTemperature: 0,

  fumigationType: "",

  methylBromideDosage: null,
  sulphurFluorideDosage: null,
  phosphineDosage: 0,

  result: "",

  // ================= QC =================
  moisturePercent: 0,
  jamurPercent: 0,
  bauPercent: 0,

  qualityStatus: "",

  // ================= K.GUDANG =================
  hasPindahStapel: false,
  hasPembersihan: false,
  hasPerapihan: false,

  volumeWeight: 0,
  workerOnDuty: 0,

  hasMask: false,
  hasSafetyGlasses: false,
  hasHandGloves: false,

  hasHelmet: false,
  hasSafetyShoes: false,
  hasSafetyVest: false,

  // ================= REBAGGING & UNBAGGING =================
  receiver: "",

  initialWeight: 0,
  totalWeight: 0,

  // ================= GLOBAL =================
  isChecked: false,
  sortOrder: 1,

 // ================= Alat berat =================

  startTime: "",
  endTime: "",
  standbyDuration1: "",
  standbyDuration2: "",
  minimumDuration: "",
  costPerHour: 0,
  totalCost: 0,
});