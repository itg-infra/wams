import React, { useState } from "react";
import { useApprovedBudgetPlanController } from "../controllers/budgeting/listGeneratePo";
import type {
  ApprovedBudgetPlan,
  SortField,
} from "../types/listGeneratePo.type";
import { useNavigate } from "react-router-dom";
import { formatNumber } from "../components/format/formatCurrency";
import { useExportFileController } from "../controllers/file/exportFileController";
import type { ExportParams } from "../api/services/file/exportService";
import ExportModal from "../components/modalExportFile";
import { DataTable, type Column } from "../components/ui/table";
import { PageHeader, lastUpdatedLabel } from "../components/ui/page-header";
import { Button } from "../components/ui/button";
import { Toolbar } from "../components/ui/toolbar";
import { SortDropdown, type SortOption } from "../components/ui/sort-dropdown";

// ─── Sort Option Dropdown ─────────────────────────────────────────────────────
const SORT_OPTIONS: SortOption<SortField>[] = [
  { value: "budgetPlanCode", label: "Budget No" },
  { value: "vendorName", label: "Vendor Name" },
  { value: "makerName", label: "Maker" },
  { value: "docDate", label: "Doc Date" },
  { value: "approvalName", label: "Approval" },
];

const ORDER_OPTIONS: SortOption[] = [
  { value: "asc", label: "Ascending" },
  { value: "desc", label: "Descending" },
];

// ─── Main Page ────────────────────────────────────────────────────────────────
const GeneratePOPage: React.FC = () => {
  const {
    paginatedPlans,
    isLoading,
    error,
    searchQuery,
    currentPage,
    pageSize,
    totalItems,
    totalPages,
    sortDirection,
    handleSearch,
    handleSort,
    handleOrder,
    handlePageChange,
    formatDocDate,
    handleNavigatePO,
  } = useApprovedBudgetPlanController();

  const { exportPurchaseOrder, isExporting } = useExportFileController();

  const [isExportModalOpen, setIsExportModalOpen] = useState(false);

  const [exportParams, setExportParams] = useState<ExportParams>({
    format: "Pdf",
    sortOrder: "asc",
  });

  const navigate = useNavigate();

  const startItem = totalItems === 0 ? 0 : (currentPage - 1) * pageSize + 1;
  const endItem = Math.min(currentPage * pageSize, totalItems);

  const columns: Column<ApprovedBudgetPlan>[] = [
    {
      key: "budgetNo",
      header: "Budget No",
      width: "140px",
      className: "whitespace-nowrap",
      render: (plan) => plan.budgetPlanCode,
    },
    {
      key: "vendorName",
      header: "Vendor Name",
      width: "220px",
      render: (plan) => plan.vendorName,
    },
    {
      key: "totalBudget",
      header: "Total Budget",
      width: "150px",
      render: (plan) => formatNumber(plan.totalBudgetPlan) || "-",
    },
    {
      key: "budgetApproved",
      header: "Budget Approved",
      width: "150px",
      render: (plan) => formatNumber(plan.budgetApproved),
    },
    {
      key: "budgetVariance",
      header: "Budget Variance",
      width: "150px",
      render: (plan) => formatNumber(plan.budgetVariance),
    },
    {
      key: "docDate",
      header: "Doc Date",
      width: "120px",
      className: "whitespace-nowrap",
      render: (plan) => formatDocDate(plan.docDate),
    },
    {
      key: "poNumber",
      header: "PO Number",
      width: "280px",
      render: (plan) =>
        plan.purchaseOrders?.length ? (
          <div className="flex flex-wrap gap-2 max-w-65">
            {plan.purchaseOrders.map((po) => (
              <button
                key={po.id}
                id="btn_PoNumber"
                onClick={() => navigate(`/generate-po/detail/${po.id}`)}
                className="
            inline-flex items-center justify-center
            rounded-lg
            border border-blue-200
            bg-blue-50
            px-3 py-1.5
            text-xs sm:text-sm
            font-medium
            text-blue-700
            transition-all
            hover:bg-blue-600
            hover:text-white
            hover:border-blue-600
            focus:outline-none
            focus:ring-2
            focus:ring-blue-300
            cursor-pointer
          "
              >
                {po.code}
              </button>
            ))}
          </div>
        ) : (
          <span className="text-gray-400">-</span>
        ),
    },
    {
      key: "action",
      header: "Action",
      width: "140px",
      className: "whitespace-nowrap",
      render: (plan) => (
        <Button
          id="btn_GeneratePo"
          variant="primary"
          size="sm"
          disabled={plan.allGenerated === true}
          onClick={() => handleNavigatePO(plan)}
        >
          Generate PO
        </Button>
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
          await exportPurchaseOrder(exportParams);
          setIsExportModalOpen(false);
        }}
      />

      <div className="flex-1 p-4 sm:p-6 lg:p-8 overflow-y-auto">
        {/* ── Page Header (breadcrumb + title + subtitle) ── */}
        <PageHeader
          breadcrumbs={[{ label: "Budgeting" }, { label: "Generate PO" }]}
          title="List of Approved Budget Plan"
          subtitle={lastUpdatedLabel()}
        />

        {/* ── Toolbar ── */}
        <Toolbar
          search={searchQuery}
          onSearchChange={handleSearch}
          showSort={false}
          filters={
            <SortDropdown options={SORT_OPTIONS} onChange={handleSort} />
          }
          showOrder={true}
          orderOptions={ORDER_OPTIONS}
          orderValue={sortDirection}
          onOrderChange={(val) => handleOrder(val as "asc" | "desc")}
          actions={
            <Button
              variant="secondary"
              id="btn_Export"
              onClick={() => setIsExportModalOpen(true)}
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
                  d="M4 16v2a2 2 0 002 2h12a2 2 0 002-2v-2M12 12V4m0 8l-3-3m3 3l3-3"
                />
              </svg>
              {isExporting ? "Export..." : "Export Data"}
            </Button>
          }
        />

        {/* ── Error ── */}
        {error && (
          <div className="mb-4 p-3 bg-red-50 border border-red-200 rounded text-sm text-red-600">
            {error}
          </div>
        )}

        {/* ── Table: overflow-x-auto + minWidth agar badge tidak terpotong ── */}
        <DataTable
          columns={columns}
          data={paginatedPlans}
          rowKey={(plan) => plan.budgetPlanId}
          isLoading={isLoading}
          error={error}
          emptyMessage="No data available"
          tableClassName="min-w-300"
        />

        {/* ── Pagination ── */}
        <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between mt-3 px-1">
          <p
            id="lbl_PaginationInfo"
            className="text-xs sm:text-sm text-gray-500 text-center sm:text-left"
          >
            Menampilkan{" "}
            <span className="font-medium text-gray-700">{startItem}</span>{" "}
            sampai <span className="font-medium text-gray-700">{endItem}</span>{" "}
            dari <span className="font-medium text-gray-700">{totalItems}</span>{" "}
            baris
          </p>

          <div className="flex items-center justify-center sm:justify-start gap-1 flex-wrap">
            {/* Prev */}
            <button
              id="btn_PrevPage"
              onClick={() => handlePageChange(currentPage - 1)}
              disabled={currentPage === 1}
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

            {/* Page numbers */}
            {Array.from({ length: totalPages }, (_, i) => i + 1)
              .filter((p) => {
                if (totalPages <= 5) return true;
                return (
                  p === 1 || p === totalPages || Math.abs(p - currentPage) <= 1
                );
              })
              .reduce<(number | "...")[]>((acc, p, idx, arr) => {
                if (
                  idx > 0 &&
                  typeof arr[idx - 1] === "number" &&
                  (p as number) - (arr[idx - 1] as number) > 1
                ) {
                  acc.push("...");
                }
                acc.push(p);
                return acc;
              }, [])
              .map((p, i) =>
                p === "..." ? (
                  <span
                    key={`ellipsis-${i}`}
                    className="w-8 h-8 flex items-center justify-center text-gray-400 text-sm"
                  >
                    ...
                  </span>
                ) : (
                  <button
                    key={p}
                    onClick={() => handlePageChange(p as number)}
                    className={`w-8 h-8 flex items-center justify-center border rounded text-sm font-medium transition-colors ${
                      currentPage === p
                        ? "bg-gray-700 text-white border-gray-700"
                        : "border-gray-300 bg-white text-gray-600 hover:bg-gray-50"
                    }`}
                  >
                    {p}
                  </button>
                ),
              )}

            {/* Next */}
            <button
              id="btn_NextPage"
              onClick={() => handlePageChange(currentPage + 1)}
              disabled={currentPage === totalPages}
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
};

export default GeneratePOPage;
