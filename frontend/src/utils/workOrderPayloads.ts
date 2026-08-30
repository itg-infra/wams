import type { EditWorkOrderPayload } from "../types/editWo.type";

type Params = {
  data: any;
  rows: any[];
  // itemShadowId: number;
  activeTab: string;
  notes: string;
};

export const buildWorkOrderPayload = ({
  data,
  rows,
  // itemShadowId,
  activeTab,
  notes,
}: Params): EditWorkOrderPayload => {
  const basePayload = {
    // budgetPlanId: data?.budgetPlanId,
    // itemShadowId: itemShadowId,
    picUserId: data?.picUserId,

    startDate: data?.startDate,
    endDate: data?.endDate,

    codeBlock: "A3-01",
    notes: notes || null,

    gpsLocation: {
      latitude: -6.1077,
      longitude: 106.8811,
      accuracy: 12.5,
      recordedAt: "2026-05-26T07:30:00Z",
    },
  };

  const row = rows[0];

  switch (activeTab) {
    case "Unloading":
      return {
        ...basePayload,
        unloadingItems: rows.map((row, index) => ({

          blNumber: row.blNumber,
          productName: row.productName,

          quantity: Number(row.quantity),
          uomCode: row.uomCode,

          noVehicle: row.noVehicle,
          noContainer: row.noContainer,
          noSeal: row.noSeal,

          grossWeight: Number(row.grossWeight),
          finalWeight: Number(row.finalWeight),
          nettWeight: Number(row.nettWeight),

          totalBag: Number(row.totalBag),
          unitWeight: Number(row.unitWeight),

          isChecked: row.isChecked,
          sortOrder: index + 1,
        })),
      };

    case "Loading":
      return {
        ...basePayload,
        loadingItems: rows.map((row, index) => ({

          blNumber: row.blNumber,
          productName: row.productName,

          quantity: Number(row.quantity),
          uomCode: row.uomCode,

          noVehicle: row.noVehicle,
          noContainer: row.noContainer,
          noSeal: row.noSeal,

          grossWeight: Number(row.grossWeight),
          finalWeight: Number(row.finalWeight),
          nettWeight: Number(row.nettWeight),

          totalBag: Number(row.totalBag),
          unitWeight: Number(row.unitWeight),

          isChecked: row.isChecked,
          sortOrder: index + 1,
        })),
      };

    case "Fumigation":
      return {
        ...basePayload,
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

    default:
      return basePayload;
  }
};
