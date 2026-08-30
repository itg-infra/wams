import { useEffect, useMemo } from "react";
import { useAuthStore } from "../store/authStore";
// import { useDashboardStore } from "../store/dashboardStore";
import { Skeleton, StatCardBox } from "../components/statCardBox";
import { useHistoryActivityController } from "../controllers/dashboard/historyController";
import { ChevronLeft, ChevronRight } from "lucide-react";
import { formatDate } from "../components/format/dateTimeFormat";
import { useDashboardSummaryController } from "../controllers/dashboard/dashboardSummaryController";
import type { StatCard } from "../types/dashboard.type";
import type { HistoryActivity } from "../types/historyActivity.type";
import { DataTable, type Column } from "../components/ui/table";

export type HistoryActivityStatus =
  | "Draft"
  | "Submitted"
  | "InApproval"
  | "Approved"
  | "Rejected";

const STATUS_STYLES: Record<HistoryActivityStatus, string> = {
  Draft: "bg-slate-100 text-slate-700 border border-slate-300",

  Submitted: "bg-blue-100 text-blue-700 border border-blue-300",

  InApproval: "bg-amber-100 text-amber-700 border border-amber-300",

  Approved: "bg-emerald-100 text-emerald-700 border border-emerald-300",

  Rejected: "bg-red-100 text-red-700 border border-red-300",
};

export function StatusBadge({ status }: { status: HistoryActivityStatus }) {
  return (
    <span
      className={`inline-flex items-center justify-center min-w-26 px-4 py-1 rounded-xl text-[13px] font-medium leading-none ${STATUS_STYLES[status]}`}
    >
      {status}
    </span>
  );
}

export default function DashboardContent() {
  // const { isLoading, fetchDashboard } = useDashboardStore();
  const { user } = useAuthStore();

  const {
    activities,
    meta,
    isLoadingHistory,
    error,
    fetchActivities,
    handlePageChange,
    page,
  } = useHistoryActivityController();

  const { summary, loading } = useDashboardSummaryController();

  const stats: StatCard[] = useMemo(
    () => [
      {
        label: "Budget Achieved",
        value: summary?.budgetAchievedPercent ?? 0,
        growth: `Budget Rp ${(
          summary?.totalBudgetValue ?? 0
        ).toLocaleString("id-ID")}`,
        icon: "shipments",
        color: "green",
      },
      {
        label: "PO Without AP",
        value: summary?.activePoWithoutApCount ?? 0,
        growth: `${
          summary?.newPoWithoutApLast7DaysCount ?? 0
        } new in last 7 days`,
        icon: "delivery",
        color: "yellow",
      },
      {
        label: "Open Work Order",
        value: summary?.openWorkOrderCount ?? 0,
        growth: `${
          summary?.activeWarehouseCount ?? 0
        } active warehouse`,
        icon: "transit",
        color: "green",
      },
      {
        label: "Pending Approval",
        value: summary?.pendingApprovalCount ?? 0,
        growth: `${summary?.overdueApprovalCount ?? 0} overdue`,
        icon: "pending",
        color: "red",
      },
    ],
    [summary]
  );

  useEffect(() => {
    fetchActivities();
  }, []);

  // useEffect(() => {
  //   fetchDashboard();
  // }, [fetchDashboard]);

  const firstName = user?.fullname?.split(" ")[0] ?? "Nama";

  const columns: Column<HistoryActivity>[] = [
    {
      key: "budgetNo",
      header: "Budget No",
      render: (row) => row.budgetNo,
    },
    {
      key: "vendorName",
      header: "Vendor Name",
      render: (row) => row.vendorName ?? "-",
    },
    {
      key: "remark",
      header: "Remark",
      render: (row) => row.remark ?? "-",
    },
    {
      key: "rfba",
      header: "RFBA",
      render: (row) => (row.anyRfba ? "Yes" : "No"),
    },
    {
      key: "location",
      header: "Location",
      render: (row) => row.location ?? "-",
    },
    {
      key: "date",
      header: "Date",
      render: (row) => formatDate(row.date),
    },
    {
      key: "status",
      header: "Status",
      render: (row) => <StatusBadge status={row.statusDisplay} />,
    },
  ];

  return (
    <div
      id="lbl_DashboardContent"
      className="flex-1 p-6 flex flex-col gap-5 overflow-y-auto"
    >
      {/* ── Hero Banner ── */}
     <div
        className="relative rounded-2xl overflow-hidden px-8 py-8 flex items-center min-h-[110px] bg-cover bg-center"
        style={{ backgroundImage: "url('/FrameHeader.png')" }}
      >
        <div className="relative z-10">
          <h2 className="text-white text-xl font-bold">
            Selamat Datang {firstName} di Dashboard GCU
          </h2>
          <p className="text-indigo-200 text-sm mt-0.5">
            {new Intl.DateTimeFormat("id-ID", {
              weekday: "long",
              day: "numeric",
              month: "long",
              year: "numeric",
            }).format(new Date())}
          </p>
        </div>
      </div>

      {/* ── Stat Cards ── */}
      {loading ? (
        <div className="grid grid-cols-4 gap-4">
          {Array(4)
            .fill(0)
            .map((_, i) => (
              <Skeleton key={i} className="h-24" />
            ))}
        </div>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-4">
          {loading
            ? Array.from({ length: 4 }).map((_, index) => (
                <Skeleton key={index} className="h-28" />
              ))
            : stats.map((s) => <StatCardBox key={s.label} stat={s} />)}
        </div>
      )}

      <div className="self-stretch justify-start text-black text-3xl font-bold leading-none flex items-center shrink-0">
        Aktivitas Hari ini
      </div>

      <div className="bg-white border border-gray-200 rounded-lg overflow-hidden">
        <DataTable
          columns={columns}
          data={activities}
          rowKey={(row) => row.budgetPlanId}
          isLoading={isLoadingHistory}
          error={error}
          emptyMessage="No data found."
        />

        {/* Footer */}
        <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between px-5 py-3 border-t border-gray-100">
          <p className="text-xs text-gray-400 text-center md:text-left">
            Showing {(meta?.page ?? 1 - 1) * (meta?.limit ?? 20) + 1} to{" "}
            {Math.min(
              (meta?.page ?? 1) * (meta?.limit ?? 20),
              meta?.total ?? 0,
            )}{" "}
            of {meta?.total ?? 0} entries
          </p>

          <div className="flex items-center justify-center gap-1">
            <button
              id="btn_PrevPage"
              onClick={() => handlePageChange(page - 1)}
              disabled={page <= 1}
              className="w-8 h-8 flex items-center justify-center border border-gray-300 rounded text-gray-500 hover:bg-gray-50 transition-colors disabled:opacity-50"
            >
              <ChevronLeft />
            </button>

            <button className="w-8 h-8 flex items-center justify-center border border-gray-300 rounded bg-white text-gray-800 text-sm font-medium">
              {meta?.page ?? 1}
            </button>

            <button
              id="btn_NextPage"
              onClick={() => handlePageChange(page + 1)}
              disabled={page >= (meta?.totalPages ?? 1)}
              className="w-8 h-8 flex items-center justify-center border border-gray-300 rounded text-gray-500 hover:bg-gray-50 transition-colors disabled:opacity-50"
            >
              <ChevronRight />
            </button>
          </div>
        </div>
      </div>

      {/* ── Recent Shipment ── */}
      {/* <div className="bg-white rounded-xl border border-gray-100 shadow-sm p-5">
        <div className="flex items-center justify-between mb-4">
          <div className="flex items-center gap-2">
            <FileText className="w-4 h-4 text-gray-600" />
            <h3 className="text-sm font-bold text-gray-800">Recent Shipment</h3>
          </div>
          <button className="flex items-center gap-1.5 text-xs text-gray-500 border border-gray-200 rounded-lg px-3 py-1.5 hover:bg-gray-50 transition">
            <Filter className="w-3 h-3" />
            Filter
          </button>
        </div>
        {isLoading ? (
          <div className="flex flex-col gap-3">
            {Array(3)
              .fill(0)
              .map((_, i) => (
                <Skeleton key={i} className="h-24" />
              ))}
          </div>
        ) : (
          <div className="flex flex-col gap-3">
            {shipments.map((s) => (
              <ShipmentCard key={s.id} shipment={s} />
            ))}
          </div>
        )}
      </div> */}

      {/* ── Calendar + Today ── */}
      {/* <div className="grid grid-cols-2 gap-4">
        <CalendarWidget />
        <TodaySchedule schedules={schedules} />
      </div> */}

      {/* ── Template Table ── */}
      {/* {isLoading ? (
        <Skeleton className="h-48" />
      ) : (
        <TemplateTable rows={templates} />
      )} */}
    </div>
  );
}
