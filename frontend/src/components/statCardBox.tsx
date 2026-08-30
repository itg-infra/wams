import { ArrowLeftRight, Clock, Package, Truck } from "lucide-react";
import type { StatCard } from "../types/dashboard.type";

export function StatCardBox({ stat }: { stat: StatCard }) {
  const STAT_ICONS = {
    shipments: Package,
    delivery: Truck,
    transit: ArrowLeftRight,
    pending: Clock,
  };

  const Icon = STAT_ICONS[stat.icon];

  const styles = {
    yellow: {
      border: "border-[#C79A2B]",
      value: "text-[#C79A2B]",
      growth: "text-[#C79A2B]",
    },
    green: {
      border: "border-[#188038]",
      value: "text-[#188038]",
      growth: "text-[#188038]",
    },
    red: {
      border: "border-[#D93025]",
      value: "text-[#D93025]",
      growth: "text-[#D93025]",
    },
  };

  const current = styles[stat.color ?? "green"];

  return (
    <div
      className={`
        bg-white
        rounded-md
        border-2
        ${current.border}
        px-4
        py-3
        min-h-27
        flex
        flex-col
        justify-between
      `}
    >
      <div className="flex items-center gap-2">
        <Icon className="w-4 h-4 shrink-0 text-gray-700" />

        <span className="text-xs sm:text-[13px] font-medium text-gray-700">
          {stat.label}
        </span>
      </div>

      <div className="flex items-center justify-between mt-2 gap-3">
        <div className="flex items-end shrink-0">
          <span
            className={`
              text-4xl
              sm:text-[48px]
              leading-none
              font-semibold
              ${current.value}
            `}
          >
            {stat.value}
          </span>

          {stat.label.toLowerCase().includes("budget") && (
            <span
              className={`
                ml-1
                mb-1
                text-sm
                sm:text-base
                font-semibold
                ${current.value}
              `}
            >
              %
            </span>
          )}
        </div>

        <div className="text-right flex-1">
          <p
            className={`
              text-[11px]
              sm:text-[13px]
              leading-4
              font-semibold
              wrap-break-word
              ${current.growth}
            `}
          >
            {stat.growth}
          </p>
        </div>
      </div>
    </div>
  );
}

export function Skeleton({ className }: { className?: string }) {
  return (
    <div className={`animate-pulse bg-gray-200 rounded-xl ${className}`} />
  );
}
