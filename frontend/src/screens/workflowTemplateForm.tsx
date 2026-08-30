import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";

import { Button } from "../components/ui/button";
import { useWorkflowTemplateController } from "../controllers/masterData/workflowTemplateController";
import { useRoleController } from "../controllers/masterData/roleController";

interface StageForm {
  stageOrder: number;
  stageName: string;
  approverRoles: string[];
}

const EMPTY_STAGE: StageForm = {
  stageOrder: 1,
  stageName: "",
  approverRoles: [],
};

export default function WorkflowTemplateFormPage() {
  const navigate = useNavigate();

  const { id } = useParams();

  const isEdit = !!id;

  const {
    workflowTemplateDetail,
    getWorkflowTemplateDetail,
    createWorkflowTemplate,
    updateWorkflowTemplate,
  } = useWorkflowTemplateController();

  const { roles, fetchRoles } = useRoleController();

  const [name, setName] = useState("");

  const [docType, setDocType] = useState("BudgetPlanApproval");

  const [isActive, setIsActive] = useState(true);

  const [stages, setStages] = useState<StageForm[]>([
    {
      ...EMPTY_STAGE,
      stageOrder: 1,
    },
    {
      ...EMPTY_STAGE,
      stageOrder: 2,
    },
  ]);

  useEffect(() => {
    if (id) {
      getWorkflowTemplateDetail(Number(id));
    }
  }, []);

  useEffect(() => {
    fetchRoles({
      page: 1,
      limit: 100,
    });
  }, [fetchRoles]);

  useEffect(() => {
    if (isEdit && workflowTemplateDetail) {
      setName(workflowTemplateDetail.name);

      setDocType(workflowTemplateDetail.docType);

      setIsActive(workflowTemplateDetail.isActive);

      setStages(
        workflowTemplateDetail.stages.map((s) => ({
          stageOrder: s.stageOrder,
          stageName: s.stageName,
          approverRoles: s.approverRoles,
        })),
      );
    }
  }, [isEdit, workflowTemplateDetail]);

  const submit = async () => {
    const payload = {
      docType,
      name,
      isActive,
      stages,
    };

    if (isEdit) {
      await updateWorkflowTemplate(Number(id), {
        stages,
      });
    } else {
      await createWorkflowTemplate(payload);
    }

    navigate("/workflow-template");
  };

  const changeStageCount = (count: number) => {
    setStages(
      Array.from({ length: count }, (_, index) => ({
        stageOrder: index + 1,
        stageName: "",
        approverRoles: [],
      })),
    );
  };

  return (
    <div className="min-h-screen bg-slate-50 p-4 md:p-6">
      <div className="max-w-7xl mx-auto space-y-6">
        {/* Header */}
        <div className="bg-white rounded-2xl border border-slate-200 shadow-sm p-6">
          <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-4">
            <div>
              <h1 className="text-3xl font-bold text-slate-800">
                {isEdit ? "Edit Workflow Template" : "Create Workflow Template"}
              </h1>

              <p className="text-slate-500 mt-1">
                Configure approval stages and approver roles for this workflow.
              </p>
            </div>

            <Button
              variant="outline"
              id="btn_Back"
              onClick={() => navigate("/workflow-template")}
            >
              Back
            </Button>
          </div>
        </div>

        {/* Basic Information */}
        <div className="bg-white rounded-2xl border border-slate-200 shadow-sm p-6">
          <h2 className="text-lg font-semibold text-slate-800 mb-5">
            Workflow Information
          </h2>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-2">
                Workflow Name
              </label>

              <input
                id="txt_WorkflowName"
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="Enter workflow name"
                className="w-full px-4 py-2.5 border border-slate-300 rounded-xl focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-slate-700 mb-2">
                Document Type
              </label>

              <input
                id="txt_DocType"
                value={docType}
                onChange={(e) => setDocType(e.target.value)}
                className="w-full px-4 py-2.5 border border-slate-300 rounded-xl focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-slate-700 mb-2">
                Number of Stages
              </label>

              <select
                id="cmb_StageCount"
                value={stages.length}
                onChange={(e) => changeStageCount(Number(e.target.value))}
                className="w-full px-4 py-2.5 border border-slate-300 rounded-xl focus:outline-none focus:ring-2 focus:ring-blue-500"
              >
                <option value={2}>2 Stage</option>
                <option value={3}>3 Stage</option>
              </select>
            </div>

            <div>
              <label className="block text-sm font-medium text-slate-700 mb-2">
                Status
              </label>

              <label className="inline-flex items-center gap-3 cursor-pointer">
                <input
                  id="chk_IsActive"
                  type="checkbox"
                  checked={isActive}
                  onChange={(e) => setIsActive(e.target.checked)}
                  className="w-5 h-5"
                />

                <span
                  className={`font-medium ${
                    isActive ? "text-green-600" : "text-red-600"
                  }`}
                >
                  {isActive ? "Active" : "Inactive"}
                </span>
              </label>
            </div>
          </div>
        </div>

        {/* Stages */}
        <div className="space-y-5">
          {stages.map((stage, index) => (
            <div
              key={index}
              className="bg-white rounded-2xl border border-slate-200 shadow-sm overflow-hidden"
            >
              <div className="bg-slate-100 border-b border-slate-200 px-6 py-4">
                <div className="flex items-center gap-3">
                  <span className="w-9 h-9 rounded-full bg-blue-600 text-white flex items-center justify-center font-bold">
                    {stage.stageOrder}
                  </span>

                  <h3 className="font-semibold text-slate-800">
                    Approval Stage {stage.stageOrder}
                  </h3>
                </div>
              </div>

              <div className="p-6">
                <div>
                  <label className="block text-sm font-medium text-slate-700 mb-2">
                    Stage Name
                  </label>

                  <input
                    id={`txt_StageName${index + 1}`}
                    value={stage.stageName}
                    onChange={(e) => {
                      const copy = [...stages];

                      copy[index].stageName = e.target.value;

                      setStages(copy);
                    }}
                    placeholder="Enter stage name"
                    className="w-full px-4 py-2.5 border border-slate-300 rounded-xl focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>

                <div className="mt-6">
                  <h4 className="font-medium text-slate-700 mb-4">
                    Approver Roles
                  </h4>

                  <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
                    {roles.map((role) => {
                      const checked = stage.approverRoles.includes(role.name);

                      return (
                        <label
                          key={role.id}
                          className={`
                            flex items-center gap-3
                            border rounded-xl p-3 cursor-pointer
                            transition
                            ${
                              checked
                                ? "border-blue-500 bg-blue-50"
                                : "border-slate-200 hover:border-slate-300"
                            }
                          `}
                        >
                          <input
                            id={`chk_ApproverRole${index + 1}`}
                            type="checkbox"
                            checked={checked}
                            onChange={(e) => {
                              setStages((prev) =>
                                prev.map((item, i) => {
                                  if (i !== index) {
                                    return item;
                                  }

                                  return {
                                    ...item,
                                    approverRoles: e.target.checked
                                      ? [...item.approverRoles, role.name]
                                      : item.approverRoles.filter(
                                          (r) => r !== role.name,
                                        ),
                                  };
                                }),
                              );
                            }}
                          />

                          <span className="text-sm font-medium text-slate-700">
                            {role.name}
                          </span>
                        </label>
                      );
                    })}
                  </div>
                </div>
              </div>
            </div>
          ))}
        </div>

        {/* Footer Action */}
        <div className="bg-white rounded-2xl border border-slate-200 shadow-sm p-6">
          <div className="flex flex-col sm:flex-row justify-end gap-3">
            <Button
              variant="outline"
              id="btn_CancelWorkflow"
              onClick={() => navigate("/workflow-template")}
            >
              Cancel
            </Button>

            <Button
              variant="primary"
              id="btn_SaveWorkflow"
              onClick={submit}
            >
              {isEdit ? "Update Workflow" : "Create Workflow"}
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
}
