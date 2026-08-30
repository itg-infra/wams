import { type ReactNode } from "react";
import { ChevronRight, ArrowLeft } from "lucide-react";
import { cn } from "../../lib/utils";

export interface Crumb {
    label: ReactNode;
    onClick?: () => void;
    /** DOM id for a clickable crumb. Defaults to `lnk_Breadcrumb<n>`. */
    id?: string;
}

interface PageHeaderProps {
    breadcrumbs?: Crumb[];
    title: ReactNode;
    subtitle?: ReactNode;
    onBack?: () => void;
    actions?: ReactNode;
    className?: string;
    /**
     * DOM id for the back button. Defaults to `btn_Back` so every screen that
     * passes `onBack` is addressable by QA automation without opting in.
     */
    backId?: string;
}

const pad = (n: number) => String(n).padStart(2, "0");

export function lastUpdatedLabel(date: Date = new Date()): string {
    return `Last updated on ${pad(date.getDate())}/${pad(date.getMonth() + 1)}/${date.getFullYear()}, ${pad(date.getHours())}:${pad(date.getMinutes())} WIB`;
}
export function PageHeader({
    breadcrumbs,
    title,
    subtitle,
    onBack,
    actions,
    className,
    backId = "btn_Back",
}: PageHeaderProps) {
    return (
        <div className={cn("mb-5", className)}>
            {breadcrumbs && breadcrumbs.length > 0 && (
                <nav className="flex items-center gap-1.5 text-sm text-gray-400 mb-3 flex-wrap">
                    {breadcrumbs.map((crumb, i) => {
                        const isLast = i === breadcrumbs.length - 1;
                        return (
                            <span key={i} className="flex items-center gap-1.5">
                                <span
                                    // Only clickable crumbs get an id: a plain text
                                    // crumb is not something automation can act on,
                                    // and an id on it would imply otherwise.
                                    id={crumb.onClick ? (crumb.id ?? `lnk_Breadcrumb${i + 1}`) : undefined}
                                    onClick={crumb.onClick}
                                    className={cn(
                                        isLast
                                            ? "text-gray-700 font-medium"
                                            : "hover:text-gray-600 transition",
                                        crumb.onClick && "cursor-pointer",
                                    )}
                                >
                                    {crumb.label}
                                </span>
                                {!isLast && <ChevronRight className="w-3.5 h-3.5 shrink-0" />}
                            </span>
                        );
                    })}
                </nav>
            )}

            <div className="flex items-start justify-between gap-3 flex-wrap">
                <div className="flex items-center gap-2.5 min-w-0">
                    {onBack && (
                        <button
                            id={backId}
                            type="button"
                            onClick={onBack}
                            aria-label="Back"
                            className="text-[#2E277C] hover:opacity-70 transition shrink-0"
                        >
                            <ArrowLeft className="w-5 h-5" />
                        </button>
                    )}
                    <div className="min-w-0">
                        <h1 className="text-xl font-bold text-gray-900 mb-0.5">{title}</h1>
                        {subtitle && <p className="text-sm text-gray-400">{subtitle}</p>}
                    </div>
                </div>
                {actions && <div className="shrink-0">{actions}</div>}
            </div>
        </div>
    );
}

export default PageHeader;
