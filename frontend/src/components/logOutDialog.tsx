import { AlertTriangle } from "lucide-react";

export function LogoutDialog({
  open,
  isLoading,
  onConfirm,
  onCancel,
}: {
  open: boolean;
  isLoading: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}) {
  if (!open) return null;
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-sm">
      <div className="bg-white rounded-2xl shadow-xl w-full max-w-sm mx-4 p-6">
        <div className="flex justify-center mb-4">
          <div className="w-14 h-14 bg-red-50 rounded-full flex items-center justify-center">
            <AlertTriangle className="w-7 h-7 text-red-500" />
          </div>
        </div>
        <h3 className="text-center text-lg font-semibold text-gray-800 mb-1">
          Sign Out
        </h3>
        <p className="text-center text-sm text-gray-400 mb-6">
          Are you sure you want to sign out?
        </p>
        <div className="flex gap-3">
          <button
            onClick={onCancel}
            disabled={isLoading}
            className="flex-1 py-2.5 rounded-xl border border-gray-200 text-sm font-medium text-gray-600 hover:bg-gray-50 transition disabled:opacity-50"
          >
            Cancel
          </button>
          <button
            id="btn_ConfirmSignOut"
            onClick={onConfirm}
            disabled={isLoading}
            className="flex-1 py-2.5 rounded-xl bg-red-500 hover:bg-red-600 text-sm font-medium text-white transition disabled:opacity-50 flex items-center justify-center gap-2"
          >
            {isLoading ? (
              <>
                <span className="w-4 h-4 border-2 border-white/40 border-t-white rounded-full animate-spin" />
                Signing out...
              </>
            ) : (
              "Sign Out"
            )}
          </button>
        </div>
      </div>
    </div>
  );
}