// import { create } from "zustand";
// import type {
//     DashboardState, Shipment, StatCard, ScheduleItem, TemplateRow,
// } from "../types/dashboard.type";



// const DUMMY_SHIPMENTS: Shipment[] = [
//     {
//         id: "1",
//         containerCode: "ONEYVTZF01978300",
//         shipName: "Container/Ship Name",
//         location: "MNP Blok A, Lampung",
//         date: "9 February",
//         status: "delivered",
//         imageType: "truck",
//     },
//     {
//         id: "2",
//         containerCode: "ONEYVTZF01978300",
//         shipName: "Container/Ship Name",
//         location: "Yangshang Deep Water Port, Zhejiang",
//         date: "1 March – 5 March",
//         status: "delivery",
//         imageType: "ship",
//     },
//     {
//         id: "3",
//         containerCode: "ONEYVTZF01978300",
//         shipName: "Container/Ship Name",
//         location: "Ulsan Port, Ulsan",
//         date: "28 March",
//         status: "delivery",
//         imageType: "ship",
//     },
//     {
//         id: "4",
//         containerCode: "ONEYVTZF01978300",
//         shipName: "Container/Ship Name",
//         location: "Ulsan Port, Ulsan",
//         date: "4 April",
//         status: "processing",
//         imageType: "forklift",
//     },
// ];

// const DUMMY_SCHEDULES: ScheduleItem[] = [
//     { id: "1", label: "Shipping [Name Product]", time: "10.00 WIB", type: "shipping" },
//     { id: "2", label: "Fumigation at [Name Warehouse]", time: "10.00 WIB", type: "fumigation" },
// ];

// const DUMMY_TEMPLATES: TemplateRow[] = [
//     { id: "1", templateName: "K.Bongkar", warehouseCode: "WHLPG01", warehouseName: "MNP Blok A", location: "Lampung", date: "02/03/2026", status: "Approved" },
//     { id: "2", templateName: "K.Muat", warehouseCode: "WHLPG01", warehouseName: "MNP Blok A", location: "Lampung", date: "02/03/2026", status: "Waiting Approval" },
//     { id: "3", templateName: "Fumigasi", warehouseCode: "WHLPG01", warehouseName: "MNP Blok A", location: "Lampung", date: "02/03/2026", status: "Draft" },
//     { id: "4", templateName: "Opname", warehouseCode: "WHLPG01", warehouseName: "MNP Blok A", location: "Lampung", date: "02/03/2026", status: "Closed" },
//     { id: "5", templateName: "K.Bongkar", warehouseCode: "WHLPG01", warehouseName: "MNP Blok A", location: "Lampung", date: "02/03/2026", status: "Rejected" },
// ];

// // ─── Store ───────────────────────────────────────────────────────────────────
// export const useDashboardStore = create<DashboardState>((set) => ({
//     shipments: [],
//     stats: [],
//     schedules: [],
//     templates: [],
//     isLoading: false,

//     fetchDashboard: async () => {
//         set({ isLoading: true });
//         // Simulasi network delay
//         await new Promise((r) => setTimeout(r, 600));
//         set({
//             stats: DUMMY_STATS,
//             shipments: DUMMY_SHIPMENTS,
//             schedules: DUMMY_SCHEDULES,
//             templates: DUMMY_TEMPLATES,
//             isLoading: false,
//         });
//     },
// }));