import { useState, useEffect } from "react";
import {
  Eye,
  EyeOff,
  Loader2,
  AlertCircle,
  X,
  ChevronDown,
} from "lucide-react";
import { useNavigate } from "react-router-dom";
import { useAuthController } from "../controllers/auth/authController";
import { useCompanyStore } from "../store/companyStore";
import type { Company } from "../types/company.types";
import { getFirstAccessibleRoute } from "../router/getAccessFirstRoute";
import { NAV_ITEMS } from "../config/navigation.config";
import { useAuthStore } from "../store/authStore";
import { Button } from "../components/ui/button";

interface LoginPageProps {
  title?: string;
  usernamePlaceholder?: string;
  passwordPlaceholder?: string;
  submitLabel?: string;
  guestContent?: React.ReactNode | null;
  onGuest?: () => void;
  brandContent?: React.ReactNode;
}

export default function LoginPage({
  title = "Sign In",
  usernamePlaceholder = "Input username",
  passwordPlaceholder = "Input password",
  submitLabel = "Sign In",
  guestContent = null,
  onGuest,
  brandContent,
}: LoginPageProps) {
  const navigate = useNavigate();

  const {
    isLoading,
    error,
    clearError,
    handleSuperAdminLogin,
    handleEmployeeLogin,
  } = useAuthController();

  const {
    companies,
    isLoading: isCompanyLoading,
    error: companyError,
    fetchCompanies,
  } = useCompanyStore();

  useEffect(() => {
    fetchCompanies();
  }, []);

const [selectedCompany, setSelectedCompany] = useState<Company | null>(() => {
  const companies = useCompanyStore.getState().companies;
  return (
    companies.find((c) => c.name === "PT. Gerbang Cahaya Utama") ??
    companies[0] ??
    null
  );
});

// Then add this to your existing fetchCompanies useEffect:
useEffect(() => {
  fetchCompanies().then(() => {
    const companies = useCompanyStore.getState().companies;
    const defaultCompany =
      companies.find((c) => c.name === "PT. Gerbang Cahaya Utama") ??
      companies[0] ??
      null;
    setSelectedCompany(defaultCompany);
  });
}, []);

  const [role] = useState<"employee" | "super_admin">("super_admin");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);

  const [showSessionExpired, setShowSessionExpired] = useState(() => {
    const expired = sessionStorage.getItem("session_expired") === "true";
    if (expired) sessionStorage.removeItem("session_expired");
    return expired;
  });

  const handleCompanyChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const id = parseInt(e.target.value);
    const company = companies.find((c) => c.id === id) ?? null;
    setSelectedCompany(company);
    clearError();
  };

  const handleSubmit = () => {
    if (!selectedCompany) return;
    useCompanyStore.getState().setSelectedCompanyId(selectedCompany.id);

    const isSuperAdmin = role === "super_admin";

    const onLoginSuccess = () => {
      // pakai role dari store hasil login, bukan dari form
      const isAdmin = useAuthStore.getState().hasRole("super_admin");

      if (isAdmin) {
        navigate("/dashboard");
        return;
      }

      // exclude "/dashboard" karena itu khusus admin
      const target = getFirstAccessibleRoute(NAV_ITEMS, ["/dashboard"]);
      navigate(target ?? "/no-access");
    };

    if (isSuperAdmin) {
      handleSuperAdminLogin(
        { email, password, companyId: selectedCompany.id },
        onLoginSuccess,
        (msg) => console.error(msg),
      );
    } else {
      handleEmployeeLogin({ email, password }, onLoginSuccess, (msg) =>
        console.error(msg),
      );
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === "Enter") handleSubmit();
  };

  return (
    <div className="min-h-screen flex font-sans">
      {/* ── LEFT: Photo panel ───────────────────────────────────────── */}
      <div className="hidden md:block w-1/2 relative overflow-hidden">
        {brandContent ?? (
          <img
            src="/login_bg.png"
            alt="Cargo ship"
            className="absolute inset-0 w-full h-full object-cover"
          />
        )}
        {/* subtle dark overlay at bottom for depth */}
        <div className="absolute inset-0 bg-linear-to-t from-black/20 to-transparent pointer-events-none" />
      </div>

      {/* ── RIGHT: Form panel ───────────────────────────────────────── */}
      <div className="w-full md:w-1/2 bg-[#F4F5F7] flex flex-col justify-center px-10 sm:px-16 lg:px-24 py-12">
        <div className="w-full max-w-md mx-auto">
          {/* Session expired banner */}
          {showSessionExpired && (
            <div className="flex items-start gap-3 bg-amber-50 border border-amber-200 text-amber-800 px-4 py-3 rounded-xl mb-6 text-sm">
              <AlertCircle className="w-4 h-4 mt-0.5 shrink-0 text-amber-500" />
              <p className="flex-1">
                Your session has expired. Please sign in again.
              </p>
              <button
                id="icn_CloseSessionExpired"
                onClick={() => setShowSessionExpired(false)}
                className="text-amber-400 hover:text-amber-600 transition shrink-0"
              >
                <X className="w-4 h-4" />
              </button>
            </div>
          )}

          {/* Title */}
          <h1 className="text-3xl font-bold text-gray-900 mb-8 tracking-tight">
            {title}
          </h1>

          <div className="flex flex-col gap-5">
            {/* ── Company ── */}
            <div className="flex flex-col gap-1.5">
              <label className="text-sm font-semibold text-gray-700">
                Company
              </label>
              {isCompanyLoading ? (
                <div className="flex items-center gap-2 px-4 py-3 bg-white rounded-xl border border-gray-200 text-sm text-gray-400">
                  <Loader2 className="w-4 h-4 animate-spin" />
                  Loading companies...
                </div>
              ) : companyError ? (
                <div className="flex flex-col gap-2">
                  <p className="text-red-500 text-sm bg-red-50 px-4 py-3 rounded-xl border border-red-100">
                    {companyError}
                  </p>
                  <Button
                    id="btn_RetryFetchCompanies"
                    variant="link"
                    size="sm"
                    onClick={fetchCompanies}
                    className="self-start px-0"
                  >
                    Try again
                  </Button>
                </div>
              ) : (
                <div className="relative">
                  <select
                    id="cmb_Company"
                    value={selectedCompany?.id ?? ""}
                    onChange={handleCompanyChange}
                    className="w-full appearance-none px-4 py-3 pr-10 bg-white rounded-xl border border-gray-200 text-sm text-gray-800 outline-none focus:border-indigo-400 focus:ring-2 focus:ring-indigo-100 transition cursor-pointer"
                  >
                    <option value="" disabled>
                      Select company
                    </option>
                    {companies.map((c) => (
                      <option key={c.id} value={c.id}>
                        {c.name}
                      </option>
                    ))}
                  </select>
                  <ChevronDown className="absolute right-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400 pointer-events-none" />
                </div>
              )}
            </div>

            {/* ── Username ── */}
            <div className="flex flex-col gap-1.5">
              <label className="text-sm font-semibold text-gray-700">
                Username
              </label>
              <input
                id="txt_Username"
                type="email"
                value={email}
                onChange={(e) => {
                  setEmail(e.target.value);
                  clearError();
                }}
                onKeyDown={handleKeyDown}
                placeholder={usernamePlaceholder}
                autoComplete="email"
                className="w-full px-4 py-3 bg-white rounded-xl border border-gray-200 text-sm text-gray-800 outline-none focus:border-indigo-400 focus:ring-2 focus:ring-indigo-100 transition placeholder:text-gray-300"
              />
            </div>

            {/* ── Password ── */}
            <div className="flex flex-col gap-1.5">
              <label className="text-sm font-semibold text-gray-700">
                Password
              </label>
              <div className="relative">
                <input
                  id="txt_Password"
                  type={showPassword ? "text" : "password"}
                  value={password}
                  onChange={(e) => {
                    setPassword(e.target.value);
                    clearError();
                  }}
                  onKeyDown={handleKeyDown}
                  placeholder={passwordPlaceholder}
                  autoComplete="current-password"
                  className="w-full px-4 py-3 pr-11 bg-white rounded-xl border border-gray-200 text-sm text-gray-800 outline-none focus:border-indigo-400 focus:ring-2 focus:ring-indigo-100 transition placeholder:text-gray-300"
                />
                <button
                  id="btn_TogglePassword"
                  type="button"
                  onClick={() => setShowPassword((p) => !p)}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600 transition"
                  tabIndex={-1}
                >
                  {showPassword ? (
                    <Eye className="w-4 h-4" />
                  ) : (
                    <EyeOff className="w-4 h-4" />
                  )}
                </button>
              </div>
            </div>

            {/* ── Error ── */}
            {error && (
              <p
                id="lbl_LoginError"
                className="text-red-500 text-sm bg-red-50 border border-red-100 px-4 py-3 rounded-xl"
              >
                {error}
              </p>
            )}

            {/* ── Sign In button ── */}
            <div className="flex justify-end mt-1">
              <Button
                id="btn_SignIn"
                variant="primary"
                size="lg"
                onClick={handleSubmit}
                disabled={isLoading || !selectedCompany || !email || !password}
              >
                {isLoading ? (
                  <>
                    <Loader2 className="w-4 h-4 animate-spin" />
                    Signing in...
                  </>
                ) : (
                  submitLabel
                )}
              </Button>
            </div>
          </div>
        </div>

        {/* Guest button — opsional */}
        {guestContent !== null && (
          <div className="mt-10 flex justify-center">
            <Button
              variant="ghost"
              size="lg"
              onClick={onGuest}
            >
              {guestContent}
            </Button>
          </div>
        )}
      </div>
    </div>
  );
}
