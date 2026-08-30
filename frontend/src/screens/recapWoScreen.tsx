import { useEffect, useState } from "react";
// import { useNavigate } from "react-router-dom";
import {
  ChevronLeft,
  ChevronRight,
  Download,
  Eye,
  Pencil,
  Trash2,
} from "lucide-react";

import { useRecapWorkOrderController } from "../controllers/operationalRealization/recapWoController";
import { useNavigate } from "react-router-dom";
import { useExportFileController } from "../controllers/file/exportFileController";
import type { ExportParams } from "../api/services/file/exportService";
import ExportModal from "../components/modalExportFile";
import { Button } from "../components/ui/button";
import { DataTable, type Column } from "../components/ui/table";
import { PageHeader, lastUpdatedLabel } from "../components/ui/page-header";
import { Toolbar } from "../components/ui/toolbar";
import type { RecapWorkOrderItem } from "../types/recapWo.type";
import type { RecapWorkOrderSortBy } from "../api/services/operationalRealization/recapWoService";
// import toast from "react-hot-toast";

// Values map to the columns the API can order by (see RecapWorkOrderRepository).
// The endpoint is paginated, so ordering is sent to the server.
const SORT_OPTIONS = [
  { label: "Latest", value: "docDate:desc" },
  { label: "Oldest", value: "docDate:asc" },
  { label: "Status A-Z", value: "status:asc" },
  { label: "Status Z-A", value: "status:desc" },
];

export default function RecapWoScreen() {
  //  const [showError, setShowError] = useState(true);
  const {
    recapWorkOrders,

    params,

    loading,

    handleFetchRecapWorkOrders,

    handleSortChange,
  } = useRecapWorkOrderController();

  const { exportRecapWorkOrders, isExporting } = useExportFileController();

  const navigate = useNavigate();

  // ======================================================
  // INITIAL FETCH
  // ======================================================

  useEffect(() => {
    void handleFetchRecapWorkOrders();
  }, [handleFetchRecapWorkOrders]);

  //   useEffect(() => {
  //     if (errorMessage) {
  //       toast.error(errorMessage);
  //       clearError();
  //     }
  //   }, [errorMessage, clearError]);

  // ======================================================
  // RENDER
  // ======================================================

  const [isExportModalOpen, setIsExportModalOpen] = useState(false);

  const [search, setSearch] = useState("");

  const [exportParams, setExportParams] = useState<ExportParams>({
    format: "Pdf",
    sortOrder: "asc",
  });

  // Client-side search over the current recap list.
  const filteredRecapWorkOrders = recapWorkOrders.filter((item) => {
    const q = search.toLowerCase();
    return (
      (item.budgetPlanCode ?? "").toLowerCase().includes(q) ||
      (item.blNumbers ?? "").toLowerCase().includes(q) ||
      (item.remark ?? "").toLowerCase().includes(q) ||
      (item.picNames ?? "").toLowerCase().includes(q)
    );
  });

  const columns: Column<RecapWorkOrderItem>[] = [
    {
      key: "budgetId",
      header: "budget ID",
      className: "font-medium",
      render: (item) => item.budgetPlanCode,
    },
    {
      key: "blNumber",
      header: "BL Number",
      render: (item) => item.blNumbers || "-",
    },
    {
      key: "remark",
      header: "Remark",
      render: (item) => item.remark,
    },
    {
      key: "pic",
      header: "PIC",
      render: (item) => item.picNames,
    },
    {
      key: "rfba",
      header: "RFBA",
      render: (item) => (item.isRfba ? "Yes" : "-"),
    },
    {
      key: "date",
      header: "Date",
      render: (item) => new Date(item.docDate).toLocaleDateString("id-ID"),
    },
    {
      key: "status",
      header: "Status",
      render: (item) => (
        <div
          className={`
      inline-flex
      min-w-19.5
      items-center
      justify-center
      rounded-full
      px-3
      py-1
      text-[10px]
      font-semibold
      ${
        item.recapStatus === "Approved"
          ? "bg-[#c8f1d3] text-[#14804a]"
          : item.recapStatus === "Pending"
            ? "bg-[#fff3cd] text-[#b58105]"
            : item.recapStatus === "Rejected"
              ? "bg-[#fde2e1] text-[#d1293d]"
              : "bg-[#e5e5e5] text-[#6b7280]"
      }
    `}
        >
          {item.recapStatus}
        </div>
      ),
    },
    {
      key: "action",
      header: "Action",
      render: (item) => (
        <div className="flex items-center gap-3">
          <button
            id="icn_ViewRecapWo"
            type="button"
            onClick={() => navigate(`/recap-work-orders/${item.id}`)}
            className="
    text-[#7c7c88]
    transition-all
    hover:text-[#3f2b96]
  "
          >
            <Eye size={15} />
          </button>

          <button
            id="icn_EditRecapWo"
            type="button"
            className="
                            text-[#7c7c88]
                            transition-all
                            hover:text-[#3f2b96]
                          "
          >
            <Pencil size={15} />
          </button>

          <button
            id="icn_DeleteRecapWo"
            type="button"
            className="
                                text-[#7c7c88]
                                transition-all
                                hover:text-[#ef4444]
                            "
          >
            <Trash2 size={15} />
          </button>
        </div>
      ),
    },
  ];

  return (
    <>
      <ExportModal
        open={isExportModalOpen}
        title="Export Budget Templates"
        loading={isExporting}
        params={exportParams}
        setParams={setExportParams}
        onClose={() => setIsExportModalOpen(false)}
        onSubmit={async () => {
          await exportRecapWorkOrders(exportParams);
          setIsExportModalOpen(false);
        }}
      />
      <div className="flex-1 p-4 sm:p-6 lg:p-8 overflow-y-auto">
        {/* ── Page Header (breadcrumb + title + subtitle) ── */}
        <PageHeader
          breadcrumbs={[
            { label: "Dashboard" },
            { label: "Operational & Realization" },
          ]}
          title="List of Work Order"
          subtitle={lastUpdatedLabel()}
        />
        <Toolbar
          search={search}
          onSearchChange={setSearch}
          sortOptions={SORT_OPTIONS}
          onSortChange={(value) => {
            const [sortBy, sortOrder] = value.split(":");
            void handleSortChange(
              sortBy as RecapWorkOrderSortBy,
              sortOrder as "asc" | "desc",
            );
          }}
          sortValue={
            SORT_OPTIONS.find(
              (o) => o.value === `${params.sortBy}:${params.sortOrder}`,
            )?.label
          }
          actions={
            <Button
              id="btn_Export"
              onClick={() => setIsExportModalOpen(true)}
              disabled={isExporting}
              type="button"
              variant="secondary"
            >
              <Download size={14} />
              <span>{isExporting ? "Export..." : "Export Data"}</span>
            </Button>
          }
        />
        {/* ====================================================== */}
        {/* TABLE */}
        {/* ====================================================== */}
        <div
          id="tbl_RecapWo"
          className="overflow-hidden rounded-xl border border-[#e5e7eb] bg-white"
        >
          <DataTable
            columns={columns}
            data={filteredRecapWorkOrders}
            rowKey={(item) => item.id}
            isLoading={loading}
            emptyMessage="No data available"
            tableClassName="min-w-225"
            striped={false}
            rowClassName="transition-all hover:bg-[#fafafa]"
          />

          {/* ====================================================== */}
          {/* FOOTER */}
          {/* ====================================================== */}

          <div
            className="
  flex
  flex-col
  gap-3
  md:flex-row
  md:items-center
  md:justify-between
  border-t
  border-[#f1f1f1]
  px-4
  py-4
"
          >
            {/* INFO */}

            <p
              id="lbl_PaginationInfo"
              className="text-center md:text-left text-[11px] text-[#7c7c88]"
            >
              Menampilkan{" "}
              <span className="font-semibold">{filteredRecapWorkOrders.length}</span>{" "}
              sampai <span className="font-semibold">10</span> dari{" "}
              <span className="font-semibold">20</span> baris
            </p>

            {/* PAGINATION */}

            <div className="flex items-center gap-2">
              {/* PREV */}

              <button
                id="btn_PrevPage"
                type="button"
                disabled
                onClick={() => {}}
                className="
                flex
                h-7
                w-7
                items-center
                justify-center
                rounded-lg
                border
                border-[#d9d9d9]
                bg-white
                text-[#7c7c88]
                transition-all
                hover:bg-[#f3f0ff]
                disabled:cursor-not-allowed
                disabled:opacity-50
              "
              >
                <ChevronLeft size={14} />
              </button>

              {/* PAGE */}

              <div
                className="
                flex
                h-7
                min-w-7
                items-center
                justify-center
                rounded-lg
                bg-[#f3f0ff]
                px-2
                text-[11px]
                font-semibold
                text-[#3f2b96]
              "
              >
                1
              </div>

              {/* NEXT */}

              <button
                id="btn_NextPage"
                type="button"
                disabled
                onClick={() => {}}
                className="
                flex
                h-7
                w-7
                items-center
                justify-center
                rounded-lg
                border
                border-[#d9d9d9]
                bg-white
                text-[#7c7c88]
                transition-all
                hover:bg-[#f3f0ff]
                disabled:cursor-not-allowed
                disabled:opacity-50
              "
              >
                <ChevronRight size={14} />
              </button>
            </div>
          </div>
        </div>
      </div>
    </>
  );
}
