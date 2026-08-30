import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";

import { useWorkflowTemplateController } from "../controllers/masterData/workflowTemplateController";
import { DataTable, type Column } from "../components/ui/table";
import { Button } from "../components/ui/button";
import type { WorkflowTemplate } from "../types/workflowTemplate.type";

export default function WorkflowTemplateListPage() {
  const navigate = useNavigate();

  const {
    workflowTemplates,
    getWorkflowTemplates,
    isLoading,
    toggleWorkflowTemplate,
  } = useWorkflowTemplateController();

  const [search, setSearch] = useState("");
  const [togglingId, setTogglingId] = useState<number | null>(null);

  const loadData = async (page = 1) => {
    await getWorkflowTemplates({
      page,
      limit: 10,
      search,
    });
  };

  useEffect(() => {
    loadData();
  }, []);

  const handleToggle = async (templateId: number, currentIsActive: boolean) => {
    const nextStatus = !currentIsActive;
    const label = nextStatus ? "Active" : "Inactive";

    const confirmed = window.confirm(
      `Are you sure you want to set this template to "${label}"?`,
    );

    if (!confirmed) return;

    try {
      setTogglingId(templateId);
      await toggleWorkflowTemplate(templateId, nextStatus);
      await loadData();
    } finally {
      setTogglingId(null);
    }
  };

  const columns: Column<WorkflowTemplate>[] = [
    {
      key: "name",
      header: "Workflow Name",
      render: (item) => (
        <div className="font-medium text-slate-800">{item.name}</div>
      ),
    },
    {
      key: "docType",
      header: "Document Type",
      className: "text-slate-600",
      render: (item) => item.docType,
    },
    {
      key: "stageCount",
      header: "Stages",
      align: "center",
      render: (item) => (
        <span className="inline-flex items-center justify-center min-w-9 h-8 px-3 rounded-full bg-blue-50 text-blue-700 font-semibold">
          {item.stageCount}
        </span>
      ),
    },
    {
      key: "status",
      header: "Status",
      align: "center",
      render: (item) => (
        <span
          className={`inline-flex items-center px-3 py-1 rounded-full text-xs font-semibold ${
            item.isActive
              ? "bg-green-100 text-green-700"
              : "bg-red-100 text-red-700"
          }`}
        >
          <span
            className={`w-2 h-2 rounded-full mr-2 ${
              item.isActive ? "bg-green-500" : "bg-red-500"
            }`}
          />
          {item.isActive ? "Active" : "Inactive"}
        </span>
      ),
    },
    {
      key: "actions",
      header: "Actions",
      align: "center",
      render: (item) => (
        <div className="flex justify-center gap-2 flex-wrap">
          <Button
            variant="outline"
            size="sm"
            id="btn_DetailWorkflow"
            onClick={() => navigate(`/workflow-template/${item.id}`)}
          >
            Detail
          </Button>

          <Button
            variant="outline"
            size="sm"
            id="btn_EditWorkflow"
            onClick={() => navigate(`/workflow-template/${item.id}/edit`)}
          >
            Edit
          </Button>

          <Button
            variant="outline"
            size="sm"
            id="btn_ToggleWorkflow"
            onClick={() => handleToggle(item.id, item.isActive)}
            disabled={togglingId === item.id}
          >
            {togglingId === item.id
              ? "Processing..."
              : item.isActive
                ? "Deactivate"
                : "Activate"}
          </Button>
        </div>
      ),
    },
  ];

  return (
    <div className="p-6 space-y-6  bg-[#F8F8F8] min-h-screen">
      {/* Header */}
      <div className="bg-white rounded-2xl border border-slate-200 shadow-sm p-6">
        <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-4">
          <div>
            <h1 className="text-3xl font-bold text-slate-800">
              Workflow Templates
            </h1>

            <p className="text-slate-500 mt-1">
              Manage workflow approval templates and activation status.
            </p>
          </div>

          <Button
            variant="primary"
            id="btn_CreateWorkflow"
            onClick={() => navigate("/workflow-template/create")}
          >
            + Create Workflow
          </Button>
        </div>
      </div>

      {/* Search */}
      <div className="bg-white rounded-2xl border border-slate-200 shadow-sm p-5">
        <div className="flex flex-col sm:flex-row gap-3">
          <input
            id="txt_Search"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Search workflow template..."
            className="flex-1 px-4 py-2.5 border border-slate-300 rounded-xl focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
          />

          <Button id="btn_Search" variant="primary" onClick={() => loadData()}>
            Search
          </Button>
        </div>
      </div>

      {/* Table */}
      <div id="tbl_Workflow" className="shadow-sm">
        <DataTable
          columns={columns}
          data={workflowTemplates}
          rowKey={(item) => item.id}
          isLoading={isLoading}
          emptyMessage="No workflow template found"
          rowClassName="hover:bg-slate-50 transition"
        />
      </div>
    </div>
  );
}
