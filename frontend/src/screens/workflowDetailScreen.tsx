import { useEffect } from "react";
import { useNavigate, useParams } from "react-router-dom";

import { Button } from "../components/ui/button";
import { useWorkflowTemplateController } from "../controllers/masterData/workflowTemplateController";

export default function WorkflowTemplateDetailPage() {
  const { id } = useParams();

  const navigate = useNavigate();

  const { workflowTemplateDetail, getWorkflowTemplateDetail } =
    useWorkflowTemplateController();

  useEffect(() => {
    if (id) {
      getWorkflowTemplateDetail(Number(id));
    }
  }, [id]);

  if (!workflowTemplateDetail) {
    return (
      <div className="min-h-screen bg-slate-50 flex items-center justify-center">
        <div className="flex items-center gap-3 text-slate-500">
          <div className="w-5 h-5 border-2 border-slate-300 border-t-blue-600 rounded-full animate-spin" />
          Loading workflow detail...
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-slate-50 p-4 md:p-6">
      <div className="max-w-7xl mx-auto space-y-6">
        {/* Header */}
        <div className="bg-white rounded-2xl border border-slate-200 shadow-sm p-6">
          <div className="flex flex-col lg:flex-row lg:items-center lg:justify-between gap-4">
            <div>
              <h1 className="text-3xl font-bold text-slate-800">
                Workflow Detail
              </h1>

              <p className="text-slate-500 mt-1">
                View workflow template information and approval stages.
              </p>
            </div>

            <div className="flex flex-wrap gap-3">
              <Button
                id="btn_Back"
                variant="outline"
                onClick={() => navigate("/workflow-template")}
              >
                Back
              </Button>
            </div>
          </div>
        </div>

        {/* Workflow Information */}
        <div className="bg-white rounded-2xl border border-slate-200 shadow-sm overflow-hidden">
          <div className="px-6 py-4 border-b border-slate-200 bg-slate-50">
            <h2 className="text-lg font-semibold text-slate-800">
              Workflow Information
            </h2>
          </div>

          <div className="p-6">
            <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
              <div>
                <div className="text-sm text-slate-500 mb-1">Workflow Name</div>

                <div className="font-semibold text-slate-800">
                  {workflowTemplateDetail.name}
                </div>
              </div>

              <div>
                <div className="text-sm text-slate-500 mb-1">Document Type</div>

                <div className="font-semibold text-slate-800">
                  {workflowTemplateDetail.docType}
                </div>
              </div>

              <div>
                <div className="text-sm text-slate-500 mb-1">Status</div>

                <span
                  className={`inline-flex items-center px-3 py-1 rounded-full text-sm font-medium ${
                    workflowTemplateDetail.isActive
                      ? "bg-green-100 text-green-700"
                      : "bg-red-100 text-red-700"
                  }`}
                >
                  <span
                    className={`w-2 h-2 rounded-full mr-2 ${
                      workflowTemplateDetail.isActive
                        ? "bg-green-500"
                        : "bg-red-500"
                    }`}
                  />
                  {workflowTemplateDetail.isActive ? "Active" : "Inactive"}
                </span>
              </div>
            </div>
          </div>
        </div>

        {/* Approval Stages */}
        <div className="bg-white rounded-2xl border border-slate-200 shadow-sm overflow-hidden">
          <div className="px-6 py-4 border-b border-slate-200 bg-slate-50">
            <div className="flex items-center justify-between">
              <h2 className="text-lg font-semibold text-slate-800">
                Approval Stages
              </h2>

              <span className="px-3 py-1 bg-blue-100 text-blue-700 rounded-full text-sm font-medium">
                {workflowTemplateDetail.stages.length} Stage
                {workflowTemplateDetail.stages.length > 1 ? "s" : ""}
              </span>
            </div>
          </div>

          <div className="p-6">
            <div className="space-y-5">
              {workflowTemplateDetail.stages.map((stage) => (
                <div
                  key={stage.id}
                  className="relative border border-slate-200 rounded-2xl p-5 hover:shadow-md transition"
                >

                  <div className="flex flex-col md:flex-row md:items-start gap-4">
                    {/* Stage Number */}
                    <div className="shrink-0">
                      <div className="w-12 h-12 rounded-full bg-blue-600 text-white flex items-center justify-center font-bold text-lg shadow">
                        {stage.stageOrder}
                      </div>
                    </div>

                    {/* Stage Content */}
                    <div className="flex-1">
                      <div className="flex flex-col gap-1">
                        <div className="text-sm text-slate-500">
                          Approval Stage
                        </div>

                        <div className="text-lg font-semibold text-slate-800">
                          {stage.stageName}
                        </div>
                      </div>

                      <div className="mt-4">
                        <div className="text-sm font-medium text-slate-600 mb-2">
                          Approver Roles
                        </div>

                        <div className="flex flex-wrap gap-2">
                          {stage.approverRoles.map((role) => (
                            <span
                              key={role}
                              className="px-3 py-1.5 rounded-full bg-slate-100 text-slate-700 text-sm font-medium border border-slate-200"
                            >
                              {role}
                            </span>
                          ))}
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              ))}

              {workflowTemplateDetail.stages.length === 0 && (
                <div className="text-center py-12 text-slate-500">
                  No stages available
                </div>
              )}
            </div>
          </div>
        </div>

        {/* Footer */}
        <div className="bg-white rounded-2xl border border-slate-200 shadow-sm p-6">
          <div className="flex justify-end">
            <Button
              id="btn_EditWorkflow"
              variant="primary"
              size="lg"
              onClick={() => navigate(`/workflow-template/${id}/edit`)}
            >
              Edit Workflow
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
}
