interface InfoFieldProps {
  label: string;
  value?: string | number | null;
}

export default function InfoField({ label, value }: InfoFieldProps) {
  return (
    <div>
      <label className="mb-2 block text-sm font-semibold text-gray-900">
        {label}
      </label>

      <div className="flex h-11 items-center rounded-md border border-gray-300 bg-white px-3 text-sm text-gray-700">
        {value || "-"}
      </div>
    </div>
  );
}
