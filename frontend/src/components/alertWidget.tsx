import { X, CheckCircle2, AlertCircle } from "lucide-react";

interface AlertWidgetProps {
  show: boolean;
  type: "success" | "error";
  title: string;
  message: string;
  onClose?: () => void;
}

export default function AlertWidget({
  show,
  type,
  title,
  message,
  onClose,
}: AlertWidgetProps) {
  return (
    <div
      className={`fixed top-5 right-5 z-9999 max-w-sm w-full transition-all duration-300 ease-out ${
        show
          ? "opacity-100 translate-y-0"
          : "opacity-0 -translate-y-4 pointer-events-none"
      }`}
    >
      <div
        className={`px-4 py-3 rounded-xl shadow-lg border ${
          type === "success"
            ? "bg-green-50 border-green-300 text-green-700"
            : "bg-red-50 border-red-300 text-red-700"
        }`}
        role="alert"
      >
        <div className="flex items-start justify-between gap-3">
          <div className="flex items-start gap-3">
            {type === "success" ? (
              <CheckCircle2 className="w-5 h-5 mt-0.5 shrink-0" />
            ) : (
              <AlertCircle className="w-5 h-5 mt-0.5 shrink-0" />
            )}

            <div>
              <p className="font-semibold">{title}</p>

              <p className="text-sm mt-1">{message}</p>
            </div>
          </div>

          {onClose && (
            <button onClick={onClose} className="hover:opacity-70 transition">
              <X className="w-4 h-4" />
            </button>
          )}
        </div>
      </div>
    </div>
  );
}
