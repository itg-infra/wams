import { type ReactNode } from "react";
import { Search, ArrowDownUp } from "lucide-react";
import { cn } from "../../lib/utils";
import { Button } from "./button";
import { SortDropdown, type SortOption } from "./sort-dropdown";

export interface ToolbarProps {
  /**
   * Controlled search value. Omit to leave the input uncontrolled (some screens
   * only wire `onSearchChange` and let the DOM own the value).
   */
  search?: string;
  /** Called with the new search text on every keystroke. */
  onSearchChange?: (value: string) => void;
  /** Placeholder for the search input. */
  searchPlaceholder?: string;
  /** DOM id for the search input (kept for existing test/automation hooks). */
  searchId?: string;
  /** Hide the search box entirely (some list pages have none). */
  hideSearch?: boolean;

  /** Show the "Sort By" button. Defaults to true. */
  showSort?: boolean;
  /** Click handler for the "Sort By" button. */
  onSort?: () => void;
  /** DOM id for the sort button. */
  sortId?: string;
  /**
   * Options for the sort menu. Pass these together with `onSortChange` and the
   * toolbar renders the real {@link SortDropdown} — the one that opens a menu
   * with id `lsb_SortOptions` and an option per entry. Without them the button
   * is a plain control that only fires `onSort`, which is what the screens that
   * have not wired sorting yet still use.
   */
  sortOptions?: SortOption[];
  /** Called with the chosen sort value. Required for the menu to render. */
  onSortChange?: (value: string) => void;
  /** Current sort label shown on the button. */
  sortValue?: string;
  /** DOM id for the sort menu. Defaults to `lsb_SortOptions`. */
  sortMenuId?: string;

  /** Show the "Order By" button. Defaults to false. */
  showOrder?: boolean;
  /** Click handler for the "Order By" button. */
  onOrder?: () => void;
  /** DOM id for the order button. */
  orderId?: string;
  /** Options for the order menu. */
  orderOptions?: SortOption[];
  /** Called with the chosen order value. Required for the menu to render. */
  onOrderChange?: (value: string) => void;
  /** Current order label shown on the button. */
  orderValue?: string;
  /** DOM id for the order menu. Defaults to `lsb_OrderOptions`. */
  orderMenuId?: string;

  /** Extra controls rendered on the left, right after search/sort (filters, tabs, dropdowns). */
  filters?: ReactNode;
  /** Right-aligned actions (Add/Create/Export buttons, PermissionGuard-wrapped). */
  actions?: ReactNode;

  className?: string;
}

/**
 * Shared list-page toolbar: search input + optional "Sort By" button + optional
 * left-side filters + right-aligned actions. Extracted so every list screen keeps
 * a consistent look and search behaviour.
 */
export function Toolbar({
  search,
  onSearchChange,
  searchPlaceholder = "Search",
  searchId = "txt_Search",
  hideSearch = false,
  showSort = true,
  onSort,
  sortId = "btn_SortBy",
  sortOptions,
  onSortChange,
  sortValue,
  sortMenuId = "lsb_SortOptions",
  showOrder = false,
  onOrder,
  orderId = "btn_OrderBy",
  orderOptions,
  onOrderChange,
  orderValue,
  orderMenuId = "lsb_OrderOptions",
  filters,
  actions,
  className,
}: ToolbarProps) {
  return (
    <div className={cn("flex items-center gap-3 mb-4 flex-wrap", className)}>
      {!hideSearch && (
        <div className="flex items-center gap-2 border border-gray-300 rounded-lg px-3 py-2 bg-white min-w-35 flex-1 max-w-xs">
          <input
            id={searchId}
            type="text"
            {...(search !== undefined ? { value: search } : {})}
            onChange={(e) => onSearchChange?.(e.target.value)}
            placeholder={searchPlaceholder}
            className="text-sm text-gray-700 placeholder-gray-400 outline-none bg-transparent flex-1 min-w-0"
          />
          <Search className="w-4 h-4 text-gray-400 shrink-0" />
        </div>
      )}

      {showSort &&
        (sortOptions && onSortChange ? (
          <SortDropdown
            options={sortOptions}
            onChange={onSortChange}
            value={sortValue}
            id={sortId}
            menuId={sortMenuId}
          />
        ) : (
          <Button id={sortId} variant="outline" onClick={onSort}>
            Sort By
            <ArrowDownUp className="w-4 h-4 text-gray-400" />
          </Button>
        ))}

      {showOrder &&
        (orderOptions && onOrderChange ? (
          <SortDropdown
            options={orderOptions}
            onChange={onOrderChange}
            value={orderValue}
            id={orderId}
            menuId={orderMenuId}
          />
        ) : (
          <Button id={orderId} variant="outline" onClick={onOrder}>
            Order By
            <ArrowDownUp className="w-4 h-4 text-gray-400" />
          </Button>
        ))}

      {filters}

      <div className="flex-1" />

      {actions}
    </div>
  );
}

export default Toolbar;
