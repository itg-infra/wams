"use client";

import { useEffect } from "react";
import { useRateCardStore } from "../master_data/store/rateCardStore";
import { formatDate } from "../components/format/dateTimeFormat";
import { useParams, useNavigate } from "react-router-dom";
import { PageHeader } from "../components/ui/page-header";

function formatCurrency(value: number): string {
  return new Intl.NumberFormat("id-ID").format(value);
}

function StatusBadge({ status }: { status: string }) {
  const styles: Record<string, string> = {
    Draft: "bg-yellow-100 text-yellow-700 border border-yellow-200",
    Submitted: "bg-blue-100 text-blue-700 border border-blue-200",
    Approved: "bg-green-100 text-green-700 border border-green-200",
    Rejected: "bg-red-100 text-red-700 border border-red-200",
  };
  const cls =
    styles[status] ?? "bg-gray-100 text-gray-600 border border-gray-200";
  return (
    <span
      className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${cls}`}
    >
      {status}
    </span>
  );
}

export default function DetailPriceList() {
  const { fetchDetail, selectedRateCard, isDetailLoading, detailError } =
    useRateCardStore();

  const { id } = useParams();
  const navigate = useNavigate();

  useEffect(() => {
    if (!id) return;

    fetchDetail(id);
  }, [id, fetchDetail]);

  return (
    <div className="flex-1 p-4 sm:p-6 lg:p-8 overflow-y-auto">
      <div className="max-w mx-auto">
        <PageHeader
          breadcrumbs={[
            { label: "Dashboard" },
            { label: "Master Data" },
            { label: "Master Rate Card" },
            { label: "Detail Price List" },
          ]}
          title="Detail Price List"
          onBack={() => navigate(-1)}
        />

        {/* Loading */}
        {isDetailLoading && (
          <p className="text-sm text-gray-400 py-10 text-center">
            Memuat data...
          </p>
        )}

        {/* Error */}
        {detailError && !isDetailLoading && (
          <p className="text-sm text-red-500 py-10 text-center">
            {detailError}
          </p>
        )}

        {/* Content */}
        {!isDetailLoading && !detailError && selectedRateCard && (
          <>
            {/* Info Cards */}
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 sm:gap-6 mb-5">
              <div>
                <label className="block text-sm font-medium text-gray-600 mb-1">
                  Vendor Code
                </label>
                <div className="h-10 px-3 flex items-center rounded-md border border-gray-300 bg-white text-sm text-gray-800 overflow-hidden">
                  {selectedRateCard.vendor.cardCode}
                </div>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-600 mb-1">
                  Vendor Name
                </label>
                <div className="h-10 px-3 flex items-center rounded-md border border-gray-300 bg-white text-sm text-gray-800">
                  {selectedRateCard.vendor.cardName}
                </div>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-600 mb-1">
                  Status
                </label>
                <div className="h-10 px-3 flex items-center rounded-md border border-gray-300 bg-white">
                  <StatusBadge status={selectedRateCard.status} />
                </div>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-600 mb-1">
                  Tanggal Dibuat
                </label>
                <div className="h-10 px-3 flex items-center rounded-md border border-gray-300 bg-white text-sm text-gray-800">
                  {formatDate(selectedRateCard.createdAt)}
                </div>
              </div>
              {selectedRateCard.submittedAt && (
                <div>
                  <label className="block text-sm font-medium text-gray-600 mb-1">
                    Tanggal Submit
                  </label>
                  <div className="h-10 px-3 flex items-center rounded-md border border-gray-300 bg-white text-sm text-gray-800">
                    {formatDate(selectedRateCard.submittedAt)}
                  </div>
                </div>
              )}
            </div>

            {/* Items Table */}
            <div className="bg-[#D6DEE7] rounded-xl p-3 sm:p-4">
              <div
                className="grid gap-3 mb-2 text-xs sm:text-sm font-semibold text-gray-800"
                style={{ gridTemplateColumns: "1fr 1fr 1fr 1fr 1fr 1fr" }}
              >
                <span>Cost ID</span>
                <span>Cost Name</span>
                <span>UoM</span>
                <span>Cost Value</span>
                <span>Pph</span>
                <span>Ppn</span>
              </div>

              <div className="space-y-2">
                {selectedRateCard.items.map((line) => (
                  <div
                    key={line.id}
                    className="grid gap-3 items-center"
                    style={{ gridTemplateColumns: "1fr 1fr  1fr 1fr 1fr 1fr" }}
                  >
                    <div className="h-9 px-2 rounded border border-gray-300 bg-white text-sm text-gray-800 flex items-center">
                      {line.item.itemCode}
                    </div>
                    <div className="h-9 px-2 rounded border border-gray-300 bg-white text-sm text-gray-800 flex items-center">
                      {line.item.itemName}
                    </div>
                    <div className="h-9 px-2 rounded border border-gray-300 bg-white text-sm text-gray-800 flex items-center">
                      {line.uom.code}
                    </div>
                    <div className="h-9 px-2 rounded border border-gray-300 bg-white text-sm text-gray-800 flex items-center whitespace-nowrap">
                      {formatCurrency(line.costValue)}
                    </div>
                    <div className="h-9 px-2 rounded border border-gray-300 bg-white text-sm text-gray-800 flex items-center">
                      {line.pphTaxType
                        ? `${line.pphTaxType.code} - ${line.pphTaxType.rate}%`
                        : "-"}
                    </div>
                    <div className="h-9 px-2 rounded border border-gray-300 bg-white text-sm text-gray-800 flex items-center">
                      {line.ppnTaxType
                        ? `${line.ppnTaxType.code} - ${line.ppnTaxType.rate}%`
                        : "-"}
                    </div>
                  </div>
                ))}
              </div>
            </div>

            {/* Footer */}
          </>
        )}
      </div>
    </div>
  );
}
