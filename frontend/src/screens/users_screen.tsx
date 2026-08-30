import { useEffect, useMemo, useState } from "react";
import {
    Loader2, AlertCircle, X, Pencil, Trash2, AlertTriangle,
    Eye,
} from "lucide-react";
import { useUserController } from "../controllers/masterData/userController";
import { useUserStore } from "../store/userStore";
import type { UpdateUserPayload, User } from "../types/users.types";
import UserDetailScreen from "./user_detail_screen";
import PermissionGuard from "../components/guards/permissionGuard";
import { useNavigate } from "react-router-dom";
import { DataTable, TablePagination, type Column, type SortState, type SortDirection } from "../components/ui/table";
import { PageHeader, lastUpdatedLabel } from "../components/ui/page-header";
import { Toolbar } from "../components/ui/toolbar";
import { Button } from "../components/ui/button";

// Value used for client-side column sorting.
const roleNameOf = (user: User) => (user.roles?.length > 0 ? user.roles[0].displayName : "");

function EditUserDialog({ user, onClose }: { user: User | null; onClose: () => void }) {
    const { isUpdating, updateError, updateUser } = useUserStore();
    const [form, setForm] = useState<UpdateUserPayload>({
        fullname: user?.fullname ?? "",
        email: user?.email ?? "",
        employeeId: user?.employeeId ?? "",
        password: "",
    });

    const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        const { name, value } = e.target;
        setForm((p) => ({ ...p, [name]: value }));
    };

    const handleSubmit = async () => {
        if (!user) return;
        const payload: UpdateUserPayload = { ...form };
        if (!payload.password) delete payload.password;
        const ok = await updateUser(user.id, payload);
        if (ok) onClose();
    };

    if (!user) return null;
    return (
        <div id="lbl_EditUserDialog" className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-sm">
            <div className="bg-white rounded-2xl shadow-xl w-full max-w-md mx-4">
                <div className="flex items-center justify-between px-6 py-4 border-b">
                    <h3 className="text-base font-semibold text-gray-800">Edit User</h3>
                    <button id="icn_CloseEditUser" onClick={onClose} disabled={isUpdating} className="text-gray-400 hover:text-gray-600 transition disabled:opacity-50">
                        <X className="w-5 h-5" />
                    </button>
                </div>
                <div className="px-6 py-5 flex flex-col gap-4">
                    {updateError && (
                        <div className="flex items-center gap-2 bg-red-50 border border-red-200 text-red-600 px-4 py-3 rounded-xl text-sm">
                            <AlertCircle className="w-4 h-4 shrink-0" />{updateError}
                        </div>
                    )}
                    <div className="flex flex-col gap-1">
                        <label className="text-xs font-semibold text-gray-500 uppercase tracking-wider">Full Name</label>
                        <input id="txt_EditFullname" name="fullname" type="text" value={form.fullname ?? ""} onChange={handleChange}
                            className="w-full px-4 py-2.5 rounded-xl border border-gray-200 focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100 text-sm outline-none transition" />
                    </div>
                    <div className="flex flex-col gap-1">
                        <label className="text-xs font-semibold text-gray-500 uppercase tracking-wider">Email</label>
                        <input id="txt_EditEmail" name="email" type="email" value={form.email ?? ""} onChange={handleChange}
                            className="w-full px-4 py-2.5 rounded-xl border border-gray-200 focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100 text-sm outline-none transition" />
                    </div>
                    <div className="flex flex-col gap-1">
                        <label className="text-xs font-semibold text-gray-500 uppercase tracking-wider">Employee ID</label>
                        <input id="txt_EditEmployeeId" name="employeeId" type="text" value={form.employeeId ?? ""} onChange={handleChange}
                            className="w-full px-4 py-2.5 rounded-xl border border-gray-200 focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100 text-sm outline-none transition" />
                    </div>
                    <div className="flex flex-col gap-1">
                        <label className="text-xs font-semibold text-gray-500 uppercase tracking-wider">
                            New Password <span className="normal-case font-normal text-gray-400">(optional)</span>
                        </label>
                        <input id="txt_EditPassword" name="password" type="password" value={form.password ?? ""} onChange={handleChange}
                            placeholder="Leave empty to keep current password"
                            className="w-full px-4 py-2.5 rounded-xl border border-gray-200 focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100 text-sm outline-none transition" />
                    </div>
                </div>
                <div className="flex gap-3 px-6 py-4 border-t">
                    <button id="btn_CancelEditUser" onClick={onClose} disabled={isUpdating}
                        className="flex-1 py-2.5 rounded-xl border border-gray-200 text-sm font-medium text-gray-600 hover:bg-gray-50 transition disabled:opacity-50">
                        Cancel
                    </button>
                    <button id="btn_SaveEditUser" onClick={handleSubmit} disabled={isUpdating}
                        className="flex-1 py-2.5 rounded-xl bg-indigo-900 hover:bg-indigo-800 text-sm font-medium text-white transition disabled:opacity-50 flex items-center justify-center gap-2">
                        {isUpdating ? <><Loader2 className="w-4 h-4 animate-spin" />Saving...</> : "Save Changes"}
                    </button>
                </div>
            </div>
        </div>
    );
}

// ─── Delete Dialog ────────────────────────────────────────────────────────────
function DeleteUserDialog({ user, onClose }: { user: User | null; onClose: () => void }) {
    const { isDeleting, deleteError, deleteUser } = useUserStore();

    const handleConfirm = async () => {
        if (!user) return;
        const ok = await deleteUser(user.id);
        if (ok) onClose();
    };

    if (!user) return null;
    return (
        <div id="lbl_DeleteUserDialog" className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-sm">
            <div className="bg-white rounded-2xl shadow-xl w-full max-w-sm mx-4 p-6">
                <div className="flex justify-center mb-4">
                    <div className="w-14 h-14 bg-red-50 rounded-full flex items-center justify-center">
                        <AlertTriangle className="w-7 h-7 text-red-500" />
                    </div>
                </div>
                <h3 className="text-center text-lg font-semibold text-gray-800 mb-1">Delete User</h3>
                <p className="text-center text-sm text-gray-400 mb-2">
                    Are you sure you want to delete <span className="font-semibold text-gray-700">{user.fullname}</span>?
                </p>
                <p className="text-center text-xs text-red-400 mb-6">This action cannot be undone.</p>
                {deleteError && (
                    <div className="flex items-center gap-2 bg-red-50 border border-red-200 text-red-600 px-4 py-3 rounded-xl text-sm mb-4">
                        <AlertCircle className="w-4 h-4 shrink-0" />{deleteError}
                    </div>
                )}
                <div className="flex gap-3">
                    <button id="btn_CancelDeleteUser" onClick={onClose} disabled={isDeleting}
                        className="flex-1 py-2.5 rounded-xl border border-gray-200 text-sm font-medium text-gray-600 hover:bg-gray-50 transition disabled:opacity-50">
                        Cancel
                    </button>
                    <button id="btn_ConfirmDeleteUser" onClick={handleConfirm} disabled={isDeleting}
                        className="flex-1 py-2.5 rounded-xl bg-red-500 hover:bg-red-600 text-sm font-medium text-white transition disabled:opacity-50 flex items-center justify-center gap-2">
                        {isDeleting ? <><Loader2 className="w-4 h-4 animate-spin" />Deleting...</> : "Delete"}
                    </button>
                </div>
            </div>
        </div>
    );
}

// ─── Mobile User Card ─────────────────────────────────────────────────────────
function MobileUserCard({ user, index, onView ,onEdit, onDelete }: {
    user: User; index: number;
    onView: (u: User) => void;
    onEdit: (u: User) => void;
    onDelete: (u: User) => void;
}) {
    const roleName = user.roles?.length > 0 ? user.roles[0].displayName : "—";

    return (
        <div className={`flex items-center justify-between px-4 py-3.5 border-b border-gray-200 last:border-b-0 ${index % 2 === 1 ? "bg-[#eef0f8]" : "bg-white"}`}>
            <div className="flex-1 min-w-0">
                <p className="text-sm text-gray-800 truncate">{user.fullname}</p>
                <p className="text-xs text-gray-500 mt-0.5">{roleName}</p>
            </div>
            <div className="flex items-center gap-3 shrink-0 ml-4">
                <button id="icn_ViewUserMobile" onClick={() => onView(user)} className="text-gray-400 hover:text-indigo-600 transition">
                    <Eye className="w-4 h-4" />
                </button>
                <button id="icn_EditUserMobile" onClick={() => onEdit(user)} className="text-gray-400 hover:text-indigo-600 transition">
                    <Pencil className="w-4 h-4" />
                </button>
                <button id="icn_DeleteUserMobile" onClick={() => onDelete(user)} className="text-gray-400 hover:text-red-500 transition">
                    <Trash2 className="w-4 h-4" />
                </button>
            </div>
        </div>
    );
}

// ─── Users Screen ─────────────────────────────────────────────────────────────
export default function UsersScreen() {
    const {
        users, meta, isLoading, error, params,
        isEmpty,
        clearDeleteError,
        handleRefresh, handleSearch,
        fetchUsers,
    } = useUserController();

    const navigate = useNavigate();

    const [detailTarget, setDetailTarget] = useState<User | null>(null);
    const [editTarget, setEditTarget] = useState<User | null>(null);
    const [deleteTarget, setDeleteTarget] = useState<User | null>(null);
    const [sort, setSort] = useState<SortState | null>(null);

    const SORT_OPTIONS = [
        { label: "Name A-Z", value: "fullname:asc" },
        { label: "Name Z-A", value: "fullname:desc" },
        { label: "Role A-Z", value: "role:asc" },
        { label: "Role Z-A", value: "role:desc" },
    ];
    const sortLabel = SORT_OPTIONS.find(
        (o) => o.value === `${sort?.key}:${sort?.direction}`,
    )?.label;
    const handleSortChange = (value: string) => {
        const [key, direction] = value.split(":");
        setSort({ key, direction: direction as SortDirection });
    };

    useEffect(() => { fetchUsers(); }, [fetchUsers]);

    // Client-side sort of the current page (API has no sort param yet).
    const sortedUsers = useMemo(() => {
        if (!sort) return users;
        const value = (u: User) =>
            sort.key === "role" ? roleNameOf(u) : (u.fullname ?? "");
        const dir = sort.direction === "asc" ? 1 : -1;
        return [...users].sort((a, b) =>
            value(a).localeCompare(value(b), undefined, { sensitivity: "base" }) * dir,
        );
    }, [users, sort]);

    // ── Navigate to UserDetailScreen ──
    if (detailTarget) {
        return (
            <UserDetailScreen
                userId={detailTarget.id}
                onBack={() => setDetailTarget(null)}
            />
        );
    }

    const handleOpenEdit = (user: User) => setEditTarget(user);
    const handleOpenDelete = (user: User) => { clearDeleteError(); setDeleteTarget(user); };

    const columns: Column<User>[] = [
        {
            key: "username",
            header: "Username",
            sortable: true,
            width: "30%",
            render: (u) => u.fullname,
        },
        {
            key: "name",
            header: "Name",
            sortable: true,
            width: "25%",
            hideBelow: "sm",
            render: (u) => u.fullname,
        },
        {
            key: "role",
            header: "Role",
            sortable: true,
            hideBelow: "md",
            render: (u) => roleNameOf(u) || "—",
        },
        {
            key: "action",
            header: "Action",
            align: "right",
            render: (u) => (
                <div className="flex items-center justify-end gap-3.5">
                    <button
                        title="View"
                        id="icn_ViewUser"
                        onClick={() => setDetailTarget(u)}
                        className="text-gray-400 hover:text-indigo-700 transition"
                    >
                        <Eye className="w-4 h-4" />
                    </button>
                    <button title="Edit" id="icn_EditUser" onClick={() => handleOpenEdit(u)} className="text-gray-400 hover:text-indigo-700 transition">
                        <Pencil className="w-4 h-4" />
                    </button>
                    <button title="Delete" id="icn_DeleteUser" onClick={() => handleOpenDelete(u)} className="text-gray-400 hover:text-red-500 transition">
                        <Trash2 className="w-4 h-4" />
                    </button>
                </div>
            ),
        },
    ];

    return (
        <div className="flex-1 p-4 sm:p-6 lg:p-8 overflow-y-auto">
            <EditUserDialog key={editTarget?.id ?? "edit"} user={editTarget} onClose={() => setEditTarget(null)} />
            <DeleteUserDialog user={deleteTarget} onClose={() => setDeleteTarget(null)} />

            {/* ── Page Header (breadcrumb + title + subtitle) ── */}
            <PageHeader
                breadcrumbs={[
                    { label: "Dashboard" },
                    { label: "Master Data" },
                    { label: "Master User" },
                ]}
                title="List of Draft User"
                subtitle={lastUpdatedLabel()}
            />

            {/* ── Toolbar ── */}
            <Toolbar
                search={params.search}
                onSearchChange={handleSearch}
                sortOptions={SORT_OPTIONS}
                onSortChange={handleSortChange}
                sortValue={sortLabel}
                actions={
                    <PermissionGuard permission="user.user.create">
                        <Button
                            id="btn_AddUser"
                            onClick={() => navigate("/master/users/create")}
                        >
                            Add User
                        </Button>
                    </PermissionGuard>
                }
            />

            {/* ── Error ── */}
            {!isLoading && error && (
                <div className="flex items-center gap-3 bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-xl text-sm mb-4">
                    <AlertCircle className="w-4 h-4 shrink-0" />
                    <span className="flex-1">{error}</span>
                    <button id="btn_RetryUser" onClick={handleRefresh} className="text-red-500 hover:text-red-700 font-medium text-xs">Retry</button>
                </div>
            )}

            {/* ── Table (sm+) ── */}
            <div id="tbl_User" className="hidden sm:block">
                <DataTable
                    columns={columns}
                    data={sortedUsers}
                    rowKey={(u) => u.id}
                    isLoading={isLoading}
                    error={error}
                    onRetry={handleRefresh}
                    emptyMessage="No users found."
                    sort={sort}
                    onSortChange={setSort}
                    skeletonRows={8}
                />
            </div>

            {/* ── Mobile List ── */}
            <div className="sm:hidden border border-gray-200 rounded-xl overflow-hidden">
                {isLoading && (
                    <div className="flex items-center justify-center py-14 gap-2 text-gray-400 bg-white">
                        <Loader2 className="w-5 h-5 animate-spin" />
                        <span className="text-sm">Loading users...</span>
                    </div>
                )}
                {!isLoading && isEmpty && (
                    <div className="bg-white text-center py-14 text-gray-400 text-sm">No users found.</div>
                )}
                {!isLoading && !isEmpty && sortedUsers.map((user, i) => (
                    <MobileUserCard
                        key={user.id}
                        user={user}
                        index={i}
                        onView={setDetailTarget}
                        onEdit={handleOpenEdit}
                        onDelete={handleOpenDelete}
                    />
                ))}
            </div>

            {/* ── Pagination ── */}
            {!isLoading && !error && meta && meta.totalPages >= 1 && (
                <TablePagination
                    page={meta.page}
                    totalPages={meta.totalPages}
                    total={meta.total}
                    limit={meta.limit}
                    onPageChange={(page) => fetchUsers({ page })}
                />
            )}
        </div>
    );
}