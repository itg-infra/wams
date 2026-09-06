import type { WorkOrderRow } from "../config/workOrderRowConfig";
import { buildWorkOrderDetailPayload } from "./workOrderDetailPayload";
import type { EditWorkOrderPayload } from "../types/editWo.type";

type Params = {
  data: {
    picUserId?: number | null;
    startDate?: string | null;
    endDate?: string | null;
  } | null;
  rows: WorkOrderRow[];
  activityTypeCode: string;
  notes: string;
};

export const buildWorkOrderPayload = ({
  data,
  rows,
  activityTypeCode,
  notes,
}: Params): EditWorkOrderPayload => ({
  picUserId: data?.picUserId ?? undefined,
  startDate: data?.startDate ?? null,
  endDate: data?.endDate ?? null,
  codeBlock: "A3-01",
  notes: notes || null,
  gpsLocation: {
    latitude: -6.1077,
    longitude: 106.8811,
    accuracy: 12.5,
    recordedAt: "2026-05-26T07:30:00Z",
  },
  ...buildWorkOrderDetailPayload(activityTypeCode, rows),
});
