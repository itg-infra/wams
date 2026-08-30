import { Routes, Route, Navigate } from "react-router-dom";

import ProtectedRoute from "./router/protectedRoute";
import GuestRoute from "./router/guestRoute";

import LoginPage from "./screens/login_screen";
import HomeDashboardScreen from "./screens/home_dashboard_screen";

import UsersScreen from "./screens/users_screen";
import RolesScreen from "./screens/role_screen";
import BudgetTemplateScreen from "./screens/budget_template_screen";
import BudgetPlanScreen from "./screens/budgetPlanScreen";
import MasterPriceList from "./screens/masterPriceListScreen";
import ListFinanceReportScreen from "./screens/listFinanceReportScreen";
import GeneratePOPage from "./screens/generatePoScreen";
import GenerateAPPage from "./screens/generateApScreen";
import RealizationListApprovedBudgetPlan from "./screens/realizationListApprovedBudgetPlanScreen";
import DashboardContent from "./screens/dashboardContent";
import FormWorkOrderScreen from "./screens/formWorkOrderScreen";
import FormGeneratePO from "./screens/formGeneratePo.";
import FormGenerateAP from "./screens/formGenerateAp";
import ListWorkOrderScreen from "./screens/listWorkOrderScreen";
import DetailWorkOrderScreen from "./screens/detailWorkOrderScreen";
import { Toaster } from "react-hot-toast";
import RecapWoScreen from "./screens/recapWoScreen";
import FormCreatePriceList from "./screens/formMasterPriceList";
import DetailPriceList from "./screens/detailPriceListScreen";
import CreateBudgetTemplateScreen from "./screens/create_budget_template_screen";
import BudgetTemplateDetailScreen from "./screens/BudgetTemplateDetailScreen";
import FormBudgetPlan from "./screens/formBudgetPlanScreen";
import { BudgetPlanDetailScreen } from "./screens/detailBudgetPlanScreen";
import RecapWoPlanReal from "./screens/recapWoPlanReal";
import { DetailPoScreen } from "./screens/detailPoScreen";
import WorkflowTemplateListPage from "./screens/workflowListScreen";
import WorkflowTemplateFormPage from "./screens/workflowTemplateForm";
import WorkflowTemplateDetailPage from "./screens/workflowDetailScreen";
import FormEditWoScreen from "./screens/formEditWoScreen";
import NotificationStream from "./components/notificaitonStream";
import MasterTaxScreen from "./screens/masterTaxScreen";
import { RequirePermission } from "./router/requirePermission";
import FormTaxScreen from "./screens/formTaxScreen";
import DetailFinanceReport from "./screens/detailFinanceReport";
import RecapApdpScreen from "./screens/recapApdpScreen";
import RecapNonApdp from "./screens/recapNonApdp";
import ListSettlementReport from "./screens/listSettlementReport";
import AddUserScreen from "./screens/add_users_screen";
import AddRoleScreen from "./screens/add_role_screen";
import { DetailApScreen } from "./screens/detailApScreen";

export default function App() {
  return (
    <>
      {/* <Toaster position="top-right" /> */}
      <Toaster
        position="top-right"
        reverseOrder={false}
        toastOptions={{
          duration: 4000,
        }}
      />
      <NotificationStream />

      <Routes>
        {/* Guest */}
        <Route element={<GuestRoute />}>
          <Route path="/login" element={<LoginPage />} />
        </Route>

        {/* Protected */}
        <Route element={<ProtectedRoute />}>
          <Route path="/" element={<HomeDashboardScreen />}>
            <Route
              element={<RequirePermission permission="report.dashboard.read" />}
            >
              <Route path="dashboard" element={<DashboardContent />} />
            </Route>

            <Route path="master/users" element={<UsersScreen />} />
            <Route path="master/users/create" element={<AddUserScreen />} />
            <Route path="master/roles" element={<RolesScreen />} />
            <Route path="master/roles/form" element={<AddRoleScreen />} />
            <Route path="master/roles/form/:id" element={<AddRoleScreen />} />
            <Route path="master/rate-card" element={<MasterPriceList />} />

            <Route
              path="/workflow-template"
              element={<WorkflowTemplateListPage />}
            />
            <Route path="/master/tax" element={<MasterTaxScreen />} />
            <Route path="/master/tax/form" element={<FormTaxScreen />}></Route>
            <Route
              path="/master/tax/form/edit/:id"
              element={<FormTaxScreen />}
            ></Route>

            <Route
              path="/workflow-template/create"
              element={<WorkflowTemplateFormPage />}
            />

            <Route
              path="/workflow-template/:id"
              element={<WorkflowTemplateDetailPage />}
            />

            <Route
              path="/workflow-template/:id/edit"
              element={<WorkflowTemplateFormPage />}
            />
            <Route
              path="master/rate-card/create"
              element={<FormCreatePriceList />}
            />
            <Route
              path="master/rate-card/detail/:id"
              element={<DetailPriceList />}
            />

            <Route
              path="master/rate-card/edit/:id"
              element={<FormCreatePriceList />}
            />

            <Route
              path="budgeting/template"
              element={<BudgetTemplateScreen />}
            />

            <Route
              path="budgeting/template/create"
              element={<CreateBudgetTemplateScreen />}
            />

            <Route
              path="budgeting/template/update/:id"
              element={<CreateBudgetTemplateScreen />}
            />

            <Route
              path="budgeting/template/:id"
              element={<BudgetTemplateDetailScreen />}
            />

            <Route path="budgeting/plan" element={<BudgetPlanScreen />} />
            <Route
              path="budgeting/plan/:id"
              element={<BudgetPlanDetailScreen />}
            />

            <Route
              path="budgeting/plan/create/:templateId"
              element={<FormBudgetPlan />}
            />

            <Route
              path="budgeting/plan/edit/:budgetPlanId"
              element={<FormBudgetPlan />}
            />

            <Route path="budgeting/generate-po" element={<GeneratePOPage />} />

            <Route path="budgeting/generate-ap" element={<GenerateAPPage />} />

            <Route
              path="operational/approved-bp"
              element={<RealizationListApprovedBudgetPlan />}
            />

            <Route
              path="/generate-po/create/:budgetPlanId"
              element={<FormGeneratePO />}
            />

            <Route
              path="/generate-po/detail/:id"
              element={<DetailPoScreen />}
            />

            <Route
              path="/generate-ap/detail/:id"
              element={<DetailApScreen />}
            />

            <Route
              path="/generate-ap/create/:budgetPlanId"
              element={<FormGenerateAP />}
            />

            <Route
              path="/work-orders/create/:budgetPlanId"
              element={<FormWorkOrderScreen />}
            />

            <Route
              path="/work-orders/edit/:id"
              element={<FormEditWoScreen />}
            />

            <Route path="/work-orders" element={<ListWorkOrderScreen />} />

            <Route
              path="/work-orders/:id"
              element={<DetailWorkOrderScreen />}
            />

            <Route path="/recap-work-orders" element={<RecapWoScreen />} />
            <Route
              path="/recap-work-orders/:id"
              element={<RecapWoPlanReal />}
            />

            <Route
              path="finance/report"
              element={<ListFinanceReportScreen />}
            />

            <Route
              path="/finance/report/:id"
              element={<DetailFinanceReport />}
            />

            <Route path="finance/recap-apdp" element={<RecapApdpScreen />} />

            <Route path="finance/recap-nonapdp" element={<RecapNonApdp />} />

            {/* The screen was written and carries its ids, but was never
                registered here — so the whole menu was unreachable. */}
            <Route
              path="finance/settlement-report"
              element={<ListSettlementReport />}
            />
          </Route>
        </Route>

        {/* fallback */}
        <Route path="*" element={<Navigate to="/login" replace />} />
      </Routes>
    </>
  );
}
