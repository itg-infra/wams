import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useRealizationRecapDetailController } from "../controllers/operationalRealization/detailRecapWoController";
import { Input, SectionCard } from "../components/helperForm";
import {
  formatCurrency,
  formatNumber,
} from "../components/format/formatCurrency";
import { CostRowRecap } from "../components/costRows";
import { formatDate } from "../components/format/dateTimeFormat";
import { RejectModal } from "./detailBudgetPlanScreen";
import type { RejectRecapPayload } from "../types/recapWo.type";
import { createBudgetRevision } from "../api/services/operationalRealization/detailRecapWoService";
import { PageHeader } from "../components/ui/page-header";
import { Button } from "../components/ui/button";
import toast from "react-hot-toast";
import { COST_GRID_COLS_PLAN_REAL } from "../components/gridCols";

// const COST_GRID_COLS =
//   "grid-cols-[120px_180px_120px_140px_160px_180px_100px_350px_140px_120px_150px_180px_180px_180px_140px_220px]";

export default function RecapWoPlanReal() {
  const { id } = useParams();

  const {
    plan,
    realization,
    fetchDetail,
    // approveRecap,
    rejectRecap,
    isLoading,
    fetchRevision,
    revisionByRecap,

    approvedRevision,
    rejectRevision,
    handleApprove,
  } = useRealizationRecapDetailController();

  const navigate = useNavigate();

  const [activeTab, setActiveTab] = useState<
    "plan" | "realization" | "revision"
  >("plan");
  const [showRejectModal, setShowRejectModal] = useState(false);
  const [showRejectRevisionModal, setShowRejectRevisionModal] = useState(false);

  const [showRevisionModal, setShowRevisionModal] = useState(false);

  const [revisedTotal, setRevisedTotal] = useState(
    realization?.budgetPlanTotal ?? 0,
  );

  useEffect(() => {
    fetchDetail(Number(id));
    fetchRevision(Number(id));
  }, []);

  useEffect(() => {
    if (realization) {
      setRevisedTotal(realization.budgetPlanTotal);
    }
  }, [realization]);

  const [revisionReason, setRevisionReason] = useState("");

  // const handleApprove = async () => {
  //   const result = await approveRecap(Number(id));

  //   if (result?.isThresholdError) {
  //     setShowRevisionModal(true);
  //   }
  // };

  const onRejectConfirm = async (reason: string) => {
    const payload: RejectRecapPayload = {
      reason,
    };

    await rejectRecap(Number(id), payload);
  };

  const onRejectRevisionConfirm = async (reason: string) => {
    const payload: RejectRecapPayload = {
      reason,
    };

    await rejectRevision(Number(id), payload);
  };

  const handleSubmitRevision = async () => {
    await createBudgetRevision({
      recapWorkOrderId: Number(id),
      revisedTotal,
      reason: revisionReason,
    });

    toast.success("Budget revision berhasil dibuat");

    setShowRevisionModal(false);

    fetchDetail(Number(id));
  };

  const getStatusClass = (status: string) => {
    switch (status.toLowerCase()) {
      case "approved":
        return "bg-green-100 text-green-700";
      case "rejected":
        return "bg-red-100 text-red-700";
      default:
        return "bg-yellow-100 text-yellow-700";
    }
  };

  return (
    <>
      {showRevisionModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
          <div
            className="
    bg-white
    rounded-xl
    w-[95vw]
    md:w-175
    max-h-[90vh]
    overflow-y-auto
    p-4
    md:p-6
  "
          >
            <h2 className="text-xl font-bold mb-6">Budget Revision Required</h2>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="text-sm font-medium">Budget Plan Total</label>

                <input
                  id="nmf_RevisedBudgetTotal"
                  value={
                    revisedTotal ? revisedTotal.toLocaleString("en-US") : ""
                  }
                  onChange={(e) => {
                    const raw = e.target.value.replace(/,/g, ""); // buang comma sebelum parse
                    if (raw === "" || /^\d*$/.test(raw)) {
                      setRevisedTotal(raw === "" ? 0 : Number(raw));
                    }
                  }}
                  className="w-full border rounded px-3 py-2"
                  type="text"
                  inputMode="numeric" // munculin numeric keyboard di mobile
                />
              </div>

              <div>
                <label className="text-sm font-medium">
                  Budget Realization
                </label>

                <Input
                  value={formatNumber(realization?.budgetRealization ?? 0)}
                  isReadonly
                />
              </div>

              <div>
                <label className="text-sm font-medium">Budget Variance</label>

                <Input
                  value={formatNumber(
                    revisedTotal - (realization?.budgetRealization ?? 0),
                  )}
                  isReadonly
                />
              </div>

              <div>
                <label className="text-sm font-medium">Realization %</label>

                <Input
                  value={(
                    ((realization?.budgetRealization ?? 0) / revisedTotal) *
                    100
                  ).toFixed(2)}
                  isReadonly
                />
              </div>
            </div>

            <div className="mt-4">
              <label className="text-sm font-medium">Revision Reason</label>

              <textarea
                id="txt_RevisionReason"
                value={revisionReason}
                onChange={(e) => setRevisionReason(e.target.value)}
                rows={4}
                className="w-full border rounded p-3"
              />
            </div>

            <div className="flex flex-col sm:flex-row justify-end gap-3 mt-6">
              <Button
                id="btn_CancelRevision"
                variant="outline"
                onClick={() => setShowRevisionModal(false)}
              >
                Cancel
              </Button>

              <Button
                id="btn_SubmitRevision"
                variant="primary"
                onClick={handleSubmitRevision}
              >
                Submit Revision
              </Button>
            </div>
          </div>
        </div>
      )}

      <div className="flex-1 p-4 sm:p-6 lg:p-8 overflow-y-auto flex flex-col overflow-x-hidden">
        <div className="w-full">
          <PageHeader title="Recap Work Orders" onBack={() => navigate(-1)} />
          <div className="relative flex items-end px-2 md:px-4 overflow-x-auto scrollbar-hide">
            <button
              id="btn_TabPlan"
              type="button"
              onClick={() => setActiveTab("plan")}
              className={`
                  relative
                  h-10
                  min-w-32.5
md:min-w-41.25
px-4
md:px-8
text-[14px]
md:text-[16px]
shrink-0
                  rounded-t-[24px]
                  border
                  border-b-0
                  font-medium
                  transition-all
                  duration-200
                  flex
                  items-center
                  justify-center
                  ${
                    activeTab === "plan"
                      ? "bg-[#EAF1FF] border-[#C8D6F0] text-[#1F2937] z-20"
                      : "bg-[#E7E7E7] border-[#D1D5DB] text-[#374151] z-10"
                  }
                `}
              style={{
                boxShadow: "0px -2px 5px rgba(0,0,0,0.08)",
              }}
            >
              Plan
            </button>

            <button
              id="btn_TabRealization"
              type="button"
              onClick={() => setActiveTab("realization")}
              className={`
                  relative
                  h-10
                  min-w-41.25
                  px-8
                  rounded-t-[24px]
                  border
                  border-b-0
                  text-[16px]
                  font-medium
                  transition-all
                  duration-200
                  flex
                  items-center
                  justify-center
                  ${
                    activeTab === "realization"
                      ? "bg-[#EAF1FF] border-[#C8D6F0] text-[#1F2937] z-20"
                      : "bg-[#E7E7E7] border-[#D1D5DB] text-[#374151] z-10"
                  }
                `}
              style={{
                boxShadow: "0px -2px 5px rgba(0,0,0,0.08)",
              }}
            >
              Realization
            </button>

            {(revisionByRecap?.length ?? 0) > 0 && (
              <button
                id="btn_TabRevision"
                type="button"
                onClick={() => setActiveTab("revision")}
                className={`
      relative
      h-10
      min-w-41.25
      px-8
      rounded-t-[24px]
      border
      border-b-0
      text-[16px]
      font-medium
      transition-all
      duration-200
      flex
      items-center
      justify-center
      ${
        activeTab === "revision"
          ? "bg-[#EAF1FF] border-[#C8D6F0] text-[#1F2937] z-20"
          : "bg-[#E7E7E7] border-[#D1D5DB] text-[#374151] z-10"
      }
    `}
                style={{
                  boxShadow: "0px -2px 5px rgba(0,0,0,0.08)",
                }}
              >
                Revision
              </button>
            )}
          </div>
        </div>

        <div
          className="
    relative
    -mt-px
    rounded-[14px]
    border
    border-[#C8D6F0]
    bg-[#EAF1FF]
    px-4
    md:px-6
    py-6
    md:py-10
  "
        >
          <div
            className="
    grid
    grid-cols-1
    sm:grid-cols-2
    lg:grid-cols-3
    gap-x-4
    gap-y-6
  "
          >
            <div>
              <p className="text-[15px] font-semibold text-[#111827] mb-2">
                Budget No
              </p>

              <div className="h-10 rounded-[5px] border border-[#D1D5DB] bg-[#ffff] px-3 flex items-center text-[14px] text-[#374151]">
                {plan?.header.budgetNo}
              </div>
            </div>
            <div>
              <p className="text-[15px] font-semibold text-[#111827] mb-2">
                Template Code
              </p>

              <div className="h-10 rounded-[5px] border border-[#D1D5DB] bg-[#ffff] px-3 flex items-center text-[14px] text-[#374151]">
                {plan?.header.templateCode}
              </div>
            </div>
            {/* <div>
              <p className="text-[15px] font-semibold text-[#111827] mb-2">
                Template Name
              </p>

              <div className="h-10 rounded-[5px] border border-[#D1D5DB] bg-[#ffff] px-3 flex items-center text-[14px] text-[#374151]">
                {plan?.header.templateName}
              </div>
            </div> */}
            <div>
              <p className="text-[15px] font-semibold text-[#111827] mb-2">
                Warehouse Code
              </p>

              <div className="h-10 rounded-[5px] border border-[#D1D5DB] bg-[#ffff] px-3 flex items-center text-[14px] text-[#374151]">
                {plan?.header.warehouseCode}
              </div>
            </div>
            <div>
              <p className="text-[15px] font-semibold text-[#111827] mb-2">
                Warehouse Name
              </p>

              <div className="h-10 rounded-[5px] border border-[#D1D5DB] bg-[#ffff] px-3 flex items-center text-[14px] text-[#374151]">
                {plan?.header.warehouseName}
              </div>
            </div>

            <div>
              <p className="text-[15px] font-semibold text-[#111827] mb-2">
                Location
              </p>

              <div className="h-10 rounded-[5px] border border-[#D1D5DB] bg-[#ffff] px-3 flex items-center text-[14px] text-[#374151]">
                {plan?.header.location}
              </div>
            </div>
          </div>
        </div>

        {activeTab === "plan" && (
          <>
            <h1 className="text-md font-bold my-4">Base Document</h1>

            <SectionCard title="Base Document">
              <div className="overflow-x-auto pb-2 overflow-y-visible">
                <div className="min-w-250 space-y-3">
                  <div className="grid grid-cols-[100px_150px_150px_180px_120px_140px_160px_140px_100px] gap-3 text-sm font-semibold">
                    <span>SPK Type</span>
                    <span>SPK No</span>
                    <span>Document No</span>
                    <span>Bill of Lading</span>
                    <span>Item Code</span>
                    <span>Item Name</span>
                    <span>Qty</span>
                    <span>Kemasan</span>
                    <span>UoM</span>
                  </div>

                  {plan?.spkDocuments.map((row, index) => (
                    <div
                      key={index}
                      className="grid grid-cols-[100px_150px_150px_180px_120px_140px_160px_140px_100px] gap-3"
                    >
                      <Input value={row.spkType} isReadonly />

                      <Input value={row.spkNo} isReadonly />

                      <Input value={row.documentNo} isReadonly />

                      <Input value={row.blNo ?? "Empty"} isReadonly />

                      <Input value={row.itemCode} isReadonly />

                      <Input value={row.itemName} isReadonly />

                      <Input value={String(row.quantity ?? 0)} isReadonly />

                      <Input value="Bag" isReadonly />

                      <Input value={row.uoM} isReadonly />
                    </div>
                  ))}
                </div>
              </div>

              <div className="flex justify-end items-end mt-4 overflow-x-auto">
                <div className="flex flex-col sm:flex-row items-start sm:items-center gap-3">
                  <span className="font-semibold text-sm">Grand Total</span>

                  <Input value={formatNumber(100000)} isReadonly />
                </div>
              </div>
            </SectionCard>

            <h1 className="text-md font-bold my-4">Cost Details</h1>

            <SectionCard title="Cost Detail">
              <div className="overflow-x-auto pb-2">
                <div className="min-w-350 space-y-3">
                  <div
                    className={`grid ${COST_GRID_COLS_PLAN_REAL} gap-3 text-sm font-semibold`}
                  >
                    <span>Cost Name</span>
                    <span>COA</span>
                    <span>COA Name</span>
                    {/* <span>Activity</span> */}
                    <span>Type</span>
                    <span>Vendor Code</span>
                    <span>Vendor Name</span>
                    <span>RFBA</span>
                    <span>Doc External</span>

                    <span>Bill of Lading</span>
                    <span>Unit Cost</span>
                    <span>Unit Count</span>
                    <span>UoM</span>
                    <span>Description</span>
                  </div>

                  {plan?.costDetails.map((row, index) => (
                    <CostRowRecap key={index} row={row} />
                  ))}
                </div>
              </div>

              <div className="flex justify-end items-end mt-4 overflow-x-auto">
                <div className="flex flex-col sm:flex-row items-start sm:items-center gap-3">
                  <span className="font-semibold text-sm">Grand Total</span>

                  <Input value={formatNumber(100000)} isReadonly />
                </div>
              </div>
            </SectionCard>

            <h1 className="text-md font-bold my-4">Budget Recap</h1>

            <SectionCard title="Base Document">
              <div className="overflow-x-auto pb-2 overflow-y-visible">
                <div className="min-w-250 space-y-3">
                  <div className="grid grid-cols-1 md:grid-cols-3 gap-3 text-sm font-semibold">
                    <span>Budget Plan</span>
                    <span>Budget Realization</span>
                    <span>Budget Variance</span>
                  </div>

                  <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
                    <Input
                      value={formatNumber(plan?.budgetPlanTotal ?? 0)}
                      isReadonly
                    />
                    <Input
                      value={formatNumber(plan?.budgetRealization ?? 0)}
                      isReadonly
                    />
                    <Input
                      value={formatNumber(plan?.budgetVariance ?? 0)}
                      isReadonly
                    />
                  </div>
                </div>
              </div>
            </SectionCard>
          </>
        )}

        {activeTab === "realization" && (
          <>
            <h1 className="text-md font-bold my-4">Base Document</h1>

            <SectionCard title="Base Document">
              <div className="overflow-x-auto pb-2 overflow-y-visible">
                <div className="min-w-250 space-y-3">
                  <div className="grid grid-cols-[100px_150px_150px_180px_120px_140px_160px_140px_100px_100px] gap-3 text-sm font-semibold">
                    <span>WO Code</span>
                    <span>BL Number</span>
                    <span>Vessel</span>
                    <span>Product</span>
                    <span>PIC</span>
                    <span>RFBA</span>
                    <span>Start Date</span>
                    <span>End Date</span>
                    <span>Total Budget</span>
                    <span>Status</span>
                  </div>

                  {realization?.workOrders.map((row, index) => (
                    <div
                      key={index}
                      className="grid grid-cols-[100px_150px_150px_180px_120px_140px_160px_140px_100px_100px] gap-3"
                    >
                      <Input value={row.workOrderCode} isReadonly />

                      <Input value={String(row.blNumber)} isReadonly />

                      <Input value={row.vehicleNo} isReadonly />

                      <Input value={row.product ?? "Empty"} isReadonly />

                      <Input value={row.picName} isReadonly />

                      <Input value={row.isRfba ? "yes" : "no"} isReadonly />

                      <Input value={formatDate(row.startDate)} isReadonly />

                      <Input value={formatDate(row.endDate)} isReadonly />

                      <Input value={formatNumber(row.actualCost)} isReadonly />

                      <Input value={row.workOrderStatus} isReadonly />
                    </div>
                  ))}
                </div>
              </div>

              <div className="flex justify-end items-end mt-4 overflow-x-auto">
                <div className="flex flex-col sm:flex-row items-start sm:items-center gap-3">
                  <span className="font-semibold text-sm">Grand Total</span>

                  <Input value={formatNumber(100000)} isReadonly />
                </div>
              </div>
            </SectionCard>

            <h1 className="text-md font-bold my-4">Budget Recap</h1>

            <SectionCard title="Base Document">
              <div className="overflow-x-auto pb-2 overflow-y-visible">
                <div className="min-w-250 space-y-3">
                  {/* <div className="grid grid-cols-1 md:grid-cols-4 gap-3 text-sm font-semibold">
                    <span>Budget Plan</span>
                    <span>Budget Realization</span>
                    <span>Budget Variance</span>
                    <span>Realization Percent</span>
                  </div>

                  <div className="grid grid-cols-1 md:grid-cols-4 gap-3">
                    <Input
                      value={formatNumber(realization?.budgetPlanTotal ?? 0)}
                      isReadonly
                    />
                    <Input
                      value={formatNumber(realization?.budgetRealization ?? 0)}
                      isReadonly
                    />
                    <Input
                      value={formatNumber(realization?.budgetVariance ?? 0)}
                      isReadonly
                    />
                    <Input
                      value={formatNumber(realization?.realizationPercent ?? 0)}
                      isReadonly
                    />
                  </div> */}
                  <div className="grid grid-cols-1 md:grid-cols-4 gap-3">
                    <div>
                      <span className="block text-sm font-semibold mb-2">
                        Budget Plan
                      </span>

                      <Input
                        value={formatNumber(realization?.budgetPlanTotal ?? 0)}
                        isReadonly
                      />
                    </div>

                    <div>
                      <span className="block text-sm font-semibold mb-2">
                        Budget Realization
                      </span>

                      <Input
                        value={formatNumber(
                          realization?.budgetRealization ?? 0,
                        )}
                        isReadonly
                      />
                    </div>

                    <div>
                      <span className="block text-sm font-semibold mb-2">
                        Budget Variance
                      </span>

                      <Input
                        value={formatNumber(realization?.budgetVariance ?? 0)}
                        isReadonly
                      />
                    </div>

                    <div>
                      <span className="block text-sm font-semibold mb-2">
                        Realization Percent
                      </span>

                      <Input
                        value={formatNumber(
                          realization?.realizationPercent ?? 0,
                        )}
                        isReadonly
                      />
                    </div>
                  </div>
                </div>
              </div>
            </SectionCard>
          </>
        )}

        {activeTab === "revision" && (
          <div className="space-y-5 py-10">
            {revisionByRecap?.map((revision) => {
              const variance = revision.revisedTotal - revision.originalTotal;

              return (
                <div
                  key={revision.id}
                  className="
            overflow-hidden
            rounded-3xl
            border border-[#E5E7EB]
            bg-white
            shadow-[0_4px_20px_rgba(0,0,0,0.06)]
          "
                >
                  {/* Header */}
                  <div className="bg-linear-to-r from-[#EEF4FF] to-[#F8FAFF] px-4 md:px-6 py-5 border-b border-[#E5E7EB]">
                    <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
                      <div>
                        <h3 className="text-lg font-semibold text-[#1F2937]">
                          Budget Revision #{revision.id}
                        </h3>

                        <p className="mt-1 text-sm text-[#6B7280]">
                          Submitted by{" "}
                          <span className="font-medium text-[#374151]">
                            {revision.submittedBy}
                          </span>
                        </p>
                      </div>

                      <span
                        className={`px-4 py-2 rounded-full text-xs font-semibold ${getStatusClass(
                          revision.status,
                        )}`}
                      >
                        {revision.status}
                      </span>
                    </div>
                  </div>

                  {/* Budget Summary */}
                  <div className="p-4 md:p-6">
                    <div className="grid gap-4 md:grid-cols-3">
                      <div className="rounded-2xl border border-[#E5E7EB] bg-[#FAFAFA] p-4">
                        <p className="text-xs uppercase tracking-wide text-gray-500">
                          Original Budget
                        </p>

                        <p className="mt-2 text-xl font-bold text-[#374151]">
                          {formatCurrency(revision.originalTotal)}
                        </p>
                      </div>

                      <div className="rounded-2xl border border-[#D9E7FF] bg-[#F5F9FF] p-4">
                        <p className="text-xs uppercase tracking-wide text-[#6B7280]">
                          Revised Budget
                        </p>

                        <p className="mt-2 text-xl font-bold text-[#2E277C]">
                          {formatCurrency(revision.revisedTotal)}
                        </p>
                      </div>

                      <div
                        className={`
                  rounded-2xl
                  border
                  p-4
                  ${
                    variance >= 0
                      ? "border-[#D9E7FF] bg-[#F8FBFF]"
                      : "border-[#FFE0E0] bg-[#FFF7F7]"
                  }
                `}
                      >
                        <p className="text-xs uppercase tracking-wide text-gray-500">
                          Variance
                        </p>

                        <p
                          className={`mt-2 text-xl font-bold ${
                            variance >= 0 ? "text-[#2563EB]" : "text-[#DC2626]"
                          }`}
                        >
                          {variance >= 0 ? "+" : ""}
                          {formatCurrency(variance)}
                        </p>
                      </div>
                    </div>

                    {/* Detail */}
                    <div className="mt-6 grid gap-5 md:grid-cols-2">
                      <div>
                        <p className="mb-1 text-xs font-medium uppercase text-gray-500">
                          Submitted At
                        </p>

                        <p className="text-sm text-gray-800">
                          {new Date(revision.createdAt).toLocaleString("id-ID")}
                        </p>
                      </div>

                      <div>
                        <p className="mb-1 text-xs font-medium uppercase text-gray-500">
                          Reviewed By
                        </p>

                        <p className="text-sm text-gray-800">
                          {revision.reviewedBy ?? "-"}
                        </p>
                      </div>

                      <div>
                        <p className="mb-1 text-xs font-medium uppercase text-gray-500">
                          Reviewed At
                        </p>

                        <p className="text-sm text-gray-800">
                          {revision.reviewedAt
                            ? new Date(revision.reviewedAt).toLocaleString(
                                "id-ID",
                              )
                            : "-"}
                        </p>
                      </div>
                    </div>

                    {/* Reason */}
                    <div className="mt-6">
                      <p className="mb-2 text-xs font-medium uppercase text-gray-500">
                        Revision Reason
                      </p>

                      <div className="rounded-2xl border border-[#E5E7EB] bg-[#FAFAFA] p-4 text-sm text-gray-700 leading-relaxed">
                        {revision.reason}
                      </div>
                    </div>

                    {/* Rejection */}
                    {revision.rejectionReason && (
                      <div className="mt-5">
                        <p className="mb-2 text-xs font-medium uppercase text-red-500">
                          Rejection Reason
                        </p>

                        <div className="rounded-2xl border border-red-200 bg-red-50 p-4 text-sm text-red-700">
                          {revision.rejectionReason}
                        </div>
                      </div>
                    )}
                  </div>

                  {revision.status !== "Approved" && (
                    <div
                      className="
    flex
    flex-col
    sm:flex-row
    justify-end
    gap-4
    py-5
  "
                    >
                      {/* {isEditMode && formDetail.status === "Draft" && ( */}
                      <Button
                        id="btn_RejectRevision"
                        variant="outline"
                        onClick={() => setShowRejectRevisionModal(true)}
                        disabled={isLoading}
                        className="w-full sm:w-auto px-6"
                      >
                        {isLoading ? "Loading..." : "Reject"}
                      </Button>
                      {/* )} */}

                      <Button
                        id="btn_ApproveRevision"
                        variant="primary"
                        onClick={() => approvedRevision(revision.id)}
                        disabled={isLoading}
                        className="w-full sm:w-auto px-6"
                      >
                        Approve
                      </Button>
                    </div>
                  )}
                </div>
              );
            })}

            {(revisionByRecap?.length ?? 0) === 0 && (
              <div className="rounded-3xl border border-dashed border-gray-300 bg-white p-12 text-center">
                <p className="text-lg font-medium text-gray-600">
                  No Revision Data
                </p>

                <p className="mt-2 text-sm text-gray-400">
                  Budget revision information will appear here.
                </p>
              </div>
            )}
          </div>
        )}

        {activeTab !== "revision" && (
          <div className="flex justify-end gap-4 py-5">
            {/* {isEditMode && formDetail.status === "Draft" && ( */}
            <Button
              id="btn_RejectRecapWo"
              variant="outline"
              onClick={() => setShowRejectModal(true)}
              disabled={isLoading}
              className="px-6"
            >
              {isLoading ? "Loading..." : "Reject"}
            </Button>
            {/* )} */}

            <Button
              id="btn_ApproveRecapWo"
              variant="primary"
              onClick={() => handleApprove(Number(id))}
              disabled={isLoading}
              className="px-6"
            >
              {/* {isSubmitting
              ? "Loading..."
              : isEditMode
                ? "Update & Submit"
                : "Submit"} */}
              Approve
            </Button>
          </div>
        )}

        <RejectModal
          isOpen={showRejectModal}
          isLoading={isLoading}
          onCancel={() => setShowRejectModal(false)}
          onConfirm={onRejectConfirm}
        />

        <RejectModal
          isOpen={showRejectRevisionModal}
          isLoading={isLoading}
          onCancel={() => setShowRejectRevisionModal(false)}
          onConfirm={onRejectRevisionConfirm}
        />
      </div>
    </>
  );
}
