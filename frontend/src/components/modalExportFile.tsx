import type { ExportParams } from "../api/services/file/exportService";

interface ExportModalProps {
  open: boolean;
  title: string;
  loading?: boolean;

  params: ExportParams;
  setParams: React.Dispatch<React.SetStateAction<ExportParams>>;

  onClose: () => void;
  onSubmit: () => Promise<void> | void;

  showStatus?: boolean;
  showSearch?: boolean;
  showSort?: boolean;

  statusOptions?: string[];
}

export default function ExportModal({
  open,
  title,
  loading = false,
  params,
  setParams,
  onClose,
  onSubmit,
  showStatus = true,
  showSearch = true,
  showSort = true,
  statusOptions = ["Draft", "Submitted", "Approved"],
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
          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700">
              Format *
            </label>

            <select
              id="cmb_ExportFormat"
              value={params.format}
              onChange={(e) =>
                setParams((prev) => ({
                  ...prev,
                  format: e.target.value as "Pdf" | "Xlsx" | "Csv",
                }))
              }
              className="w-full rounded-lg border border-gray-300 px-3 py-2"
            >
              <option value="Pdf">PDF</option>
              <option value="Xlsx">Excel</option>
              <option value="Csv">CSV</option>
            </select>
          </div>

          {/* Status */}
          {showStatus && (
            <div>
              <label className="mb-1 block text-sm font-medium text-gray-700">
                Status
              </label>

              <select
                value={params.status ?? ""}
                onChange={(e) =>
                  setParams((prev) => ({
                    ...prev,
                    status:
                      (e.target.value as ExportParams["status"]) || undefined,
                  }))
                }
                className="w-full rounded-lg border border-gray-300 px-3 py-2"
              >
                <option value="">All</option>

                {statusOptions.map((status) => (
                  <option key={status} value={status}>
                    {status}
                  </option>
                ))}
              </select>
            </div>
          )}

          {/* Date From */}
          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700">
              Date From
            </label>

            <input
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

          {/* Search */}
          {showSearch && (
            <div className="md:col-span-2">
              <label className="mb-1 block text-sm font-medium text-gray-700">
                Search
              </label>

              <input
                type="text"
                placeholder="Search..."
                value={params.search ?? ""}
                onChange={(e) =>
                  setParams((prev) => ({
                    ...prev,
                    search: e.target.value || undefined,
                  }))
                }
                className="w-full rounded-lg border border-gray-300 px-3 py-2"
              />
            </div>
          )}

          {/* Sort */}
          {showSort && (
            <>
              <div>
                <label className="mb-1 block text-sm font-medium text-gray-700">
                  Sort By
                </label>

                <input
                  type="text"
                  placeholder="e.g code"
                  value={params.sortBy ?? ""}
                  onChange={(e) =>
                    setParams((prev) => ({
                      ...prev,
                      sortBy: e.target.value || undefined,
                    }))
                  }
                  className="w-full rounded-lg border border-gray-300 px-3 py-2"
                />
              </div>

              <div>
                <label className="mb-1 block text-sm font-medium text-gray-700">
                  Sort Order
                </label>

                <select
                  id="cmb_ExportSortOrder"
                  value={params.sortOrder ?? "asc"}
                  onChange={(e) =>
                    setParams((prev) => ({
                      ...prev,
                      sortOrder: e.target.value as "asc" | "desc",
                    }))
                  }
                  className="w-full rounded-lg border border-gray-300 px-3 py-2"
                >
                  <option value="asc">Ascending</option>
                  <option value="desc">Descending</option>
                </select>
              </div>
            </>
          )}
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
