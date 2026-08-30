import {
    Upload,
    ChevronLeft,
    ChevronRight,
} from "lucide-react";
import type {
    // ApprovedBudgetPlanItem,
    ApprovedBudgetPlanSortValue,
} from "../types/approvedBudgetPlan.type";
import { useApprovedBudgetPlansController } from "../controllers/operationalRealization/listApprovedBudgetPlanController";
import type { ApprovedBudgetPlanApiItem } from "../types/listApproveBudgetPlan.type";
import { DataTable, type Column } from "../components/ui/table";
import { PageHeader, lastUpdatedLabel } from "../components/ui/page-header";
import { Button } from "../components/ui/button";
import { Toolbar } from "../components/ui/toolbar";
import { SortDropdown, type SortOption } from "../components/ui/sort-dropdown";

const SORT_OPTIONS: SortOption<ApprovedBudgetPlanSortValue>[] = [
    { label: "Latest", value: "latest" },
    { label: "Oldest", value: "oldest" },
    { label: "Name A-Z", value: "name_asc" },
    { label: "Name Z-A", value: "name_desc" },
];

// Columns mirror the original 7-header / 6-cell table exactly: the six body
// cells fill the first six columns and the "Action" column body stays empty.
const columns: Column<ApprovedBudgetPlanApiItem>[] = [
    {
        key: "templateId",
        header: "Template ID",
        render: (row) => row.activityTypeName,
    },
    {
        key: "templateName",
        header: (<>Template <br /> Name</>),
        headerClassName: "leading-4.5",
        render: (row) => row.warehouseCode ?? "Warehouse",
    },
    {
        key: "warehouseCode",
        header: (<>Warehouse <br /> Code</>),
        headerClassName: "leading-4.5",
        render: (row) => row.warehouseName,
    },
    {
        key: "warehouseName",
        header: (<>Warehouse <br /> Name</>),
        headerClassName: "leading-4.5",
        // Original cell rendered the literal "Jakarta" (row.location was commented out).
        render: () => "Jakarta",
    },
    {
        key: "location",
        header: "Location",
        render: (row) => row.docDate,
    },
    {
        key: "date",
        header: "Date",
        render: () => (
            <Button
                id="btn_CreateWorkOrder"
                type="button"
                variant="primary"
                // onClick={() => onCreateWO(row)}
                className="h-8.5 px-3 rounded-xl text-[15px]"
            >
                Create WO
            </Button>
        ),
    },
    {
        key: "action",
        header: "Action",
        render: () => null,
    },
];

export default function ApprovedBudgetPlanScreen() {
    const {
        approvedBudgetPlans,
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
        // handleCreateWO,
    } = useApprovedBudgetPlansController();

    return (
        <div className="flex-1 p-4 sm:p-6 lg:p-8 overflow-y-auto">
            <div className="w-full max-w-7xl">
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
                    <DataTable
                        columns={columns}
                        data={approvedBudgetPlans}
                        rowKey={(row) => row.templateCode}
                        isLoading={isLoading}
                        error={error}
                        emptyMessage="No approved budget plan found"
                        tableClassName="min-w-262.5 border-collapse"
                        className="bg-white border-[#D7DEE8] rounded-2xl"
                        skeletonRows={8}
                    />
                </div>

                {/* Footer Pagination */}
                <div className="mt-4 flex flex-col md:flex-row md:items-center md:justify-between gap-4">
                    <p id="lbl_PaginationInfo" className="text-[14px] text-[#3F3F46]">
                        Menampilkan [{from}] sampai [{to}] dari [{total}] baris
                    </p>

                    <div className="flex items-center justify-end gap-3">
                        <button
                            id="btn_PrevPage"
                            type="button"
                            onClick={handlePrevPage}
                            disabled={page === 1}
                            className="w-10 h-10 rounded-[10px] bg-[#A7A7B0] text-white flex items-center justify-center disabled:opacity-50 disabled:cursor-not-allowed"
                        >
                            <ChevronLeft className="w-5 h-5" />
                        </button>

                        <button
                            type="button"
                            className="w-10 h-10 rounded-[10px] bg-[#D9DEE8] text-black text-[20px] font-medium"
                        >
                            {page}
                        </button>

                        <button
                            id="btn_NextPage"
                            type="button"
                            onClick={handleNextPage}
                            disabled={page === totalPages}
                            className="w-10 h-10 rounded-[10px] bg-[#A7A7B0] text-white flex items-center justify-center disabled:opacity-50 disabled:cursor-not-allowed"
                        >
                            <ChevronRight className="w-5 h-5" />
                        </button>
                    </div>
                </div>
            </div>
        </div>
    );
}