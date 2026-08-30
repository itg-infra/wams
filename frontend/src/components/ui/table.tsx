import { type ReactNode } from "react";
import {
  ChevronLeft, ChevronRight, AlertCircle,
} from "lucide-react";
import { cn } from "../../lib/utils";

// ─── Types ──────────────────────────────────────────────────────────────────
export type SortDirection = "asc" | "desc";

export interface SortState {
    key: string;
    direction: SortDirection;
}

type Align = "left" | "center" | "right";
type Breakpoint = "sm" | "md" | "lg";

export interface Column<T> {
    /** Unique key; also used as the sort key unless `sortKey` is provided. */
    key: string;
    header: ReactNode;
    /** Cell renderer for a row. */
    render: (row: T, index: number) => ReactNode;
    sortable?: boolean;
    /** Override the value passed to `onSortChange` (defaults to `key`). */
    sortKey?: string;
    align?: Align;
    /** Extra classes for the `<td>`. */
    className?: string;
    /** Extra classes for the `<th>`. */
    headerClassName?: string;
    /** Hide the column below this breakpoint (matches the users screen pattern). */
    hideBelow?: Breakpoint;
    /** Column width, e.g. "30%". */
    width?: string;
}

interface DataTableProps<T> {
    columns: Column<T>[];
    data: T[];
    rowKey: (row: T, index: number) => string | number;
    isLoading?: boolean;
    error?: string | null;
    onRetry?: () => void;
    emptyMessage?: string;
    /** Current sort state (controlled). */
    sort?: SortState | null;
    onSortChange?: (sort: SortState) => void;
    /** Number of skeleton rows while loading. */
    skeletonRows?: number;
    /** Zebra-stripe rows like the users screen. */
    striped?: boolean;
    /** Extra classes on the outer container. */
    className?: string;
    /** Extra classes on the `<table>` element, e.g. "min-w-225" for wide tables. */
    tableClassName?: string;
    /** Optional footer content rendered in a `<tfoot>` (e.g. totals row). */
    footer?: ReactNode;
    /** Extra classes applied to each data `<tr>` (in addition to zebra/border). */
    rowClassName?: string | ((row: T, index: number) => string);
    /** Row click handler; when set, rows get `cursor-pointer`. */
    onRowClick?: (row: T, index: number) => void;
}

// ─── Helpers ────────────────────────────────────────────────────────────────
const alignClass: Record<Align, string> = {
    left: "text-left",
    center: "text-center",
    right: "text-right",
};

const hideBelowClass: Record<Breakpoint, string> = {
    sm: "hidden sm:table-cell",
    md: "hidden md:table-cell",
    lg: "hidden lg:table-cell",
};


// ─── Skeleton ───────────────────────────────────────────────────────────────
function TableSkeleton<T>({ columns, rows }: { columns: Column<T>[]; rows: number }) {
    return (
        <>
            {Array.from({ length: rows }).map((_, r) => (
                <tr key={r} className={r % 2 === 1 ? "bg-[#eef0f8]" : "bg-white"}>
                    {columns.map((col) => (
                        <td
                            key={col.key}
                            className={cn(
                                "px-5 py-3.5",
                                col.hideBelow && hideBelowClass[col.hideBelow],
                            )}
                        >
                            <div className="h-3.5 w-24 max-w-full bg-gray-200 animate-pulse rounded" />
                        </td>
                    ))}
                </tr>
            ))}
        </>
    );
}

// ─── DataTable ──────────────────────────────────────────────────────────────
export function DataTable<T>({
    columns,
    data,
    rowKey,
    isLoading = false,
    error = null,
    onRetry,
    emptyMessage = "No data found.",
    skeletonRows = 8,
    striped = true,
    className,
    tableClassName,
    footer,
    rowClassName,
    onRowClick,
}: DataTableProps<T>) {

    const isEmpty = !isLoading && !error && data.length === 0;

    return (
        <div className={cn("border-2 rounded-md overflow-x-auto", className)}>
            <table className={cn("w-full", tableClassName)}>
                <thead>
                    <tr className="bg-white border-b-3 border-gray-200">
                        {columns.map((col) => {
                            const align = col.align ?? "left";
                            return (
                                <th
                                    key={col.key}
                                    style={col.width ? { width: col.width } : undefined}
                                    className={cn(
                                        "px-5 py-3.5 text-sm font-semibold text-gray-800 whitespace-nowrap",
                                        alignClass[align],
                                        col.hideBelow && hideBelowClass[col.hideBelow],
                                        col.sortable && "select-none",
                                        col.headerClassName,
                                    )}
                                >
                                    <span
                                        className={cn(
                                            "inline-flex items-center gap-1.5",
                                            align === "right" && "flex-row-reverse",
                                            align === "center" && "justify-center",
                                        )}
                                    >
                                        {col.header}
                                    </span>
                                </th>
                            );
                        })}
                    </tr>
                </thead>
                <tbody>
                    {isLoading && <TableSkeleton columns={columns} rows={skeletonRows} />}

                    {!isLoading && error && (
                        <tr className="bg-white">
                            <td colSpan={columns.length} className="px-5 py-14 text-center">
                                <div className="inline-flex items-center gap-2 text-sm text-red-600">
                                    <AlertCircle className="w-4 h-4 shrink-0" />
                                    <span>{error}</span>
                                    {onRetry && (
                                        <button
                                            onClick={onRetry}
                                            className="ml-2 text-red-500 hover:text-red-700 font-medium text-xs"
                                        >
                                            Retry
                                        </button>
                                    )}
                                </div>
                            </td>
                        </tr>
                    )}

                    {isEmpty && (
                        <tr className="bg-white">
                            <td colSpan={columns.length} className="px-5 py-16 text-center text-sm text-gray-400">
                                {emptyMessage}
                            </td>
                        </tr>
                    )}

                    {!isLoading && !error && data.map((row, i) => {
                        const isOdd = striped && i % 2 === 1;
                        const extraRowClass =
                            typeof rowClassName === "function" ? rowClassName(row, i) : rowClassName;
                        return (
                            <tr
                                key={rowKey(row, i)}
                                onClick={onRowClick ? () => onRowClick(row, i) : undefined}
                                className={cn(
                                    "border-b border-gray-200 last:border-b-0",
                                    isOdd ? "bg-[#DFE6ED]" : "bg-white",
                                    onRowClick && "cursor-pointer",
                                    extraRowClass,
                                )}
                            >
                                {columns.map((col) => (
                                    <td
                                        key={col.key}
                                        className={cn(
                                            "px-5 py-3.5 text-sm text-gray-800",
                                            alignClass[col.align ?? "left"],
                                            col.hideBelow && hideBelowClass[col.hideBelow],
                                            col.className,
                                        )}
                                    >
                                        {col.render(row, i)}
                                    </td>
                                ))}
                            </tr>
                        );
                    })}
                </tbody>
                {!isLoading && !error && footer && <tfoot>{footer}</tfoot>}
            </table>
        </div>
    );
}

// ─── Pagination ─────────────────────────────────────────────────────────────
interface TablePaginationProps {
    page: number;
    totalPages: number;
    total: number;
    limit: number;
    onPageChange: (page: number) => void;
    className?: string;
}

export function TablePagination({
    page, totalPages, total, limit, onPageChange, className,
}: TablePaginationProps) {
    const from = total === 0 ? 0 : (page - 1) * limit + 1;
    const to = Math.min(page * limit, total);

    return (
        <div className={cn("flex items-center justify-between mt-4 gap-3 flex-wrap", className)}>
            <p id="lbl_PaginationInfo" className="text-sm text-gray-500">
                Menampilkan {from} sampai {to} dari {total} baris
            </p>

            <div className="flex items-center gap-1">
                <button
                    id="btn_PrevPage"
                    onClick={() => onPageChange(page - 1)}
                    disabled={page <= 1}
                    className="w-8 h-8 flex items-center justify-center rounded-md bg-white border border-gray-300 text-gray-500 hover:bg-gray-50 disabled:opacity-30 disabled:cursor-not-allowed transition"
                >
                    <ChevronLeft className="w-4 h-4" />
                </button>

                {Array.from({ length: totalPages }, (_, i) => i + 1).map((p) => (
                    <button
                        key={p}
                        // Numbered per page so a case can jump to a known page;
                        // `btn_ActivePage` additionally marks the current one.
                        id={page === p ? "btn_ActivePage" : `btn_Page${p}`}
                        onClick={() => onPageChange(p)}
                        className={cn(
                            "w-8 h-8 flex items-center justify-center rounded-md text-sm font-medium border transition",
                            page === p
                                ? "bg-indigo-900 text-white border-indigo-900"
                                : "bg-white text-gray-600 border-gray-300 hover:bg-gray-50",
                        )}
                    >
                        {p}
                    </button>
                ))}

                <button
                    id="btn_NextPage"
                    onClick={() => onPageChange(page + 1)}
                    disabled={page >= totalPages}
                    className="w-8 h-8 flex items-center justify-center rounded-md bg-white border border-gray-300 text-gray-500 hover:bg-gray-50 disabled:opacity-30 disabled:cursor-not-allowed transition"
                >
                    <ChevronRight className="w-4 h-4" />
                </button>
            </div>
        </div>
    );
}

export default DataTable;
