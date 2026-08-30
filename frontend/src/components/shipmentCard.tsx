import { CalendarDays } from "lucide-react";
import type { Shipment, TemplateStatus } from "../types/dashboard.type";
import { useEffect, useRef, useState } from "react";

export function ShipmentCard({ shipment }: { shipment: Shipment }) {
  return (
    <div className="border border-gray-100 rounded-xl p-4 flex items-center gap-4">
      <div className="flex-1 min-w-0">
        <div className="flex items-center gap-2 mb-0.5">
          <span className="text-sm font-bold text-gray-800">
            {shipment.containerCode}
          </span>
          <span className="text-xs text-gray-400">[{shipment.shipName}]</span>
        </div>
        <div className="flex items-center justify-between text-xs text-gray-400">
          <span>📍 {shipment.location}</span>
          <span className="flex items-center gap-1">
            <CalendarDays
             className="w-3 h-3" />
            {shipment.date}
          </span>
        </div>
        <ShipmentProgressBar status={shipment.status} />
      </div>
      <div className="w-14 flex items-center justify-center shrink-0">
        <ShipmentImage status={shipment.status} />
      </div>
    </div>
  );
}



// ─── Shipment Progress Bar ────────────────────────────────────────────────────
const STEPS = ["Processing", "Delivery", "In Transit", "Delivered"] as const;

const STATUS_STEP_MAP: Record<Shipment["status"], number> = {
    processing: 0,
    delivery: 1,
    in_transit: 2,
    delivered: 3,
};

const STATUS_IMAGE_MAP: Record<Shipment["status"], string> = {
    processing: "/Group 2.png",
    delivery: "/_x32__1_.png",
    in_transit: "/Group.png",
    delivered: "/Frame 69.png",
};

function ShipmentImage({ status }: { status: Shipment["status"] }) {
    const [displayedStatus, setDisplayedStatus] = useState<Shipment["status"]>("processing");
    const imgRef = useRef<HTMLImageElement>(null);
    const isAnimating = useRef(false);
    const nextStatus = useRef<Shipment["status"] | null>(null);

    const runAnimation = (to: Shipment["status"]) => {
        const img = imgRef.current;
        if (!img || isAnimating.current) return;

        isAnimating.current = true;
        nextStatus.current = to;

        img.style.transition = "opacity 0.25s ease, transform 0.25s ease";
        img.style.opacity = "0";
        img.style.transform = "translateX(-18px)";

        const t1 = setTimeout(() => {
            setDisplayedStatus(nextStatus.current!);
            nextStatus.current = null;

            img.style.transition = "none";
            img.style.opacity = "0";
            img.style.transform = "translateX(18px)";

            requestAnimationFrame(() => {
                requestAnimationFrame(() => {
                    img.style.transition = "opacity 0.35s ease, transform 0.35s ease";
                    img.style.opacity = "1";
                    img.style.transform = "translateX(0)";
                    isAnimating.current = false;
                });
            });
        }, 270);

        return () => clearTimeout(t1);
    };

    useEffect(() => {
        const t = setTimeout(() => runAnimation(status), 400);
        return () => clearTimeout(t);
    }, []); // eslint-disable-line react-hooks/exhaustive-deps

    useEffect(() => {
        if (status === displayedStatus) return;
        runAnimation(status);
    }, [status]); // eslint-disable-line react-hooks/exhaustive-deps

    return (
        <div className="w-14 h-14 flex items-center justify-center overflow-hidden">
            <img
                ref={imgRef}
                src={STATUS_IMAGE_MAP[displayedStatus]}
                alt={displayedStatus}
                className="w-14 h-14 object-contain"
            />
        </div>
    );
}





// ─── Status Badge ─────────────────────────────────────────────────────────────
const STATUS_STYLES: Record<TemplateStatus, string> = {
    "Approved": "bg-green-100 text-green-700",
    "Waiting Approval": "bg-yellow-100 text-yellow-700",
    "Draft": "bg-gray-100 text-gray-500",
    "Closed": "bg-gray-700 text-white",
    "Rejected": "bg-red-100 text-red-500",
};

// ─── Template Table ───────────────────────────────────────────────────────────
export function TemplateTable({ rows }: { rows: { id: string; templateName: string; warehouseCode: string; warehouseName: string; location: string; date: string; status: TemplateStatus }[] }) {
    const headers = ["Template ID", "Template Name", "Warehouse Code", "Warehouse Name", "Location", "Date", "Status"];
    return (
        <div className="bg-white rounded-xl border border-gray-100 shadow-sm overflow-hidden">
            <table className="w-full text-sm">
                <thead>
                    <tr className="border-b border-gray-100">
                        {headers.map((h) => (
                            <th key={h} className="text-left px-4 py-3 text-xs font-semibold text-gray-500 whitespace-nowrap">{h}</th>
                        ))}
                    </tr>
                </thead>
                <tbody>
                    {rows.map((row, i) => (
                        <tr key={row.id} className={`border-b border-gray-50 last:border-0 hover:bg-gray-50/50 transition ${i % 2 === 1 ? "bg-indigo-50/20" : ""}`}>
                            <td className="px-4 py-3 text-indigo-700 font-medium">T.0001</td>
                            <td className="px-4 py-3 text-gray-700">{row.templateName}</td>
                            <td className="px-4 py-3 text-gray-600">{row.warehouseCode}</td>
                            <td className="px-4 py-3 text-gray-600">{row.warehouseName}</td>
                            <td className="px-4 py-3 text-gray-600">{row.location}</td>
                            <td className="px-4 py-3 text-gray-600">{row.date}</td>
                            <td className="px-4 py-3">
                                <span className={`px-2.5 py-1 rounded-md text-xs font-medium ${STATUS_STYLES[row.status]}`}>
                                    {row.status}
                                </span>
                            </td>
                        </tr>
                    ))}
                </tbody>
            </table>
        </div>
    );
}

// ─── Skeleton ─────────────────────────────────────────────────────────────────


// ─── Dashboard Content ────────────────────────────────────────────────────────

function ShipmentProgressBar({ status }: { status: Shipment["status"] }) {
    const currentStep = STATUS_STEP_MAP[status];
    const targetWidth = currentStep === 0 ? 0 : (currentStep / (STEPS.length - 1)) * 100;
    const [animatedWidth, setAnimatedWidth] = useState(0);

    useEffect(() => {
        const t = setTimeout(() => setAnimatedWidth(targetWidth), 80);
        return () => clearTimeout(t);
    }, [targetWidth]);

    return (
        <div className="mt-3 mb-1">
            <div className="relative h-1.5 bg-gray-200 rounded-full">
                <div
                    className="absolute left-0 top-0 h-full bg-indigo-700 rounded-full"
                    style={{
                        width: `${animatedWidth}%`,
                        transition: "width 0.9s cubic-bezier(0.4, 0, 0.2, 1)",
                    }}
                />
                {STEPS.map((_, i) => {
                    const pct = i === 0 ? 0 : (i / (STEPS.length - 1)) * 100;
                    const filled = i <= currentStep;
                    return (
                        <div
                            key={i}
                            className={`absolute top-1/2 -translate-y-1/2 w-3 h-3 rounded-full border-2 transition-colors duration-700 ${filled ? "bg-indigo-700 border-indigo-700" : "bg-white border-gray-300"}`}
                            style={{ left: `calc(${pct}% - 6px)` }}
                        />
                    );
                })}
            </div>
            <div className="flex justify-between mt-2">
                {STEPS.map((step, i) => (
                    <span key={step} className={`text-[10px] ${i <= currentStep ? "text-indigo-700 font-medium" : "text-gray-400"}`}>
                        {step}
                    </span>
                ))}
            </div>
        </div>
    );
}
