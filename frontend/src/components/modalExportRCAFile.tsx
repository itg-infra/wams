import type { ExportRCAParams } from "../api/services/file/exportService";

interface ExportModalProps {
  open: boolean;
  title: string;
  loading?: boolean;

  params: ExportRCAParams;
  setParams: React.Dispatch<React.SetStateAction<ExportRCAParams>>;

  onClose: () => void;
  onSubmit: () => Promise<void> | void;

  showStatus?: boolean;
  showSearch?: boolean;
  showSort?: boolean;

  statusOptions?: string[];
}

export default function ExportModalRCA({
  open,
  title,
  loading = false,
  params,
  setParams,
  onClose,
  onSubmit,
  showSearch = true,
}: ExportModalProps) {
  if (!open) return null;

  return (
    <div id="lbl_ExportDialog" className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
      <div className="w-full max-w-2xl rounded-xl bg-white shadow-xl">
        {/* Header */}
        <div className="border-b px-6 py-4">
          <h2 className="text-lg font-semibold text-gray-800">{title}</h2>
        </div>

        {/* Body */}
        <div className="grid grid-cols-1 gap-4 p-6 md:grid-cols-2">
          {/* Format */}
          {showSearch && (
            <div className="md:col-span-2">
              <label className="mb-1 block text-sm font-medium text-gray-700">
                Warehouse Code
              </label>

              <input
                id="txt_WarehouseCode"
                type="text"
                placeholder="Warehouse Code..."
                value={params.warehouseCode ?? ""}
                onChange={(e) =>
                  setParams((prev) => ({
                    ...prev,
                    warehouseCode: e.target.value || undefined, 
                  }))
                }
                className="w-full rounded-lg border border-gray-300 px-3 py-2"
              />
            </div>
          )}

          {/* Date From */}
          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700">
              Date From
            </label>

            <input
              id="dtp_DateFrom"
              type="date"
              value={params.dateFrom ?? ""}
              onChange={(e) =>
                setParams((prev) => ({
                  ...prev,
                  dateFrom: e.target.value || undefined,
                }))
              }
              className="w-full rounded-lg border border-gray-300 px-3 py-2"
            />
          </div>

          {/* Date To */}
          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700">
              Date To
            </label>

            <input
              id="dtp_DateTo"
              type="date"
              value={params.dateTo ?? ""}
              onChange={(e) =>
                setParams((prev) => ({
                  ...prev,
                  dateTo: e.target.value || undefined,
                }))
              }
              className="w-full rounded-lg border border-gray-300 px-3 py-2"
            />
          </div>
        </div>

        {/* Footer */}
        <div className="flex justify-end gap-3 border-t px-6 py-4">
          <button
            id="btn_CancelExport"
            onClick={onClose}
            className="rounded-lg border border-gray-300 px-4 py-2 text-gray-700 hover:bg-gray-50"
          >
            Cancel
          </button>

          <button
            id="btn_ConfirmExport"
            disabled={loading}
            onClick={onSubmit}
            className="rounded-lg bg-indigo-600 px-4 py-2 text-white hover:bg-indigo-700 disabled:opacity-50"
          >
            {loading ? "Exporting..." : "Export"}
          </button>
        </div>
      </div>
    </div>
  );
}
