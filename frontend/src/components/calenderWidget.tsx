import { CalendarDays } from "lucide-react";

// ─── Calendar ─────────────────────────────────────────────────────────────────
const DAYS_OF_WEEK = ["Su", "Mo", "Tu", "We", "Th", "Fr", "Sa"];

export function CalendarWidget() {
  const year = 2026,
    month = 1,
    today = 9;
  const firstDay = new Date(year, month, 1).getDay();
  const daysInMonth = new Date(year, month + 1, 0).getDate();
  const cells: (number | null)[] = [
    ...Array(firstDay).fill(null),
    ...Array.from({ length: daysInMonth }, (_, i) => i + 1),
  ];

  return (
    <div className="bg-white rounded-xl border border-gray-100 shadow-sm p-5">
      <div className="flex items-center justify-between mb-4">
        <h3 className="text-base font-bold text-gray-800">February 2026</h3>
        <CalendarDays className="w-4 h-4 text-gray-400" />
      </div>
      <div className="grid grid-cols-7 gap-1 mb-1">
        {DAYS_OF_WEEK.map((d) => (
          <div
            key={d}
            className="text-center text-xs text-gray-400 font-medium py-1"
          >
            {d}
          </div>
        ))}
      </div>
      <div className="grid grid-cols-7 gap-1">
        {cells.map((day, i) => (
          <div
            key={i}
            className={`h-8 flex items-center justify-center rounded-full text-xs transition ${
              day === null
                ? ""
                : day === today
                  ? "bg-indigo-700 text-white font-bold"
                  : "text-gray-600 hover:bg-gray-100 cursor-pointer"
            }`}
          >
            {day}
          </div>
        ))}
      </div>
    </div>
  );
}
