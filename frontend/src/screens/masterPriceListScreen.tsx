import { useEffect, useState } from "react";
import { useRateCardStore } from "../master_data/store/rateCardStore";
import { formatDate } from "../components/format/dateTimeFormat";
import { useNavigate } from "react-router-dom";
import { DataTable, type Column } from "../components/ui/table";
import type {
  RateCardItemResponse,
  RateCardSortBy,
} from "../master_data/types/rateCard.type";
import { PageHeader, lastUpdatedLabel } from "../components/ui/page-header";
import { Button } from "../components/ui/button";
import { Toolbar } from "../components/ui/toolbar";

// ─── Icons ────────────────────────────────────────────────────────────────────

export function EyeIcon() {
  return (
    <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
      <path
        d="M1 8C1 8 3.5 3 8 3C12.5 3 15 8 15 8C15 8 12.5 13 8 13C3.5 13 1 8 1 8Z"
        stroke="#9CA3AF"
        strokeWidth="1.3"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
      <circle cx="8" cy="8" r="2" stroke="#9CA3AF" strokeWidth="1.3" />
    </svg>
  );
}
export function EditIcon() {
  return (
    <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
      <path
        d="M11.333 2.00004C11.5081 1.82494 11.7162 1.68605 11.945 1.59129C12.1738 1.49653 12.4191 1.44775 12.6667 1.44775C12.9143 1.44775 13.1596 1.49653 13.3884 1.59129C13.6172 1.68605 13.8253 1.82494 14.0003 2.00004C14.1754 2.17513 14.3143 2.38321 14.4091 2.61201C14.5038 2.84081 14.5526 3.08612 14.5526 3.33371C14.5526 3.58129 14.5038 3.8266 14.4091 4.0554C14.3143 4.2842 14.1754 4.49228 14.0003 4.66737L5.00033 13.6674L1.33366 14.6674L2.33366 11.0007L11.333 2.00004Z"
        stroke="#9CA3AF"
        strokeWidth="1.3"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}
export function DeleteIcon() {
  return (
    <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
      <path
        d="M2 4H14"
        stroke="#9CA3AF"
        strokeWidth="1.3"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
      <path
        d="M5.33301 4V2.66667C5.33301 2.31304 5.47349 1.97391 5.72354 1.72386C5.97358 1.47381 6.31272 1.33334 6.66634 1.33334H9.33301C9.68663 1.33334 10.0258 1.47381 10.2758 1.72386C10.5259 1.97391 10.6663 2.31304 10.6663 2.66667V4"
        stroke="#9CA3AF"
        strokeWidth="1.3"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
      <path
        d="M6.66699 7.33334V11.3333"
        stroke="#9CA3AF"
        strokeWidth="1.3"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
      <path
        d="M9.33301 7.33334V11.3333"
        stroke="#9CA3AF"
        strokeWidth="1.3"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
      <path
        d="M3.33301 4L3.99967 12.6667C3.99967 13.0203 4.14015 13.3594 4.39019 13.6095C4.64024 13.8595 4.97938 14 5.33301 14H10.6663C11.02 14 11.3591 13.8595 11.6092 13.6095C11.8592 13.3594 11.9997 13.0203 11.9997 12.6667L12.6663 4"
        stroke="#9CA3AF"
        strokeWidth="1.3"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}
export function ChevronLeft() {
  return (
    <svg width="14" height="14" viewBox="0 0 14 14" fill="none">
      <path
        d="M9 11L5 7L9 3"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}
export function ChevronRight() {
  return (
    <svg width="14" height="14" viewBox="0 0 14 14" fill="none">
      <path
        d="M5 3L9 7L5 11"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}

// ─── Component ────────────────────────────────────────────────────────────────

// Only the columns the API knows how to order by are offered. The rate-card
// list is fetched with page/limit, so ordering is sent to the server; sorting
// the fetched rows would only reorder the page currently in hand.
const SORT_OPTIONS = [
  { label: "Latest", value: "createdAt:desc" },
  { label: "Oldest", value: "createdAt:asc" },
];

export default function MasterPriceList() {
  const [search, setSearch] = useState("");
  const [currentPage] = useState(1);
  const [sort, setSort] = useState<string | null>(null);

  const navigate = useNavigate();

  const {
    fetchRateCard,
    rateCard,
    isLoading,
    error,
    delete: deleteRateCard,
    isDeleting,
  } = useRateCardStore();

  useEffect(() => {
    fetchRateCard();
  }, [fetchRateCard]);

  async function handleDelete(id: number) {
    if (!confirm("Yakin ingin menghapus rate card ini?")) return;
    const ok = await deleteRateCard(id);
    if (ok) fetchRateCard();
  }

  // ── List Page ──
  const filtered = rateCard.filter(
    (d) =>
      d.vendor.cardCode.toLowerCase().includes(search.toLowerCase()) ||
      d.vendor.cardName.toLowerCase().includes(search.toLowerCase()),
  );

  const totalRows = filtered.length;
  const startRow = totalRows === 0 ? 0 : 1;
  const endRow = totalRows;

  const columns: Column<RateCardItemResponse>[] = [
    {
      key: "vendorCode",
      header: "Vendor Code",
      width: "25%",
      render: (row) => row.vendor.cardCode,
    },
    {
      key: "vendorName",
      header: "Vendor Name",
      width: "33.3333%",
      render: (row) => row.vendor.cardName,
    },
    {
      key: "date",
      header: "Date",
      width: "25%",
      render: (row) => formatDate(row.createdAt),
    },
    {
      key: "action",
      header: "Action",
      width: "6rem",
      render: (row) => (
        <div className="flex items-center gap-2 whitespace-nowrap">
          {/* Eye → ke detail */}
          <button
            id="icn_ViewRateCard"
            className="hover:opacity-70 transition-opacity"
            onClick={() => navigate(`/master/rate-card/detail/${row.id}`)}
          >
            <EyeIcon />
          </button>
          <button
            id="icn_EditRateCard"
            className="hover:opacity-70 transition-opacity"
            onClick={() => navigate(`/master/rate-card/edit/${row.id}`)}
          >
            <EditIcon />
          </button>
          <button
            id="icn_DeleteRateCard"
            className="hover:opacity-70 transition-opacity disabled:opacity-30"
            disabled={isDeleting}
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
            { label: "Master Price List" },
          ]}
          title="List of Price List"
          subtitle={lastUpdatedLabel()}
        />

        {/* Toolbar */}
        <Toolbar
          search={search}
          onSearchChange={setSearch}
          sortOptions={SORT_OPTIONS}
          onSortChange={(value) => {
            const [sortBy, sortOrder] = value.split(":");
            setSort(value);
            void fetchRateCard({
              sortBy: sortBy as RateCardSortBy,
              sortOrder: sortOrder as "asc" | "desc",
              page: 1,
            });
          }}
          sortValue={SORT_OPTIONS.find((o) => o.value === sort)?.label}
          actions={
            <Button
              id="btn_CreateRateCard"
              onClick={() => navigate("/master/rate-card/create")}
            >
              Create Price List
            </Button>
          }
        />

        {/* Table */}
        <div id="tbl_RateCard">
          <DataTable
            columns={columns}
            data={filtered}
            rowKey={(row) => row.id}
            isLoading={isLoading}
            error={error}
            onRetry={() => fetchRateCard()}
            emptyMessage="Tidak ada data."
            tableClassName="min-w-175"
          />
        </div>

        {/* Pagination */}
        <div className="flex items-center justify-between mt-4 gap-3 flex-wrap">
          <p id="lbl_PaginationInfo" className="text-sm text-gray-500">
            Menampilkan {startRow} sampai {endRow} dari {totalRows} baris
          </p>
          <div className="flex items-center gap-1">
            <button
              id="btn_PrevPage"
              disabled
              className="w-8 h-8 flex items-center justify-center rounded-md bg-white border border-gray-300 text-gray-500 hover:bg-gray-50 disabled:opacity-30 disabled:cursor-not-allowed transition"
            >
              <ChevronLeft />
            </button>
            <button
              id="btn_ActivePage"
              className="w-8 h-8 flex items-center justify-center rounded-md text-sm font-medium border bg-[#2B2469] text-white border-[#2B2469]"
            >
              {currentPage}
            </button>
            <button
              id="btn_NextPage"
              disabled
              className="w-8 h-8 flex items-center justify-center rounded-md bg-white border border-gray-300 text-gray-500 hover:bg-gray-50 disabled:opacity-30 disabled:cursor-not-allowed transition"
            >
              <ChevronRight />
            </button>
          </div>
        </div>
    </div>
  );
}
