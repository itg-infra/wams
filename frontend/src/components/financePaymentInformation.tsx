interface PaymentInformationCardProps {
  paymentStatus: string;
  dueDate: string;
  paidDate: string;
  paymentMethod: string;
  bankAccount: string;
  notes?: string;
}

export default function PaymentInformationCard({
  paymentStatus,
  dueDate,
  paidDate,
  paymentMethod,
  bankAccount,
  notes,
}: PaymentInformationCardProps) {
  return (
    <div className="rounded-lg bg-[#DCE5EF] p-5 my-5">
      <div className="grid grid-cols-2 gap-y-6">
        {/* Payment Status */}
        <div>
          <p className="mb-1 text-sm text-gray-500">Payment Status</p>

          <span className="inline-flex rounded-full bg-[#DDF8DF] px-4 py-1 text-sm font-medium text-[#2E9E4D]">
            {paymentStatus}
          </span>
        </div>

        {/* Due Date */}
        <div className="flex justify-end">
          <div className="w-45">
            <p className="mb-1 text-sm text-gray-500">Due Date</p>
            <p className="text-xl font-semibold">{dueDate}</p>
          </div>
        </div>

        {/* Paid Date */}
        <div>
          <p className="mb-1 text-sm text-gray-500">Paid Date</p>
          <p className="text-xl font-semibold">{paidDate}</p>
        </div>

        {/* Payment Method */}
        <div className="flex justify-end">
          <div className="w-45">
            <p className="mb-1 text-sm text-gray-500">Payment Method</p>
            <p className="text-xl font-semibold">{paymentMethod}</p>
          </div>
        </div>

        {/* Bank Account */}
        <div>
          <p className="mb-1 text-sm text-gray-500">Bank Account</p>
          <p className="text-xl font-semibold">{bankAccount}</p>
        </div>
      </div>

      <div className="mt-7">
        <p className="mb-2 text-sm font-semibold text-[#2D2D2D]">Notes</p>

        <textarea
          readOnly
          value={notes ?? ""}
          className="h-32 w-full resize-none rounded-md border border-gray-200 bg-white px-4 py-3 text-sm outline-none"
        />
      </div>
    </div>
  );
}
