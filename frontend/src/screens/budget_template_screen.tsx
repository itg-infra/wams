import { useEffect, useRef, useState } from "react";
import {
  Plus,
  Upload,
  Eye,
  Pencil,
  Trash2,
  ChevronLeft,
  ChevronRight,
  MoreHorizontal,
} from "lucide-react";
import { useBudgetTemplateController } from "../controllers/budgeting/budgetTemplateController";
import type {
  BudgetTemplateItem,
  BudgetTemplateStatus,
} from "../types/budgetTemplate.type";
import PermissionGuard from "../components/guards/permissionGuard";
import { useAuthStore } from "../store/authStore";
import { useNavigate } from "react-router-dom";
import { useExportFileController } from "../controllers/file/exportFileController";
import type { ExportParams } from "../api/services/file/exportService";
import ExportModal from "../components/modalExportFile";
import { getPageNumbers } from "../components/getPageNumber";
import { DataTable, type Column } from "../components/ui/table";
import { PageHeader, lastUpdatedLabel } from "../components/ui/page-header";
import { Button } from "../components/ui/button";
import { Toolbar } from "../components/ui/toolbar";
import { type SortOption } from "../components/ui/sort-dropdown";

const STATUS_STYLES: Record<BudgetTemplateStatus, string> = {
  Submitted: "bg-[#8BE6A2] text-[#226A39] border border-[#8BE6A2]",
  Approved: "bg-[#8BE6A2] text-[#226A39] border border-[#8BE6A2]",
  Draft: "bg-[#E6E6E6] text-[#6B6B6B] border border-[#C8C8C8]",
  Rejected: "bg-[#ff0101] text-[#ffffff] border border-[#ff0101]",
};

export function StatusBadge({ status }: { status: BudgetTemplateStatus }) {
  return (
    <span
      className={`inline-flex items-center justify-center min-w-22.5 whitespace-nowrap px-4 py-1 rounded-xl text-[13px] font-medium leading-none ${STATUS_STYLES[status]}`}
    >
      {status}
    </span>
  );
}

function BudgetTemplateTable({
  rows,
  // onEdit,
  onDelete,
  // onCreatePlan,
}: {
  rows: BudgetTemplateItem[];
  // onEdit: (item: BudgetTemplateItem) => void;
  onDelete: (item: BudgetTemplateItem) => void;
  // onCreatePlan: (item: BudgetTemplateItem) => void; // ✅ FIX
}) {
  const hasRole = useAuthStore((s) => s.hasRole);
  const isSuperAdmin = hasRole("SUPER_ADMIN");
  const isWarehouseHead = hasRole("WAREHOUSE_HEAD");

  const navigate = useNavigate();

  const columns: Column<BudgetTemplateItem>[] = [
    {
      key: "templateId",
      header: "Template ID",
      className: "font-medium",
      render: (row) => row.templateId,
    },
    // {
    //   key: "templateName",
    //   header: "Activity Name",
    //   render: (row) => row.templateName,
    // },
    {
      key: "location",
      header: "Location",
      render: (row) => row.provinceDisplay,
    },
    {
      key: "date",
      header: "Date",
      render: (row) => row.date,
    },
    // Status column shown only when the user is NOT a WAREHOUSE_HEAD
    // (preserves the previous <PermissionGuard role="WAREHOUSE_HEAD"> fallback).
    ...(isWarehouseHead
      ? []
      : [
          {
            key: "status",
            header: "Status",
            render: (row: BudgetTemplateItem) => (
              <StatusBadge status={row.status} />
            ),
          } satisfies Column<BudgetTemplateItem>,
        ]),
    {
      key: "action",
      header: "Action",
      render: (row) =>
        isSuperAdmin ? (
          <div className="flex items-center gap-3 whitespace-nowrap">
            <button
              id="icn_ViewBudgetTemplate"
              onClick={() => navigate(`/budgeting/template/${row.id}`)}
              className="text-[#7A7A7A] hover:text-indigo-700 transition"
            >
              <Eye className="w-5 h-5" />
            </button>

            <button
              id="icn_EditBudgetTemplate"
              onClick={() =>
                navigate(`/budgeting/template/update/${row.id}`)
              }
              disabled={
                row.status === "Submitted" || row.status === "Approved"
              }
              className={`transition ${
                row.status === "Submitted" || row.status === "Approved"
                  ? "text-[#D1D5DB] cursor-not-allowed"
                  : "text-[#7A7A7A] hover:text-indigo-700"
              }`}
            >
              <Pencil className="w-5 h-5" />
            </button>

            <button
              id="icn_DeleteBudgetTemplate"
              onClick={() => onDelete(row)}
              disabled={
                row.status === "Submitted" || row.status === "Approved"
              }
              className={`transition ${
                row.status === "Submitted" || row.status === "Approved"
                  ? "text-[#D1D5DB] cursor-not-allowed"
                  : "text-[#7A7A7A] hover:text-red-500"
              }`}
            >
              <Trash2 className="w-5 h-5" />
            </button>

            <MoreActionsDropdown
              onCreatePlan={() =>
                navigate(`/budgeting/plan/create/${row.id}`)
              }
            />
          </div>
        ) : (
          <PermissionGuard
            permission="budget.plan.create"
            fallback={
              <div className="flex items-center gap-3 whitespace-nowrap">
                <button
                  id="icn_ViewBudgetTemplateGuard"
                  onClick={() => navigate(`/budgeting/template/${row.id}`)}
                  className="text-[#7A7A7A] hover:text-indigo-700 transition"
                >
                  <Eye className="w-5 h-5" />
                </button>

                <button
                  id="icn_EditBudgetTemplateGuard"
                  onClick={() =>
                    navigate(`/budgeting/template/create?id=${row.id}`)
                  }
                  disabled={
                    row.status === "Submitted" || row.status === "Approved"
                  }
                  className={`transition ${
                    row.status === "Submitted" || row.status === "Approved"
                      ? "text-[#D1D5DB] cursor-not-allowed"
                      : "text-[#7A7A7A] hover:text-indigo-700"
                  }`}
                >
                  <Pencil className="w-5 h-5" />
                </button>

                <button
                  id="icn_DeleteBudgetTemplateGuard"
                  onClick={() => onDelete(row)}
                  disabled={
                    row.status === "Submitted" || row.status === "Approved"
                  }
                  className={`transition ${
                    row.status === "Submitted" || row.status === "Approved"
                      ? "text-[#D1D5DB] cursor-not-allowed"
                      : "text-[#7A7A7A] hover:text-red-500"
                  }`}
                >
                  <Trash2 className="w-5 h-5" />
                </button>
              </div>
            }
          >
            <button
              id="btn_CreatePlan"
              // onClick={() => onCreatePlan(row)}
              onClick={() => navigate(`/budgeting/plan/create/${row.id}`)}
            >
              <div className="h-6 p-2 transition duration-200 bg-indigo-50 rounded outline -outline-offset-1 outline-violet-950 inline-flex justify-center items-center gap-2.5 overflow-hidden hover:bg-violet-950">
                <div className="text-center text-violet-950 text-base hover:text-white transition duration-200">
                  Create Plan
                </div>
              </div>
            </button>
          </PermissionGuard>
        ),
    },
  ];

  return (
    <DataTable
      columns={columns}
      data={rows}
      rowKey={(row) => row.id}
      tableClassName="min-w-275"
      emptyMessage="No budget templates found."
    />
  );
}

function MoreActionsDropdown({
  onCreatePlan,
  // canCreatePlan,
}: {
  onCreatePlan: () => void;
  // canCreatePlan: boolean;
}) {
  const [open, setOpen] = useState(false);
  const [pos, setPos] = useState({ top: 0, left: 0 });
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (ref.current && !ref.current.contains(e.target as Node)) {
        setOpen(false);
      }
    }
    function handleScroll() {
      setOpen(false);
    }
    document.addEventListener("mousedown", handleClickOutside);
    document.addEventListener("scroll", handleScroll, true);
    return () => {
      document.removeEventListener("mousedown", handleClickOutside);
      document.removeEventListener("scroll", handleScroll, true);
    };
  }, []);

  function handleToggle() {
    if (!open && ref.current) {
      const rect = ref.current.getBoundingClientRect();
      const dropdownHeight = 40;
      const spaceBelow = window.innerHeight - rect.bottom;

      if (spaceBelow < dropdownHeight + 8) {
        // muncul ke atas
        setPos({
          top: rect.top - dropdownHeight - 4,
          left: rect.right - 176, // w-44 = 176px
        });
      } else {
        // muncul ke bawah
        setPos({
          top: rect.bottom + 4,
          left: rect.right - 176,
        });
      }
    }
    setOpen((prev) => !prev);
  }

  return (
    <div ref={ref}>
      <button
        id="icn_MoreActions"
        onClick={handleToggle}
        className="text-[#7A7A7A] hover:text-indigo-700 transition p-1 rounded hover:bg-gray-100"
      >
        <MoreHorizontal className="w-5 h-5" />
      </button>

      {open && (
        <div
          id="lsb_MoreActions"
          style={{ top: pos.top, left: pos.left }}
          className="fixed z-9999 w-44 bg-white rounded-md shadow-lg border border-gray-200 py-1"
        >
          <button
            id="btn_CreatePlanMenu"
            onClick={() => {
              // if (!canCreatePlan) return;
              onCreatePlan();
              setOpen(false);
            }}
            // disabled={!canCreatePlan}
            className={`w-full text-left px-4 py-2 text-sm transition "text-violet-950 hover:bg-indigo-50 cursor-pointer"
`}
          >
            Create Plan
          </button>
        </div>
      )}
    </div>
  );
}

function BudgetTemplateSkeleton() {
  return (
    <div className="bg-white border border-[#D7DEE8] rounded-2xl overflow-hidden animate-pulse">
      <div className="h-16 border-b bg-gray-100" />
      <div className="space-y-2 p-4">
        {Array.from({ length: 6 }).map((_, i) => (
          <div key={i} className="h-14 rounded-lg bg-gray-100" />
        ))}
      </div>
    </div>
  );
}

const SORT_OPTIONS: SortOption[] = [
  { value: "date", label: "Date" },
  { value: "templateCode", label: "Template Code" },
  { value: "location", label: "Location" },
];

// 2. Definisikan opsi arah untuk Order By
const ORDER_OPTIONS: SortOption[] = [
  { value: "asc", label: "Ascending" },
  { value: "desc", label: "Descending" },
];

export default function BudgetTemplateScreen() {
  const navigate = useNavigate();

  const { exportBudgetTemplates, isExporting } = useExportFileController();

  const [isExportModalOpen, setIsExportModalOpen] = useState(false);

  const [exportParams, setExportParams] = useState<ExportParams>({
    format: "Pdf",
    sortOrder: "asc",
  });

  const {
    templates,
    isLoading,
    error,
    searchInput,
    page,
    totalPages,
    total,
    from,
    to,

    sortBy,
    sortOrder,
    handleSort,
    handleOrder,

    // fetchTemplates,

    handleSearchChange,
    handlePageClick,
    handlePrevPage,
    handleNextPage,

    handleDelete,
  } = useBudgetTemplateController();

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
          await exportBudgetTemplates(exportParams);
          setIsExportModalOpen(false);
        }}
      />
      <div className="flex-1 p-4 sm:p-6 lg:p-8 overflow-y-auto">
        <div className="w-full">
          {/* ── Page Header (breadcrumb + title + subtitle) ── */}
          <PageHeader
            breadcrumbs={[
              { label: "Dashboard" },
              { label: "Budgeting" },
              { label: "Budget Template" },
            ]}
            title="List of Budget Template"
            subtitle={lastUpdatedLabel()}
          />

          {/* Toolbar */}
          <Toolbar
            search={searchInput}
            onSearchChange={handleSearchChange}
            showSort={true}
            sortOptions={SORT_OPTIONS}
            sortValue={sortBy ?? "date"}
            onSortChange={handleSort}
            // --- BAGIAN ORDER (Arah) ---
            showOrder={true}
            orderOptions={ORDER_OPTIONS}
            orderValue={sortOrder}
            onOrderChange={(val) => handleOrder(val as "asc" | "desc")}
            actions={
              <>
                <PermissionGuard permission="budget.template.create">
                  <Button
                    variant="primary"
                    id="btn_CreateBudgetTemplate"
                    onClick={() => navigate("/budgeting/template/create")}
                  >
                    <Plus className="w-4 h-4" />
                    Create Template
                  </Button>
                </PermissionGuard>

                <Button
                  variant="secondary"
                  id="btn_Export"
                  onClick={() => setIsExportModalOpen(true)}
                >
                  <Upload className="w-4 h-4" />
                  Export Data
                </Button>
              </>
            }
          />

          {/* Error */}
          {error && (
            <div className="mt-4 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-500">
              {error}
            </div>
          )}

          {/* Table */}
          <div className="mt-5">
            {isLoading ? (
              <BudgetTemplateSkeleton />
            ) : (
              <BudgetTemplateTable rows={templates} onDelete={handleDelete} />
            )}
          </div>

          {/* Footer Pagination */}
          <div className="mt-4 flex flex-col md:flex-row md:items-center md:justify-between gap-4">
            <p
              id="lbl_PaginationInfo"
              className="text-[14px] text-[#3F3F46] text-center md:text-left"
            >
              Menampilkan [{from}] sampai [{to}] dari [{total}] baris
            </p>

            <div className="flex items-center justify-center md:justify-end gap-3">
              <button
                id="btn_PrevPage"
                onClick={handlePrevPage}
                disabled={page === 1}
                className="w-10 h-10 rounded-[10px] bg-[#A7A7B0] text-white flex items-center justify-center disabled:opacity-50 disabled:cursor-not-allowed"
              >
                <ChevronLeft className="w-5 h-5" />
              </button>

              {getPageNumbers(page, totalPages).map((p, idx) =>
                p === "..." ? (
                  <span
                    key={`ellipsis-${idx}`}
                    className="w-10 h-10 flex items-center justify-center text-[#7A7A7A] text-[16px]"
                  >
                    ...
                  </span>
                ) : (
                  <button
                    key={p}
                    onClick={() => handlePageClick(p)}
                    className={`w-10 h-10 rounded-[10px] text-[16px] font-medium transition ${
                      p === page
                        ? "bg-[#D9DEE8] text-black"
                        : "bg-white border border-[#D8DCE5] text-[#3F3F46] hover:bg-gray-50"
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
                className="w-10 h-10 rounded-[10px] bg-[#A7A7B0] text-white flex items-center justify-center disabled:opacity-50 disabled:cursor-not-allowed"
              >
                <ChevronRight className="w-5 h-5" />
              </button>
            </div>
          </div>
        </div>
      </div>
    </>
  );
}
