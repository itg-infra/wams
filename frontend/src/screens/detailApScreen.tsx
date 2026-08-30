import { useEffect } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { DataTable, type Column } from "../components/ui/table";
import { PageHeader } from "../components/ui/page-header";
import type { AccountPayableItemDetail } from "../types/DetailAp.type";
import { useAccountPayableController } from "../controllers/budgeting/listGeerateApController";

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

const itemColumns: Column<AccountPayableItemDetail>[] = [
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

        {/* <p className="text-xs text-gray-400">Item ID #{item.itemShadowId}</p> */}
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
  // {
  //   key: "cost",
  //   header: "Cost",
  //   align: "right",
  //   render: (item) => (
  //     <div className="font-medium text-gray-800">
  //       {formatCurrency(item.costValue)}
  //     </div>
  //   ),
  // },
  // {
  //   key: "quantity",
  //   header: "Quantity",
  //   align: "right",
  //   render: (item) => (
  //     <div className="font-medium text-gray-800">
  //       {item.quantity.toLocaleString("id-ID")}
  //     </div>
  //   ),
  // },
  // {
  //   key: "total",
  //   header: "Total",
  //   align: "right",
  //   render: (item) => (
  //     <div className="space-y-1">
  //       <p className="text-lg font-bold text-[#2E277C]">
  //         {formatCurrency(item.totalValue)}
  //       </p>

  //       <p className="text-xs text-gray-400">Line Total</p>
  //     </div>
  //   ),
  // },
];

export function DetailApScreen() {
  const { id } = useParams();

   const { handleGetDetail, isDetailLoading, selectedAccountPayable } =
      useAccountPayableController();
  
    const navigate = useNavigate();
  
  useEffect(() => {
    if (id) {
      handleGetDetail(Number(id));
    }
  }, [id, handleGetDetail]);
  
    if (isDetailLoading) {
      return (
        <div className="flex items-center justify-center min-h-100">
          Loading...
        </div>
      );
    }
  
    if (!selectedAccountPayable) {
      return (
        <div className="flex items-center justify-center min-h-100">
          Account Payable not found
        </div>
      );
    }

  return (
    <div className="flex-1 space-y-6 p-4 sm:p-6 lg:p-8 overflow-y-auto">
      <PageHeader
        title={selectedAccountPayable.code}
        onBack={() => navigate(-1)}
      />

      {/* Header */}
      <div className="relative overflow-hidden rounded-[32px] bg-linear-to-r from-[#2E277C] via-[#4338CA] to-[#6366F1] p-8 shadow-xl">
        <div className="absolute top-0 right-0 h-48 w-48 rounded-full bg-white/10 blur-3xl" />

        <div className="relative">
          <div className="flex flex-col gap-6 lg:flex-row lg:items-center lg:justify-between">
            <div>
              <p className="text-indigo-100 text-sm tracking-[0.2em] uppercase">
                Account Payable
              </p>

              <p className="mt-3 text-2xl font-bold text-white">
                {selectedAccountPayable.sapApNumber}
              </p>

              <p className="mt-3 text-indigo-100">
                Vendor: {selectedAccountPayable.vendorName}
              </p>
            </div>

            <div className="flex flex-col items-end gap-3">
              <span className="rounded-full bg-white/20 backdrop-blur px-5 py-2 text-sm font-semibold text-white border border-white/20">
                {selectedAccountPayable.status}
              </span>

              <span className="text-indigo-100 text-sm">
                {formatDate(selectedAccountPayable.docDate)}
              </span>
            </div>
          </div>
        </div>
      </div>

      {/* Summary */}
      <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-4 gap-5">
        <div className="rounded-3xl bg-white border border-indigo-100 p-6 shadow-sm hover:shadow-md transition">
          <p className="text-sm text-gray-500">Grand Total</p>

          <h3 className="mt-3 text-3xl font-bold text-[#2E277C]">
            {formatCurrency(selectedAccountPayable.grandTotal)}
          </h3>

          <p className="mt-2 text-xs text-gray-400">
            Total Account Payable amount
          </p>
        </div>

        <div className="rounded-3xl bg-white border border-gray-200 p-6 shadow-sm">
          <p className="text-sm text-gray-500">Total Items</p>

          <h3 className="mt-3 text-3xl font-bold text-gray-900">
            {selectedAccountPayable.items.length}
          </h3>

          <p className="mt-2 text-xs text-gray-400">Line items</p>
        </div>

        <div className="rounded-3xl bg-white border border-gray-200 p-6 shadow-sm">
          <p className="text-sm text-gray-500">Created By</p>

          <h3 className="mt-3 text-lg font-semibold">
            {selectedAccountPayable.createdByName}
          </h3>

          <p className="text-xs text-gray-500 mt-1">
            {formatDate(selectedAccountPayable.createdAt)}
          </p>
        </div>

        <div className="rounded-3xl bg-white border border-gray-200 p-6 shadow-sm">
          <p className="text-sm text-gray-500">Generated By</p>

          <h3 className="mt-3 text-lg font-semibold">
            {selectedAccountPayable.generatedByName ?? "-"}
          </h3>

          <p className="text-xs text-gray-500 mt-1">
            {formatDate(selectedAccountPayable.generatedAt)}
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
          {selectedAccountPayable.remark || "No remarks available"}
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
            {selectedAccountPayable.linkedBudgetPlanCodes.length} Plans
          </div>
        </div>

        <div className="mt-6 flex flex-wrap gap-3">
          {selectedAccountPayable.linkedBudgetPlanCodes.map((item) => (
            <button
              id="btn_LinkedBudgetPlan"
              // key={item.}
              type="button"
              // onClick={() => navigate(`/budgeting/plan/${item.id}`)}
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

              <span className="font-medium text-indigo-700">{item}</span>
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
              {selectedAccountPayable.items.length} Items
            </div>
          </div>
        </div>

        <DataTable
          columns={itemColumns}
          data={selectedAccountPayable.items}
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
                {formatCurrency(selectedAccountPayable.grandTotal)}
              </p>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
