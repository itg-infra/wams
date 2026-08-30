// ─── Shipment ─────────────────────────────────────────────────────────────────
export type ShipmentStatus = "processing" | "delivery" | "in_transit" | "delivered";

export interface Shipment {
    id: string;
    containerCode: string;
    shipName: string;
    location: string;
    date: string;          // display string e.g. "9 February"
    status: ShipmentStatus;
    imageType: "truck" | "ship" | "forklift";
}

// ─── Stat Card ────────────────────────────────────────────────────────────────
export interface StatCard {
  label: string;
  value: number;
  growth: string; // e.g. "+ 23,3%"
  icon: "shipments" | "delivery" | "transit" | "pending";
  color: "yellow" | "green" | "red";
}

// ─── Schedule ────────────────────────────────────────────────────────────────
export interface ScheduleItem {
    id: string;
    label: string;
    time: string;
    type: "shipping" | "fumigation";
}

// ─── Template Table ──────────────────────────────────────────────────────────
export type TemplateStatus = "Approved" | "Waiting Approval" | "Draft" | "Closed" | "Rejected";

export interface TemplateRow {
    id: string;
    templateName: string;
    warehouseCode: string;
    warehouseName: string;
    location: string;
    date: string;
    status: TemplateStatus;
}

// ─── Dashboard Store State ───────────────────────────────────────────────────
export interface DashboardState {
    shipments: Shipment[];
    stats: StatCard[];
    schedules: ScheduleItem[];
    templates: TemplateRow[];
    isLoading: boolean;
    fetchDashboard: () => Promise<void>;
}