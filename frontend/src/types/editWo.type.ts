export type EditWorkOrderItemPayload = {
  itemShadowId?: number;

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

  isChecked: boolean;
  sortOrder: number;
};

export type EditWorkOrderPayload = {
  // budgetPlanId: number;
  // itemShadowId?: number;

  picUserId?: number;

  startDate: string;
  endDate: string;

  codeBlock: string;
  notes: string | null;

  // ================= TRANSPORT =================
  unloadingItems?: EditWorkOrderItemPayload[];

  loadingItems?: EditWorkOrderItemPayload[];

  // ================= FUMIGASI =================
  fumigation?: {
    fumiId: string;

    totalDuration: string;

    blNumber: string;

    mvName: string;

    initialTemperature: number;
    finalTemperature: number;

    fumigationType: string;

    methylBromideDosage: number | null;
    sulphurFluorideDosage: number | null;
    phosphineDosage: number;

    result: string;
  };

  // ================= STORAGE =================
  storage?: {
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
  };

  // ================= QC =================
  qc?: {
    moisturePercent: number;
    jamurPercent: number;
    bauPercent: number;

    qualityStatus: string;
  };

  // ================= HEAVY EQUIPMENT =================
  heavyEquipment?: {
    blNumber: string;

    startTime: string;
    endTime: string;

    standbyDuration1: string;
    standbyDuration2: string;

    minimumDuration: string;

    costPerHour: number;
    totalCost: number;
  };

  // ================= UNBAGGING =================
  unbagging?: {
    noVehicle: string;
    noContainer: string;
    noSeal: string;

    initialWeight: number;

    totalBag: number;

    finalWeight: number;

    unitWeight: number;

    totalWeight: number;
  };

  // ================= REBAGGING =================
  rebagging?: {
    receiver: string;

    noVehicle: string;
    noContainer: string;
    noSeal: string;

    initialWeight: number;

    finalWeight: number;

    totalWeight: number;
  };
};

export type EditWorkOrderResponse = {
  success: boolean;
  message: string;
  requestId: string;
  data: {
    id: number;
  };
};
