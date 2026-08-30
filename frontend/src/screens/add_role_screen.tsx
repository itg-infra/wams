import { useMemo, useEffect, useState } from "react";
import { Loader2, AlertCircle, CheckCircle2 } from "lucide-react";

import { useRoleStore } from "../store/roleStore";
import { usePermissionStore } from "../store/permissionStore";
import { groupByModule } from "../lib/groupPermission";

import type { CreateRolePayload, Role } from "../types/role.type";

import type { Permission } from "../types/permission.type";

import AlertWidget from "../components/alertWidget";
import { PageHeader } from "../components/ui/page-header";
import { Button } from "../components/ui/button";
import { useNavigate } from "react-router-dom";

interface Props {
  /**
   * Where Back / breadcrumbs go. Master Role renders this screen inline and
   * passes a state-clearing callback; the standalone `/master/roles/form`
   * route passes nothing, so it falls back to navigating to the list. The old
   * default was a no-op, which left a Back button that did nothing.
   */
  onBack?: () => void;
  role?: Role | null;
  isEdit?: boolean;
}

export default function AddRoleScreen({ onBack, role, isEdit = false }: Props) {
  const navigate = useNavigate();
  const goBack = onBack ?? (() => navigate("/master/roles"));
  const {
    isCreating,
    createError,
    createSuccess,
    createRole,
    clearCreateError,
    clearCreateSuccess,

    isUpdating,
    updateError,
    updateSuccess,
    updateRole,
    clearUpdateError,
    clearUpdateSuccess,
  } = useRoleStore();

  const {
    permissions,
    fetchPermissions,
    loading: permissionLoading,
    reset,
  } = usePermissionStore();

  const empty: CreateRolePayload = {
    name: "",
    displayName: "",
    description: "",
    permissionIds: [],
  };

  const [permissionError, setPermissionError] = useState("");

  const initialPermissionIds = role?.permissions?.map((p) => p.id) ?? [];

  const [selectedPermissions, setSelectedPermissions] =
    useState<number[]>(initialPermissionIds);

  // ALERT STATE
  //   const [showAlert, setShowAlert] = useState(false);

  const [errors, setErrors] = useState<
    Partial<Record<keyof CreateRolePayload, string>>
  >({});

  const [form, setForm] = useState<CreateRolePayload>({
    name: role?.name ?? "",
    displayName: role?.displayName ?? "",
    description: role?.description ?? "",
    permissionIds: initialPermissionIds,
  });

  // ─────────────────────────────────────────────────────────────
  // AUTO ALERT LISTENER
  // ─────────────────────────────────────────────────────────────
  const alertData = useMemo(() => {
    if (createError) {
      return {
        type: "error" as const,
        title: "Create Failed",
        message: createError,
      };
    }

    if (updateError) {
      return {
        type: "error" as const,
        title: "Update Failed",
        message: updateError,
      };
    }

    if (createSuccess) {
      return {
        type: "success" as const,
        title: "Success",
        message: createSuccess,
      };
    }

    if (updateSuccess) {
      return {
        type: "success" as const,
        title: "Success",
        message: updateSuccess,
      };
    }

    return null;
  }, [createError, updateError, createSuccess, updateSuccess]);

  // ─────────────────────────────────────────────────────────────
  // FETCH ALL PERMISSIONS
  // ─────────────────────────────────────────────────────────────
  useEffect(() => {
    const loadAllPermissions = async () => {
      reset();

      while (usePermissionStore.getState().hasMore) {
        await usePermissionStore.getState().fetchPermissions();
      }
    };

    loadAllPermissions();
  }, [fetchPermissions, reset]);

  // ─────────────────────────────────────────────────────────────
  // GROUP PERMISSIONS
  // ─────────────────────────────────────────────────────────────
  const groupedPermissions = useMemo(
    () => groupByModule(permissions),
    [permissions],
  );

  // ─────────────────────────────────────────────────────────────
  // HANDLE INPUT
  // ─────────────────────────────────────────────────────────────
  const handleChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>,
  ) => {
    const { name, value } = e.target;

    setForm((p) => ({
      ...p,
      [name]: name === "name" ? value.toUpperCase() : value,
    }));

    if (errors[name as keyof CreateRolePayload]) {
      setErrors((p) => ({
        ...p,
        [name]: undefined,
      }));
    }
  };

  // ─────────────────────────────────────────────────────────────
  // TOGGLE PERMISSION
  // ─────────────────────────────────────────────────────────────
  const togglePermission = (permissionId: number) => {
    setSelectedPermissions((prev) => {
      const exists = prev.includes(permissionId);

      const updated = exists
        ? prev.filter((id) => id !== permissionId)
        : [...prev, permissionId];

      if (updated.length > 0) {
        setPermissionError("");
      }

      return updated;
    });
  };

  // ─────────────────────────────────────────────────────────────
  // TOGGLE MODULE
  // ─────────────────────────────────────────────────────────────
  const toggleModulePermissions = (modulePermissions: Permission[]) => {
    const ids = modulePermissions.map((p) => p.id);

    const allSelected = ids.every((id) => selectedPermissions.includes(id));

    if (allSelected) {
      setSelectedPermissions((prev) => prev.filter((id) => !ids.includes(id)));
    } else {
      setSelectedPermissions((prev) => [...new Set([...prev, ...ids])]);
    }

    setPermissionError("");
  };

  // ─────────────────────────────────────────────────────────────
  // VALIDATE
  // ─────────────────────────────────────────────────────────────
  const validate = () => {
    const e: Partial<Record<keyof CreateRolePayload, string>> = {};

    if (!form.name.trim()) {
      e.name = "Role name is required.";
    }

    if (!form.displayName.trim()) {
      e.displayName = "Display name is required.";
    }

    if (!form.description.trim()) {
      e.description = "Description is required.";
    }

    setErrors(e);

    if (selectedPermissions.length === 0) {
      setPermissionError("At least one permission is required.");
    }

    return Object.keys(e).length === 0 && selectedPermissions.length > 0;
  };

  // ─────────────────────────────────────────────────────────────
  // DRAFT
  // ─────────────────────────────────────────────────────────────
  const handleDraft = () => {
    console.log("Saved as draft", {
      ...form,
      permissionIds: selectedPermissions,
    });
  };

  // ─────────────────────────────────────────────────────────────
  // SUBMIT
  // ─────────────────────────────────────────────────────────────
  const handleSubmit = async () => {
    clearCreateError();
    clearCreateSuccess();

    clearUpdateError();
    clearUpdateSuccess();

    if (!validate()) return;

    let ok = false;

    if (isEdit && role) {
      const payload = {
        displayName: form.displayName,
        description: form.description,
        permissionIds: selectedPermissions,
      };

      ok = await updateRole(role.id, payload);
    } else {
      const payload: CreateRolePayload = {
        name: form.name,
        displayName: form.displayName,
        description: form.description,
        permissionIds: selectedPermissions,
      };

      ok = await createRole(payload);
    }

    if (ok) {
      setForm(empty);

      setSelectedPermissions([]);

      setTimeout(() => {
        clearCreateSuccess();
        clearUpdateSuccess();

        goBack();
      }, 1500);
    }
  };

  const loadingSubmit = isEdit ? isUpdating : isCreating;

  const errorMessage = isEdit ? updateError : createError;

  const successMessage = isEdit ? updateSuccess : createSuccess;

  return (
    <>
      {/* ALERT */}
      {alertData && (
        <AlertWidget
          show={true}
          type={alertData.type}
          title={alertData.title}
          message={alertData.message}
          onClose={() => {
            clearCreateError();
            clearCreateSuccess();
            clearUpdateError();
            clearUpdateSuccess();
          }}
        />
      )}

      <div className="flex-1 p-4 sm:p-6 lg:p-8 overflow-y-auto">
        <PageHeader
          breadcrumbs={[
            { label: "Dashboard", onClick: goBack },
            { label: "Master Data" },
            { label: "Master Role", onClick: goBack },
            { label: isEdit ? "Edit Role" : "Add Role" },
          ]}
          title={isEdit ? "Form Edit Role" : "Form Add Role"}
          onBack={goBack}
        />

        {/* ── Success Banner ── */}
        {successMessage && (
          <div className="flex items-center gap-3 bg-green-50 border border-green-200 text-green-700 px-4 py-3 rounded-xl text-sm mb-5">
            <CheckCircle2 className="w-4 h-4 shrink-0" />

            <span className="flex-1">{successMessage}</span>
          </div>
        )}

        {/* ── Error Banner ── */}
        {errorMessage && (
          <div className="flex items-center gap-3 bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-xl text-sm mb-5">
            <AlertCircle className="w-4 h-4 shrink-0" />

            <span className="flex-1">{errorMessage}</span>
          </div>
        )}

        {/* ── Form Card ── */}
        <div className="bg-white rounded-xl border border-gray-200 p-6 mb-6">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-5">
            {/* Name */}
            <div className="flex flex-col gap-1">
              <label className="text-sm font-medium text-gray-700">
                Role Name
              </label>

              <input
                id="txt_RoleName"
                name="name"
                type="text"
                value={form.name}
                onChange={handleChange}
                placeholder="e.g. VISITOR"
                className={`w-full px-3.5 py-2.5 rounded-lg border text-sm outline-none bg-white transition ${
                  errors.name
                    ? "border-red-400 focus:ring-2 focus:ring-red-100"
                    : "border-gray-200 focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100"
                }`}
              />

              {errors.name && (
                <p className="text-xs text-red-500">{errors.name}</p>
              )}
            </div>

            {/* Display Name */}
            <div className="flex flex-col gap-1">
              <label className="text-sm font-medium text-gray-700">
                Display Name
              </label>

              <input
                id="txt_RoleDisplayName"
                name="displayName"
                type="text"
                value={form.displayName}
                onChange={handleChange}
                placeholder="e.g. Visitor"
                className={`w-full px-3.5 py-2.5 rounded-lg border text-sm outline-none bg-white transition ${
                  errors.displayName
                    ? "border-red-400 focus:ring-2 focus:ring-red-100"
                    : "border-gray-200 focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100"
                }`}
              />

              {errors.displayName && (
                <p className="text-xs text-red-500">{errors.displayName}</p>
              )}
            </div>

            {/* Description */}
            <div className="flex flex-col gap-1 sm:col-span-2">
              <label className="text-sm font-medium text-gray-700">
                Description
              </label>

              <textarea
                id="txt_RoleDescription"
                name="description"
                value={form.description}
                onChange={handleChange}
                placeholder="e.g. Visitor to the facility and granted limited access"
                rows={3}
                className={`w-full px-3.5 py-2.5 rounded-lg border text-sm outline-none bg-white transition resize-none ${
                  errors.description
                    ? "border-red-400 focus:ring-2 focus:ring-red-100"
                    : "border-gray-200 focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100"
                }`}
              />

              {errors.description && (
                <p className="text-xs text-red-500">{errors.description}</p>
              )}
            </div>
          </div>
        </div>

        {/* ── Permission Section ── */}
        <div className="bg-white rounded-xl border border-gray-200 p-6 mb-6">
          <div className="flex items-center justify-between mb-5">
            <div>
              <h2 className="text-base font-semibold text-gray-900">
                Permissions
              </h2>

              <p className="text-sm text-gray-500 mt-1">
                Select permissions for this role.
              </p>
            </div>

            <div className="text-sm text-indigo-700 font-medium">
              {selectedPermissions.length} Selected
            </div>
          </div>

          {permissionError && (
            <div className="mb-4 text-sm text-red-500">{permissionError}</div>
          )}

          {permissionLoading && permissions.length === 0 ? (
            <div className="py-8 flex items-center justify-center text-gray-500">
              <Loader2 className="w-5 h-5 animate-spin mr-2" />
              Loading permissions...
            </div>
          ) : (
            <div className="space-y-5">
              {Object.entries(groupedPermissions).map(([module, perms]) => {
                const allSelected = perms.every((p) =>
                  selectedPermissions.includes(p.id),
                );

                return (
                  <div
                    key={module}
                    className="border border-gray-200 rounded-xl overflow-hidden"
                  >
                    {/* Module Header */}
                    <div className="bg-gray-50 px-4 py-3 flex items-center justify-between">
                      <div>
                        <h3 className="font-semibold text-gray-800 text-sm capitalize">
                          {module}
                        </h3>

                        <p className="text-xs text-gray-500 mt-0.5">
                          {perms.length} permissions
                        </p>
                      </div>

                      <button
                        id="btn_ToggleModulePermissions"
                        type="button"
                        onClick={() => toggleModulePermissions(perms)}
                        className={`text-xs px-3 py-1.5 rounded-lg border transition ${
                          allSelected
                            ? "bg-indigo-900 text-white border-indigo-900"
                            : "bg-white border-gray-300 text-gray-700 hover:border-indigo-500"
                        }`}
                      >
                        {allSelected ? "Unselect All" : "Select All"}
                      </button>
                    </div>

                    {/* Permission Items */}
                    <div className="p-4 grid grid-cols-1 md:grid-cols-2 gap-3">
                      {perms.map((perm) => {
                        const checked = selectedPermissions.includes(perm.id);

                        return (
                          <label
                            key={perm.id}
                            className={`border rounded-xl p-4 cursor-pointer transition flex items-start gap-3 ${
                              checked
                                ? "border-indigo-500 bg-indigo-50"
                                : "border-gray-200 hover:border-indigo-300"
                            }`}
                          >
                            <input
                              id="chk_Permission"
                              type="checkbox"
                              checked={checked}
                              onChange={() => togglePermission(perm.id)}
                              className="mt-1 w-4 h-4 accent-indigo-700"
                            />

                            <div className="flex-1">
                              <div className="text-sm font-medium text-gray-800">
                                {perm.resource} - {perm.action}
                              </div>

                              <div className="text-xs text-gray-500 mt-1">
                                {perm.description}
                              </div>
                            </div>
                          </label>
                        );
                      })}
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </div>

        {/* ── Action Buttons ── */}
        <div className="flex items-center justify-end gap-3">
          <Button
            id="btn_DraftRole"
            variant="secondary"
            onClick={handleDraft}
            disabled={loadingSubmit}
            className="px-8 py-2.5"
          >
            Draft
          </Button>

          <Button
            id="btn_SaveRole"
            variant="primary"
            onClick={handleSubmit}
            disabled={loadingSubmit}
            className="px-8 py-2.5"
          >
            {loadingSubmit ? (
              <>
                <Loader2 className="w-4 h-4 animate-spin" />
                {isEdit ? "Updating..." : "Submitting..."}
              </>
            ) : isEdit ? (
              "Update"
            ) : (
              "Submit"
            )}
          </Button>
        </div>
      </div>
    </>
  );
}
