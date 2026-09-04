// workOrderMapper.ts
import { createEmptyRow, type WorkOrderRow } from "../config/workOrderRowConfig";
import type { LoadingItem, UnloadingItem, WorkOrderDetail } from "./detailWo.type";

let rowIdCounter = 0;
const nextId = () => ++rowIdCounter;

// const baseFromDetail = (detail: WorkOrderDetail): Partial<WorkOrderRow> => ({
//   // field-field global yang mau selalu ikut ke setiap row, kalau ada
//   // contoh: kalau WorkOrderRow butuh productName default dari root
// });

function mapUnloadingItem(item: UnloadingItem): WorkOrderRow {
  return {
    ...createEmptyRow(item.id ?? nextId()),
    source: "budgetPlan",
    blNumber: item.blNumber,
    productName: item.productName,
    quantity: item.quantity,
    uomCode: item.uomCode,
    noVehicle: item.noVehicle,
    noContainer: item.noContainer,
    noSeal: item.noSeal,
    grossWeight: item.grossWeight,
    finalWeight: item.finalWeight,
    nettWeight: item.nettWeight,
    totalBag: item.totalBag,
    unitWeight: item.unitWeight,
    isChecked: item.isChecked,
    sortOrder: item.sortOrder,
  };
}

function mapLoadingItem(item: LoadingItem): WorkOrderRow {
  return {
    ...createEmptyRow(item.id ?? nextId()),
    source: "budgetPlan",
    blNumber: item.blNumber,
    productName: item.productName,
    quantity: item.quantity,
    uomCode: item.uomCode,
    noVehicle: item.noVehicle,
    noContainer: item.noContainer,
    noSeal: item.noSeal,
    grossWeight: item.grossWeight,
    finalWeight: item.finalWeight,
    nettWeight: item.nettWeight,
    totalBag: item.totalBag,
    unitWeight: item.unitWeight,
    isChecked: item.isChecked,
    sortOrder: item.sortOrder,
  };
}

export function mapWorkOrderDetailToRows(
  detail: WorkOrderDetail,
): WorkOrderRow[] {
  const code = detail.activityTypeCode;

  console.log(`Code activity: ${code}`)

  switch (code) {
    case "K.BONGKAR": {
      const items = detail.unloadingItems ?? [];
      return items.map(mapUnloadingItem);
    }

    case "K.MUAT": {
      const items = detail.loadingItems ?? [];
      return items.map(mapLoadingItem);
    }

    case "FUMIGASI": {
      const f = detail.fumigation;
      if (!f) return [];
      return [
        {
          ...createEmptyRow(nextId()),
          fumiId: f.fumiId,
          totalDuration: f.totalDuration,
          blNumber: f.blNumber,
          mvName: f.mvName,
          initialTemperature: f.initialTemperature,
          finalTemperature: f.finalTemperature,
          fumigationType: f.fumigationType,
          methylBromideDosage: f.methylBromideDosage,
          sulphurFluorideDosage: f.sulphurFluorideDosage,
          phosphineDosage: f.phosphineDosage,
          result: f.result,
        },
      ];
    }

    case "QC": {
      const qc = detail.qc;
      if (!qc) return [];
      return [
        {
          ...createEmptyRow(nextId()),
          moisturePercent: qc.moisturePercent,
          jamurPercent: qc.jamurPercent,
          bauPercent: qc.bauPercent,
          qualityStatus: qc.qualityStatus,
        },
      ];
    }

    // case "K.GUDANG":
    case "OTHERS": {
      const s = detail.storage;

      // Jika data 'others' dari backend kosong (belum diisi),
      // berikan nilai default (false / 0) agar form TETEAP MUNCUL.
      if (!s) {
        return [
          {
            ...createEmptyRow(nextId()),
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
          },
        ];
      }

      // Jika data 'others' sudah ada dari backend (saat mode Edit)
      return [
        {
          ...createEmptyRow(nextId()),
          hasPindahStapel: s.hasPindahStapel ?? false,
          hasPembersihan: s.hasPembersihan ?? false,
          hasPerapihan: s.hasPerapihan ?? false,
          volumeWeight: s.volumeWeight ?? 0,
          workerOnDuty: s.workerOnDuty ?? 0,
          hasMask: s.hasMask ?? false,
          hasSafetyGlasses: s.hasSafetyGlasses ?? false,
          hasHandGloves: s.hasHandGloves ?? false,
          hasHelmet: s.hasHelmet ?? false,
          hasSafetyShoes: s.hasSafetyShoes ?? false,
          hasSafetyVest: s.hasSafetyVest ?? false,
        },
      ];
    }

    case "UNBAGGING": {
      const u = detail.unbagging;
      if (!u) return [];
      return [
        {
          ...createEmptyRow(nextId()),
          noVehicle: u.noVehicle,
          noContainer: u.noContainer,
          noSeal: u.noSeal,
          initialWeight: u.initialWeight,
          totalBag: u.totalBag,
          finalWeight: u.finalWeight,
          unitWeight: u.unitWeight,
          totalWeight: u.totalWeight,
        },
      ];
    }

    case "REBAGGING": {
      const r = detail.rebagging;
      if (!r) return [];
      return [
        {
          ...createEmptyRow(nextId()),
          receiver: r.receiver,
          noVehicle: r.noVehicle,
          noContainer: r.noContainer,
          noSeal: r.noSeal,
          initialWeight: r.initialWeight,
          finalWeight: r.finalWeight,
          totalWeight: r.totalWeight,
        },
      ];
    }

    case "ALAT_BERAT": {
      const h = detail.heavyEquipment;
      if (!h) return [];
      return [
        {
          ...createEmptyRow(nextId()),
          blNumber: h.blNumber,
          startTime: h.startTime,
          endTime: h.endTime,
          standbyDuration1: h.standbyDuration1,
          standbyDuration2: h.standbyDuration2,
          minimumDuration: h.minimumDuration,
          costPerHour: h.costPerHour,
          totalCost: h.totalCost,
        },
      ];
    }

    default:
      return [];
  }
}
