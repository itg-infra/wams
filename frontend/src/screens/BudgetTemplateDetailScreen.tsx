import { useBudgetTemplateDetailController } from "../controllers/budgeting/budgetTemplateDetailController";
import type { BudgetTemplateStatus } from "../types/budgetTemplate.type";
import { useNavigate, useParams } from "react-router-dom";
import { PageHeader } from "../components/ui/page-header";

const STATUS_STYLES: Record<BudgetTemplateStatus, string> = {
  Submitted: "bg-[#8BE6A2] text-[#226A39] border border-[#5CC97A]",
  Approved: "bg-[#8BE6A2] text-[#226A39] border border-[#5CC97A]",
  Draft: "bg-[#E6E6E6] text-[#6B6B6B] border border-[#C8C8C8]",
  Rejected: "bg-[#E6E6E6] text-[#6B6B6B] border border-[#C8C8C8]",
};

function StatusBadge({ status }: { status: BudgetTemplateStatus }) {
  return (
    <span
      className={`inline-flex items-center justify-center min-w-25 px-4 py-1 rounded-xl text-[13px] font-medium leading-none ${STATUS_STYLES[status]}`}
    >
      {status}
    </span>
  );
}

function ReadOnlyField({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex flex-col gap-2">
      <label className="text-[16px] font-semibold text-black">{label}</label>
      <div className="h-12 md:h-13 rounded-xl border border-[#D8DCE5] bg-[#F7F8FA] px-4 flex items-center text-[16px] text-[#222222]">
        {value || "-"}
      </div>
    </div>
  );
}

function DetailSkeleton() {
  return (
    <div className="space-y-6 animate-pulse">
      <div className="h-8 w-72 bg-gray-200 rounded-lg" />
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        {Array.from({ length: 4 }).map((_, index) => (
          <div key={index} className="space-y-2">
            <div className="h-5 w-32 bg-gray-200 rounded" />
            <div className="h-12 bg-gray-100 rounded-xl" />
          </div>
        ))}
      </div>
      <div className="h-64 bg-gray-100 rounded-2xl" />
    </div>
  );
}

export default function BudgetTemplateDetailScreen() {
  const { id } = useParams();
  const { detail, isLoading, error } = useBudgetTemplateDetailController(id!);
  const navigate = useNavigate();

  return (
    <div className="flex-1 p-4 sm:p-6 lg:p-8 overflow-y-auto">
      <div className="max-w-7xl">
        <PageHeader
          breadcrumbs={[
            { label: "Dashboard" },
            { label: "Budgeting" },
            { label: "Budget Template", onClick: () => navigate(-1) },
            { label: "Budget Template Detail" },
          ]}
          title="Detail Budget Template"
          onBack={() => navigate(-1)}
        />

        {error && (
          <div className="mb-5 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-500">
            {error}
          </div>
        )}

        {isLoading ? (
          <DetailSkeleton />
        ) : detail ? (
          <div className="space-y-7">
            {/* Top Form */}
            <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-5 md:gap-6">
              <ReadOnlyField label="Template ID" value={detail.templateId} />
              {/* <ReadOnlyField
                label="Template Name"
                value={detail.templateName}
              /> */}
              <ReadOnlyField label="Location" value={detail.provinceDisplay} />

              <div className="flex flex-col gap-2">
                <label className="text-[16px] font-semibold text-black">
                  Status
                </label>
                <div className="h-12 md:h-13 rounded-xl border border-[#D8DCE5] bg-[#F7F8FA] px-4 flex items-center">
                  <StatusBadge status={detail.status} />
                </div>
              </div>
            </div>

            {/* Items Table */}
            <div className="rounded-2xl bg-[#DDE3EB] p-4 md:p-6">
              <div className="overflow-x-auto">
                <table className="min-w-225 w-full border-separate border-spacing-y-3">
                  <thead>
                    <tr>
                      <th className="text-left text-[15px] font-semibold text-black px-2">
                        Cost Detail
                      </th>
                      <th className="text-left text-[15px] font-semibold text-black px-2">
                        Cost Name
                      </th>
                      <th className="text-left text-[15px] font-semibold text-black px-2">
                        COA
                      </th>
                      <th className="text-left text-[15px] font-semibold text-black px-2">
                        COA Name
                      </th>
                      <th className="text-left text-[15px] font-semibold text-black px-2">
                        Activity Name
                      </th>
                    </tr>
                  </thead>
                  <tbody>
                    {detail.items.map((item) => (
                      <tr key={item.id}>
                        <td className="px-2">
                          <div className="h-12 rounded-xl border border-[#D8DCE5] bg-white px-4 flex items-center text-[15px] text-[#222222]">
                            {item.costDetail}
                          </div>
                        </td>
                        <td className="px-2">
                          <div className="h-12 rounded-xl border border-[#D8DCE5] bg-white px-4 flex items-center text-[15px] text-[#222222]">
                            {item.costName}
                          </div>
                        </td>
                        <td className="px-2">
                          <div className="h-12 rounded-xl border border-[#D8DCE5] bg-white px-4 flex items-center text-[15px] text-[#222222]">
                            {item.coa}
                          </div>
                        </td>
                        <td className="px-2">
                          <div className="h-12 rounded-xl border border-[#D8DCE5] bg-white px-4 flex items-center text-[15px] text-[#222222]">
                            {item.coaName}
                          </div>
                        </td>
                        <td className="px-2">
                          <div className="h-12 rounded-xl border border-[#D8DCE5] bg-white px-4 flex items-center text-[15px] text-[#222222]">
                            {item.activityTypeName}
                          </div>
                        </td>
                      </tr>
                    ))}

                    {detail.items.length === 0 && (
                      <tr>
                        <td
                          colSpan={4}
                          className="text-center text-sm text-[#7A7A7A] py-10"
                        >
                          No cost items available
                        </td>
                      </tr>
                    )}
                  </tbody>
                </table>
              </div>
            </div>
          </div>
        ) : (
          <div className="rounded-2xl border border-[#D7DEE8] bg-white px-6 py-10 text-center text-[#7A7A7A]">
            Data detail tidak ditemukan
          </div>
        )}
      </div>
    </div>
  );
}
