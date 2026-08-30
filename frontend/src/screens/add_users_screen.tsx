import { useEffect, useState } from "react";
import {
  Eye,
  EyeOff,
  Loader2,
  AlertCircle,
  ChevronDown,
} from "lucide-react";
import { useUserStore } from "../store/userStore";
import type { CreateUserPayload } from "../types/users.types";
import { useRoleStore } from "../store/roleStore";
import AlertWidget from "../components/alertWidget";
import { useNavigate } from "react-router-dom";
import { PageHeader } from "../components/ui/page-header";
import { Button } from "../components/ui/button";

export default function AddUserScreen() {
  const [alert, setAlert] = useState<{
    show: boolean;
    type: "success" | "error";
    title: string;
    message: string;
  }>({
    show: false,
    type: "success",
    title: "",
    message: "",
  });

  const {
    isCreating,
    createError,
    createUser,
    clearCreateError,
    clearCreatesuccess,
  } = useUserStore();

  const { roles, fetchRoles } = useRoleStore();

  const navigate = useNavigate();

  const empty: CreateUserPayload = {
    email: "",
    password: "",
    fullname: "",
    employeeId: "",
  };

  const [form, setForm] = useState<CreateUserPayload>(empty);
  const [errors, setErrors] = useState<
    Partial<
      Record<keyof CreateUserPayload | "confirmPassword" | "role", string>
    >
  >({});
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirm, setShowConfirm] = useState(false);
  const [confirmPassword, setConfirmPassword] = useState("");
  const [selectedRole, setSelectedRole] = useState("");
  const [roleOpen, setRoleOpen] = useState(false);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setForm((p) => ({ ...p, [name]: value }));
    if (errors[name as keyof CreateUserPayload])
      setErrors((p) => ({ ...p, [name]: undefined }));
  };

  const validate = () => {
    const e: Partial<
      Record<keyof CreateUserPayload | "confirmPassword" | "role", string>
    > = {};
    if (!form.fullname.trim()) e.fullname = "Full name is required.";
    if (!selectedRole) e.role = "Role is required.";
    if (!form.email.trim()) e.email = "Email is required.";
    if (!form.password) e.password = "Password is required.";
    if (!confirmPassword) {
      e.confirmPassword = "Please confirm your password.";
    } else if (form.password !== confirmPassword) {
      e.confirmPassword = "Passwords do not match.";
    }
    if (!form.employeeId.trim()) e.employeeId = "Employee ID is required.";
    setErrors(e);
    return Object.keys(e).length === 0;
  };

  const handleDraft = () => console.log("Saved as draft", form);

  const handleSubmit = async () => {
    clearCreateError?.();

    if (!validate()) return;

    const ok = await createUser(form);

    // SUCCESS
    if (ok) {
      setAlert({
        show: true,
        type: "success",
        title: "Success",
        message: "User created successfully.",
      });

      setForm(empty);
      setConfirmPassword("");
      setSelectedRole("");

      setTimeout(() => {
        setAlert((prev) => ({
          ...prev,
          show: false,
        }));

        navigate(-1);
      }, 1500);

      return;
    }

    // ERROR
    const latestError = useUserStore.getState().createError;

    if (latestError) {
      setAlert({
        show: true,
        type: "error",
        title: "Create User Failed",
        message: latestError,
      });

      setTimeout(() => {
        setAlert((prev) => ({
          ...prev,
          show: false,
        }));
      }, 3000);
    }
  };

  useEffect(() => {
    fetchRoles({ page: 1, limit: 100 }); // bebas mau limit berapa
  }, [fetchRoles]);

  return (
    <>
      {alert.show && (
        <AlertWidget
          show={alert.show}
          type={alert.type}
          title={alert.title}
          message={alert.message}
          onClose={() => {
            setAlert((prev) => ({
              ...prev,
              show: false,
            }));

            clearCreateError();
            clearCreatesuccess();
          }}
        />
      )}

      <div className="flex-1 p-4 sm:p-6 lg:p-8 overflow-y-auto">
        {/* ── Page Header (breadcrumb + title + back) ── */}
        <PageHeader
          breadcrumbs={[
            { label: "Dashboard", onClick: () => navigate(-1) },
            { label: "Master Data" },
            { label: "Master User", onClick: () => navigate(-1) },
            { label: "Add User" },
          ]}
          title="Form Add User"
          onBack={() => navigate(-1)}
        />

        {/* ── Error Banner ── */}
        {createError && (
          <div className="flex items-center gap-3 bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-xl text-sm mb-5">
            <AlertCircle className="w-4 h-4 shrink-0" />
            <span className="flex-1">{createError}</span>
          </div>
        )}

        {/* ── Section 1: Name / Role / Company ── */}
        <div className="bg-white rounded-xl border border-gray-200 p-6 mb-4">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-5">
            {/* Name → maps to fullname */}
            <div className="flex flex-col gap-1">
              <label className="text-sm font-semibold text-gray-800">
                Name
              </label>
              <input
                id="txt_Fullname"
                name="fullname"
                type="text"
                value={form.fullname}
                onChange={handleChange}
                placeholder="e.g. John Doe"
                className={`w-full px-3.5 py-2.5 rounded-lg border text-sm outline-none bg-white transition ${
                  errors.fullname
                    ? "border-red-400 focus:ring-2 focus:ring-red-100"
                    : "border-gray-200 focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100"
                }`}
              />
              {errors.fullname && (
                <p className="text-xs text-red-500">{errors.fullname}</p>
              )}
            </div>

            <div className="flex flex-col gap-1">
              <label className="text-sm font-semibold text-gray-800">
                Role
              </label>
              <div className="relative">
                <button
                  id="cmb_Role"
                  type="button"
                  onClick={() => setRoleOpen((v) => !v)}
                  className={`w-full px-3.5 py-2.5 rounded-lg border text-sm outline-none bg-white transition text-left flex items-center justify-between ${
                    errors.role
                      ? "border-red-400 focus:ring-2 focus:ring-red-100"
                      : "border-gray-200 focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100"
                  }`}
                >
                  <span
                    className={selectedRole ? "text-gray-900" : "text-gray-400"}
                  >
                    {selectedRole || "Select role"}
                  </span>
                  <ChevronDown className="w-4 h-4 text-gray-400" />
                </button>
                {roleOpen && (
                  <ul id="lsb_Role" className="absolute z-20 mt-1 w-full bg-white border border-gray-200 rounded-lg shadow-lg text-sm overflow-hidden">
                    {roles.map((r) => (
                      <li
                        key={r.id}
                        onClick={() => {
                          setSelectedRole(r.name);
                          setRoleOpen(false);
                          setErrors((p) => ({ ...p, role: undefined }));
                        }}
                        className={`px-3.5 py-2.5 cursor-pointer hover:bg-indigo-50 transition ${
                          selectedRole === r.name
                            ? "text-indigo-700 font-medium bg-indigo-50"
                            : "text-gray-700"
                        }`}
                      >
                        {r.name}
                      </li>
                    ))}
                  </ul>
                )}
              </div>
              {errors.role && (
                <p className="text-xs text-red-500">{errors.role}</p>
              )}
            </div>
          </div>
        </div>

        {/* ── Section 2: Username / Password / Confirm Password ── */}
        {/* ── Section 2: Username / Password / Confirm Password / Email ── */}
        <div className="bg-[#eef0f8] rounded-xl border border-gray-200 p-6 mb-6">
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-5">
            {/* Username → employeeId */}
            <div className="flex flex-col gap-1">
              <label className="text-sm font-semibold text-gray-800">
                Username
              </label>
              <input
                id="txt_EmployeeId"
                name="employeeId"
                type="text"
                value={form.employeeId}
                onChange={handleChange}
                placeholder="e.g. Adminhwho3"
                className={`w-full px-3.5 py-2.5 rounded-lg border text-sm outline-none bg-white transition ${
                  errors.employeeId
                    ? "border-red-400 focus:ring-2 focus:ring-red-100"
                    : "border-gray-200 focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100"
                }`}
              />
              {errors.employeeId && (
                <p className="text-xs text-red-500">{errors.employeeId}</p>
              )}
            </div>

            {/* Email → email */}
            <div className="flex flex-col gap-1">
              <label className="text-sm font-semibold text-gray-800">
                Email
              </label>
              <input
                id="txt_Email"
                name="email"
                type="email"
                value={form.email}
                onChange={handleChange}
                placeholder="e.g. john@example.com"
                className={`w-full px-3.5 py-2.5 rounded-lg border text-sm outline-none bg-white transition ${
                  errors.email
                    ? "border-red-400 focus:ring-2 focus:ring-red-100"
                    : "border-gray-200 focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100"
                }`}
              />
              {errors.email && (
                <p className="text-xs text-red-500">{errors.email}</p>
              )}
            </div>

            {/* Password */}
            <div className="flex flex-col gap-1">
              <label className="text-sm font-semibold text-gray-800">
                Password
              </label>
              <div className="relative">
                <input
                  id="txt_Password"
                  name="password"
                  type={showPassword ? "text" : "password"}
                  value={form.password}
                  onChange={handleChange}
                  placeholder="Min. 8 characters"
                  className={`w-full px-3.5 py-2.5 pr-10 rounded-lg border text-sm outline-none bg-white transition ${
                    errors.password
                      ? "border-red-400 focus:ring-2 focus:ring-red-100"
                      : "border-gray-200 focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100"
                  }`}
                />
                <button
                  id="btn_TogglePassword"
                  type="button"
                  onClick={() => setShowPassword((v) => !v)}
                  className="absolute inset-y-0 right-3 flex items-center text-gray-400 hover:text-gray-600 transition"
                  tabIndex={-1}
                >
                  {showPassword ? (
                    <Eye className="w-4 h-4" />
                  ) : (
                    <EyeOff className="w-4 h-4" />
                  )}
                </button>
              </div>
              {errors.password && (
                <p className="text-xs text-red-500">{errors.password}</p>
              )}
            </div>

            {/* Confirm Password */}
            <div className="flex flex-col gap-1">
              <label className="text-sm font-semibold text-gray-800">
                Confirm Password
              </label>
              <div className="relative">
                <input
                  id="txt_ConfirmPassword"
                  name="confirmPassword"
                  type={showConfirm ? "text" : "password"}
                  value={confirmPassword}
                  onChange={(e) => {
                    setConfirmPassword(e.target.value);
                    if (errors.confirmPassword)
                      setErrors((p) => ({ ...p, confirmPassword: undefined }));
                  }}
                  placeholder="Repeat password"
                  className={`w-full px-3.5 py-2.5 pr-10 rounded-lg border text-sm outline-none bg-white transition ${
                    errors.confirmPassword
                      ? "border-red-400 focus:ring-2 focus:ring-red-100"
                      : "border-gray-200 focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100"
                  }`}
                />
                <button
                  id="btn_ToggleConfirmPassword"
                  type="button"
                  onClick={() => setShowConfirm((v) => !v)}
                  className="absolute inset-y-0 right-3 flex items-center text-gray-400 hover:text-gray-600 transition"
                  tabIndex={-1}
                >
                  {showConfirm ? (
                    <Eye className="w-4 h-4" />
                  ) : (
                    <EyeOff className="w-4 h-4" />
                  )}
                </button>
              </div>
              {errors.confirmPassword && (
                <p className="text-xs text-red-500">{errors.confirmPassword}</p>
              )}
            </div>
          </div>
        </div>

        {/* ── Action Buttons ── */}
        <div className="flex items-center justify-end gap-3">
          <Button
            variant="secondary"
            id="btn_DraftUser"
            onClick={handleDraft}
            disabled={isCreating}
            className="px-8 py-2.5"
          >
            Draft
          </Button>
          <Button
            variant="primary"
            id="btn_SaveUser"
            onClick={handleSubmit}
            disabled={isCreating}
            className="px-8 py-2.5"
          >
            {isCreating ? (
              <>
                <Loader2 className="w-4 h-4 animate-spin" />
                Submitting...
              </>
            ) : (
              "Submit"
            )}
          </Button>
        </div>
      </div>
    </>
  );
}
