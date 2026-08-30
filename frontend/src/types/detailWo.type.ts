export interface GpsLocation {
  latitude: number;
  longitude: number;
  accuracy: number;
  recordedAt: string;
}

export interface StorageItem {
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
}

export interface QcItem {
  moisturePercent: number;
  jamurPercent: number;
  bauPercent: number;

  qualityStatus: string;
}

export interface HeavyEquipmentItem {
  blNumber: string;

  startTime: string;
  endTime: string;

  standbyDuration1: string;
  standbyDuration2: string;

  minimumDuration: string;

  costPerHour: number;
  totalCost: number;
}

export interface UnbaggingItem {
  noVehicle: string;
  noContainer: string;
  noSeal: string;

  initialWeight: number;

  totalBag: number;

  finalWeight: number;

  unitWeight: number;

  totalWeight: number;
}

export interface RebaggingItem {
  receiver: string;

  noVehicle: string;
  noContainer: string;
  noSeal: string;

  initialWeight: number;

  finalWeight: number;

  totalWeight: number;
}

export interface UnloadingItem {
  id: number;
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
}

export interface LoadingItem {
  id: number;
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
}

export interface FumigationItem {
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
}

export interface WorkOrderDetail {
  id: number;
  code: string;
  budgetPlanId: number;
  budgetPlanCode: string;
  activityTypeCode: string;
  itemShadowId: number;
  activityName: string;
  warehouseShadowId: number;
  warehouseCode: string;
  warehouseName: string;
  templateCode: string;
  vendorName: string;
  codeBlock: string;
  picUserId: number;
  picName: string;
  startDate: string;
  endDate: string;
  isRfba: boolean;
  status: string;
  notes: string;
  gpsLocation: GpsLocation | null;

  productName: string;
  quantity: number;
  uomCode: string;
  blNumber: string;
  vesselName: string;

  transportOrders: unknown | null;

  unloadingItems: UnloadingItem[] | null;
  loadingItems: LoadingItem[] | null;

  fumigation: FumigationItem | null;

  others: StorageItem | null;
  qc: QcItem | null;
  heavyEquipment: HeavyEquipmentItem | null;
  unbagging: UnbaggingItem | null;
  rebagging: RebaggingItem | null;

  createdAt: string;
  createdByName: string;
  submittedAt: string | null;
  submittedByName: string | null;
}

export interface WorkOrderDetailResponse {
  success: boolean;
  data: WorkOrderDetail;
  message: string;
  requestId: string;
}
