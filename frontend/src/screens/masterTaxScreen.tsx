import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
// import { formatDate } from '../components/format/dateTimeFormat';
import { DeleteIcon, EditIcon} from "./masterPriceListScreen";
import { useTaxStore } from "../store/taxStore";
import {
  deleteTaxController,
  getTaxListController,
} from "../controllers/masterData/taxController";
import toast from "react-hot-toast";
import { DataTable, type Column } from "../components/ui/table";
import { PageHeader, lastUpdatedLabel } from "../components/ui/page-header";
import type { TaxData } from "../types/tax.type";
import { Button } from "../components/ui/button";
import { Toolbar } from "../components/ui/toolbar";

const SORT_OPTIONS = [
  { label: "Name A-Z", value: "name_asc" },
  { label: "Name Z-A", value: "name_desc" },
  { label: "Code A-Z", value: "code_asc" },
  { label: "Code Z-A", value: "code_desc" },
];

export default function MasterTaxScreen() {
  const [search, setSearch] = useState("");
  const [sort, setSort] = useState("name_asc");

  const navigate = useNavigate();

  const { loading, error, taxes } = useTaxStore();

  useEffect(() => {
    getTaxListController();
  }, []);

  async function handleDelete(id: number) {
    if (!confirm("Yakin ingin menghapus rate card ini?")) return;
    try {
      await deleteTaxController(id);
      getTaxListController();
      toast.success("Success delete tax data");
    } catch {
      toast.error("Error delete tax data");
    }
  }

  // ── List Page ──
  const filtered = taxes
    .filter(
      (d) =>
        d.code.toLowerCase().includes(search.toLowerCase()) ||
        d.name.toLowerCase().includes(search.toLowerCase()),
    )
    .slice()
    .sort((a, b) => {
      const [field, dir] = sort.split("_") as ["name" | "code", "asc" | "desc"];
      const cmp = (a[field] ?? "").localeCompare(b[field] ?? "");
      return dir === "asc" ? cmp : -cmp;
    });

  const columns: Column<TaxData>[] = [
    { key: "name", header: "Name", width: "20%", render: (row) => row.name },
    { key: "code", header: "Code", width: "20%", render: (row) => row.code },
    { key: "category", header: "Category", width: "20%", render: (row) => row.category },
    { key: "rate", header: "Rate", width: "20%", render: (row) => row.rate },
    {
      key: "isActive",
      header: "IsActive",
      width: "20%",
      render: (row) => (
        <span
          className={`inline-flex items-center px-3 py-1 rounded-full text-xs font-semibold ${
            row.isActive
              ? "bg-green-100 text-green-700"
              : "bg-red-100 text-red-700"
          }`}
        >
          <span
            className={`w-2 h-2 rounded-full mr-2 ${
              row.isActive ? "bg-green-500" : "bg-red-500"
            }`}
          />
          {row.isActive ? "Active" : "Inactive"}
        </span>
      ),
    },
    {
      key: "action",
      header: "Action",
      headerClassName: "w-30",
      render: (row) => (
        <div className="flex items-center gap-2 whitespace-nowrap">
          {/* Eye → ke detail */}

          <button
            id="icn_EditTax"
            className="hover:opacity-70 transition-opacity"
            onClick={() => navigate(`/master/tax/form/edit/${row.id}`)}
          >
            <EditIcon />
          </button>
          <button
            id="icn_DeleteTax"
            className="hover:opacity-70 transition-opacity disabled:opacity-30"
            //   disabled={isDeleting}
            onClick={() => handleDelete(row.id)}
          >
            <DeleteIcon />
          </button>
        </div>
      ),
    },
  ];

  return (
    <div className="flex-1 p-4 sm:p-6 lg:p-8 overflow-y-auto font-sans text-gray-800">
        {/* ── Page Header (breadcrumb + title + subtitle) ── */}
        <PageHeader
          breadcrumbs={[
            { label: "Dashboard" },
            { label: "Master Data" },
            { label: "Master Tax" },
          ]}
          title="List of Master Tax"
          subtitle={lastUpdatedLabel()}
        />

        {/* Toolbar */}
        <Toolbar
          search={search}
          onSearchChange={setSearch}
          sortOptions={SORT_OPTIONS}
          onSortChange={setSort}
          sortValue={SORT_OPTIONS.find((o) => o.value === sort)?.label}
          actions={
            <Button
              id="btn_CreateTax"
              variant="primary"
              onClick={() => navigate("/master/tax/form")}
            >
              Create Tax
            </Button>
          }
        />

        {/* Table */}
        <div id="tbl_Tax">
          <DataTable
            columns={columns}
            data={filtered}
            rowKey={(row) => row.id}
            isLoading={loading}
            error={error}
            onRetry={() => getTaxListController()}
            emptyMessage="Tidak ada data."
            tableClassName="min-w-175"
          />
        </div>
    </div>
  );
}
