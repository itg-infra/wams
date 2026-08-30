import {
    Upload,
} from "lucide-react";
import { useRealizationApprovedBpController } from "../controllers/operationalRealization/realizationApprovedBpcontroller";
import type {
    RealizationApprovedBpApiItem,
    RealizationApprovedBpSortValue,
} from "../types/realizationApprovedBp.type";
import { getPageNumbers } from "../components/getPageNumber";
import { DataTable, type Column } from "../components/ui/table";
import { PageHeader, lastUpdatedLabel } from "../components/ui/page-header";
import { Button } from "../components/ui/button";
import { Toolbar } from "../components/ui/toolbar";
import { SortDropdown, type SortOption } from "../components/ui/sort-dropdown";

const SORT_OPTIONS: SortOption<RealizationApprovedBpSortValue>[] = [
    { label: "Latest", value: "latest" },
    { label: "Oldest", value: "oldest" },
    { label: "Name A-Z", value: "name_asc" },
    { label: "Name Z-A", value: "name_desc" },
];

function ApprovedBudgetPlanTable({
  rows,
  isLoading,
  error,
  onCreateWO,
}: {
  rows: RealizationApprovedBpApiItem[];
  isLoading: boolean;
  error: string | null;
  onCreateWO: (item: RealizationApprovedBpApiItem) => void;
}) {
  const columns: Column<RealizationApprovedBpApiItem>[] = [
    {
      key: "budgetNo",
      header: "Budget No",
      render: (row) => row.budgetPlanCode,
    },
    {
      key: "vendorName",
      header: (
        <>
          Vendor <br /> Name
        </>
      ),
      render: (row) => row.vendorName,
    },
    {
      key: "remark",
      header: "Remark",
      render: (row) => row.remark,
    },
    {
      key: "maker",
      header: "Maker",
      render: (row) => row.makerName,
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
      key: "action",
      header: "Action",
      render: (row) => (
        <Button
          variant="primary"
          type="button"
          onClick={() => {
            console.log("row.isLocked:", row.isLocked);
            console.log("row:", row);
            if (!row.isLocked) onCreateWO(row);
          }}
          disabled={row.isLocked}
          className="h-8.5 min-w-30 text-[15px]"
        >
          Update WO
        </Button>
      ),
    },
  ];

  return (
    <DataTable
      columns={columns}
      data={rows}
      rowKey={(row) => row.budgetPlanId}
      isLoading={isLoading}
      error={error}
      emptyMessage="No approved budget plan found"
      tableClassName="min-w-262.5"
    />
  );
}

export default function RealizationListApprovedBudgetPlan() {
    const {
      realizationApprovedBp,
      isLoading,
      error,
      searchInput,
      sortLabel,
      page,
      totalPages,
      total,
      from,
      to,
      handleSearchChange,
      handleSortChange,
      handlePrevPage,
      handleNextPage,
      handleCreateWO,
    } = useRealizationApprovedBpController();

    return (
        <div className="flex-1 overflow-y-auto  bg-[#F8F8F8]">
            <div className="px-4 md:px-6 pt-7 pb-6 w-full">
                {/* ── Page Header (breadcrumb + title + subtitle) ── */}
                <PageHeader
                    breadcrumbs={[
                        { label: "Dashboard" },
                        { label: "Budgeting" },
                        { label: "Draft" },
                    ]}
                    title="List of Approved Budget Plan"
                    subtitle={lastUpdatedLabel()}
                />

                {/* Toolbar */}
                <Toolbar
                    search={searchInput}
                    onSearchChange={handleSearchChange}
                    showSort={false}
                    filters={<SortDropdown options={SORT_OPTIONS} value={sortLabel} onChange={handleSortChange} />}
                    actions={
                        <Button
                            id="btn_Export"
                            type="button"
                            variant="secondary"
                        >
                            <Upload className="w-4 h-4" />
                            Export Data
                        </Button>
                    }
                />

                {/* Error */}
                {error && (
                    <div className="mt-4 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-500">
                        {error}
                    </div>
                )}

                {/* Table */}
                <div className="mt-4">
                    <ApprovedBudgetPlanTable
                        rows={realizationApprovedBp}
                        isLoading={isLoading}
                        error={error}
                        onCreateWO={handleCreateWO}
                    />
                </div>
            </div>

              <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between mb-5 mx-5 px-1">
                      <p id="lbl_PaginationInfo" className="text-xs sm:text-sm text-gray-500 text-center sm:text-left">
                        Menampilkan <span className="font-medium text-gray-700">{from}</span>{" "}
                        sampai <span className="font-medium text-gray-700">{to}</span> dari{" "}
                        <span className="font-medium text-gray-700">{total}</span> baris
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
                              // onClick={() => handlePageClick(p)}
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
    );
}