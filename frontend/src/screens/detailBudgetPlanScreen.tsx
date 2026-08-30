import React, { useState } from "react";
import { useBudgetPlanDetailController } from "../controllers/budgeting/budgetPlanDetailController";
import type { BudgetPlanDetailItem } from "../types/budgetPlanDetial.type";
import PermissionGuard from "../components/guards/permissionGuard";
import { useAuthStore } from "../store/authStore";
import { useNavigate, useParams } from "react-router-dom";
import { DataTable, type Column } from "../components/ui/table";
import { PageHeader } from "../components/ui/page-header";
import { Button } from "../components/ui/button";

// ─── Helpers ─────────────────────────────────────────────────────────────────

function formatCurrency(value: number): string {
  return new Intl.NumberFormat("id-ID", {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(value);
}

// ─── Cost Detail Columns ───────────────────────────────────────────────────────

const costColumns: Column<BudgetPlanDetailItem>[] = [
  {
    key: "id",
    header: "Cost ID",
    className: "whitespace-nowrap text-[#0F172A]",
    render: (item) => item.id,
  },
  {
    key: "type",
    header: "Type",
    className: "whitespace-nowrap",
    render: (item) => (
      <span
        className={`
                  inline-flex items-center rounded-full px-2.5 py-1 text-[11px] font-medium
                  ${
                    item.type === "Internal"
                      ? "bg-blue-50 text-blue-700"
                      : "bg-emerald-50 text-emerald-700"
                  }
                `}
      >
        {item.type}
      </span>
    ),
  },
  {
    key: "vendorCode",
    header: "Vendor Code",
    className: "whitespace-nowrap text-[#334155]",
    render: (item) => item.vendorCode,
  },
  {
    key: "vendorName",
    header: "Vendor Name",
    className: "whitespace-nowrap text-[#334155]",
    render: (item) => item.vendorName,
  },
  {
    key: "activityName",
    header: "Activity Name",
    className: "whitespace-nowrap text-[#334155]",
    render: (item) => item.activityTypeName ?? "-",
  },
  {
    key: "docExternal",
    header: "Doc. External",
    className: "whitespace-nowrap text-[#334155]",
    render: (item) => item.docExternal ?? "-",
  },
  {
    key: "rfba",
    header: "RFBA",
    className: "whitespace-nowrap",
    render: (item) => (
      <span
        className={`
                  inline-flex items-center rounded-full px-2.5 py-1 text-[11px] font-medium
                  ${
                    item.isRfba
                      ? "bg-emerald-50 text-emerald-700"
                      : "bg-rose-50 text-rose-700"
                  }
                `}
      >
        {item.isRfba ? "Yes" : "No"}
      </span>
    ),
  },
  {
    key: "coa",
    header: "COA",
    className: "whitespace-nowrap text-[#334155]",
    render: (item) => item.coa,
  },
  {
    key: "coaName",
    header: "COA Name",
    className: "whitespace-nowrap text-[#334155]",
    render: (item) => item.coaName,
  },
  {
    key: "itemCode",
    header: "Item Code",
    className: "whitespace-nowrap text-[#334155]",
    render: (item) => item.costDetail,
  },
  {
    key: "itemName",
    header: "Item Name",
    className: "whitespace-nowrap text-[#334155]",
    render: (item) => item.costName,
  },
  {
    key: "unitCost",
    header: "Unit Cost",
    align: "right",
    className: "whitespace-nowrap font-medium text-[#0F172A]",
    render: (item) => formatCurrency(item.costValue),
  },
  {
    key: "qty",
    header: "Qty",
    align: "right",
    className: "whitespace-nowrap font-medium text-[#0F172A]",
    render: (item) => item.quantity.toLocaleString("id-ID"),
  },
  {
    key: "totalValue",
    header: "Total Value",
    align: "right",
    className: "whitespace-nowrap font-semibold text-[#0F172A]",
    render: (item) => formatCurrency(item.totalValue),
  },
  {
    key: "uom",
    header: "UoM",
    className: "whitespace-nowrap text-[#334155]",
    render: (item) => item.uomName,
  },
];

// ─── Status Badge ─────────────────────────────────────────────────────────────

const STATUS_CONFIG: Record<string, { className: string; label: string }> = {
  Draft: { className: "bg-gray-100 text-gray-500", label: "Draft" },
  Submitted: { className: "bg-blue-50 text-blue-600", label: "Submitted" },
  PartialApproved: {
    className: "bg-orange-50 text-orange-600",
    label: "Partial Approved",
  },
  Approved: { className: "bg-green-50 text-green-600", label: "Approved" },
  Rejected: { className: "bg-red-50 text-red-600", label: "Rejected" },
};

function StatusBadge({ status }: { status: string }) {
  const config = STATUS_CONFIG[status] ?? {
    className: "bg-gray-100 text-gray-500",
    label: status,
  };
  return (
    <span
      className={`inline-flex items-center px-2.5 py-0.5 rounded text-xs font-medium ${config.className}`}
    >
      {config.label}
    </span>
  );
}

// ─── Info Field ───────────────────────────────────────────────────────────────

function InfoField({
  label,
  value,
  children,
}: {
  label: string;
  value?: string | null;
  children?: React.ReactNode;
}) {
  return (
    <div className="flex flex-col gap-1">
      <span className="text-[11px] font-medium text-gray-400 uppercase tracking-wide">
        {label}
      </span>
      {children ?? (
        <span className="text-sm text-gray-900">{value || "-"}</span>
      )}
    </div>
  );
}

// ─── Section Card ─────────────────────────────────────────────────────────────

function SectionCard({
  title,
  children,
}: {
  title?: string;
  children: React.ReactNode;
}) {
  return (
    <div className="mb-6 overflow-hidden rounded-2xl border border-[#E2E8F0] bg-white shadow-sm">
      {title && (
        <div className="flex items-center justify-between border-b border-[#E2E8F0] bg-[#F8FAFC] px-6 py-4">
          <h2 className="text-[15px] font-semibold tracking-[0.2px] text-[#1E293B]">
            {title}
          </h2>
        </div>
      )}

      <div className="p-5 md:p-6">{children}</div>
    </div>
  );
}

// ─── Reject Modal ─────────────────────────────────────────────────────────────

export function RejectModal({
  isOpen,
  isLoading,
  onCancel,
  onConfirm,
}: {
  isOpen: boolean;
  isLoading: boolean;
  onCancel: () => void;
  onConfirm: (notes: string) => void;
}) {
  const [notes, setNotes] = useState("");

  if (!isOpen) return null;

  return (
    <div
      id="lbl_RejectBudgetPlanDialog"
      className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/40"
    >
      <div className="w-full max-w-md bg-white rounded-lg shadow-2xl overflow-hidden">
        <div className="p-5">
          <p className="text-sm font-semibold text-gray-700 mb-2">Notes</p>
          <textarea
            id="txt_RejectNotes"
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            placeholder="Masukkan alasan penolakan..."
            rows={4}
            className="w-full border border-gray-300 rounded-md px-3 py-2 text-sm text-gray-900 resize-y outline-none focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
          />
        </div>
        <div className="px-5 py-3 bg-gray-50 border-t border-gray-200 flex justify-end gap-2">
          <Button
            id="btn_CancelReject"
            variant="outline"
            onClick={onCancel}
            disabled={isLoading}
          >
            Cancel
          </Button>
          <Button
            id="btn_ConfirmReject"
            variant="destructive"
            onClick={() => onConfirm(notes)}
            disabled={isLoading || !notes.trim()}
          >
            {isLoading ? "Menolak..." : "Reject"}
          </Button>
        </div>
      </div>
    </div>
  );
}

// ─── Main Screen ──────────────────────────────────────────────────────────────

export function BudgetPlanDetailScreen() {
  const { id } = useParams();
  const navigate = useNavigate();
  const {
    detail,
    isLoading,
    isApproving,
    isRejecting,
    error,
    handleApprove,
    handleReject,
  } = useBudgetPlanDetailController(id!);

  const { user } = useAuthStore();

  const currentUserName = user?.fullname;

  const [showRejectModal, setShowRejectModal] = useState(false);
  const [actionSuccess, setActionSuccess] = useState<string | null>(null);

  const approvedStages = detail?.approval.stages.filter(
    (stage) => stage.status === "Approved",
  );

  const canShowApprovalAction =
    approvedStages?.length !== detail?.approval.totalStages &&
    !approvedStages?.some((stage) => stage.approvedByName === currentUserName);

  const onApprove = async () => {
    const ok = await handleApprove();
    if (ok) setActionSuccess("Budget plan berhasil disetujui.");
  };

  const onRejectConfirm = async (notes: string) => {
    const ok = await handleReject(notes);
    if (ok) {
      setShowRejectModal(false);
      setActionSuccess("Budget plan berhasil ditolak.");
    }
  };

  // ── Loading ──
  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-72">
        <div className="text-center">
          <div className="w-9 h-9 rounded-full border-[3px] border-gray-200 border-t-indigo-900 animate-spin mx-auto mb-3" />
          <p className="text-sm text-gray-500">Memuat data...</p>
        </div>
      </div>
    );
  }

  // ── Error ──
  if (error || !detail) {
    return (
      <div className="p-6">
        <Button
          variant="outline"
          onClick={() => navigate(-1)}
        >
          ← Kembali
        </Button>
        <div className="mt-6 text-center text-red-600 text-sm">
          {error ?? "Data tidak ditemukan."}
        </div>
      </div>
    );
  }

  const stages = detail.approval?.stages ?? [];

  const currentStage = stages.find(
    (stage) => stage.stageOrder === detail.approval?.currentStageOrder,
  );

  const previousStagesApproved = stages
    .filter((stage) => stage.stageOrder < (currentStage?.stageOrder ?? 0))
    .every((stage) => stage.status === "Approved");

  const canAction =
    !!currentStage &&
    currentStage.status === "Pending" &&
    previousStagesApproved;
    
  const items: BudgetPlanDetailItem[] = detail.items;

  return (
    <div className="flex-1 p-4 sm:p-6 lg:p-8 overflow-y-auto">
      <PageHeader
        breadcrumbs={[
          { label: "Dashboard", onClick: () => navigate(-1) },
          { label: "Budgeting" },
          { label: detail.status },
          { label: "Detail" },
        ]}
        title="Approval Budget Plan"
        onBack={() => navigate(-1)}
      />

      {/* Success banner */}
      {actionSuccess && (
        <div className="mb-4 flex items-center justify-between px-3.5 py-2.5 bg-green-50 border border-green-200 rounded-md text-sm text-green-700">
          {actionSuccess}
          <button
            onClick={() => setActionSuccess(null)}
            className="text-gray-400 hover:text-gray-600 cursor-pointer bg-transparent border-none text-base leading-none"
          >
            ✕
          </button>
        </div>
      )}

      {/* ── Header Info ── */}
      <SectionCard>
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
          <InfoField label="Budget No" value={detail.budgetNo} />
          <InfoField label="Template Id" value={detail.templateId} />
          <InfoField label="Status">
            <StatusBadge status={detail.status} />
          </InfoField>
        </div>
        <div className="h-px bg-gray-100 my-3" />
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
          <InfoField label="Remark" value={detail.remark || "-"} />
          {/* <InfoField label="Template Name" value={detail.templateName} /> */}
          <InfoField label="Document Date" value={detail.docDate} />
        </div>
        <div className="h-px bg-gray-100 my-3" />
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
          <InfoField label="Warehouse Code" value={detail.warehouseCode} />
          <InfoField label="Warehouse Name" value={detail.warehouseName} />
          <InfoField label="Location" value={detail.location} />
        </div>
      </SectionCard>

      {/* ── Cost Detail Table ── */}
      <SectionCard title="Cost Detail">
        <DataTable
          columns={costColumns}
          data={items}
          rowKey={(item) => item.id}
          striped={false}
          tableClassName="min-w-full border-collapse text-sm"
          rowClassName="transition-colors duration-150 hover:bg-[#FAFBFC]"
          emptyMessage="No cost details found."
        />

        <div className="mt-5 flex justify-end border-t border-[#E2E8F0] pt-4">
          <div className="flex items-center gap-4 rounded-xl bg-[#F8FAFC] px-5 py-3">
            <span className="text-sm font-semibold text-[#475569]">
              Grand Total
            </span>

            <div className="min-w-45 rounded-lg border border-[#CBD5E1] bg-white px-4 py-2 text-right text-sm font-bold text-[#0F172A]">
              {detail.grandTotalFormatted}
            </div>
          </div>
        </div>
      </SectionCard>

      {/* ── Approval Info ── */}
      {detail.approval.stages.some((stage) => stage.status === "Approved") && (
        <SectionCard title="Approval Info">
          <div className="flex flex-col gap-2">
            {detail.approval.stages
              .filter((stage) => stage.status === "Approved")
              .map((stage) => (
                <div
                  key={stage.stageOrder}
                  className="flex justify-between items-center px-3 py-2.5 bg-gray-50 rounded-md border border-gray-200"
                >
                  <span className="text-xs font-semibold text-gray-500">
                    {stage.stageName} — {stage.approverRoles.join(", ")}
                  </span>

                  <span className="text-xs text-gray-700">
                    {stage.approvedByName}
                  </span>
                </div>
              ))}
          </div>
        </SectionCard>
      )}

      {/* ── Rejection Info ── */}
      {detail.status === "Rejected" && detail.rejectionReason && (
        <SectionCard title="Rejection Info">
          <div className="flex flex-col gap-3">
            <InfoField label="Rejected By" value={detail.rejectedByName} />
            <InfoField label="Reason" value={detail.rejectionReason} />
          </div>
        </SectionCard>
      )}
      <PermissionGuard permission="budget.plan.approve">
        {canAction && canShowApprovalAction && (
          <div className="flex justify-end gap-2 mt-2 pt-4">
            <Button
              id="btn_RejectBudgetPlan"
              variant="destructive"
              onClick={() => setShowRejectModal(true)}
              disabled={isApproving || isRejecting}
            >
              Reject
            </Button>
            <Button
              id="btn_ApproveBudgetPlan"
              variant="primary"
              onClick={onApprove}
              disabled={isApproving || isRejecting}
            >
              {isApproving ? "Menyetujui..." : "Approve"}
            </Button>
          </div>
        )}
      </PermissionGuard>

      {/* ── Reject Modal ── */}
      <RejectModal
        isOpen={showRejectModal}
        isLoading={isRejecting}
        onCancel={() => setShowRejectModal(false)}
        onConfirm={onRejectConfirm}
      />
    </div>
  );
}
