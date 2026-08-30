import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  Download,
  Eye,
  Pencil,
  Trash2,
} from "lucide-react";

import { useWorkOrderController } from "../controllers/operationalRealization/listWorkOrderController";
import toast from "react-hot-toast";
import { useExportFileController } from "../controllers/file/exportFileController";
import ExportModal from "../components/modalExportFile";
import type { ExportParams } from "../api/services/file/exportService";
import { DataTable, TablePagination, type Column } from "../components/ui/table";
import { PageHeader, lastUpdatedLabel } from "../components/ui/page-header";
import type { WorkOrderItem } from "../types/workOrder.type";
import { Button } from "../components/ui/button";
import { Toolbar } from "../components/ui/toolbar";

// Values map to `WorkOrderQueryParams["sortBy"]` + sort order. The list is
// paginated by the API, so ordering goes through the controller's handleSort
// instead of reordering the rows already fetched.
const SORT_OPTIONS = [
  { label: "Latest", value: "startDate:desc" },
  { label: "Oldest", value: "startDate:asc" },
  { label: "Status A-Z", value: "status:asc" },
  { label: "Status Z-A", value: "status:desc" },
];

export default function ListWorkOrderScreen() {
  //  const [showError, setShowError] = useState(true);
  const {
    workOrders,
    page,
    total,
    totalPages,
    limit,
    isLoading,
    params,

    fetchWorkOrders,
    handlePageChange,
    handleSearch,
    handleSort,

    handleDeleteWorkOrder,
    deleteLoadingId,

    errorMessage,
    clearError,
  } = useWorkOrderController();

  const { exportWorkOrders, isExporting } = useExportFileController();

  const [isExportModalOpen, setIsExportModalOpen] = useState(false);

  const [exportParams, setExportParams] = useState<ExportParams>({
    format: "Pdf",
    sortOrder: "asc",
  });

  const navigate = useNavigate();

  // ======================================================
  // INITIAL FETCH
  // ======================================================

  useEffect(() => {
    void fetchWorkOrders();
  }, [fetchWorkOrders]);

  useEffect(() => {
    if (errorMessage) {
      toast.error(errorMessage);
      clearError();
    }
  }, [errorMessage, clearError]);

  // ======================================================
  // COLUMNS
  // ======================================================

  const columns: Column<WorkOrderItem>[] = [
    {
      key: "wo_id",
      header: "WO ID",
      className: "font-medium",
      render: (item) => item.code,
    },
    {
      key: "bl_number",
      header: "BL Number",
      render: (item) => item.blNumber || "-",
    },
    {
      key: "vessel",
      header: "Vessel",
      render: (item) => item.vesselName,
    },
    {
      key: "product",
      header: "Product",
      render: (item) => item.productName,
    },
    {
      key: "pic",
      header: "PIC",
      render: (item) => item.picName,
    },
    {
      key: "rfba",
      header: "RFBA",
      render: (item) => (item.isRfba ? "Yes" : "-"),
    },
    {
      key: "date",
      header: "Date",
      render: (item) =>
        new Date(item.startDate).toLocaleDateString("id-ID"),
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
                            item.status === "Submitted"
                              ? `
                                bg-[#c8f1d3]
                                text-[#14804a]
                              `
                              : `
                                bg-[#e5e5e5]
                                text-[#6b7280]
                              `
                          }
                        `}
        >
          {item.status}
        </div>
      ),
    },
    {
      key: "action",
      header: "Action",
      render: (item) => (
        <div className="flex items-center gap-3">
          <button
            id="icn_ViewWorkOrder"
            type="button"
            onClick={() => navigate(`/work-orders/${item.id}`)}
            className="
    text-[#7c7c88]
    transition-all
    hover:text-[#3f2b96]
  "
          >
            <Eye size={15} />
          </button>

          <button
            id="icn_EditWorkOrder"
            disabled={item.status === "Submitted"}
            type="button"
            className="
    text-[#7c7c88]
    transition-all
    hover:text-[#3f2b96]

    disabled:cursor-not-allowed
    disabled:text-gray-300
    disabled:hover:text-gray-300
    disabled:opacity-50
  "
            onClick={() => {
              navigate(`/work-orders/edit/${item.id}`);
            }}
          >
            <Pencil size={15} />
          </button>

          <button
            id="icn_DeleteWorkOrder"
            type="button"
            disabled={deleteLoadingId === item.id}
            onClick={() => {
              void handleDeleteWorkOrder(item.id);
            }}
            className="
    text-[#7c7c88]
    transition-all
    hover:text-[#ef4444]
    disabled:cursor-not-allowed
    disabled:opacity-50
  "
          >
            {deleteLoadingId === item.id ? (
              <div
                className="
        h-3.75
        w-3.75
        animate-spin
        rounded-full
        border-2
        border-[#ef4444]
        border-t-transparent
      "
              />
            ) : (
              <Trash2 size={15} />
            )}
          </button>
        </div>
      ),
    },
  ];

  // ======================================================
  // RENDER
  // ======================================================

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
          await exportWorkOrders(exportParams);
          setIsExportModalOpen(false);
        }}
      />

      <div className="flex-1 p-4 sm:p-6 lg:p-8 overflow-y-auto">
        <PageHeader
          breadcrumbs={[
            { label: "Dashboard" },
            { label: "Operational & Realization" },
          ]}
          title="List of Work Order"
          subtitle={lastUpdatedLabel()}
        />

        <Toolbar
          onSearchChange={(value) => {
            void handleSearch(value);
          }}
          sortOptions={SORT_OPTIONS}
          onSortChange={(value) => {
            const [sortBy, sortOrder] = value.split(":");
            void handleSort(
              sortBy as "status" | "startDate" | "createdAt",
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
        <div id="tbl_WorkOrderList">
          <DataTable
            columns={columns}
            data={workOrders}
            rowKey={(item) => item.id}
            isLoading={isLoading}
            emptyMessage="No data available"
            tableClassName="min-w-237.5"
            striped={false}
            rowClassName="hover:bg-[#fafafa] transition-all"
          />
        </div>

        {/* ====================================================== */}
        {/* PAGINATION */}
        {/* ====================================================== */}

        <TablePagination
          page={page}
          totalPages={totalPages}
          total={total}
          limit={limit}
          onPageChange={(nextPage) => {
            void handlePageChange(nextPage);
          }}
        />
      </div>
    </>
  );
}
