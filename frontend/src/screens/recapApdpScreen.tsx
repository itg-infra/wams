"use client";

import { useMemo, useState } from "react";
import { useExportFileController } from "../controllers/file/exportFileController";
import type { ExportRCAParams } from "../api/services/file/exportService";
import ExportModalRCA from "../components/modalExportRCAFile";
import { useNavigate } from "react-router-dom";
import { EyeIcon, File } from "lucide-react";
import { formatCurrency } from "../components/format/formatCurrency";
import { formatDate } from "../components/format/dateTimeFormat";
import { getPageNumbers } from "../components/getPageNumber";
import { useRecapApdpListController } from "../controllers/finance/recapApdpController";
import { DataTable, type Column } from "../components/ui/table";
import { PageHeader, lastUpdatedLabel } from "../components/ui/page-header";
import { Button } from "../components/ui/button";
import { Toolbar } from "../components/ui/toolbar";
import type { BudgetPlanListItem } from "../types/recapApdp.type";

// ─── Icons ─────────────────────────────────────────────

function ExportIcon() {
  return (
    <svg
      width="18"
      height="18"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
    >
      <path d="M12 3v10M8 7l4-4 4 4" />
      <path d="M4 17h12" />
    </svg>
  );
}

// ─── Main Component ────────────────────────────────────

export default function RecapApdpScreen() {
  const [search, setSearch] = useState("");
  const navigate = useNavigate();

  const { isExporting, exportRca, exportRFBA } = useExportFileController();

  const handlePrint = async (id: number) => {
    try {
      await exportRFBA(Number(id));
    } catch (error) {
      console.error("Gagal export purchase order:", error);
    }
  };

  // ── data asli dari API (list + pagination) ──
  const {
    data: list,
    meta,
    isLoading,
    error,
    goToPage,
  } = useRecapApdpListController({ page: 1, limit: 10 });

  const handlePageClick = (page: number) => {
    if (page === meta?.page) return;
    goToPage(page);
  };

  // search client-side di halaman yang sedang dimuat
  const filteredData = useMemo(
    () =>
      list.filter(
        (item) =>
          item.budgetPlanCode.toLowerCase().includes(search.toLowerCase()) ||
          item.vendorName.toLowerCase().includes(search.toLowerCase()),
      ),
    [list, search],
  );

  const [isExportModalOpen, setIsExportModalOpen] = useState(false);
  const [exportParams, setExportParams] = useState<ExportRCAParams>({
    warehouseCode: "",
    dateFrom: "",
    dateTo: "",
  });

  const columns: Column<BudgetPlanListItem>[] = [
    {
      key: "budgetNo",
      header: "Budget No",
      render: (item) => (
        <div className="font-medium text-gray-900">{item.budgetPlanCode}</div>
      ),
    },
    {
      key: "vendorName",
      header: "Vendor Name",
      className: "text-wrap",
      render: (item) => item.vendorName,
    },
    {
      key: "apdp",
      header: "APDP",
      className: "text-nowrap",
      render: () => "AP-2607001",
    },
    {
      key: "totalBudget",
      header: "Total Budget",
      className: "text-nowrap",
      render: (item) => formatCurrency(item.totalBudgetPlan),
    },
    {
      key: "budgetApproved",
      header: "Budget Approved",
      className: "text-nowrap",
      render: (item) => formatCurrency(item.budgetApproved),
    },
    {
      key: "budgetVariance",
      header: "Budget Variance",
      className: "whitespace-nowrap",
      render: (item) => (
        <span
          className={
            item.budgetVariance !== 0 ? "text-red-500" : "text-gray-900"
          }
        >
          {item.budgetVariance !== 0
            ? `- ${formatCurrency(item.budgetVariance)}`
            : formatCurrency(item.budgetVariance)}
        </span>
      ),
    },
    {
      key: "docDate",
      header: "Doc Date",
      render: (item) => formatDate(item.docDate),
    },
    {
      key: "poNumber",
      header: "Po Number",
      render: (item) =>
        item.purchaseOrders?.length ? (
          <div className="flex flex-wrap gap-2 max-w-65">
            {item.purchaseOrders.map((po) => (
              <button
                id="btn_ViewPurchaseOrder"
                key={po.id}
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
                    text-nowrap
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
      key: "report",
      header: "Report",
      align: "center",
      render: (item) => (
        <div className="flex items-center justify-center gap-2 whitespace-nowrap">
          <button
            id="icn_ViewReport"
            onClick={() => navigate(`/finance/report/${item.budgetPlanId}`)}
            className="flex h-8 w-8 items-center justify-center rounded-md border border-gray-300 hover:bg-gray-100"
          >
            <EyeIcon className="h-4 w-4" />
          </button>

          <button
            id="icn_PrintReport"
            onClick={()=> {
              handlePrint(item.budgetPlanId)
            }}
            className="flex h-8 w-8 items-center justify-center rounded-md border border-gray-300 hover:bg-gray-100"
          >
            <File className="h-4 w-4" />
          </button>
        </div>
      ),
    },
  ];

  return (
    <>
      <ExportModalRCA
        open={isExportModalOpen}
        title="Export Budget Templates"
        loading={isExporting}
        params={exportParams}
        setParams={setExportParams}
        onClose={() => setIsExportModalOpen(false)}
        onSubmit={async () => {
          await exportRca(exportParams);
          setIsExportModalOpen(false);
        }}
      />

      <div className="flex-1 p-4 sm:p-6 lg:p-8 overflow-y-auto">
        <PageHeader
          breadcrumbs={[
            { label: "Dashboard" },
            { label: "Recap Purchase Order" },
            { label: "Recap APDP" },
          ]}
          title="List of Recap APDP"
          subtitle={lastUpdatedLabel()}
        />

        <Toolbar
          search={search}
          onSearchChange={setSearch}
          actions={
            <Button
              id="btn_Export"
              variant="primary"
              onClick={() => setIsExportModalOpen(true)}
              disabled={isExporting}
            >
              <ExportIcon />
              {isExporting ? "Export..." : "Export Data"}
            </Button>
          }
        />

        <DataTable
          columns={columns}
          data={filteredData}
          rowKey={(item) => item.budgetPlanId}
          isLoading={isLoading}
          error={error}
          emptyMessage="Tidak ada data"
          tableClassName="min-w-225"
          rowClassName="hover:bg-gray-100"
        />
        {/* Pagination */}
        {meta && meta.totalPages > 1 && (
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between mt-3 px-1">
            <span>
              Halaman {meta.page} dari {meta.totalPages} ({meta.total} data)
            </span>
            <div className="flex items-center justify-center sm:justify-start gap-1 flex-wrap">
              <button
                id="btn_PrevPage"
                disabled={meta.page <= 1}
                onClick={() => goToPage(meta.page - 1)}
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
              {getPageNumbers(meta.page, meta.totalPages).map((p, idx) =>
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
                      p === meta.page
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
                disabled={meta.page >= meta.totalPages}
                onClick={() => goToPage(meta.page + 1)}
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
        )}
      </div>
    </>
  );
}
