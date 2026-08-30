import type { FinanceReportBudgetRecap } from "../types/financeReport.type";
import { formatNumber } from "./format/formatCurrency";
import { Input, SectionCard } from "./helperForm";

interface BudgetRecapSectionProps {
  budgetRecap?: FinanceReportBudgetRecap;
}

export default function BudgetRecapSection({
  budgetRecap,
}: BudgetRecapSectionProps) {
  return (
    <>
      <h1 className="text-md my-4 font-bold">Budget Recap</h1>

      <SectionCard title="Base Document">
        <div className="overflow-x-auto overflow-y-visible pb-2">
          <div className="min-w-250 space-y-3">
            <div className="grid grid-cols-1 gap-3 text-sm font-semibold md:grid-cols-3">
              <span>Budget Plan</span>
              <span>Budget Realization</span>
              <span>Budget Variance</span>
            </div>

            <div className="grid grid-cols-1 gap-3 md:grid-cols-3">
              <Input
                value={formatNumber(budgetRecap?.budgetPlan ?? 0)}
                isReadonly
              />

              <Input
                value={formatNumber(budgetRecap?.budgetRealization ?? 0)}
                isReadonly
              />

              <Input
                value={formatNumber(budgetRecap?.budgetVariance ?? 0)}
                isReadonly
              />
            </div>
          </div>
        </div>
      </SectionCard>
    </>
  );
}
