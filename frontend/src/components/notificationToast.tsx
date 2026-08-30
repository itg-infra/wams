
import { Bell, X } from "lucide-react";
import type { NotificationStreamResponse } from "../types/notificationStream";

interface NotificationToastProps {
  notification: NotificationStreamResponse;
  onDismiss: () => void;
}

export function NotificationToast({
  notification,
  onDismiss,
}: NotificationToastProps) {
  return (
    <div className="group w-90 max-w-[calc(100vw-32px)] overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-xl transition-all duration-300 hover:-translate-y-0.5 hover:shadow-2xl">
      <div className="flex items-start gap-4 p-4">
        {/* Icon */}
        <div className="flex h-12 w-12 shrink-0 items-center justify-center rounded-xl bg-linear-to-br from-blue-500 to-indigo-600 shadow-md">
          <Bell className="h-6 w-6 text-white" />
        </div>

        {/* Content */}
        <div className="min-w-0 flex-1">
          <div className="mb-1 flex items-center gap-2">
            <h3 className="truncate text-sm font-semibold text-slate-900">
              {notification.Title}
            </h3>

            <span className="rounded-full bg-blue-100 px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wide text-blue-700">
              New
            </span>
          </div>

          <p className="line-clamp-2 text-sm leading-5 text-slate-600">
            {notification.Message}
          </p>

          <div className="mt-3 flex items-center justify-between">
            <span className="text-xs font-medium text-slate-400">
              {new Date(notification.CreatedAt).toLocaleTimeString("id-ID", {
                hour: "2-digit",
                minute: "2-digit",
              })}
            </span>
          </div>
        </div>

        {/* Close */}
        <button
          onClick={onDismiss}
          className="rounded-lg p-1.5 text-slate-400 transition-colors duration-200 hover:bg-slate-100 hover:text-slate-700"
          aria-label="Close notification"
        >
          <X className="h-4 w-4" />
        </button>
      </div>

      {/* Bottom Accent */}
      <div className="h-1 w-full bg-linear-to-r from-blue-500 via-indigo-500 to-violet-500" />
    </div>
  );
}
