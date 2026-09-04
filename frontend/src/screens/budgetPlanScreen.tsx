import { useSearchParams } from "react-router-dom";
import { DownloadIcon, Eye, Printer, Trash2, Pencil } from "lucide-react";
import { useBudgetPlanController } from "../controllers/budgeting/budgetPlanController";
import type {
  BudgetPlanStatus,
  BudgetPlanItem,
} from "../types/budgetPlan.type";
import { DataTable, type Column } from "../components/ui/table";
import { useState } from "react";
import { budgetPlanService } from "../api/services/budgeting/budgetPlan/budgetPlanService";
import { useNavigate } from "react-router-dom";
import { useExportFileController } from "../controllers/file/exportFileController";
import ExportModal from "../components/modalExportFile";
import type { ExportParams } from "../api/services/file/exportService";
import { getPageNumbers } from "../components/getPageNumber";
import { PageHeader, lastUpdatedLabel } from "../components/ui/page-header";
import { Button } from "../components/ui/button";
import { Toolbar } from "../components/ui/toolbar";
import type { SortOption } from "../components/ui/sort-dropdown";

const STATUS_STYLES: Record<BudgetPlanStatus, string> = {
  Draft: "bg-slate-100 text-slate-700 border border-slate-300",

  Submitted: "bg-blue-100 text-blue-700 border border-blue-300",

  InApproval: "bg-amber-100 text-amber-700 border border-amber-300",

  Approved: "bg-emerald-100 text-emerald-700 border border-emerald-300",

  Rejected: "bg-red-100 text-red-700 border border-red-300",
};

export function StatusBadge({ status }: { status: BudgetPlanStatus }) {
  return (
    <span
      className={`inline-flex items-center justify-center min-w-26 px-4 py-1 rounded-xl text-[13px] font-medium leading-none ${STATUS_STYLES[status]}`}
    >
      {status}
    </span>
  );
}

const SORT_OPTIONS: SortOption[] = [
  { value: "docDate", label: "Document Date" },
  { value: "budgetNo", label: "Budget No" },
  { value: "templateCode", label: "Template Code" },
  { value: "location", label: "Location" },
];

// 2. Definisikan opsi arah urutan
const ORDER_OPTIONS: SortOption[] = [
  { value: "asc", label: "Ascending" },
  { value: "desc", label: "Descending" },
];

export default function BudgetPlanScreen() {
  const [searchParams] = useSearchParams();
  const statusFromUrl = searchParams.get("status");

  const KNOWN_STATUSES: BudgetPlanStatus[] = [
    "Draft",
    "Submitted",
    "InApproval",
    "Approved",
    "Rejected",
  ];
  const initialStatus = KNOWN_STATUSES.includes(
    statusFromUrl as BudgetPlanStatus,
  )
    ? (statusFromUrl as BudgetPlanStatus)
    : undefined;
  const {
    plans,
    isLoading,
    error,
    params,
    page,
    totalPages,
    handleSearchChange,
    handleSortChange,
    handleOrderChange,
    handlePageClick,
    handleNextPage,
    handlePrevPage,
    handleRefresh,
  } = useBudgetPlanController({ initialStatus });

  const { exportBudgetPlans, isExporting } = useExportFileController();

  const navigate = useNavigate();

  // 2. Tambah state dan handler — letakkan setelah deklarasi `view` state
  const [deletingId, setDeletingId] = useState<string | null>(null);

  const handleDelete = async (id: string) => {
    if (!confirm("Apakah Anda yakin ingin menghapus budget plan ini?")) return;
    try {
      setDeletingId(id);
      await budgetPlanService.deleteBudgetPlan(id);
      handleRefresh();
    } catch (err) {
      console.error(err);
      alert("Gagal menghapus budget plan.");
    } finally {
      setDeletingId(null);
    }
  };

  const canEdit = (status: BudgetPlanStatus) =>
    status !== "Submitted" && status !== "InApproval" && status !== "Approved";

  const canDelete = (status: BudgetPlanStatus) => status === "Draft";

  const [isExportModalOpen, setIsExportModalOpen] = useState(false);

  const [exportParams, setExportParams] = useState<ExportParams>({
    format: "Pdf",
    sortOrder: "asc",
  });

  const columns: Column<BudgetPlanItem>[] = [
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
      key: "maker",
      header: "Maker",
      render: (row) => row.makerName ?? "-",
    },
    {
      key: "rfba",
      header: "RFBA",
      render: (row) => (row.isRfba ? "Yes" : "No"),
    },
    {
      key: "docDate",
      header: "Doc Date",
      render: (row) => row.docDate,
    },
    {
      key: "status",
      header: "Status",
      render: (row) => <StatusBadge status={row.status} />,
    },
    {
      key: "action",
      header: "Action",
      render: (row) => (
        <div className="flex items-center gap-3">
          {/* View */}
          <button
            id="icn_ViewBudgetPlan"
            onClick={() => navigate(`/budgeting/plan/${row.id}`)}
            className="text-gray-500 hover:text-indigo-600 transition-colors"
            title="View detail"
          >
            <Eye size={18} />
          </button>

          <button
            id="icn_EditBudgetPlan"
            onClick={() => navigate(`/budgeting/plan/edit/${row.id}`)}
            disabled={!canEdit(row.status)}
            title={canEdit(row.status) ? "Edit" : "Cannot edit"}
            className={`transition ${
              !canEdit(row.status)
                ? "text-[#D1D5DB] cursor-not-allowed"
                : "text-[#7A7A7A] hover:text-indigo-700 cursor-pointer"
            }`}
          >
            <Pencil className="w-5 h-5" />
          </button>

          {/* Print */}
          <button
            id="icn_PrintBudgetPlan"
            className="text-gray-500 hover:text-indigo-600 transition-colors"
          >
            <Printer size={18} />
          </button>

          {/* Delete */}
          <button
            id="icn_DeleteBudgetPlan"
            onClick={() => canDelete(row.status) && handleDelete(row.id)}
            disabled={!canDelete(row.status) || deletingId === row.id}
            className={`transition-colors ${
              !canDelete(row.status) || deletingId === row.id
                ? "text-[#D1D5DB] cursor-not-allowed"
                : "text-gray-500 hover:text-red-600"
            }`}
            title={canDelete(row.status) ? "Delete" : "Cannot delete"}
          >
            <Trash2 size={18} />
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
          await exportBudgetPlans(exportParams);
          setIsExportModalOpen(false);
        }}
      />

      <div className="flex-1 p-4 sm:p-6 lg:p-8 overflow-y-auto">
        {/* ── Page Header (breadcrumb + title + subtitle) ── */}
        <PageHeader
          breadcrumbs={[
            { label: "Dashboard" },
            { label: "Budgeting" },
            { label: "Budget Plan" },
          ]}
          title="List of Budget Plan"
          subtitle={lastUpdatedLabel()}
        />

        {/* Toolbar */}
        <Toolbar
          search={params.search ?? ""}
          onSearchChange={handleSearchChange}
          // --- BAGIAN SORT BY (Pilih Kolom) ---
          showSort={true}
          sortOptions={SORT_OPTIONS}
          sortValue={params.sortBy ?? "docDate"} // Sinkron dengan state aktif
          onSortChange={(val) =>
            handleSortChange(
              val as "budgetNo" | "templateCode" | "location" | "docDate",
            )
          }
          // --- BAGIAN ORDER BY (Pilih Arah) ---
          showOrder={true}
          orderOptions={ORDER_OPTIONS}
          orderValue={params.sortOrder ?? "desc"} // Sinkron dengan state aktif
          onOrderChange={(val) => handleOrderChange(val as "asc" | "desc")}
          actions={
            <Button
              id="btn_Export"
              variant="secondary"
              onClick={() => setIsExportModalOpen(true)}
            >
              <DownloadIcon />
              {isExporting ? "Exporting..." : "Export Data"}
            </Button>
          }
        />

        {/* Table */}
        <DataTable
          columns={columns}
          data={plans}
          rowKey={(row) => row.id}
          isLoading={isLoading}
          error={error}
          onRetry={handleRefresh}
          emptyMessage="No data found."
        />

        <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between mt-3 px-1">
          <p className="text-xs sm:text-sm text-gray-500 text-center sm:text-left">
            Menampilkan <span className="font-medium text-gray-700">{10}</span>{" "}
            sampai <span className="font-medium text-gray-700">{10}</span> dari{" "}
            <span className="font-medium text-gray-700">{10}</span> baris
          </p>

          <div className="flex items-center justify-center sm:justify-start gap-1 flex-wrap">
            <button
              id="btn_PrevPage"
              onClick={handlePrevPage}
              disabled={page === 1}
              className="w-8 h-8 flex items-center justify-center border border-gray-300 rounded bg-white text-gray-600 hover:bg-gray-50 disabled:opacity-40 disabled:cursor-not-allowed"
            >
              <svg
                className="w-4 h-4"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M15 19l-7-7 7-7"
                />
              </svg>
            </button>

            {getPageNumbers(page, totalPages).map((p, idx) =>
              p === "..." ? (
                <span
                  key={`ellipsis-${idx}`}
                  className="w-8 h-8 flex items-center justify-center text-gray-400 text-sm"
                >
                  ...
                </span>
              ) : (
                <button
                  key={p}
                  onClick={() => handlePageClick(p)}
                  className={`w-8 h-8 flex items-center justify-center border rounded text-sm font-medium transition-colors ${
                    p === page
                      ? "bg-gray-700 text-white border-gray-700"
                      : "border-gray-300 bg-white text-gray-600 hover:bg-gray-50"
                  }`}
                >
                  {p}
                </button>
              ),
            )}

            <button
              id="btn_NextPage"
              onClick={handleNextPage}
              disabled={page === totalPages}
              className="w-8 h-8 flex items-center justify-center border border-gray-300 rounded bg-white text-gray-600 hover:bg-gray-50 disabled:opacity-40 disabled:cursor-not-allowed"
            >
              <svg
                className="w-4 h-4"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M9 5l7 7-7 7"
                />
              </svg>
            </button>
          </div>
        </div>
      </div>
    </>
  );
}
