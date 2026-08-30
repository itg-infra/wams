import { useCallback, useEffect, useState } from "react";
import { useActivityController } from "../master_data/controller/activityController";
// import { useItemController } from "../master_data/controller/itemController";
import { useBudgetRealizationStore } from "../store/budgetRealizationStore";
import type {
  pphTaxTypeItem,
  ppnTaxTypeItem,
  RfbaRowItem,
} from "../types/budgetRealization.type";
import { Combobox } from "./combobox";
import { ChevronDown } from "lucide-react";
import { useRateCardVendors } from "../master_data/store/rateCardByItemStore";
// import { useUomStore } from "../master_data/store/unitofmeasurementStore";
import type { BudgetPlanCostDetail } from "../types/detailRecapWo";
import type { FinanceReportCostDetail } from "../types/financeReport.type";
import { formatDate } from "./format/dateTimeFormat";
import { formatNumber } from "./format/formatCurrency";
import { COST_GRID_COLS, COST_GRID_COLS_PLAN_REAL } from "./gridCols";
import { useItemSearch } from "../hook/useItemSearch";

// const COST_GRID_COLS =
//   "grid-cols-[120px_180px_120px_140px_160px_180px_100px_350px_140px_120px_150px_180px_180px_180px_140px_220px]";

  const FINANCE_COST_GRID_COLS =
    "grid-cols-[120px_180px_280px_140px_160px_160px_160px_160px_160px__180px_100px_150px_140px_120px_180px_180px_]";

export type SpkItemCodeOption = {
  spkId: number;
  itemCode: string;
  label: string;
  blNo: string;
  quantity: number; // ← tambahkan ini di sumber data spkItemCodeOptions
  kemasan: string;
};

export function CostRow({
  row,
  spkItemCodeOptions,
}: {
  row: RfbaRowItem;
  spkItemCodeOptions: SpkItemCodeOption[];
}) {
  const { updateRow, removeRow } = useBudgetRealizationStore();

  // const { items } = useItemController();

  const {
    items,
    isLoading,
    isLoadingMore,
    hasMore,
    onSearchChange,
    onEndReached,
  } = useItemSearch();
  const { activities } = useActivityController();

  const update = useCallback(
    (payload: Partial<RfbaRowItem>) => updateRow(row.id, payload),
    [row.id, updateRow],
  );
  return (
    <div className={`grid ${COST_GRID_COLS} gap-3 items-center group`}>
      {/* {row.isManual ? (
        <Combobox
          options={items.map((item) => ({
            value: String(item.id),

            // text utama
            label: item.itemCode,

            // text kecil dibawah
            sublabel: `${item.acctCode} • ${item.acctName}`,
          }))}
          value={row.itemShadowId !== null ? String(row.itemShadowId) : null}
          placeholder="Search Cost ID..."
          onChange={(opt) => {
            if (!opt) return;

            const selected = items.find((i) => String(i.id) === opt.value);

            if (!selected) return;

            update({
              itemShadowId: Number(selected.id),

              costDetail: selected.itemCode,
              costName: selected.itemName,

              coa: selected.acctCode,
              coaName: selected.acctName ?? "",
            });
          }}
        />
      ) : (
        <ReadonlyCell value={row.costDetail} />
      )} */}
      {row.isManual ? (
        <Combobox
          options={items.map((item) => ({
            value: String(item.id),

            // text utama
            label: item.itemCode,

            // text kecil dibawah
            sublabel: `${item.acctCode} • ${item.acctName}`,
          }))}
          value={row.itemShadowId !== null ? String(row.itemShadowId) : null}
          placeholder="Search Cost ID..."
          isLoading={isLoading}
          isLoadingMore={isLoadingMore}
          hasMore={hasMore}
          onSearchChange={onSearchChange}
          onEndReached={onEndReached}
          onChange={(opt) => {
            if (!opt) return;

            const selected = items.find((i) => String(i.id) === opt.value);

            if (!selected) return;

            update({
              itemShadowId: Number(selected.id),

              costDetail: selected.itemCode,
              costName: selected.itemName,

              coa: selected.acctCode,
              coaName: selected.acctName ?? "",
            });
          }}
        />
      ) : (
        <ReadonlyCell value={row.costDetail} />
      )}

      {/* COST NAME */}
      <ReadonlyCell value={row.costName} />

      {/* COA */}
      <ReadonlyCell value={row.coa} />

      {/* COA NAME */}
      <ReadonlyCell value={row.coaName} />

      {/* ACTIVITY */}
      <div className="flex flex-col gap-1.5 min-w-0">
        {row.isManual ? (
          <div className="relative">
            <select
              className="w-full h-10 px-3 pr-8 rounded border border-gray-300 bg-white text-sm appearance-none focus:border-blue-500 focus:ring-1 focus:ring-blue-500 outline-none"
              value={row.activityTypeId ?? ""}
              onChange={(e) =>
                update({
                  activityTypeId: Number(e.target.value),
                })
              }
            >
              <option value="">Select Activity</option>

              {activities.map((a) => (
                <option key={a.id} value={a.id}>
                  {a.name}
                </option>
              ))}
            </select>

            <ChevronDown className="w-4 h-4 absolute right-2 top-1/2 -translate-y-1/2 text-gray-500 pointer-events-none" />
          </div>
        ) : (
          <ReadonlyCell value={`${row.activityTypeName}`} />
        )}
      </div>

      {/* TYPE */}
      <SelectCell
        value={row.type}
        onChange={(v) => update({ type: v as RfbaRowItem["type"] })}
        options={[
          { label: "Internal", value: "internal" },
          { label: "External", value: "external" },
        ]}
      />

      {/* VENDOR */}
      <VendorComboboxCell
        itemShadowId={row.itemShadowId}
        vendorCode={row.vendorCode}
        onSelect={(shadowId, code, name, cost, uom, ppnTaxType, pphTaxType) =>
          updateRow(row.id, {
            vendorId: shadowId !== null ? String(shadowId) : null,
            vendorCode: code,
            vendorName: name,
            unitCost: cost,
            uomName: uom,
            pphTaxType: ppnTaxType,
            ppnTaxType: pphTaxType,
          })
        }
      />

      {/* VENDOR NAME */}
      <div className="h-10 px-3 flex items-center bg-gray-50 border border-gray-200 rounded text-sm text-gray-600 truncate">
        {row.vendorName || <span className="text-gray-300">—</span>}
      </div>

      {/* Ppn */}
      <div className="h-10 px-3 flex items-center bg-gray-50 border border-gray-200 rounded text-sm text-gray-600 truncate">
        {row.pphTaxType != null ? (
          "Yes"
        ) : (
          <span className="text-gray-300">No</span>
        )}
      </div>

      {/* status */}
      <div className="h-10 px-3 flex items-center bg-gray-50 border border-gray-200 rounded text-sm text-gray-600 truncate">
        {row.costTreatment != "TidakDibiayakan" ? (
          "Paid"
        ) : (
          <span className="text-gray-300">No</span>
        )}
      </div>

      {/* Total Rate */}
      <div className="h-10 px-3 flex items-center bg-gray-50 border border-gray-200 rounded text-sm text-gray-600 truncate">
        {row.ppnTaxType?.rate || (
          <span className="text-gray-300">No Ppn Rate</span>
        )}
      </div>

      {/* Pph */}
      <div className="h-10 px-3 flex items-center bg-gray-50 border border-gray-200 rounded text-sm text-gray-600 truncate">
        {row.pphTaxType?.code || <span className="text-gray-300">No Ppn</span>}
      </div>

      {/* RFBA */}
      <SelectCell
        value={row.isRfba === null ? null : row.isRfba ? "yes" : "no"}
        onChange={(v) => update({ isRfba: v === null ? null : v === "yes" })}
        options={[
          { label: "Yes", value: "yes" },
          { label: "No", value: "no" },
        ]}
      />

      {/* DOC EXTERNAL */}
      <TextInputCell
        value={row.docExternal}
        onChange={(v) => update({ docExternal: v })}
        placeholder="Doc external"
      />

      <SelectCell
        value={row.billOfLading ?? null}
        onChange={(v) => {
          if (v === "no-bl") {
            update({
              billOfLading: "no-bl", // simpan value asli, bukan label
              selectedSpkItemCode: "No Item Code",
              selectedSpkId: null,
              quantity: 0,
              unitCount: 0,
              kemasan: "-",
            });
            return;
          }

          const selected = spkItemCodeOptions.find((opt) => opt.blNo === v);

          update({
            billOfLading: v,
            selectedSpkItemCode: selected?.itemCode ?? null,
            selectedSpkId: selected?.spkId ?? null,
            selectedSpk: selected ?? null,
            unitCount: selected?.quantity ?? null,
            kemasan: selected?.kemasan ?? null,
          });
        }}
        options={[
          { label: "No BL", value: "no-bl" },
          ...spkItemCodeOptions.map((opt) => ({
            label: opt.blNo,
            value: opt.blNo,
          })),
        ]}
        placeholder="Select BL"
      />

      <ReadonlyCell value={row.selectedSpkItemCode ?? "-"} />
      {/* <NumberInputCell
        value={row.costValue}
        onChange={(v) => update({ costValue: v })}
      /> */}
      {/* <NumberInputCell
        value={row.unitCost}
        onChange={(v) => update({ costValue: v })}
      /> */}
      <NumberInputCell
        value={row.costValue ?? row.unitCost}
        onChange={(v) => update({ costValue: v })}
      />

      {/* UNIT COUNT */}
      <NumberInputCell
        value={row.unitCount}
        max={row.selectedSpk?.quantity ?? undefined}
        onChange={(v) => update({ unitCount: v })}
      />

      <ReadonlyCell value={row.kemasan ?? "-"} />

      {/* UOM */}
      {/* <UomComboboxCell
        uomId={row.uomId}
        onSelect={(id, code, name) =>
          update({
            uomId: id,
            uomCode: code,
            uomName: name,
          })
        }
      /> */}
      <div className="h-10 px-3 flex items-center bg-gray-50 border border-gray-200 rounded text-sm text-gray-600 truncate">
        {row.uomName || <span className="text-gray-300">—</span>}
      </div>

      {/* DESCRIPTION */}
      <div className="flex items-center gap-1">
        <TextInputCell
          value={row.description}
          onChange={(v) => update({ description: v })}
          placeholder="Enter description"
        />

        {row.isManual && (
          <button
            type="button"
            onClick={() => removeRow(row.id)}
            className="opacity-0 group-hover:opacity-100 transition-opacity shrink-0 w-7 h-7 flex items-center justify-center rounded text-red-400 hover:text-red-600 hover:bg-red-50"
          >
            <svg
              className="w-3.5 h-3.5"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M6 18L18 6M6 6l12 12"
              />
            </svg>
          </button>
        )}
      </div>
    </div>
  );
}

export function CostRowFinanceReport({
  row,
}: {
  row: FinanceReportCostDetail;
}) {
  return (
    <div className={`grid ${FINANCE_COST_GRID_COLS} gap-3 items-center group`}>
      <ReadonlyCell value={row.workOrderId} />

      {/* COST NAME */}
      <ReadonlyCell value={String(row.blNumber)} />

      {/* COA */}
      <ReadonlyCell value={String(row.vessel)} />

      {/* COA NAME */}
      <ReadonlyCell value={row.product} />

      <ReadonlyCell value={row.pic} />

      <ReadonlyCell value={row.isRfba ? "yes" : "no"} />

      <ReadonlyCell value={String(formatDate(row?.startDate ?? "-"))} />

      <ReadonlyCell value={String(formatDate(row?.endDate ?? "-"))} />

      <ReadonlyCell value={String(formatNumber(row.totalPrice))} />

      <ReadonlyCell value={String(row.isPpnApplied ? "yes" : "no")} />

      <ReadonlyCell value={String(row.paymentStatus)} />

      <ReadonlyCell value={String(row.ppnRatePercent)} />

      <ReadonlyCell value={String(formatNumber(row.totalPricePpn))} />

      <ReadonlyCell value={String(row.isPphApplied ? "yes" : "no")} />

      <ReadonlyCell value={String(row.pphType ?? "-")} />

      <ReadonlyCell value={String(formatNumber(row.totalPricePph) ?? "-")} />
    </div>
  );
}

export function CostRowRecap({
  row,
}: {
  row: BudgetPlanCostDetail;
  //   spkItemCodeOptions: {
  //     spkId: number;
  //     itemCode: string;
  //     label: string;
  //   }[];
}) {
  return (
    <div
      className={`grid ${COST_GRID_COLS_PLAN_REAL} gap-3 items-center group`}
    >
      <ReadonlyCell value={row.costName} />

      {/* COA */}
      <ReadonlyCell value={row.coaCode} />

      {/* COA Name */}
      <ReadonlyCell value={row.coaName} />

      {/* Activity */}
      {/* <ReadonlyCell value={row.activity} /> */}

      {/* Type */}
      <ReadonlyCell value={row.type} />

      {/* Vendor Code */}
      <ReadonlyCell value={row.vendorCode} />

      {/* Vendor Name */}
      <ReadonlyCell value={row.vendorName} />

      {/* RFBA */}
      <ReadonlyCell value={row.isRfba ? "Yes" : "No"} />

      {/* Doc External */}
      <ReadonlyCell value={String(row.docExternal)} />

      {/* Bill of Lading */}
      <ReadonlyCell value={String(row.billOfLading)} />

      {/* Unit Cost */}
      <ReadonlyCell value={String(row.unitCost)} />

      {/* Unit Count */}
      <ReadonlyCell value={String(row.unitCount)} />

      {/* UoM */}
      <ReadonlyCell value={row.uomCode} />

      {/* Description */}
      <ReadonlyCell value={row.description ?? ""} />
    </div>
  );
}

function ReadonlyCell({ value }: { value: string }) {
  return (
    <div className="h-10 px-3 flex items-center bg-gray-100 border border-gray-200 rounded text-sm text-gray-600 truncate">
      {value || <span className="text-gray-300">—</span>}
    </div>
  );
}

function TextInputCell({
  value,
  onChange,
  placeholder,
  disabled = false,
}: {
  value: string | null;
  onChange: (v: string | null) => void;
  placeholder?: string;
  disabled?: boolean;
}) {
  const [local, setLocal] = useState(value ?? "");

  useEffect(() => {
    setLocal(value ?? "");
  }, [value]);

  return (
    <input
      type="text"
      value={local}
      placeholder={placeholder}
      disabled={disabled}
      onChange={(e) => setLocal(e.target.value)}
      onBlur={() => onChange(local.trim() || null)}
      className={`h-10 px-3 text-sm border rounded outline-none w-full ${
        disabled
          ? "bg-gray-100 text-gray-500 cursor-not-allowed border-gray-200"
          : "bg-white border-gray-300 focus:border-blue-500 focus:ring-1 focus:ring-blue-500"
      }`}
    />
  );
}

function NumberInputCell({
  value,
  onChange,
  max,
}: {
  value: number | null;
  onChange: (v: number | null) => void;
  max?: number; // ← opsional
}) {
  const formatNumber = (num: string) => {
    if (!num) return "";

    const cleaned = num.replace(/\D/g, "");

    return new Intl.NumberFormat("id-ID").format(Number(cleaned));
  };

  const [local, setLocal] = useState(
    value !== null ? new Intl.NumberFormat("id-ID").format(value) : "",
  );

  useEffect(() => {
    setLocal(
      value !== null ? new Intl.NumberFormat("id-ID").format(value) : "",
    );
  }, [value]);

  return (
    <input
      type="text"
      value={local}
      placeholder="0"
      onChange={(e) => {
        const raw = e.target.value.replace(/\D/g, "");
        setLocal(formatNumber(raw));
      }}
      onBlur={() => {
        const raw = local.replace(/\./g, "");
        let n = Number(raw);

        if (isNaN(n) || raw === "") {
          onChange(null);
          return;
        }

        // cap value ke max kalau max diberikan
        if (max !== undefined && n > max) {
          n = max;
          setLocal(new Intl.NumberFormat("id-ID").format(n));
        }

        onChange(n);
      }}
      className="h-10 px-3 text-sm border border-gray-300 rounded bg-white focus:border-blue-500 focus:ring-1 focus:ring-blue-500 outline-none w-full"
    />
  );
}

function SelectCell({
  value,
  onChange,
  options,
  placeholder = "— Select —",
}: {
  value: string | null;
  onChange: (v: string | null) => void;
  options: { label: string; value: string }[];
  placeholder?: string;
}) {
  return (
    <div className="relative">
      <select
        value={value ?? ""}
        onChange={(e) => onChange(e.target.value || null)}
        className="w-full h-10 px-3 pr-8 rounded border border-gray-300 bg-white text-sm appearance-none focus:border-blue-500 focus:ring-1 focus:ring-blue-500 outline-none"
      >
        <option value="">{placeholder}</option>

        {options.map((o) => (
          <option key={o.value} value={o.value}>
            {o.label}
          </option>
        ))}
      </select>

      <ChevronDown className="w-4 h-4 absolute right-2 top-1/2 -translate-y-1/2 text-gray-500 pointer-events-none" />
    </div>
  );
}

/* ================= VENDOR COMBOBOX ================= */

function VendorComboboxCell({
  itemShadowId,
  vendorCode,
  onSelect,
}: {
  itemShadowId: number | null;
  vendorCode: string | null;
  onSelect: (
    vendorShadowId: number | null,
    vendorCode: string | null,
    vendorName: string | null,
    costValue: number | null,
    uomValue: string | null,
    ppnTaxType: ppnTaxTypeItem | null,
    pphTaxType: pphTaxTypeItem | null,
  ) => void;
}) {
  const { vendors, isLoading } = useRateCardVendors(itemShadowId);

  const options = vendors.map((v) => ({
    value: String(v.vendorShadowId),
    label: v.vendorCode,
    sublabel: v.vendorName,
    costValue: v.costValue,
  }));

  const currentOption = options.find((o) => o.label === vendorCode) ?? null;

  return (
    <Combobox
      options={options}
      value={currentOption?.value ?? null}
      onChange={(opt) => {
        if (!opt) {
          onSelect(null, null, null, null, null, null, null);
          return;
        }

        const selected = vendors.find(
          (v) => String(v.vendorShadowId) === opt.value,
        );

        console.log(selected);

        onSelect(
          selected?.vendorShadowId ?? null,
          selected?.vendorCode ?? null,
          selected?.vendorName ?? null,
          selected?.costValue ?? null,
          selected?.uomName ?? null,
          selected?.pphTaxType ?? null,
          selected?.ppnTaxType ?? null,
        );
      }}
      isLoading={isLoading}
      placeholder="Search code..."
    />
  );
}

/* ================= UOM COMBOBOX ================= */

// function UomComboboxCell({
//   uomId,
//   onSelect,
// }: {
//   uomId: number | null;
//   onSelect: (
//     id: number | null,
//     code: string | null,
//     name: string | null,
//   ) => void;
// }) {
//   const { uoms, isLoading, fetchUoms } = useUomStore();

//   useEffect(() => {
//     fetchUoms({ page: 1, limit: 50 });
//   }, [fetchUoms]);

//   const handleSearch = useCallback(
//     (search: string) => {
//       fetchUoms({ search, page: 1 });
//     },
//     [fetchUoms],
//   );

//   const options = uoms.map((u) => ({
//     value: String(u.id),
//     label: u.name,
//     sublabel: u.code,
//   }));

//   return (
//     <Combobox
//       options={options}
//       value={uomId !== null ? String(uomId) : null}
//       onChange={(opt) =>
//         onSelect(
//           opt ? Number(opt.value) : null,
//           opt?.sublabel ?? null,
//           opt?.label ?? null,
//         )
//       }
//       onSearchChange={handleSearch}
//       isLoading={isLoading}
//       placeholder="Search UOM..."
//     />
//   );
// }
