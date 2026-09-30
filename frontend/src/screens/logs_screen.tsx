import { useCallback, useEffect, useState } from "react";
import { LoaderCircle, RefreshCw } from "lucide-react";
import axiosProvider from "../api/providers/axiosProvider";
import { PageHeader } from "../components/ui/page-header";
import { Button } from "../components/ui/button";

const LINE_COUNTS = [100, 250, 500, 1000];

export default function LogsScreen() {
  const [lineCount, setLineCount] = useState(100);
  const [logs, setLogs] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState(false);

  const loadLogs = useCallback(async () => {
    setIsLoading(true);
    setError(false);
    try {
      const response = await axiosProvider.get<string>("/api/v1/logs", {
        params: { lines: lineCount },
        responseType: "text",
      });
      setLogs(response.data);
    } catch {
      setError(true);
    } finally {
      setIsLoading(false);
    }
  }, [lineCount]);

  useEffect(() => {
    void loadLogs();
  }, [loadLogs]);

  return (
    <div className="flex-1 overflow-y-auto p-4 sm:p-6 lg:p-8">
      <PageHeader
        breadcrumbs={[{ label: "Dashboard" }, { label: "System" }, { label: "Logs" }]}
        title="System Logs"
        subtitle="Recent application log output"
      />

      <section className="overflow-hidden rounded-xl border border-gray-200 bg-white shadow-sm">
        <div className="flex flex-wrap items-center justify-between gap-3 border-b border-gray-200 px-4 py-3 sm:px-5">
          <div className="text-sm text-gray-500">
            Showing up to <span className="font-medium text-gray-800">{lineCount}</span> latest lines
          </div>
          <div className="flex items-center gap-2">
            <label htmlFor="sel_LogLineCount" className="text-sm text-gray-600">
              Lines
            </label>
            <select
              id="sel_LogLineCount"
              value={lineCount}
              onChange={(event) => setLineCount(Number(event.target.value))}
              className="h-9 rounded-lg border border-gray-300 bg-white px-3 text-sm text-gray-700 outline-none focus:border-[#2E277C] focus:ring-2 focus:ring-[#2E277C]/15"
            >
              {LINE_COUNTS.map((count) => (
                <option key={count} value={count}>{count}</option>
              ))}
            </select>
            <Button
              id="btn_RefreshLogs"
              variant="outline"
              onClick={() => void loadLogs()}
              disabled={isLoading}
            >
              {isLoading ? <LoaderCircle className="animate-spin" /> : <RefreshCw />}
              Refresh
            </Button>
          </div>
        </div>

        {error ? (
          <div role="alert" className="px-5 py-10 text-center text-sm text-red-600">
            Could not load logs. Please try again.
          </div>
        ) : isLoading && !logs ? (
          <div className="flex items-center justify-center gap-2 px-5 py-12 text-sm text-gray-500">
            <LoaderCircle className="h-4 w-4 animate-spin" /> Loading logs…
          </div>
        ) : logs ? (
          <pre className="max-h-[70vh] min-h-96 overflow-auto bg-gray-50 p-4 font-mono text-xs leading-5 text-gray-700 sm:p-5">
            {logs}
          </pre>
        ) : (
          <div className="px-5 py-12 text-center text-sm text-gray-500">
            No log lines available.
          </div>
        )}
      </section>
    </div>
  );
}
