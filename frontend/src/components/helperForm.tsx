import { ChevronDown } from "lucide-react";

export function SectionCard({
  children,
}: {
  title: string;
  children: React.ReactNode;
}) {
  return <div className="bg-[#D6DEE7] rounded-2xl px-6 py-5">{children}</div>;
}

export function Input({
  label,
  value,
  isReadonly = false,
  onChange,
  id,
}: {
  label?: string;
  value: string | number;
  isReadonly?: boolean;
  onChange?: (val: string) => void;
  /** DOM id forwarded to the <input>, so call sites can name the field for QA. */
  id?: string;
}) {
  return (
    <div className="flex flex-col gap-1 w-full">
      {label && <label className="text-sm">{label}</label>}

      <input
        id={id}
        value={value}
        readOnly={isReadonly}
        onChange={(e) => onChange?.(e.target.value)}
        className={`border rounded px-3 py-2 w-full text-sm ${
          isReadonly ? "bg-white cursor-not-allowed" : "bg-white"
        }`}
      />
    </div>
  );
}

export function Select({
  value,
  options,
  onChange,
  id,
  disabled = false,
}: {
  value: string;
  options?: string[];
  onChange?: (val: string) => void;
  /** DOM id forwarded to the <select>. */
  id?: string;
  disabled?: boolean;
}) {
  return (
    <div className="relative">
      <select
        id={id}
        value={value}
        disabled={disabled || !onChange}
        onChange={(e) => onChange?.(e.target.value)}
        className="w-full h-10 px-3 pr-8 rounded border bg-white text-sm appearance-none disabled:bg-gray-100 disabled:cursor-not-allowed"
      >
        {options ? (
          options.map((o) => <option key={o}>{o}</option>)
        ) : (
          <option>{value}</option>
        )}
      </select>

      <ChevronDown className="w-4 h-4 absolute right-2 top-1/2 -translate-y-1/2 text-gray-500" />
    </div>
  );
}

