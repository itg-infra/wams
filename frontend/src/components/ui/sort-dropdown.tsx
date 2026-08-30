import { useEffect, useRef, useState } from "react";
import { ArrowDownUp } from "lucide-react";
import { cn } from "../../lib/utils";
import { Button } from "./button";

export interface SortOption<T extends string = string> {
    label: string;
    value: T;
}

export interface SortDropdownProps<T extends string = string> {
    /** Options shown in the menu. */
    options: SortOption<T>[];
    /** Called with the selected option value. */
    onChange: (value: T) => void;
    /**
     * Current selection label to show on the button. When omitted the button
     * shows `placeholder` (menu-style sort, e.g. "sort by this column").
     */
    value?: string;
    placeholder?: string;
    /** Button DOM id (kept for QA automation). Defaults to `btn_SortBy`. */
    id?: string;
    /** Menu DOM id. Defaults to `lsb_SortOptions`. */
    menuId?: string;
    className?: string;
}

/**
 * Shared "Sort By" dropdown. Built on the same {@link Button} as the toolbar so
 * its height/typography line up with the search box. Every interactive node
 * carries a stable id (`btn_SortBy`, `lsb_SortOptions`, `btn_SortOption_<value>`)
 * so QA automation can target it.
 */
export function SortDropdown<T extends string = string>({
    options,
    onChange,
    value,
    placeholder = "Sort By",
    id = "btn_SortBy",
    menuId = "lsb_SortOptions",
    className,
}: SortDropdownProps<T>) {
    const [open, setOpen] = useState(false);
    const ref = useRef<HTMLDivElement>(null);

    useEffect(() => {
        if (!open) return;
        const onClickOutside = (e: MouseEvent) => {
            if (ref.current && !ref.current.contains(e.target as Node)) {
                setOpen(false);
            }
        };
        document.addEventListener("mousedown", onClickOutside);
        return () => document.removeEventListener("mousedown", onClickOutside);
    }, [open]);

    return (
        <div className={cn("relative", className)} ref={ref}>
            <Button
                id={id}
                type="button"
                variant="outline"
                onClick={() => setOpen((prev) => !prev)}
            >
                {value ?? placeholder}
                <ArrowDownUp className="w-4 h-4 text-gray-400" />
            </Button>

            {open && (
                <div
                    id={menuId}
                    className="absolute left-0 top-11 z-30 min-w-full w-44 bg-white border border-gray-200 rounded-lg shadow-lg overflow-hidden py-1"
                >
                    {options.map((option) => (
                        <button
                            type="button"
                            key={option.value}
                            id={`btn_SortOption_${option.value}`}
                            onClick={() => {
                                onChange(option.value);
                                setOpen(false);
                            }}
                            className="w-full px-4 py-2 text-left text-sm text-gray-700 hover:bg-gray-50 transition"
                        >
                            {option.label}
                        </button>
                    ))}
                </div>
            )}
        </div>
    );
}

export default SortDropdown;
