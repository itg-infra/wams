import { useEffect, useState, useCallback } from "react";
import { useBudgetRealizationStore } from "../store/budgetRealizationStore";
import { useVendorStore } from "../master_data/store/vendorStore";
import { useUomStore } from "../master_data/store/unitofmeasurementStore";
import type { RfbaRowItem } from "../types/budgetRealization.type";
import { Combobox } from "./combobox";

// ─── Inline cell components ───────────────────────────────────────────────────

function TypeCell({ value, onChange }: { value: RfbaRowItem["type"]; onChange: (v: RfbaRowItem["type"]) => void }) {
    return (
        <select
            value={value ?? ""}
            onChange={(e) => onChange((e.target.value as RfbaRowItem["type"]) || null)}
            className="w-full h-8 px-2 text-xs border border-gray-300 rounded-md bg-white focus:border-blue-500 focus:ring-1 focus:ring-blue-500 outline-none"
        >
            <option value="">— Select —</option>
            <option value="internal">Internal</option>
            <option value="external">External</option>
        </select>
    );
}

function RfbaCell({ value, onChange }: { value: boolean | null; onChange: (v: boolean | null) => void }) {
    return (
        <select
            value={value === null ? "" : value ? "yes" : "no"}
            onChange={(e) => {
                const v = e.target.value;
                onChange(v === "" ? null : v === "yes");
            }}
            className="w-full h-8 px-2 text-xs border border-gray-300 rounded-md bg-white focus:border-blue-500 focus:ring-1 focus:ring-blue-500 outline-none"
        >
            <option value="">— Select —</option>
            <option value="yes">Yes</option>
            <option value="no">No</option>
        </select>
    );
}

function NumberCell({
    value,
    onChange,
    placeholder,
}: {
    value: number | null;
    onChange: (v: number | null) => void;
    placeholder?: string;
}) {
    const [local, setLocal] = useState(value !== null ? String(value) : "");

    useEffect(() => {
        setLocal(value !== null ? String(value) : "");
    }, [value]);

    return (
        <input
            type="number"
            value={local}
            placeholder={placeholder}
            onChange={(e) => setLocal(e.target.value)}
            onBlur={() => {
                const parsed = parseFloat(local);
                onChange(isNaN(parsed) ? null : parsed);
            }}
            className="w-full h-8 px-2 text-xs border border-gray-300 rounded-md bg-white focus:border-blue-500 focus:ring-1 focus:ring-blue-500 outline-none [appearance:textfield]"
        />
    );
}

function TextCell({
    value,
    onChange,
    placeholder,
}: {
    value: string | null;
    onChange: (v: string | null) => void;
    placeholder?: string;
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
            onChange={(e) => setLocal(e.target.value)}
            onBlur={() => onChange(local.trim() || null)}
            className="w-full h-8 px-2 text-xs border border-gray-300 rounded-md bg-white focus:border-blue-500 focus:ring-1 focus:ring-blue-500 outline-none"
        />
    );
}

// ─── Vendor Combobox Cell ─────────────────────────────────────────────────────

function VendorCell({
    vendorId,
    onSelect,
}: {
    vendorId: string | null;
    onSelect: (id: string | null, code: string | null, name: string | null) => void;
}) {
    const { vendors, isLoading, fetchVendors } = useVendorStore();

    const handleSearch = useCallback(
        (search: string) => {
            fetchVendors({ search, page: 1 });
        },
        [fetchVendors]
    );

    useEffect(() => {
        fetchVendors({ page: 1, limit: 20 });
    }, [fetchVendors]);

    const options = vendors.map((v) => ({
        value: v.id,
        label: v.cardName,
        sublabel: v.cardCode,
    }));

    return (
        <Combobox
            options={options}
            value={vendorId}
            onChange={(opt) => onSelect(opt?.value ?? null, opt?.sublabel ?? null, opt?.label ?? null)}
            onSearchChange={handleSearch}
            isLoading={isLoading}
            placeholder="Search vendor..."
            className="min-w-35"
        />
    );
}

// ─── UOM Combobox Cell ────────────────────────────────────────────────────────

function UomCell({
    uomId,
    onSelect,
}: {
    uomId: number | null;
    onSelect: (id: number | null, code: string | null, name: string | null) => void;
}) {
    const { uoms, isLoading, fetchUoms } = useUomStore();

    const handleSearch = useCallback(
        (search: string) => {
            fetchUoms({ search, page: 1 });
        },
        [fetchUoms]
    );

    useEffect(() => {
        fetchUoms({ page: 1, limit: 50 });
    }, [fetchUoms]);

    const options = uoms.map((u) => ({
        value: String(u.id),
        label: u.name,
        sublabel: u.code,
    }));

    return (
        <Combobox
            options={options}
            value={uomId !== null ? String(uomId) : null}
            onChange={(opt) =>
                onSelect(
                    opt ? Number(opt.value) : null,
                    opt?.sublabel ?? null,
                    opt?.label ?? null
                )
            }
            onSearchChange={handleSearch}
            isLoading={isLoading}
            placeholder="Search UOM..."
            className="min-w-25"
        />
    );
}

// ─── Row Component ────────────────────────────────────────────────────────────

function RfbaTableRow({ row }: { row: RfbaRowItem }) {
    const updateRow = useBudgetRealizationStore((s) => s.updateRow);

    const update = useCallback(
        (payload: Partial<RfbaRowItem>) => updateRow(row.id, payload),
        [row.id, updateRow]
    );

    return (
        <tr className="border-b border-gray-100 hover:bg-gray-50/50 transition-colors">
            {/* Cost ID */}
            <td className="px-3 py-2 text-xs text-gray-700 whitespace-nowrap font-mono">
                {row.costDetail}
            </td>

            {/* Type */}
            <td className="px-2 py-1.5 min-w-27.5">
                <TypeCell value={row.type} onChange={(v) => update({ type: v })} />
            </td>

            {/* Vendor Code */}
            <td className="px-2 py-1.5 text-xs text-gray-500 whitespace-nowrap">
                {row.vendorCode ?? <span className="text-gray-300">—</span>}
            </td>

            {/* Vendor Name */}
            <td className="px-2 py-1.5 min-w-40">
                <VendorCell
                    vendorId={row.vendorId}
                    onSelect={(id, code, name) =>
                        update({ vendorId: id, vendorCode: code, vendorName: name })
                    }
                />
            </td>

            {/* RFBA */}
            <td className="px-2 py-1.5 min-w-22.5">
                <RfbaCell value={row.isRfba} onChange={(v) => update({ isRfba: v })} />
            </td>

            {/* Doc External */}
            <td className="px-2 py-1.5 min-w-30">
                <TextCell
                    value={row.docExternal}
                    onChange={(v) => update({ docExternal: v })}
                    placeholder="Doc external"
                />
            </td>

            {/* Cost Name */}
            <td className="px-3 py-2 text-xs text-gray-700 whitespace-nowrap">
                {row.costName}
            </td>

            {/* COA */}
            <td className="px-3 py-2 text-xs text-gray-700 whitespace-nowrap font-mono">
                {row.coa}
            </td>

            {/* COA Name */}
            <td className="px-3 py-2 text-xs text-gray-600 whitespace-nowrap">
                {row.coaName}
            </td>

            {/* Bill of Lading */}
            <td className="px-2 py-1.5 min-w-30">
                <TextCell
                    value={row.billOfLading}
                    onChange={(v) => update({ billOfLading: v })}
                    placeholder="BL number"
                />
            </td>

            {/* Unit Cost */}
            <td className="px-2 py-1.5 min-w-25">
                <NumberCell
                    value={row.unitCost}
                    onChange={(v) => update({ unitCost: v })}
                    placeholder="0"
                />
            </td>

            {/* Unit Count */}
            <td className="px-2 py-1.5 min-w-22.5">
                <NumberCell
                    value={row.unitCount}
                    onChange={(v) => update({ unitCount: v })}
                    placeholder="0"
                />
            </td>

            {/* UoM */}
            <td className="px-2 py-1.5 min-w-30">
                <UomCell
                    uomId={row.uomId}
                    onSelect={(id, code, name) =>
                        update({ uomId: id, uomCode: code, uomName: name })
                    }
                />
            </td>

            {/* Description */}
            <td className="px-2 py-1.5 min-w-40">
                <TextCell
                    value={row.description}
                    onChange={(v) => update({ description: v })}
                    placeholder="Enter description"
                />
            </td>
        </tr>
    );
}

// ─── Main Table ───────────────────────────────────────────────────────────────

export function BudgetRealizationTable() {
    const rows = useBudgetRealizationStore((s) => s.rows);

    if (rows.length === 0) {
        return (
            <div className="flex items-center justify-center h-32 text-sm text-gray-400">
                No items loaded. Please select a budget template first.
            </div>
        );
    }

    return (
        <div className="w-full overflow-x-auto rounded-lg border border-gray-200">
            <table className="w-full border-collapse text-sm">
                <thead>
                    <tr className="bg-gray-50 border-b border-gray-200">
                        <th className="px-3 py-2.5 text-left text-xs font-semibold text-gray-600 whitespace-nowrap">Cost ID</th>
                        <th className="px-3 py-2.5 text-left text-xs font-semibold text-gray-600 whitespace-nowrap">Type</th>
                        <th className="px-3 py-2.5 text-left text-xs font-semibold text-gray-600 whitespace-nowrap">Vendor Code</th>
                        <th className="px-3 py-2.5 text-left text-xs font-semibold text-gray-600 whitespace-nowrap">Vendor Name</th>
                        <th className="px-3 py-2.5 text-left text-xs font-semibold text-gray-600 whitespace-nowrap">RFBA</th>
                        <th className="px-3 py-2.5 text-left text-xs font-semibold text-gray-600 whitespace-nowrap">Doc External</th>
                        <th className="px-3 py-2.5 text-left text-xs font-semibold text-gray-600 whitespace-nowrap">Cost Name</th>
                        <th className="px-3 py-2.5 text-left text-xs font-semibold text-gray-600 whitespace-nowrap">COA</th>
                        <th className="px-3 py-2.5 text-left text-xs font-semibold text-gray-600 whitespace-nowrap">COA Name</th>
                        <th className="px-3 py-2.5 text-left text-xs font-semibold text-gray-600 whitespace-nowrap">Bill of Lading</th>
                        <th className="px-3 py-2.5 text-left text-xs font-semibold text-gray-600 whitespace-nowrap">Unit Cost</th>
                        <th className="px-3 py-2.5 text-left text-xs font-semibold text-gray-600 whitespace-nowrap">Unit Count</th>
                        <th className="px-3 py-2.5 text-left text-xs font-semibold text-gray-600 whitespace-nowrap">UoM</th>
                        <th className="px-3 py-2.5 text-left text-xs font-semibold text-gray-600 whitespace-nowrap">Description</th>
                    </tr>
                </thead>
                <tbody>
                    {rows.map((row) => (
                        <RfbaTableRow key={row.id} row={row} />
                    ))}
                </tbody>
            </table>
        </div>
    );
}