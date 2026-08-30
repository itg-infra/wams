"use client";

import { useState } from "react";
import { DataTable, type Column } from "../components/ui/table";
import { PageHeader, lastUpdatedLabel } from "../components/ui/page-header";
import { Button } from "../components/ui/button";
import { Toolbar } from "../components/ui/toolbar";

// ─── Icons ─────────────────────────────────────────────

function ExportIcon() {
    return (
        <svg width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
            <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" />
            <polyline points="17 8 12 3 7 8" />
            <line x1="12" y1="3" x2="12" y2="15" />
        </svg>
    );
}

function FileIcon() {
    return (
        <svg width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
            <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
            <polyline points="14 2 14 8 20 8" />
        </svg>
    );
}

function CloseIcon() {
    return (
        <svg width="18" height="18" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
            <line x1="18" y1="6" x2="6" y2="18" />
            <line x1="6" y1="6" x2="18" y2="18" />
        </svg>
    );
}

function ChevronLeftIcon() {
    return (
        <svg width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
            <polyline points="15 18 9 12 15 6" />
        </svg>
    );
}

function ChevronRightIcon() {
    return (
        <svg width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
            <polyline points="9 18 15 12 9 6" />
        </svg>
    );
}



// ─── Types ─────────────────────────────────────────────

type PaymentStatus = "Paid" | "Pending" | "Overdue";

type Settlement = {
    invoiceId: string;
    workOrderId: string;
    vendor: string;
    totalCost: string;
    status: PaymentStatus;
    approvalDate: string;
};

// ─── Data ──────────────────────────────────────────────

const settlementData: Settlement[] = [
    { invoiceId: "INV-2026-001", workOrderId: "WO-2025-001", vendor: "PT. Fumigasi Nusantara", totalCost: "Rp 15.000.000", status: "Paid", approvalDate: "02/03/2026" },
    { invoiceId: "INV-2026-002", workOrderId: "WO-2025-002", vendor: "PT. Citra Quality", totalCost: "Rp 3.500.000", status: "Pending", approvalDate: "02/03/2026" },
    { invoiceId: "INV-2026-003", workOrderId: "WO-2025-003", vendor: "CV Maju Karya", totalCost: "Rp 8.000.000", status: "Paid", approvalDate: "02/03/2026" },
    { invoiceId: "INV-2026-004", workOrderId: "WO-2025-004", vendor: "PT. Fumigasi Nusantara", totalCost: "Rp 18.500.000", status: "Pending", approvalDate: "02/03/2026" },
    { invoiceId: "INV-2026-005", workOrderId: "WO-2025-005", vendor: "CV Maju Karya", totalCost: "Rp 12.000.000", status: "Paid", approvalDate: "02/03/2026" },
    { invoiceId: "INV-2026-006", workOrderId: "WO-2025-006", vendor: "PT. Citra Quality", totalCost: "Rp 4.000.000", status: "Overdue", approvalDate: "02/03/2026" },
];

// ─── Status Badge ───────────────────────────────────────

function StatusBadge({ status }: { status: PaymentStatus }) {
    const styles: Record<PaymentStatus, string> = {
        Paid: "bg-green-100 text-green-700",
        Pending: "bg-yellow-100 text-yellow-700",
        Overdue: "bg-red-100 text-red-600",
    };
    return (
        <span className={`px-3 py-1 rounded-full text-xs font-medium ${styles[status]}`}>
            {status}
        </span>
    );
}


// ─── Modal Component ───────────────────────────────────

function DetailModal({ open, onClose, data }: { open: boolean; onClose: () => void; data: Settlement | null }) {
    if (!open || !data) return null;

    return (
      <div id="lbl_SettlementDetailDialog" className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
        <div className="bg-white w-full max-w-2xl rounded-none sm:rounded-2xl shadow-xl relative max-h-screen sm:max-h-[90vh] overflow-y-auto">
          {/* Header */}
          <div className="flex justify-between items-center px-4 sm:px-6 py-4 border-b">
            <h2 className="text-base font-semibold text-gray-800">
              Detail Kegiatan
            </h2>
            <button
              id="icn_CloseSettlementDialog"
              onClick={onClose}
              className="text-gray-400 hover:text-gray-600"
            >
              <CloseIcon />
            </button>
          </div>

          <div className="px-4 sm:px-6 py-5 space-y-4">
            {/* Informasi Dasar */}
            <div className="bg-gray-50 rounded-xl p-5">
              <h3 className="font-semibold text-sm text-gray-800 mb-4">
                Informasi Dasar
              </h3>
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-x-8 gap-y-4 text-sm">
                <div>
                  <p className="text-gray-400 text-xs mb-0.5">Invoice ID</p>
                  <p className="font-semibold text-gray-900">INV-2025-001</p>
                </div>
                <div>
                  <p className="text-gray-400 text-xs mb-0.5">Work Order ID</p>
                  <p className="font-semibold text-gray-900">
                    {data.workOrderId}
                  </p>
                </div>
                <div>
                  <p className="text-gray-400 text-xs mb-0.5">Activity Type</p>
                  <p className="font-semibold text-gray-900">{"Loading"}</p>
                </div>
                <div>
                  <p className="text-gray-400 text-xs mb-0.5">Warehouse</p>
                  <p className="font-semibold text-gray-900">
                    MNP Blok A, Lampung
                  </p>
                </div>
                <div>
                  <p className="text-gray-400 text-xs mb-0.5">PIC</p>
                  <p className="font-semibold text-gray-900">Agam</p>
                </div>
                <div>
                  <p className="text-gray-400 text-xs mb-0.5">Vendor</p>
                  <p className="font-semibold text-gray-900">
                    PT Fumigasi Nusantara
                  </p>
                </div>
              </div>
            </div>

            {/* Rincian Biaya */}
            <div className="bg-gray-50 rounded-xl p-5">
              <h3 className="font-semibold text-sm text-gray-800 mb-4">
                Rincian Biaya
              </h3>
              <div className="space-y-4 text-sm">
                <div className="flex flex-col sm:flex-row sm:justify-between sm:items-start gap-1">
                  <div>
                    <p className="font-medium text-gray-800">
                      Biaya Tenaga Kerja
                    </p>
                    <p className="text-xs text-gray-400 mt-0.5">5 Pekerja</p>
                  </div>
                  <span className="text-gray-800">Rp 5.000.000</span>
                </div>
                <div className="flex flex-col sm:flex-row sm:justify-between sm:items-start gap-1">
                  <div>
                    <p className="font-medium text-gray-800">Biaya Material</p>
                    <p className="text-xs text-gray-400 mt-0.5">
                      5 Liter Methyl Bromide
                    </p>
                  </div>
                  <span className="text-gray-800">Rp 8.000.000</span>
                </div>
                <div className="flex flex-col sm:flex-row sm:justify-between sm:items-start gap-1">
                  <div>
                    <p className="font-medium text-gray-800">Biaya Equipment</p>
                    <p className="text-xs text-gray-400 mt-0.5">Tools</p>
                  </div>
                  <span className="text-gray-800">Rp 1.500.000</span>
                </div>
                <div className="flex flex-col sm:flex-row sm:justify-between sm:items-start gap-1">
                  <div>
                    <p className="font-medium text-gray-800">Biaya Transport</p>
                    <p className="text-xs text-gray-400 mt-0.5">Akomodasi</p>
                  </div>
                  <span className="text-gray-800">Rp 500.000</span>
                </div>
                <div className="border-t pt-3 flex flex-col sm:flex-row sm:justify-between gap-1 font-semibold">
                  <span className="text-gray-800">Total Cost</span>
                  <span className="text-indigo-700 font-bold text-base">
                    Rp 15.000.000
                  </span>
                </div>
              </div>
            </div>

            {/* Informasi Pembayaran */}
            <div className="bg-gray-50 rounded-xl p-5">
              <h3 className="font-semibold text-sm text-gray-800 mb-4">
                Informasi Pembayaran
              </h3>
              <div className="grid grid-cols-2 gap-x-8 gap-y-4 text-sm">
                <div>
                  <p className="text-gray-400 text-xs mb-1">Payment Status</p>
                  <span className="bg-green-100 text-green-700 px-3 py-1 rounded-full text-xs font-medium">
                    Paid
                  </span>
                </div>
                <div>
                  <p className="text-gray-400 text-xs mb-0.5">Due Date</p>
                  <p className="font-semibold text-gray-900">20 Maret 2025</p>
                </div>
                <div>
                  <p className="text-gray-400 text-xs mb-0.5">Paid Date</p>
                  <p className="font-semibold text-gray-900">18 Maret 2025</p>
                </div>
                <div>
                  <p className="text-gray-400 text-xs mb-0.5">Payment Method</p>
                  <p className="font-semibold text-gray-900">Transfer Bank</p>
                </div>
                <div>
                  <p className="text-gray-400 text-xs mb-0.5">Bank Account</p>
                  <p className="font-semibold text-gray-900">BCA 1234567890</p>
                </div>
              </div>
            </div>

            {/* Catatan */}
            <div className="bg-gray-50 rounded-xl p-5">
              <h3 className="font-semibold text-sm text-gray-800 mb-2">
                Catatan
              </h3>
              <p className="text-sm text-gray-500">Pembayaran tepat waktu</p>
            </div>
          </div>

          {/* Footer */}
          <div className="flex justify-end px-4 sm:px-6 py-4 border-t">
            <Button id="btn_ExportSettlementDetail" variant="primary">
              <FileIcon />
              Export Data Ini
            </Button>
          </div>
        </div>
      </div>
    );
}

// ─── Main Page ──────────────────────────────────────────

const SETTLEMENT_SORT_OPTIONS = [
  { label: "Invoice A-Z", value: "invoice_asc" },
  { label: "Invoice Z-A", value: "invoice_desc" },
  { label: "Vendor A-Z", value: "vendor_asc" },
  { label: "Vendor Z-A", value: "vendor_desc" },
  { label: "Latest", value: "date_desc" },
  { label: "Oldest", value: "date_asc" },
];

export default function ListSettlementReport() {
    const [search, setSearch] = useState("");
    const [sort, setSort] = useState("invoice_asc");
    const [selected, setSelected] = useState<Settlement | null>(null);
    const [open, setOpen] = useState(false);

    const filtered = settlementData
        .filter(
            (item) =>
                item.invoiceId.toLowerCase().includes(search.toLowerCase()) ||
                item.vendor.toLowerCase().includes(search.toLowerCase()) ||
                item.workOrderId.toLowerCase().includes(search.toLowerCase())
        )
        // This screen holds its whole dataset in memory, so sorting here is the
        // honest thing — there is no server page to disagree with.
        .slice()
        .sort((a, b) => {
            const [field, dir] = sort.split("_");
            const key = field === "invoice" ? "invoiceId" : field === "vendor" ? "vendor" : "approvalDate";
            const cmp = String(a[key as keyof Settlement]).localeCompare(
                String(b[key as keyof Settlement]),
            );
            return dir === "asc" ? cmp : -cmp;
        });

    const columns: Column<Settlement>[] = [
        { key: "invoiceId", header: "Invoice ID", render: (item) => item.invoiceId },
        { key: "workOrderId", header: "Work Order ID", render: (item) => item.workOrderId },
        { key: "vendor", header: "Vendor", render: (item) => item.vendor },
        { key: "totalCost", header: "Total Cost", render: (item) => item.totalCost },
        { key: "status", header: "Status", render: (item) => <StatusBadge status={item.status} /> },
        { key: "approvalDate", header: "Approval Date", render: (item) => item.approvalDate },
        {
            key: "action",
            header: "Action",
            align: "center",
            render: (item) => (
                <div className="flex justify-center gap-2">
                    <button
                        id="btn_ViewSettlementDetail"
                        onClick={() => {
                            setSelected(item);
                            setOpen(true);
                        }}
                        className="flex items-center gap-1 border px-3 py-1 rounded-md text-xs hover:bg-gray-100 text-gray-700"
                    >
                        Detail
                    </button>
                    <button id="icn_PrintSettlement" className="border p-1.5 rounded-md hover:bg-gray-100 text-gray-600">
                        <FileIcon />
                    </button>
                </div>
            ),
        },
    ];

    return (
      <div className="flex-1 p-4 sm:p-6 lg:p-8 overflow-y-auto">
        <DetailModal
          open={open}
          data={selected}
          onClose={() => setOpen(false)}
        />

        {/* ── Page Header (breadcrumb + title + subtitle) ── */}
        <PageHeader
          breadcrumbs={[
            { label: "Dashboard" },
            { label: "Finance & Settlement" },
            { label: "Report" },
          ]}
          title="List of Settlement Report"
          subtitle={lastUpdatedLabel()}
        />

        {/* Summary Cards */}
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4 lg:gap-24 mb-5">
          <div className="bg-green-50 border border-green-200 rounded-xl px-5 py-4">
            <p className="text-xs text-green-600 font-medium mb-1">
              Total Paid
            </p>
            <p className="text-lg font-bold text-green-700">Rp 35.000.000</p>
          </div>
          <div className="bg-yellow-50 border border-yellow-200 rounded-xl px-5 py-4">
            <p className="text-xs text-yellow-600 font-medium mb-1">
              Total Pending
            </p>
            <p className="text-lg font-bold text-yellow-700">Rp 7.500.000</p>
          </div>
          <div className="bg-red-50 border border-red-200 rounded-xl px-5 py-4">
            <p className="text-xs text-red-500 font-medium mb-1">
              Total Overdue
            </p>
            <p className="text-lg font-bold text-red-600">Rp 18.500.000</p>
          </div>
        </div>

        {/* Toolbar */}
        <Toolbar
          search={search}
          onSearchChange={setSearch}
          sortOptions={SETTLEMENT_SORT_OPTIONS}
          onSortChange={setSort}
          sortValue={SETTLEMENT_SORT_OPTIONS.find((o) => o.value === sort)?.label}
          actions={
            <Button id="btn_Export" variant="secondary">
              <ExportIcon />
              Export Data
            </Button>
          }
        />

        {/* Table */}
        <div id="tbl_SettlementReport">
          <DataTable
            columns={columns}
            data={filtered}
            rowKey={(item) => item.invoiceId}
            tableClassName="min-w-225"
            rowClassName="hover:bg-gray-100"
            emptyMessage="No settlement reports found."
          />

          {/* Pagination */}
          <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 px-4 py-3 border-t text-xs text-gray-500">
            <span>Menampilkan [1] sampai [6] dari [6] baris</span>
            <div className="flex items-center gap-1">
              <button id="btn_PrevPage" className="border rounded p-1 hover:bg-gray-100 text-gray-500">
                <ChevronLeftIcon />
              </button>
              <button className="w-7 h-7 rounded border bg-indigo-700 text-white font-semibold flex items-center justify-center text-xs">
                1
              </button>
              <button id="btn_NextPage" className="border rounded p-1 hover:bg-gray-100 text-gray-500">
                <ChevronRightIcon />
              </button>
            </div>
          </div>
        </div>
      </div>
    );
}