import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useDetailPoController } from "../controllers/budgeting/detailPoController";
import { DataTable, type Column } from "../components/ui/table";
import { PageHeader } from "../components/ui/page-header";
import {
  detailPoService,
  type PurchaseOrderDetailItem,
} from "../api/services/budgeting/purchaseOrders/detailPoService";
import { useExportFileController } from "../controllers/file/exportFileController";
import { Button } from "../components/ui/button";
import toast from "react-hot-toast";

const formatCurrency = (value: number) =>
  new Intl.NumberFormat("id-ID", {
    style: "currency",
    currency: "IDR",
    minimumFractionDigits: 0,
  }).format(value);

const formatDate = (date?: string | null) => {
  if (!date) return "-";

  return new Date(date).toLocaleString("id-ID", {
    day: "2-digit",
    month: "short",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
};

const itemColumns: Column<PurchaseOrderDetailItem>[] = [
  {
    key: "no",
    header: "#",
    render: (_item, index) => (
      <div className="flex h-8 w-8 items-center justify-center rounded-full bg-slate-100 text-xs font-semibold text-slate-600">
        {index + 1}
      </div>
    ),
  },
  {
    key: "item",
    header: "Item",
    render: (item) => (
      <div className="space-y-1">
        <p className="font-semibold text-gray-900">{item.itemCode}</p>

        <p className="text-sm text-gray-600">{item.itemName}</p>

        <p className="text-xs text-gray-400">Item ID #{item.itemShadowId}</p>
      </div>
    ),
  },
  {
    key: "coa",
    header: "COA",
    render: (item) => (
      <div className="space-y-1">
        <p className="font-medium text-gray-800">{item.coaCode}</p>

        <p className="text-xs text-gray-500">{item.coaName}</p>
      </div>
    ),
  },

  {
    key: "rfba",
    header: "RFBA",
    render: (item) => (
      <div className="space-y-1">
        <p className="font-medium text-gray-800">
          {item.isRfba ? "Yes" : "No"}
        </p>

        <p className="text-xs text-gray-500">{item.isRfba ? "Yes" : "No"}</p>
      </div>
    ),
  },
  {
    key: "vendor",
    header: "Vendor",
    render: (item) => (
      <div className="space-y-1">
        <p className="font-medium text-gray-800">{item.vendorName}</p>

        <p className="text-xs text-gray-500">{item.vendorCode}</p>
      </div>
    ),
  },
  {
    key: "uom",
    header: "UOM",
    align: "center",
    render: (item) => (
      <span className="inline-flex rounded-xl bg-slate-100 px-3 py-1 text-xs font-semibold text-slate-700">
        {item.uomCode}
      </span>
    ),
  },
  {
    key: "type",
    header: "Type",
    align: "center",
    render: (item) => (
      <span
        className={`inline-flex items-center rounded-full px-3 py-1 text-xs font-semibold ${
          item.isRfba
            ? "bg-emerald-100 text-emerald-700"
            : "bg-slate-100 text-slate-700"
        }`}
      >
        {item.isRfba ? "Yes" : "No"}
      </span>
    ),
  },
  {
    key: "bl",
    header: "BL Number",
    render: (item) =>
      item.billOfLading ? (
        <span className="rounded-lg bg-blue-50 px-3 py-1 text-sm font-medium text-blue-700">
          {item.billOfLading}
        </span>
      ) : (
        <span className="text-gray-400">-</span>
      ),
  },
  {
    key: "cost",
    header: "Cost",
    align: "right",
    render: (item) => (
      <div className="font-medium text-gray-800">
        {formatCurrency(item.costValue)}
      </div>
    ),
  },
  {
    key: "quantity",
    header: "Quantity",
    align: "right",
    render: (item) => (
      <div className="font-medium text-gray-800">
        {item.quantity.toLocaleString("id-ID")}
      </div>
    ),
  },
  {
    key: "total",
    header: "Total",
    align: "right",
    render: (item) => (
      <div className="space-y-1">
        <p className="text-lg font-bold text-[#2E277C]">
          {formatCurrency(item.totalValue)}
        </p>

        <p className="text-xs text-gray-400">Line Total</p>
      </div>
    ),
  },
];

export function DetailPoScreen() {
  const { id } = useParams();

  const { detail, isLoading, loadDetail } = useDetailPoController();

  const { isExporting, exportPurchaseOrderDetails } = useExportFileController();

  const handlePrint = async () => {
    try {
      await exportPurchaseOrderDetails(Number(id));
    } catch (error) {
      console.error("Gagal export purchase order:", error);
    }
  };

  const [isSubmitting, setIsSubmitting] = useState(false);

  const navigate = useNavigate();

  useEffect(() => {
    if (id) {
      loadDetail(Number(id));
    }
  }, [id, loadDetail]);

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-100">
        Loading...
      </div>
    );
  }

  if (!detail) {
    return (
      <div className="flex items-center justify-center min-h-100">
        Purchase Order not found
      </div>
    );
  }
const hasRfbaItem = detail.items?.some((item) => item.isRfba === true) ?? false;
const hasSapPONumber = Boolean(detail.sapPoNumber);
const isStatusGenerated = detail.status?.toLowerCase() === "generated";
const isAlreadyGenerated = detail.apdp?.sapDocEntry != null;

const canGenerate =
  hasRfbaItem && hasSapPONumber && isStatusGenerated && !isAlreadyGenerated;
const isGenerateDisabled = isSubmitting || !canGenerate;

  console.log("GENERATE APDP CONDITION:", {
    status: detail.status,
    isAlreadyGenerated: detail.status === "Generated",
    sapPONumber: detail.sapPoNumber,
    hasSapPONumber: Boolean(detail.sapPoNumber),
    hasRfbaItem,
    isSubmitting,
    onsubmit,
  });

  const handleGenerate = async () => {
    if (!detail?.id) {
      return;
    }

    try {
      setIsSubmitting(true);

      const response = await detailPoService.generateAPDP(detail.id);

      if (response.success) {
        // optional: refresh detail PO
        await loadDetail(detail.id);

        // optional notification
        toast.success(response.message || "APDP berhasil di-generate");
      } else {
        toast.error(response.message || "Gagal generate APDP");
      }
    } catch (error) {
      console.error("Generate APDP error:", error);

      toast.error("Terjadi kesalahan saat generate APDP");
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="flex-1 space-y-6 p-4 sm:p-6 lg:p-8 overflow-y-auto">
      <div className="flex justify-between">
        <PageHeader title={detail.code} onBack={() => navigate(-1)} />
        <button
          type="button"
          onClick={handlePrint}
          disabled={isExporting}
          className="
    inline-flex items-center justify-center gap-2
    min-w-27.5
    h-10
    px-4
    rounded-lg
    bg-blue-600
    text-white
    text-sm
    font-semibold
    shadow-sm
    hover:bg-blue-700
    hover:shadow-md
    active:scale-[0.98]
    disabled:cursor-not-allowed
    disabled:opacity-60
    disabled:hover:bg-blue-600
    transition-all duration-200
    focus:outline-none
    focus:ring-2
    focus:ring-blue-500/30
  "
        >
          {isExporting ? (
            <>
              <svg
                className="h-4 w-4 animate-spin"
                viewBox="0 0 24 24"
                fill="none"
              >
                <circle
                  className="opacity-25"
                  cx="12"
                  cy="12"
                  r="9"
                  stroke="currentColor"
                  strokeWidth="3"
                />
                <path
                  className="opacity-90"
                  d="M21 12a9 9 0 0 0-9-9"
                  stroke="currentColor"
                  strokeWidth="3"
                  strokeLinecap="round"
                />
              </svg>

              <span>Memproses...</span>
            </>
          ) : (
            <>
              <svg
                className="h-4 w-4"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="2"
                strokeLinecap="round"
                strokeLinejoin="round"
              >
                <polyline points="6 9 6 2 18 2 18 9" />
                <path d="M6 18H4a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-2" />
                <rect x="6" y="14" width="12" height="8" />
              </svg>

              <span>Print</span>
            </>
          )}
        </button>
      </div>

      {/* Header */}
      <div className="relative overflow-hidden rounded-[32px] bg-linear-to-r from-[#2E277C] via-[#4338CA] to-[#6366F1] p-8 shadow-xl">
        <div className="absolute top-0 right-0 h-48 w-48 rounded-full bg-white/10 blur-3xl" />

        <div className="relative">
          <div className="flex flex-col gap-6 lg:flex-row lg:items-center lg:justify-between">
            <div>
              <p className="text-indigo-100 text-sm tracking-[0.2em] uppercase">
                Purchase Order
              </p>

              <p className="mt-3 text-2xl font-bold text-white">
                {detail.code}
              </p>

              <p className="mt-3 text-indigo-100">
                Vendor: {detail.vendorName}
              </p>
            </div>

            <div className="flex flex-col items-end gap-3">
              <span className="rounded-full bg-white/20 backdrop-blur px-5 py-2 text-sm font-semibold text-white border border-white/20">
                {detail.status}
              </span>

              <span className="text-indigo-100 text-sm">
                {formatDate(detail.docDate)}
              </span>
            </div>
          </div>
        </div>
      </div>

      {/* Summary */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4 lg:gap-5">
        {/* Grand Total */}
        <div className="rounded-2xl bg-white border border-indigo-100 p-5 shadow-sm hover:shadow-md transition">
          <p className="text-sm text-gray-500">Grand Total</p>

          <h3 className="mt-2 text-2xl font-bold text-[#2E277C] wrap-break-word">
            {formatCurrency(detail.grandTotal)}
          </h3>

          <p className="mt-1 text-xs text-gray-400">
            Total purchase order amount
          </p>
        </div>

        {/* SAP Doc Entry */}
        <div className="rounded-2xl bg-white border border-indigo-100 p-5 shadow-sm hover:shadow-md transition">
          <p className="text-sm text-gray-500">SAP Doc Entry</p>

          <h3 className="mt-2 text-xl font-bold text-[#2E277C] wrap-break-word">
            {detail.apdp?.sapDocEntry ?? "No Generated APDP"}
          </h3>

          <p className="mt-1 text-xs text-gray-400">
            {formatDate(detail.apdp?.generatedAt)}
          </p>
        </div>

        {/* Total Items */}
        <div className="rounded-2xl bg-white border border-gray-200 p-5 shadow-sm hover:shadow-md transition">
          <p className="text-sm text-gray-500">Total Items</p>

          <h3 className="mt-2 text-2xl font-bold text-gray-900">
            {detail.items.length}
          </h3>

          <p className="mt-1 text-xs text-gray-400">Line items</p>
        </div>

        {/* Created By */}
        <div className="rounded-2xl bg-white border border-gray-200 p-5 shadow-sm hover:shadow-md transition">
          <p className="text-sm text-gray-500">Created By</p>

          <h3 className="mt-2 text-lg font-semibold text-gray-900 break-words">
            {detail.createdByName}
          </h3>

          <p className="mt-1 text-xs text-gray-500">
            {formatDate(detail.createdAt)}
          </p>
        </div>

        {/* Generated By */}
        <div className="rounded-2xl bg-white border border-gray-200 p-5 shadow-sm hover:shadow-md transition">
          <p className="text-sm text-gray-500">Generated By</p>

          <h3 className="mt-2 text-lg font-semibold text-gray-900 break-words">
            {detail.generatedByName ?? "-"}
          </h3>

          <p className="mt-1 text-xs text-gray-500">
            {formatDate(detail.generatedAt)}
          </p>
        </div>
      </div>

      {/* Remark */}
      <div className="rounded-[28px] border border-amber-200 bg-linear-to-br from-amber-50 to-orange-50 p-6 shadow-sm">
        <div className="flex items-center gap-3">
          <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-amber-100">
            📝
          </div>

          <div>
            <h2 className="text-lg font-semibold text-gray-900">Remark</h2>

            <p className="text-sm text-gray-500">
              Additional information from purchase order
            </p>
          </div>
        </div>

        <div className="mt-5 rounded-2xl border border-amber-200 bg-white/70 p-5 text-[15px] leading-relaxed text-gray-700">
          {detail.remark || "No remarks available"}
        </div>
      </div>

      {/* Linked Budget Plans */}
      <div className="rounded-[28px] border border-gray-200 bg-white p-6 shadow-sm">
        <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
          <div>
            <h2 className="text-lg font-semibold text-gray-900">
              Linked Budget Plans
            </h2>

            <p className="text-sm text-gray-500">
              Budget plans associated with this PO
            </p>
          </div>

          <div className="rounded-xl bg-indigo-50 px-4 py-2 text-sm font-semibold text-indigo-700">
            {detail.linkedBudgetPlans.length} Plans
          </div>
        </div>

        <div className="mt-6 flex flex-wrap gap-3">
          {detail.linkedBudgetPlans.map((item) => (
            <button
              id="btn_LinkedBudgetPlan"
              key={item.id}
              type="button"
              onClick={() => navigate(`/budgeting/plan/${item.id}`)}
              className="
        group
        flex
        items-center
        gap-2
        rounded-2xl
        border
        border-indigo-200
        bg-indigo-50
        px-4
        py-3
        transition-all
        hover:-translate-y-0.5
        hover:bg-indigo-100
        hover:shadow-md
        cursor-pointer
      "
            >
              <span className="text-indigo-600">📋</span>

              <span className="font-medium text-indigo-700">{item.code}</span>
            </button>
          ))}
        </div>
      </div>

      {/* Items */}
      <div className="rounded-3xl border border-gray-200 bg-white shadow-sm overflow-hidden">
        <div className="border-b bg-linear-to-r from-slate-50 to-white px-6 py-5">
          <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
            <div>
              <h2 className="text-xl font-semibold text-gray-900">
                Purchase Order Items
              </h2>

              <p className="text-sm text-gray-500">
                List of items included in this purchase order
              </p>
            </div>

            <div className="rounded-xl bg-[#EEF2FF] px-4 py-2 text-sm font-semibold text-[#2E277C]">
              {detail.items.length} Items
            </div>
          </div>
        </div>

        <DataTable
          columns={itemColumns}
          data={detail.items}
          rowKey={(item) => item.id}
          emptyMessage="No items found."
          tableClassName="min-w-350"
          className="border-0 rounded-none"
          striped={false}
          rowClassName="border-gray-100 transition-all duration-200 hover:bg-indigo-50/40"
        />

        <div className="border-t bg-[#FAFAFA] px-6 py-5">
          <div className="flex justify-end">
            <div className="text-right">
              <p className="text-sm text-gray-500">Grand Total</p>

              <p className="text-2xl font-bold text-[#2E277C]">
                {formatCurrency(detail.grandTotal)}
              </p>
            </div>
          </div>
        </div>
      </div>
      <Button
        variant="primary"
        onClick={handleGenerate}
        disabled={isGenerateDisabled}
        className="w-full sm:w-auto px-7"
      >
        {isSubmitting ? "Generating..." : "Generate APDP"}
      </Button>
    </div>
  );
}
