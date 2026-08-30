import { useEffect, useState } from "react";
import {
  Loader2,
  AlertCircle,
  X,
  Pencil,
  Trash2,
  AlertTriangle,
  Eye,
} from "lucide-react";
import { useRoleController } from "../controllers/masterData/roleController";
import { useRoleStore } from "../store/roleStore";
import type { Role, RoleSortBy } from "../types/role.type";
import AddRoleScreen from "./add_role_screen";
import PermissionGuard from "../components/guards/permissionGuard";
import DetailRoleScreen from "./detailRoleScreen";
import { useNavigate } from "react-router-dom";
import { DataTable, TablePagination, type Column } from "../components/ui/table";
import { PageHeader, lastUpdatedLabel } from "../components/ui/page-header";
import { Button } from "../components/ui/button";
import { Toolbar } from "../components/ui/toolbar";


function DeleteRoleDialog({
  role,
  onClose,
}: {
  role: Role | null;
  onClose: () => void;
}) {
  const { isDeleting, deleteError, deleteRole } = useRoleStore();

  const handleConfirm = async () => {
    if (!role) return;
    const ok = await deleteRole(role.id);
    if (ok) onClose();
  };

  if (!role) return null;

  return (
    <div
      id="lbl_DeleteRoleDialog"
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-sm"
    >
      <div className="bg-white rounded-2xl shadow-xl w-full max-w-sm mx-4 p-6">
        <div className="flex justify-center mb-4">
          <div className="w-14 h-14 bg-red-50 rounded-full flex items-center justify-center">
            <AlertTriangle className="w-7 h-7 text-red-500" />
          </div>
        </div>

        <h3 className="text-center text-lg font-semibold text-gray-800 mb-1">
          Delete Role
        </h3>

        <p className="text-center text-sm text-gray-400 mb-2">
          Are you sure you want to delete{" "}
          <span className="font-semibold text-gray-700">
            {role.displayName ?? role.name}
          </span>
          ?
        </p>

        <p className="text-center text-xs text-red-400 mb-6">
          This action cannot be undone.
        </p>

        {deleteError && (
          <div className="flex items-center gap-2 bg-red-50 border border-red-200 text-red-600 px-4 py-3 rounded-xl text-sm mb-4">
            <AlertCircle className="w-4 h-4 shrink-0" />
            {deleteError}
          </div>
        )}

        <div className="flex gap-3">
          <Button
            id="btn_CancelDeleteRole"
            variant="outline"
            onClick={onClose}
            disabled={isDeleting}
            className="flex-1"
          >
            Cancel
          </Button>
          <button
            id="btn_ConfirmDeleteRole"
            onClick={handleConfirm}
            disabled={isDeleting}
            className="flex-1 py-2.5 rounded-xl bg-red-500 hover:bg-red-600 text-sm font-medium text-white transition disabled:opacity-50 flex items-center justify-center gap-2"
          >
            {isDeleting ? (
              <>
                <Loader2 className="w-4 h-4 animate-spin" />
                Deleting...
              </>
            ) : (
              "Delete"
            )}
          </button>
        </div>
      </div>
    </div>
  );
}

function MobileRoleCard({
  role,
  index,
  onView,
  onEdit,
  onDelete,
}: {
  role: Role;
  index: number;
  onView: (r: Role) => void;
  onEdit: (r: Role) => void;
  onDelete: (r: Role) => void;
}) {
  return (
    <div
      className={`flex items-center justify-between px-4 py-3.5 border-b border-gray-200 last:border-b-0 ${
        index % 2 === 1 ? "bg-[#eef0f8]" : "bg-white"
      }`}
    >
      <div className="flex-1 min-w-0">
        <p className="text-sm text-gray-800 truncate">{role.name}</p>
        <p className="text-xs text-gray-500 mt-0.5">
          {role.displayName ?? "—"}
        </p>
      </div>
      <div className="flex items-center gap-3 shrink-0 ml-4">
        <button
          id="icn_ViewRoleMobile"
          onClick={() => onView(role)}
          className="text-gray-400 hover:text-indigo-600 transition"
        >
          <Eye className="w-4 h-4" />
        </button>
        <button
          id="icn_EditRoleMobile"
          onClick={() => onEdit(role)}
          className="text-gray-400 hover:text-indigo-600 transition"
        >
          <Pencil className="w-4 h-4" />
        </button>
        <button
          id="icn_DeleteRoleMobile"
          onClick={() => onDelete(role)}
          className="text-gray-400 hover:text-red-500 transition"
        >
          <Trash2 className="w-4 h-4" />
        </button>
      </div>
    </div>
  );
}

// Values map to `RoleListParams["sortBy"]` + sort order. The list is paginated
// by the API, so these go to the server rather than reordering the rows in hand.
const SORT_OPTIONS = [
  { label: "Name A-Z", value: "name:asc" },
  { label: "Name Z-A", value: "name:desc" },
  { label: "Display Name A-Z", value: "displayName:asc" },
  { label: "Display Name Z-A", value: "displayName:desc" },
];

export default function RolesScreen() {
  const {
    roles,
    meta,
    isLoading,
    error,
    params,
    isEmpty,
    clearDeleteError,
    handleRefresh,
    handleSearch,
    handleSortChange,
    handleGoToPage,
    fetchRoles,
  } = useRoleController();

  // const [showAddRole, setShowAddRole] = useState(false);
  const [detailTarget, setDetailTarget] = useState<Role | null>(null);
  const [detailRole, setDetailRole] = useState<Role | null>(null);
  const [editTarget, setEditTarget] = useState<Role | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<Role | null>(null);

  const navigate = useNavigate();

  useEffect(() => {
    fetchRoles();
  }, [fetchRoles]);

  // if (showAddRole) {
  //   return (
  //     <AddRoleScreen
  //       onBack={() => {
  //         setShowAddRole(false);
  //         fetchRoles();
  //       }}
  //     />
  //   );
  // }

  if (editTarget) {
    return (
      <AddRoleScreen
        role={editTarget}
        isEdit={true}
        onBack={() => {
          setEditTarget(null);
          fetchRoles();
        }}
      />
    );
  }
  if (detailRole) {
    return <DetailRoleScreen role={detailRole} onBack={() => setDetailRole(null)} />;
  }

  const handleOpenEdit = (role: Role) => setEditTarget(role);

  const handleOpenDelete = (role: Role) => {
    clearDeleteError();
    setDeleteTarget(role);
  };

  const columns: Column<Role>[] = [
    {
      key: "name",
      header: "Role Name",
      width: "25%",
      render: (role) => role.name,
    },
    {
      key: "displayName",
      header: "Display Name",
      width: "25%",
      hideBelow: "sm",
      render: (role) => role.displayName ?? "—",
    },
    {
      key: "description",
      header: "Description",
      hideBelow: "md",
      render: (role) => role.description ?? "—",
    },
    {
      key: "action",
      header: "Action",
      align: "right",
      render: (role) => (
        <div className="flex items-center justify-end gap-3.5">
          <button
            id="icn_ViewRole"
            title="View"
            // onClick={() => setDetailTarget(role)}
            onClick={() => setDetailRole(role)}
            className="text-gray-400 hover:text-indigo-700 transition"
          >
            <Eye className="w-4 h-4" />
          </button>
          <button
            id="icn_EditRole"
            title="Edit"
            onClick={() => handleOpenEdit(role)}
            className="text-gray-400 hover:text-indigo-700 transition"
          >
            <Pencil className="w-4 h-4" />
          </button>
          <button
            id="icn_DeleteRole"
            title="Delete"
            onClick={() => handleOpenDelete(role)}
            className="text-gray-400 hover:text-red-500 transition"
          >
            <Trash2 className="w-4 h-4" />
          </button>
        </div>
      ),
    },
  ];

  return (
    <div className="flex-1 p-4 sm:p-6 lg:p-8 overflow-y-auto">
      {/* <EditRoleDialog key={editTarget?.id ?? "edit"} role={editTarget} onClose={() => setEditTarget(null)} /> */}
      <DeleteRoleDialog
        role={deleteTarget}
        onClose={() => setDeleteTarget(null)}
      />

      {/* ── Page Header (breadcrumb + title + subtitle) ── */}
      <PageHeader
        breadcrumbs={[
          { label: "Dashboard" },
          { label: "Master Data" },
          { label: "Master Role" },
        ]}
        title="List of Draft Role"
        subtitle={lastUpdatedLabel()}
      />

      {/* Toolbar */}
      <Toolbar
        search={params.search}
        onSearchChange={handleSearch}
        sortOptions={SORT_OPTIONS}
        onSortChange={(value) => {
          const [sortBy, sortOrder] = value.split(":");
          handleSortChange(
            sortBy as RoleSortBy,
            sortOrder as "asc" | "desc",
          );
        }}
        sortValue={
          SORT_OPTIONS.find(
            (o) => o.value === `${params.sortBy}:${params.sortOrder}`,
          )?.label
        }
        actions={
          <PermissionGuard permission="user.user.create">
            <Button
              id="btn_AddRole"
              variant="secondary"
              onClick={() => navigate("/master/roles/form")}
            >
              Add Role
            </Button>
          </PermissionGuard>
        }
      />

      {/* Error */}
      {!isLoading && error && (
        <div className="flex items-center gap-3 bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-xl text-sm mb-4">
          <AlertCircle className="w-4 h-4 shrink-0" />
          <span className="flex-1">{error}</span>
          <button
            id="btn_RetryRole"
            onClick={handleRefresh}
            className="text-red-500 hover:text-red-700 font-medium text-xs"
          >
            Retry
          </button>
        </div>
      )}

      {/* Desktop Table */}
      <div id="tbl_Role" className="hidden sm:block">
        <DataTable
          columns={columns}
          data={roles}
          rowKey={(role) => role.id}
          isLoading={isLoading}
          error={error}
          onRetry={handleRefresh}
          emptyMessage="No roles found."
          skeletonRows={8}
        />
      </div>

      {/* Mobile List */}
      <div className="sm:hidden border border-gray-200 rounded-xl overflow-hidden">
        {isLoading && (
          <div className="flex items-center justify-center py-14 gap-2 text-gray-400 bg-white">
            <Loader2 className="w-5 h-5 animate-spin" />
            <span className="text-sm">Loading roles...</span>
          </div>
        )}
        {!isLoading && isEmpty && (
          <div className="bg-white text-center py-14 text-gray-400 text-sm">
            No roles found.
          </div>
        )}
        {!isLoading &&
          !isEmpty &&
          roles.map((role, i) => (
            <MobileRoleCard
              key={role.id}
              role={role}
              index={i}
              onView={setDetailTarget}
              onEdit={handleOpenEdit}
              onDelete={handleOpenDelete}
            />
          ))}
      </div>

      {/* Detail Modal */}
      {detailTarget && (
        <div
          id="lbl_RoleDetailDialog"
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-sm"
        >
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-lg mx-4">
            <div className="flex items-center justify-between px-6 py-4 border-b">
              <h3 className="text-base font-semibold text-gray-800">
                Role Detail
              </h3>
              <button
                id="icn_CloseRoleDetail"
                onClick={() => setDetailTarget(null)}
                className="text-gray-400 hover:text-gray-600 transition"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            <div className="px-6 py-5 space-y-4 text-sm">
              <div>
                <p className="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-1">
                  Name
                </p>
                <p className="text-gray-800">{detailTarget.name}</p>
              </div>
              <div>
                <p className="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-1">
                  Display Name
                </p>
                <p className="text-gray-800">
                  {detailTarget.displayName ?? "—"}
                </p>
              </div>
              <div>
                <p className="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-1">
                  Description
                </p>
                <p className="text-gray-800">
                  {detailTarget.description ?? "—"}
                </p>
              </div>
            </div>

            <div className="px-6 py-4 border-t">
              <Button
                id="btn_CloseRoleDetail"
                variant="outline"
                onClick={() => setDetailTarget(null)}
                className="w-full"
              >
                Close
              </Button>
            </div>
          </div>
        </div>
      )}

      {/* Pagination */}
      {!isLoading && !error && meta && meta.totalPages > 1 && (
        <TablePagination
          page={meta.page}
          totalPages={meta.totalPages}
          total={meta.total}
          limit={meta.limit}
          onPageChange={handleGoToPage}
        />
      )}
    </div>
  );
}
