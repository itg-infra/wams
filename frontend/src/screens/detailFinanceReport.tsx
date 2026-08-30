import { useNavigate, useParams } from "react-router-dom";
import { useFinanceReportDetailController } from "../controllers/finance/financeReportController";
import FinanceReportHeaderCard from "../components/financeReportHeader";
import CostDetailsSection from "../components/financeCostDetail";
import BudgetRecapSection from "../components/financeReportBRecap";
import PaymentInformationCard from "../components/financePaymentInformation";
import { PageHeader } from "../components/ui/page-header";


export default function DetailFinanceReport() {
  const { id } = useParams();

  const { data, isLoading } = useFinanceReportDetailController(id);

  const navigate = useNavigate();

  if (isLoading) return <div>Loading...</div>;

  return (
    <div className="rounded-lg bg-white p-6">
      <PageHeader
        breadcrumbs={[{ label: "Dashboard" }, { label: "Detail Finance Report" }]}
        title="Detail Finance Report"
        onBack={() => navigate(-1)}
      />

      <FinanceReportHeaderCard header={data?.header} />

      {/*  cost details */}
      <CostDetailsSection costDetails={data?.costDetails ?? []} />

      {/* budget plan recap */}
      <BudgetRecapSection budgetRecap={data?.budgetRecap} />

      {/* payment section */}
      <PaymentInformationCard
        paymentStatus="Paid"
        dueDate="20 Maret 2025"
        paidDate="18 Maret 2025"
        paymentMethod="Transfer Bank"
        bankAccount="BCA 1234567890"
        notes="Hello"
      />
    </div>
  );
}
