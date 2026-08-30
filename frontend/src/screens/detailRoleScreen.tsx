"use client";

import { useMemo } from "react";
import { useNavigate } from "react-router-dom";
import { ShieldCheck, CheckCircle2 } from "lucide-react";

import { PageHeader } from "../components/ui/page-header";
import { groupByModule } from "../lib/groupPermission";

import type { Role } from "../types/role.type";

interface Props {
  role: Role;
  /**
   * Called when the user presses Back. The list renders this screen inline
   * without changing the URL, so `navigate(-1)` would leave Master Role
   * entirely instead of returning to it — the caller has to clear the state
   * that opened the detail.
   */
  onBack?: () => void;
}

export default function DetailRoleScreen({ role, onBack }: Props) {
  const navigate = useNavigate();

  // ================= ASSIGNED ONLY =================
  const assignedPermissions = useMemo(() => {
    return role.permissions ?? [];
  }, [role.permissions]);

  // ================= GROUP ASSIGNED =================
  const assignedGrouped = useMemo(() => {
    return groupByModule(assignedPermissions);
  }, [assignedPermissions]);

  return (
    <div className="flex-1 p-4 sm:p-6 lg:p-8 overflow-y-auto space-y-6">
      {/* ================= HEADER ================= */}
      <PageHeader title="Detail Role" onBack={onBack ?? (() => navigate(-1))} />

      {/* ================= ROLE INFO ================= */}
      <div className="bg-white border border-gray-200 rounded-2xl p-6">
        <div className="grid grid-cols-1 md:grid-cols-3 gap-5">
          <div>
            <p className="text-xs font-medium text-gray-500 uppercase tracking-wide">
              Role Name
            </p>

            <p className="mt-1 text-sm font-semibold text-gray-900">
              {role.name}
            </p>
          </div>

          <div>
            <p className="text-xs font-medium text-gray-500 uppercase tracking-wide">
              Display Name
            </p>

            <p className="mt-1 text-sm font-semibold text-gray-900">
              {role.displayName ?? "-"}
            </p>
          </div>

          <div>
            <p className="text-xs font-medium text-gray-500 uppercase tracking-wide">
              Description
            </p>

            <p className="mt-1 text-sm text-gray-700">
              {role.description ?? "-"}
            </p>
          </div>
        </div>
      </div>

      {/* ================= ASSIGNED PERMISSIONS ================= */}
      <div className="bg-white border border-gray-200 rounded-2xl p-6">
        <div className="flex items-center justify-between mb-5">
          <div>
            <h2 className="text-lg font-semibold text-gray-900">
              Granted Permissions
            </h2>

            <p className="text-sm text-gray-500 mt-1">
              List of permissions granted to this role.
            </p>
          </div>

          <div className="flex items-center gap-2 text-sm font-medium text-indigo-700">
            <ShieldCheck className="w-4 h-4" />
            {assignedPermissions.length} Permissions
          </div>
        </div>

        {assignedPermissions.length === 0 ? (
          <div className="border border-dashed border-gray-300 rounded-xl p-8 text-center">
            <p className="text-sm text-gray-500">
              No permissions assigned yet.
            </p>
          </div>
        ) : (
          <div className="space-y-5">
            {Object.entries(assignedGrouped).map(([module, perms]) => (
              <div
                key={module}
                className="border border-gray-200 rounded-xl overflow-hidden"
              >
                {/* MODULE HEADER */}
                <div className="bg-green-50 border-b border-green-100 px-4 py-3">
                  <div className="flex items-center justify-between">
                    <div>
                      <h3 className="text-sm font-semibold text-gray-900 capitalize">
                        {module}
                      </h3>

                      <p className="text-xs text-gray-500 mt-0.5">
                        {perms.length} granted permissions
                      </p>
                    </div>

                    <div className="flex items-center gap-1 text-green-700 text-xs font-medium">
                      <CheckCircle2 className="w-4 h-4" />
                      Granted
                    </div>
                  </div>
                </div>

                {/* PERMISSIONS */}
                <div className="p-4 grid grid-cols-1 md:grid-cols-2 gap-3">
                  {perms.map((perm) => (
                    <div
                      key={perm.id}
                      className="border border-green-200 bg-green-50 rounded-xl p-4"
                    >
                      <div className="flex items-start justify-between gap-3">
                        <div>
                          <p className="text-sm font-semibold text-gray-900">
                            {perm.resource} - {perm.action}
                          </p>

                          <p className="text-xs text-gray-500 mt-1">
                            {perm.description}
                          </p>
                        </div>

                        <div className="flex items-center gap-1 text-green-700 text-xs font-medium whitespace-nowrap">
                          <CheckCircle2 className="w-4 h-4" />
                          Granted
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}