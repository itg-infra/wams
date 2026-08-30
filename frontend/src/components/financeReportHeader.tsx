import type { FinanceReportHeader } from "../types/financeReport.type";
import { formatDate } from "./format/dateTimeFormat";

interface Props {
  header?: FinanceReportHeader;
}

const InputField = ({ label, value }: { label: string; value?: string }) => (
  <div className="flex flex-col gap-1">
    <label className="text-sm font-semibold text-gray-800">{label}</label>

    <input
      readOnly
      value={value ?? "-"}
      className="h-10 w-full rounded-md border border-gray-200 bg-white px-3 text-sm text-gray-700 outline-none"
    />
  </div>
);

export default function FinanceReportHeaderCard({ header }: Props) {
  const fields = [
    { label: "Budget No", value: header?.budgetNo },
    { label: "Template Id", value: header?.templateId },
    { label: "Status", value: header?.status },
    { label: "Remark", value: header?.remark },
    { label: "Template Name", value: header?.templateName },
    {
      label: "Document Date",
      value: header?.docDate ? formatDate(header.docDate) : "-",
    },
    { label: "Warehouse Code", value: header?.warehouseCode },
    { label: "Warehouse Name", value: header?.warehouseName },
    { label: "Location", value: header?.location },
  ];

  return (
    <div className="grid grid-cols-1 gap-x-5 gap-y-4 md:grid-cols-2 xl:grid-cols-3">
      {fields.map((field) => (
        <InputField key={field.label} label={field.label} value={field.value} />
      ))}
    </div>
  );
}
