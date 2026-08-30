import React, { useState, useEffect, useMemo } from "react";

import { formatNumber } from "../components/format/formatCurrency";
import { useAccountPayableController } from "../controllers/budgeting/listGeerateApController";

import { useNavigate } from "react-router-dom";
import { useExportFileController } from "../controllers/file/exportFileController";
import type { ExportParams } from "../api/services/file/exportService";
import ExportModal from "../components/modalExportFile";
import { DataTable, type Column } from "../components/ui/table";
import { PageHeader, lastUpdatedLabel } from "../components/ui/page-header";
import { Button } from "../components/ui/button";
import { Toolbar } from "../components/ui/toolbar";
import type { AccountPayableItem } from "../types/listGenerateAp.type";
import type { SortOption } from "../components/ui/sort-dropdown";

const SORT_OPTIONS: SortOption[] = [
  { value: "createdAt", label: "Created At" },
  { value: "docDate", label: "Document Date" },
  { value: "status", label: "Status" },
];

// 2. Definisikan opsi arah urutan
const ORDER_OPTIONS: SortOption[] = [
  { value: "desc", label: "Descending" }, 
  { value: "asc", label: "Ascending" },
];

// ─── Main Page ────────────────────────────────────────────────────────────────
const GenerateAPPage: React.FC = () => {
  const {
    accountPayables,

    isLoading,

    error,

    fetchAccountPayables,

    sortBy,
    sortOrder,
    handleSortChange,
    handleOrderChange,
  } = useAccountPayableController();

  const { exportAccountPayable, isExporting } = useExportFileController();

  const navigate = useNavigate();

  const [isExportModalOpen, setIsExportModalOpen] = useState(false);

  const [exportParams, setExportParams] = useState<ExportParams>({
    format: "Pdf",
    sortOrder: "asc",
  });

  //  const [view, setView] = useState<ActiveView>({
  //    screen: "list",
  //  });

  // ======================================================
  // LOCAL STATE
  // ======================================================

  const [searchQuery, setSearchQuery] = useState("");

  const [currentPage, setCurrentPage] = useState(1);

  const pageSize = 10;

  // ======================================================
  // FETCH
  // ======================================================

  useEffect(() => {
    void fetchAccountPayables();
  }, [fetchAccountPayables]);

  // ======================================================
  // FILTER & SORTING (Client-Side)
  // ======================================================

  const filteredPlans = useMemo(() => {
    const keyword = searchQuery.toLowerCase();

    const matched = accountPayables.filter(
      (item) =>
        item.budgetPlanCode?.toLowerCase().includes(keyword) ||
        item.vendorName?.toLowerCase().includes(keyword) ||
        item.location?.toLowerCase().includes(keyword),
    );

    if (!sortBy) return matched;

    const direction = sortOrder === "asc" ? 1 : -1;

    // Sort a copy — `matched` may alias the store array when nothing is filtered.
    return [...matched].sort((a, b) => {
      if (sortBy === "docDate") {
        return (
          (new Date(a.docDate ?? 0).getTime() -
            new Date(b.docDate ?? 0).getTime()) *
          direction
        );
      }

      if (sortBy === "createdAt") {
        return (
          (new Date(a.createdAt ?? 0).getTime() -
            new Date(b.createdAt ?? 0).getTime()) *
          direction
        );
      }

      if (sortBy === "status") {
        return (
          (a.accountPayableStatus ?? "").localeCompare(
            b.accountPayableStatus ?? "",
            undefined,
            {
              sensitivity: "base",
            },
          ) * direction
        );
      }

      return (
        (a.vendorName ?? "").localeCompare(b.vendorName ?? "", undefined, {
          sensitivity: "base",
        }) * direction
      );
    });
  }, [accountPayables, searchQuery, sortBy, sortOrder]);

  // ======================================================
  // PAGINATION
  // ======================================================

  const totalItems = filteredPlans.length;

  const totalPages = Math.ceil(totalItems / pageSize);

  const paginatedPlans = filteredPlans.slice(
    (currentPage - 1) * pageSize,
    currentPage * pageSize,
  );

  const startItem = totalItems === 0 ? 0 : (currentPage - 1) * pageSize + 1;

  const endItem = Math.min(currentPage * pageSize, totalItems);

  // ======================================================
  // HANDLER
  // ======================================================

  const handleSearch = (value: string) => {
    setSearchQuery(value);

    setCurrentPage(1);
  };

  const handlePageChange = (page: number) => {
    setCurrentPage(page);
  };

  const formatDocDate = (date: string) => {
    return new Date(date).toLocaleDateString("id-ID");
  };

  const columns: Column<AccountPayableItem>[] = [
    {
      key: "budgetNo",
      header: "Budget No",
      className: "whitespace-nowrap",
      render: (plan) => plan.budgetPlanCode ?? "-",
    },
    {
      key: "vendorName",
      header: "Vendor Name",
      className: "whitespace-nowrap",
      render: (plan) => plan.vendorName ?? "-",
    },
    {
      key: "bPlan",
      header: "B.Plan",
      render: (plan) => formatNumber(plan.budgetPlanTotal),
    },
    {
      key: "bRealization",
      header: "B.Realization",
      render: (plan) => formatNumber(plan.budgetApproved),
    },
    {
      key: "bVariance",
      header: "B.Variance",
      render: (plan) => formatNumber(plan.budgetVariance),
    },
    {
      key: "docDate",
      header: "Doc Date",
      className: "whitespace-nowrap",
      render: (plan) => formatDocDate(plan.docDate),
    },
    {
      key: "sapNumber",
      header: "SAP Number",
      className: "whitespace-nowrap",
      render: (plan) =>
        plan.accountPayables?.length ? (
          <div className="flex flex-wrap gap-2 max-w-65">
            {plan.accountPayables.map((ap) => (
              <button
                key={ap.id}
                id="btn_PoNumber"
                onClick={() => navigate(`/generate-ap/detail/${ap.id}`)}
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
                {ap.code}
              </button>
            ))}
          </div>
        ) : (
          <span className="text-gray-400">-</span>
        ),
    },
    // {
    //   key: "status",
    //   header: "Status",
    //   width: "120px",
    //   className: "whitespace-nowrap",
    //   render: (plan) =>
    //     plan.isAllGenerate == false ? (
    //       <Button
    //         id="btn_GenerateAp"
    //         variant="primary"
    //         size="sm"
    //         onClick={() =>
    //           navigate(`/generate-ap/create/${plan.budgetPlanId}`, {
    //             state: {
    //               budgetPlan: plan,
    //             },
    //           })
    //         }
    //       >
    //         Generate AP
    //       </Button>
    //     ) : (
    //       <span
    //         className="
    //   inline-flex items-center justify-center
    //   rounded-lg
    //   border border-blue-200
    //   bg-blue-50
    //   px-3 py-1.5
    //   text-xs sm:text-sm
    //   font-medium
    //   text-blue-700
    // "
    //       >
    //         {plan.accountPayableStatus ?? "-"}
    //       </span>
    //     ),
    // },

     {
          key: "action",
          header: "Action",
          width: "140px",
          className: "whitespace-nowrap",
          render: (plan) => (
            <Button
              id="btn_GenerateAp"
              variant="primary"
              size="sm"
              disabled={plan.isAllGenerate == true}
              onClick={() => {
                const draftAccountPayable = plan.accountPayables?.find(
                  (ap) => ap.status === "Draft",
                );
                const query = draftAccountPayable
                  ? `?accountPayableId=${draftAccountPayable.id}`
                  : "";

                navigate(`/generate-ap/create/${plan.budgetPlanId}${query}`, {
                  state: {
                    budgetPlan: plan,
                    accountPayableId: draftAccountPayable?.id,
                  },
                });
              }}
            >
              Generate AP
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
          await exportAccountPayable(exportParams);
          setIsExportModalOpen(false);
        }}
      />

      <div className="flex-1 p-4 sm:p-6 lg:p-8 overflow-y-auto">
        <div>
          {/* ── Page Header (breadcrumb + title + subtitle) ── */}
          <PageHeader
            breadcrumbs={[{ label: "Budgeting" }, { label: "Generate AP" }]}
            title="Recap of Realizations"
            subtitle={lastUpdatedLabel()}
          />

          {/* ── Toolbar ── */}
          <Toolbar
            search={searchQuery}
            onSearchChange={handleSearch}
            showSort={true}
            sortOptions={SORT_OPTIONS}
            sortValue={sortBy}
            onSortChange={(val) =>
              handleSortChange(val as "status" | "docDate" | "createdAt")
            }
            // --- BAGIAN ORDER BY (Pilih Arah) ---
            showOrder={true}
            orderOptions={ORDER_OPTIONS}
            orderValue={sortOrder}
            onOrderChange={(val) => handleOrderChange(val as "asc" | "desc")}
            actions={
              <Button
                id="btn_Export"
                variant="secondary"
                onClick={() => setIsExportModalOpen(true)}
                disabled={isExporting}
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
            rowKey={(plan) => plan.recapWorkOrderId}
            isLoading={isLoading}
            emptyMessage="No data available"
            tableClassName="min-w-240"
            className="touch-pan-x"
          />

          {/* ── Pagination ── */}
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between mt-3 px-1">
            <p
              id="lbl_PaginationInfo"
              className="text-xs sm:text-sm text-gray-500 text-center sm:text-left"
            >
              Menampilkan{" "}
              <span className="font-medium text-gray-700">{startItem}</span>{" "}
              sampai{" "}
              <span className="font-medium text-gray-700">{endItem}</span> dari{" "}
              <span className="font-medium text-gray-700">{totalItems}</span>{" "}
              baris
            </p>

            <div className="flex items-center justify-center sm:justify-end gap-1 flex-wrap">
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
                    p === 1 ||
                    p === totalPages ||
                    Math.abs(p - currentPage) <= 1
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
      </div>
    </>
  );
};

export default GenerateAPPage;
