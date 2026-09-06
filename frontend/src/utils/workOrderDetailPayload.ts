import type { CreateWorkOrderPayload } from "../types/createWorkOrder.type";
import type { WorkOrderRow } from "../config/workOrderRowConfig";
import { getWorkOrderActivityDefinition } from "../config/workOrderActivityDefinitions";

export type WorkOrderDetailPayload = Partial<CreateWorkOrderPayload>;

export function buildStorageHandlingDetail(
  row: WorkOrderRow,
): NonNullable<CreateWorkOrderPayload["storage"]> {
  return {
    hasPindahStapel: row.hasPindahStapel,
    hasPembersihan: row.hasPembersihan,
    hasPerapihan: row.hasPerapihan,
    volumeWeight: Number(row.volumeWeight || 0),
    workerOnDuty: Number(row.workerOnDuty || 0),
    hasMask: row.hasMask,
    hasSafetyGlasses: row.hasSafetyGlasses,
    hasHandGloves: row.hasHandGloves,
    hasHelmet: row.hasHelmet,
    hasSafetyShoes: row.hasSafetyShoes,
    hasSafetyVest: row.hasSafetyVest,
  };
}

export function buildWorkOrderDetailPayload(
  code: string,
  rows: WorkOrderRow[],
): WorkOrderDetailPayload {
  const definition = getWorkOrderActivityDefinition(code);
  if (!definition) throw new Error(`Unsupported work order activity: ${code}`);

  const row = rows[0];

  switch (definition.formKind) {
    case "unloading":
      return {
        unloadingItems: rows.map((item, index) => ({
          blNumber: item.blNumber,
          productName: item.productName,
          quantity: Number(item.quantity),
          uomCode: item.uomCode,
          noVehicle: item.noVehicle,
          noContainer: item.noContainer,
          noSeal: item.noSeal,
          grossWeight: Number(item.grossWeight),
          finalWeight: Number(item.finalWeight),
          nettWeight: Number(item.nettWeight),
          totalBag: Number(item.totalBag),
          unitWeight: Number(item.unitWeight),
          isChecked: item.isChecked,
          sortOrder: index + 1,
        })),
      };
    case "loading":
      return {
        loadingItems: rows.map((item, index) => ({
          blNumber: item.blNumber,
          productName: item.productName,
          quantity: Number(item.quantity),
          uomCode: item.uomCode,
          noVehicle: item.noVehicle,
          noContainer: item.noContainer,
          noSeal: item.noSeal,
          grossWeight: Number(item.grossWeight),
          finalWeight: Number(item.finalWeight),
          nettWeight: Number(item.nettWeight),
          totalBag: Number(item.totalBag),
          unitWeight: Number(item.unitWeight),
          isChecked: item.isChecked,
          sortOrder: index + 1,
        })),
      };
    case "fumigation":
      return {
        fumigation: {
          fumiId: row?.fumiId || "",
          totalDuration: row?.totalDuration || "",
          blNumber: row?.blNumber || "",
          mvName: row?.mvName || "",
          initialTemperature: Number(row?.initialTemperature || 0),
          finalTemperature: Number(row?.finalTemperature || 0),
          fumigationType: row?.fumigationType || "",
          methylBromideDosage: row?.methylBromideDosage ?? null,
          sulphurFluorideDosage: row?.sulphurFluorideDosage ?? null,
          phosphineDosage: Number(row?.phosphineDosage || 0),
          result: row?.result || "",
        },
      };
    case "storageHandling":
      return { [definition.detailKey]: buildStorageHandlingDetail(row) };
    case "qc":
      return {
        qc: {
          moisturePercent: Number(row?.moisturePercent || 0),
          jamurPercent: Number(row?.jamurPercent || 0),
          bauPercent: Number(row?.bauPercent || 0),
          qualityStatus: row?.qualityStatus || "",
        },
      };
    case "heavyEquipment":
      return {
        heavyEquipment: {
          blNumber: row?.blNumber || "",
          startTime: row?.startTime || "",
          endTime: row?.endTime || "",
          standbyDuration1: row?.standbyDuration1 || "",
          standbyDuration2: row?.standbyDuration2 || "",
          minimumDuration: row?.minimumDuration || "",
          costPerHour: Number(row?.costPerHour || 0),
          totalCost: Number(row?.totalCost || 0),
        },
      };
    case "unbagging":
      return {
        unbagging: {
          noVehicle: row?.noVehicle || "",
          noContainer: row?.noContainer || "",
          noSeal: row?.noSeal || "",
          initialWeight: Number(row?.initialWeight || 0),
          totalBag: Number(row?.totalBag || 0),
          finalWeight: Number(row?.finalWeight || 0),
          unitWeight: Number(row?.unitWeight || 0),
          totalWeight: Number(row?.totalWeight || 0),
        },
      };
    case "rebagging":
      return {
        rebagging: {
          receiver: row?.receiver || "",
          noVehicle: row?.noVehicle || "",
          noContainer: row?.noContainer || "",
          noSeal: row?.noSeal || "",
          initialWeight: Number(row?.initialWeight || 0),
          finalWeight: Number(row?.finalWeight || 0),
          totalWeight: Number(row?.totalWeight || 0),
        },
      };
  }
}
