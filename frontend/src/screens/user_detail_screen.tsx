import { useEffect, useState } from "react";
import { Loader2, AlertCircle, Building2, MapPinned, ShieldCheck, X } from "lucide-react";
import { PageHeader } from "../components/ui/page-header";
import { Button } from "../components/ui/button";
import { userService } from "../api/services/masterData/userService";
import type { User } from "../types/users.types";
import { useRoleStore } from "../store/roleStore";
import { useUserStore } from "../store/userStore";
import { useWarehouseStore } from "../store/warehouseStore";
import { useCompanyStore } from "../store/companyStore";
import { useAuthStore } from "../store/authStore";

interface UserDetailScreenProps {
  userId: number;
  onBack: () => void;
}

export default function UserDetailScreen({
  userId,
  onBack,
}: UserDetailScreenProps) {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [roleOpen, setRoleOpen] = useState(false);
  const [warehouseOpen, setWarehouseOpen] = useState(false);
  const [warehouseSearch, setWarehouseSearch] = useState("");
  const [membershipCompanyId, setMembershipCompanyId] = useState("");
  const [membershipDialogOpen, setMembershipDialogOpen] = useState(false);
  const [copyRoles, setCopyRoles] = useState(true);
  const [copyProvinces, setCopyProvinces] = useState(false);
  const [isAddingMembership, setIsAddingMembership] = useState(false);

  const {
    warehouses,
    fetchWarehouses,
    isLoading: isLoadingWarehouse,
  } = useWarehouseStore();
  const { companies, fetchCompanies } = useCompanyStore();
  const actingCompanyId = Number(useAuthStore((state) => state.user?.companyId));

  const { roles, fetchRoles } = useRoleStore();
  const {
    assignRoleToUser,
    removeRoleFromUser,
    isRemovingRole,
    assignWarehouseToUser,
    removeWarehouseFromUser,
    isRemovingWarehouse,
    memberships,
    isLoadingMemberships,
    membershipError,
    fetchMemberships,
    addMembership,
    removeMembership,
  } = useUserStore();

  const fetchDetail = async () => {
    try {
      setIsLoading(true);
      setError(null);

      const response = await userService.getUserById(userId);

      if (!response.success) {
        setError(response.message ?? "Failed to load user detail.");
        return;
      }

      setUser(response.data);
    } catch (err: unknown) {
      const e = err as { response?: { data?: { message?: string } } };
      setError(e?.response?.data?.message ?? "Failed to load user detail.");
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchDetail();
  }, [userId]);

  useEffect(() => {
    fetchRoles({ page: 1, limit: 100 });
  }, [fetchRoles]);

  useEffect(() => {
    fetchWarehouses({ page: 1, search: warehouseSearch });
  }, [fetchWarehouses, warehouseSearch]);

  useEffect(() => {
    fetchMemberships(userId);
  }, [fetchMemberships, userId]);

  useEffect(() => {
    fetchCompanies();
  }, [fetchCompanies]);

  const currentRole = user?.roles?.[0] ?? null;
  const hasGlobalAccess = user?.roles?.some(
    (role) => role.roleName === "SUPER_ADMIN",
  ) ?? false;

  const companyName = user?.warehouses?.length
    ? user.warehouses[0].name
    : "No Company";

  const fullName = user?.fullname ?? "-";

  const sourceMembership = memberships.find(
    (membership) => membership.companyId === actingCompanyId,
  );
  const selectedMembershipCompany = companies.find(
    (company) => company.id === Number(membershipCompanyId),
  );
  const copyableRoleIds = (sourceMembership?.roles ?? [])
    .filter((role) => role.roleName !== "SUPER_ADMIN")
    .map((role) => role.roleId);
  const copyableProvinceIds = (sourceMembership?.scopes ?? []).map(
    (scope) => scope.provinceId,
  );

  const closeMembershipDialog = () => {
    if (isAddingMembership) return;
    setMembershipDialogOpen(false);
    setCopyRoles(true);
    setCopyProvinces(false);
  };

  const confirmMembership = async () => {
    if (!selectedMembershipCompany) return;
    setIsAddingMembership(true);
    const ok = await addMembership(userId, selectedMembershipCompany.id, {
      roleIds: copyRoles ? copyableRoleIds : [],
      provinceIds: copyProvinces ? copyableProvinceIds : [],
    });
    setIsAddingMembership(false);
    if (!ok) return;
    setMembershipCompanyId("");
    setMembershipDialogOpen(false);
    setCopyRoles(true);
    setCopyProvinces(false);
  };

  return (
    <div
      id="lbl_UserDetail"
      className="flex-1 p-4 sm:p-6 lg:p-8 overflow-y-auto"
    >
      <PageHeader
        breadcrumbs={[
          { label: "Dashboard", onClick: onBack },
          { label: "Master Data" },
          { label: "Detail User" },
        ]}
        title="Detail User Information"
        onBack={onBack}
      />

      {/* ── Loading ── */}
      {isLoading && (
        <div className="flex items-center justify-center py-20 text-gray-500 gap-3">
          <Loader2 className="w-5 h-5 animate-spin" />
          <span className="text-sm">Loading user detail...</span>
        </div>
      )}

      {/* ── Error ── */}
      {!isLoading && error && (
        <div className="flex items-center gap-3 bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-xl text-sm">
          <AlertCircle className="w-4 h-4 shrink-0" />
          <span className="flex-1">{error}</span>
          <Button
            id="btn_RetryUserDetail"
            variant="ghost"
            size="xs"
            onClick={fetchDetail}
            className="text-red-500 hover:text-red-700 hover:bg-transparent font-medium"
          >
            Retry
          </Button>
        </div>
      )}

      {/* ── Card ── */}
      {!isLoading && !error && user && (
        <div className="bg-[#dfe4ea] rounded-2xl px-6 py-5 sm:px-8 sm:py-6">
          <div className="flex flex-col sm:flex-row sm:items-center gap-6">
            {/* Avatar */}
            <div className="shrink-0 flex justify-center sm:block">
              <div className="w-20 h-20 sm:w-24 sm:h-24 rounded-full bg-gray-300 flex items-center justify-center border-2 border-white shadow-sm">
                <span className="text-xs text-gray-500">PHOTO</span>
              </div>
            </div>

            {/* Info */}
            <div className="flex-1 grid grid-cols-1 sm:grid-cols-2 gap-x-10 gap-y-4">
              {/* Name */}
              <div>
                <p className="text-xs text-gray-500 mb-0.5">Name</p>
                <p className="text-base font-bold text-gray-900">{fullName}</p>
              </div>

              {/* ✅ ROLE (SINGLE) */}
              <div className="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-4 sm:gap-4">
                <div className="w-full">
                  <p className="text-xs text-gray-500 mb-1">Role</p>

                  <div className="flex flex-wrap items-center gap-2 mb-2">
                    {/* Role Pill */}
                    {currentRole ? (
                      <div className="flex items-center gap-2 bg-indigo-100 text-indigo-700 text-xs font-medium px-3 py-1 rounded-lg">
                        <span>{currentRole.displayName}</span>

                        <button
                          id="icn_RemoveRole"
                          onClick={async () => {
                            await removeRoleFromUser(
                              userId,
                              currentRole.roleId,
                            );
                            fetchDetail();
                          }}
                          disabled={isRemovingRole}
                          className="text-indigo-400 hover:text-red-500 transition"
                        >
                          {isRemovingRole ? (
                            <Loader2 className="w-3 h-3 animate-spin" />
                          ) : (
                            <span className="text-xs">✕</span>
                          )}
                        </button>
                      </div>
                    ) : (
                      <span className="text-gray-400 text-xs">No Role</span>
                    )}

                    {/* Change button */}
                    <button
                      id="btn_ChangeRole"
                      onClick={() => setRoleOpen((v) => !v)}
                      className="text-xs px-2.5 py-1 border border-gray-300 rounded-md bg-white hover:bg-gray-50 transition"
                    >
                      Change
                    </button>
                  </div>

                  {/* Dropdown */}
                  {roleOpen && (
                    <div className="relative">
                      <div
                        id="lsb_RoleOptions"
                        className="absolute z-20 mt-1 w-full sm:w-56 bg-white border border-gray-200 rounded-xl shadow-lg text-sm overflow-hidden"
                      >
                        {/* Header */}
                        <div className="px-3 py-2 text-xs text-gray-400 border-b">
                          Select Role
                        </div>

                        {/* Role List */}
                        <div className="max-h-48 overflow-y-auto">
                          {roles.map((role) => {
                            const isActive = currentRole?.roleId === role.id;

                            return (
                              <div
                                key={role.id}
                                onClick={async () => {
                                  if (isActive) return;

                                  await assignRoleToUser(userId, role.id);
                                  setRoleOpen(false);
                                  fetchDetail();
                                }}
                                className={`flex items-center justify-between px-3 py-2 cursor-pointer transition
                                        ${
                                          isActive
                                            ? "bg-indigo-50 text-indigo-700 font-medium"
                                            : "hover:bg-gray-50 text-gray-700"
                                        }
                                    `}
                              >
                                <span>{role.name}</span>

                                {isActive && (
                                  <span className="text-xs text-indigo-500">
                                    Current
                                  </span>
                                )}
                              </div>
                            );
                          })}
                        </div>
                      </div>
                    </div>
                  )}
                </div>

                {/* Button kanan tetap */}
                <Button
                  id="btn_DeactivateAccount"
                  variant="destructive"
                  size="sm"
                  className="w-full sm:w-auto font-semibold text-xs whitespace-nowrap"
                >
                  Deactive Account
                </Button>
              </div>

              {/* Username */}
              <div>
                <p className="text-xs text-gray-500 mb-0.5">Username</p>
                <p className="text-base font-bold text-gray-900">{fullName}</p>
              </div>

              {/* Company */}
              <div>
                <p className="text-xs text-gray-500 mb-0.5">Company</p>
                <p className="text-base font-bold text-gray-900">
                  {companyName}
                </p>
              </div>

              {/* Warehouse */}
              <div className="col-span-1 sm:col-span-2">
                <p className="text-xs text-gray-500 mb-1">Warehouse</p>

                {/* Selected Warehouses */}
                <div className="flex flex-wrap gap-2 mb-2">
                  {user?.warehouses?.length ? (
                    user.warehouses.map((wh) => (
                      <div
                        key={wh.warehouseId}
                        className="flex items-center gap-2 bg-indigo-100 text-indigo-700 text-xs font-medium px-3 py-1 rounded-lg"
                      >
                        <span>{wh.name}</span>

                        <button
                          id="icn_RemoveWarehouse"
                          onClick={async () => {
                            await removeWarehouseFromUser(
                              userId,
                              wh.warehouseId,
                            );
                            fetchDetail();
                          }}
                          className="text-indigo-400 hover:text-red-500"
                        >
                          {isRemovingWarehouse ? (
                            <Loader2 className="w-3 h-3 animate-spin" />
                          ) : (
                            "✕"
                          )}
                        </button>
                      </div>
                    ))
                  ) : (
                    <span className="text-gray-400 text-xs">No Warehouse</span>
                  )}
                </div>

                {/* Button */}
                <button
                  id="btn_ManageWarehouse"
                  onClick={() => setWarehouseOpen((v) => !v)}
                  className="text-xs px-3 py-1.5 border border-gray-300 rounded-md bg-white hover:bg-gray-50"
                >
                  Manage Warehouse
                </button>

                {/* Dropdown Panel */}
                {warehouseOpen && (
                  <div className="relative">
                    <div
                      id="lsb_WarehouseOptions"
                      className="absolute left-0 right-0 sm:right-auto z-30 mt-2 w-full sm:w-80 bg-white border border-gray-200 rounded-xl shadow-lg p-3"
                    >
                      {/* Search */}
                      <input
                        id="txt_SearchWarehouse"
                        type="text"
                        placeholder="Search warehouse..."
                        value={warehouseSearch}
                        onChange={(e) => setWarehouseSearch(e.target.value)}
                        className="w-full mb-2 px-3 py-2 text-sm border border-gray-200 rounded-lg outline-none focus:ring-2 focus:ring-indigo-100"
                      />

                      {/* List */}
                      <div className="max-h-60 overflow-y-auto">
                        {isLoadingWarehouse ? (
                          <div className="flex items-center justify-center py-6 text-gray-400">
                            <Loader2 className="w-4 h-4 animate-spin" />
                          </div>
                        ) : (
                          warehouses.map((wh) => {
                            const isSelected = user?.warehouses?.some(
                              (uwh) => uwh.warehouseId === wh.id,
                            );

                            return (
                              <div
                                key={wh.id}
                                onClick={async () => {
                                  if (isSelected) {
                                    await removeWarehouseFromUser(
                                      userId,
                                      wh.id,
                                    );
                                  } else {
                                    await assignWarehouseToUser(userId, wh.id);
                                  }
                                  fetchDetail();
                                }}
                                className={`flex items-center justify-between px-3 py-2 rounded-lg cursor-pointer transition
                                        ${
                                          isSelected
                                            ? "bg-indigo-50 text-indigo-700"
                                            : "hover:bg-gray-50"
                                        }`}
                              >
                                <div>
                                  <p className="text-sm font-medium">
                                    {wh.name}
                                  </p>
                                  <p className="text-xs text-gray-400">
                                    {wh.code}
                                  </p>
                                </div>

                                {isSelected && (
                                  <span className="text-xs text-indigo-500">
                                    ✓
                                  </span>
                                )}
                              </div>
                            );
                          })
                        )}
                      </div>
                    </div>
                  </div>
                )}
              </div>
            </div>
          </div>

          <div className="mt-6 border-t border-white/70 pt-5">
            {hasGlobalAccess ? (
              <div
                id="lbl_GlobalCompanyAccess"
                className="flex items-start gap-3 rounded-xl border border-indigo-200 bg-indigo-50 px-4 py-3"
              >
                <ShieldCheck className="mt-0.5 h-5 w-5 shrink-0 text-indigo-600" />
                <div>
                  <p className="text-sm font-semibold text-indigo-950">
                    Access to all active companies
                  </p>
                  <p className="mt-1 text-xs leading-5 text-indigo-700">
                    Super Administrators have global company access automatically.
                  </p>
                </div>
              </div>
            ) : (
              <>
            <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
              <div>
                <p className="flex items-center gap-2 text-sm font-semibold text-gray-800">
                  <Building2 className="h-4 w-4 text-indigo-600" /> Company Access
                </p>
              </div>
              <div className="flex gap-2">
                <select
                  id="txt_MembershipCompanyId"
                  value={membershipCompanyId}
                  onChange={(event) => setMembershipCompanyId(event.target.value)}
                  className="w-40 rounded-lg border border-gray-300 bg-white px-3 py-2 text-xs outline-none focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100"
                >
                  <option value="">Select company</option>
                  {companies
                    .filter((company) => !memberships.some((membership) => membership.companyId === company.id))
                    .map((company) => (
                      <option key={company.id} value={company.id}>
                        {company.code} · {company.name}
                      </option>
                    ))}
                </select>
                <Button
                  id="btn_AddMembership"
                  size="sm"
                  disabled={isLoadingMemberships || !/^\d+$/.test(membershipCompanyId)}
                  onClick={() => setMembershipDialogOpen(true)}
                >
                  Add Access
                </Button>
              </div>
            </div>

            {membershipError && (
              <div className="mt-3 flex items-center gap-2 rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-xs text-red-700">
                <AlertCircle className="h-4 w-4 shrink-0" /> {membershipError}
              </div>
            )}

            <div className="mt-3 flex flex-col gap-2">
              {isLoadingMemberships ? (
                <div className="flex items-center gap-2 py-3 text-xs text-gray-500">
                  <Loader2 className="h-4 w-4 animate-spin" /> Loading...
                </div>
              ) : memberships.length > 0 ? (
                memberships.map((membership) => (
                  <div
                    key={membership.id}
                    className="flex items-center justify-between rounded-xl border border-gray-200 bg-white px-3 py-2.5"
                  >
                    <div>
                      <p className="text-sm font-medium text-gray-800">
                        {membership.companyName}
                      </p>
                      <p className="text-xs text-gray-400">
                        {membership.companyCode || `Company #${membership.companyId}`}
                      </p>
                    </div>
                    <button
                      id={`btn_RemoveMembership_${membership.companyId}`}
                      type="button"
                      className="rounded-md px-2 py-1 text-xs font-medium text-red-600 hover:bg-red-50 disabled:opacity-50"
                      disabled={isLoadingMemberships}
                      onClick={() => removeMembership(userId, membership.companyId)}
                    >
                      Remove access
                    </button>
                  </div>
                ))
              ) : (
                <p className="py-3 text-xs text-gray-400">No company access found.</p>
              )}
            </div>
              </>
            )}
          </div>
        </div>
      )}

      {!hasGlobalAccess && membershipDialogOpen && selectedMembershipCompany && (
        <div
          id="lbl_AddMembershipDialog"
          className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/45 px-4 backdrop-blur-sm"
          role="dialog"
          aria-modal="true"
          aria-labelledby="lbl_AddMembershipTitle"
        >
          <div className="w-full max-w-lg overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-2xl shadow-slate-950/20">
            <div className="flex items-start justify-between border-b border-slate-100 px-6 py-5">
              <div>
                <p className="text-xs font-semibold uppercase tracking-[0.16em] text-indigo-600">
                  Company access
                </p>
                <h2 id="lbl_AddMembershipTitle" className="mt-1 text-lg font-bold text-slate-900">
                  Add {fullName} to {selectedMembershipCompany.name}
                </h2>
                <p className="mt-1 text-sm text-slate-500">
                  Choose which safe assignments to carry into the new Company Access.
                </p>
              </div>
              <button
                id="icn_CloseMembershipDialog"
                type="button"
                onClick={closeMembershipDialog}
                disabled={isAddingMembership}
                className="rounded-lg p-2 text-slate-400 transition hover:bg-slate-100 hover:text-slate-700 disabled:opacity-50"
                aria-label="Close"
              >
                <X className="h-4 w-4" />
              </button>
            </div>

            <div className="space-y-3 px-6 py-5">
              {membershipError && (
                <div className="flex items-start gap-2 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
                  <AlertCircle className="mt-0.5 h-4 w-4 shrink-0" />
                  <span>{membershipError}</span>
                </div>
              )}

              <label className="flex cursor-pointer items-start gap-3 rounded-xl border border-slate-200 p-4 transition hover:border-indigo-200 hover:bg-indigo-50/40">
                <input
                  id="chk_CopyMembershipRoles"
                  type="checkbox"
                  checked={copyRoles}
                  onChange={(event) => setCopyRoles(event.target.checked)}
                  className="mt-1 h-4 w-4 accent-indigo-600"
                />
                <ShieldCheck className="mt-0.5 h-5 w-5 shrink-0 text-indigo-600" />
                <span>
                  <span className="block text-sm font-semibold text-slate-800">
                    Copy roles from current company
                  </span>
                  <span className="mt-1 block text-xs leading-5 text-slate-500">
                    {copyableRoleIds.length > 0
                      ? `${copyableRoleIds.length} global role${copyableRoleIds.length === 1 ? "" : "s"} will be reused.`
                      : "No reusable company roles are currently assigned."}
                  </span>
                </span>
              </label>

              <label className="flex cursor-pointer items-start gap-3 rounded-xl border border-slate-200 p-4 transition hover:border-emerald-200 hover:bg-emerald-50/40">
                <input
                  id="chk_CopyMembershipProvinces"
                  type="checkbox"
                  checked={copyProvinces}
                  onChange={(event) => setCopyProvinces(event.target.checked)}
                  className="mt-1 h-4 w-4 accent-emerald-600"
                />
                <MapPinned className="mt-0.5 h-5 w-5 shrink-0 text-emerald-600" />
                <span>
                  <span className="block text-sm font-semibold text-slate-800">
                    Copy province scopes
                  </span>
                  <span className="mt-1 block text-xs leading-5 text-slate-500">
                    {copyableProvinceIds.length > 0
                      ? `${copyableProvinceIds.length} province scope${copyableProvinceIds.length === 1 ? "" : "s"} will be copied.`
                      : "No province scopes are currently assigned."}
                  </span>
                </span>
              </label>
            </div>

            <div className="flex justify-end gap-3 border-t border-slate-100 bg-slate-50/80 px-6 py-4">
              <Button
                id="btn_CancelAddMembership"
                type="button"
                variant="outline"
                onClick={closeMembershipDialog}
                disabled={isAddingMembership}
              >
                Cancel
              </Button>
              <Button
                id="btn_ConfirmAddMembership"
                type="button"
                onClick={confirmMembership}
                disabled={isAddingMembership}
              >
                {isAddingMembership && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                Add Access
              </Button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
