import { Clock, FileText } from "lucide-react";

// ─── Today Schedule ───────────────────────────────────────────────────────────
export function TodaySchedule({
  schedules,
}: {
  schedules: { id: string; label: string; time: string; type: string }[];
}) {
  return (
    <div className="bg-white rounded-xl border border-gray-100 shadow-sm p-5 flex flex-col gap-4">
      <div className="flex items-center justify-between">
        <span className="text-sm font-bold text-gray-800">Today</span>
        <span className="text-sm text-gray-400">9 February 2026</span>
      </div>
      <div className="flex flex-col gap-4">
        {schedules.map((s) => (
          <div key={s.id} className="flex items-center gap-3">
            {s.type === "shipping" ? (
              <FileText className="w-4 h-4 text-indigo-400 shrink-0" />
            ) : (
              <Clock className="w-4 h-4 text-gray-400 shrink-0" />
            )}
            <span className="flex-1 text-sm text-gray-700">{s.label}</span>
            <span className="text-sm text-gray-500 font-medium whitespace-nowrap">
              {s.time}
            </span>
          </div>
        ))}
      </div>
    </div>
  );
}
