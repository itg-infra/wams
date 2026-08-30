import type { SidebarItem } from "../components/sidebar/sidebar";

export interface PageMeta {
  title: string;
  subtitle: string;
}

export const PAGE_META: Record<string, PageMeta> = {
  dashboard: { title: "", subtitle: "Lorem ipsum dolor sit amet" },
  "master.user": { title: "Master User", subtitle: "Manage system users" },
  "master.role": { title: "Master Role", subtitle: "Manage system roles" },
  "master.product": { title: "Master Product", subtitle: "Manage products" },
  "master.warehouse": {
    title: "Master Warehouse",
    subtitle: "Manage warehouses",
  },
  "master.vendor": { title: "Master Vendor", subtitle: "Manage vendors" },
  "master.ratecord": {
    title: "Master Rate Card",
    subtitle: "Manage rate cards",
  },
  "master.bl": { title: "Master BL", subtitle: "Manage BL" },
  "master.coa": { title: "Master COA", subtitle: "Manage COA" },
  "master.workflowTemplate": {
    title: "Master Workflow",
    subtitle: "Manage Workflow",
  },
  budgeting: { title: "Budgeting", subtitle: "" },
  "budgeting.template": { title: "Budget Template", subtitle: "" },
  "budgeting.template.create": {
    title: "Create Budget Template",
    subtitle: "",
  },
  "budgeting.plan": { title: "Budget Plan", subtitle: "" },
  "budgeting.generate-po": { title: "Generate PO", subtitle: "" },
  "budgeting.generate-ap": { title: "Generate AP", subtitle: "" },
  quality: { title: "Quality Management", subtitle: "" },

  operational: { title: "Operational & Realization", subtitle: "" },
  "operational.approved-bp": { title: "List Approved BP", subtitle: "" },
  "operational.work-order": { title: "Work Order", subtitle: "" },
  "operational.recap-work-order": { title: "Recap Work Order", subtitle: "" },
  finance: { title: "Finance & Settlement", subtitle: "" },
  reports: { title: "Reports & Analytics", subtitle: "" },
};

export const NAV_ITEMS: SidebarItem[] = [
  {
    id: "/dashboard",
    label: "Dashboard",
    elementId: "trm_MenuDashboard",
    icon: (
      <img
        src="/sidebaricon/dashboard.png"
        alt="Dashboard"
        className="w-4 h-4"
      />
    ),
    permission: "*.*.*",
  },

  {
    id: "master",
    label: "Master Data",
    elementId: "trm_MenuMasterData",
    icon: (
      <img
        src="/sidebaricon/masterdata.png"
        alt="Master Data"
        className="w-4 h-4"
      />
    ),
    children: [
      {
        id: "/master/users",
        label: "Master User",
        elementId: "trm_MenuMasterUser",
        permission: "user.user.read",
      },
      {
        id: "/master/roles",
        label: "Master Role",
        elementId: "trm_MenuMasterRole",
        permission: "user.role.read",
      },
      {
        id: "/master/rate-card",
        label: "Master Rate Card",
        elementId: "trm_MenuMasterRateCard",
        permission: "budget.rate_card.read",
      },
      {
        id: "/workflow-template",
        label: "Master Workflow",
        elementId: "trm_MenuMasterWorkflow",
        permission: "workflow.template.read",
      },
      {
        id: "/master/tax",
        label: "Master Tax",
        elementId: "trm_MenuMasterTax",
        permission: "workflow.template.read",
      },
    ],
  },

  {
    id: "budgeting",
    label: "Budgeting",
    elementId: "trm_MenuBudgeting",
    icon: (
      <img
        src="/sidebaricon/budgeting.png"
        alt="Budgeting"
        className="w-4 h-4"
      />
    ),
    children: [
      {
        id: "/budgeting/template",
        label: "Budget Template",
        elementId: "trm_MenuBudgetTemplate",
        permission: "budget.template.read",
      }, // confirmed
      {
        id: "/budgeting/plan",
        label: "Budget Plan",
        elementId: "trm_MenuBudgetPlan",
        permission: "budget.plan.read",
      }, // confirmed
      {
        id: "/budgeting/generate-po",
        label: "Generate PO",
        elementId: "trm_MenuGeneratePO",
        permission: "budget.po.read",
      },
      {
        id: "/budgeting/generate-ap",
        label: "Generate AP",
        elementId: "trm_MenuGenerateAP",
        permission: "workorder.ap.read",
      },
    ],
  },

  {
    id: "operational",
    label: "Operational & Realization",
    elementId: "trm_MenuOperational",
    icon: (
      <img
        src="/sidebaricon/operational.png"
        alt="Operational & Realization"
        className="w-4 h-4"
      />
    ),
    children: [
      {
        id: "/operational/approved-bp",
        label: "List Approved BP",
        elementId: "trm_MenuListApprovedBP",
        permission: "workorder.workorder.read",
      },
      // {
      //   id: "/work-orders",
      //   label: "Work Orders",
      //   elementId: "trm_MenuWorkOrders",
      //   permission: "workorder.workorder.read",
      // }, // confirmed
      {
        id: "/recap-work-orders",
        label: "Recap Work Orders",
        elementId: "trm_MenuRecapWorkOrders",
        permission: "workorder.recap.read",
      },
    ],
  },

  {
    id: "finance",
    label: "Finance & Settlement",
    elementId: "trm_MenuFinance",
    icon: (
      <img
        src="/sidebaricon/finance.png"
        alt="Finance & Settlement"
        className="w-4 h-4"
      />
    ),
    children: [
      {
        id: "/finance/report",
        label: "List Finance Report",
        elementId: "trm_MenuListFinanceReport",
        permission: "report.finance-report.read",
      },
      {
        id: "recap-finance",
        label: "Recap Purchase Order",
        elementId: "trm_MenuRecapPurchaseOrder",
        permission: "report.finance-report.read",
        children: [
          {
            id: "/finance/recap-apdp",
            label: "Recap APDP",
            elementId: "trm_MenuRecapAPDP",
            permission: "report.finance-report.read",
          },
          {
            id: "/finance/recap-nonapdp",
            label: "Recap Non APDP",
            elementId: "trm_MenuRecapNonAPDP",
            permission: "report.finance-report.read",
          },
        ],
      },
    ],
  },
];