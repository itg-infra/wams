import type { FinanceReportCostDetail } from "../types/financeReport.type";
import { CostRowFinanceReport } from "./costRows";
import { SectionCard } from "./helperForm";

interface CostDetailsSectionProps {
  costDetails: FinanceReportCostDetail[];
}


const COST_GRID_COLS =
  "grid-cols-[120px_180px_280px_140px_160px_160px_160px_160px_160px_180px_100px_150px_140px_120px_180px_180px]";

export default function CostDetailsSection({
  costDetails,
}: CostDetailsSectionProps) {
  return (
    <>
      <h1 className="text-md my-4 font-bold">Cost Details</h1>

      <SectionCard title="cost details">
        <div className="overflow-x-auto touch-pan-x pb-2">
          <div className="w-max min-w-full space-y-3">
            <div
              className={`grid ${COST_GRID_COLS} gap-3 text-sm font-semibold`}
            >
              <span>WO ID</span>
              <span>BL Number</span>
              <span>Vessel</span>
              <span>Product</span>
              <span>PIC</span>
              <span>RFBA</span>
              <span>Start Date</span>
              <span>End Date</span>
              <span>Total Price</span>
              <span>PPN</span>
              <span>Status</span>
              <span>% Total</span>
              <span>Total Price (PPN)</span>
              <span>PPH</span>
              <span>Type</span>
              <span>Total Price (PPH)</span>
            </div>

            {costDetails.length === 0 ? (
              <div className="py-6 text-center text-sm text-gray-400">
                No cost items loaded.
              </div>
            ) : (
              costDetails.map((row) => (
                <CostRowFinanceReport key={row.purchaseOrderItemId} row={row} />
              ))
            )}
          </div>
        </div>
      </SectionCard>
    </>
  );
}
