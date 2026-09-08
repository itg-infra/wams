import { useState } from "react";
import type { FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { useAuthStore } from "../store/authStore";
import { PageHeader } from "../components/ui/page-header";
import { Button } from "../components/ui/button";

export default function ProfileScreen() {
  const navigate = useNavigate();
  const user = useAuthStore((s) => s.user);
  const updateProfile = useAuthStore((s) => s.updateProfile);
  const changePassword = useAuthStore((s) => s.changePassword);
  const [fullname, setFullname] = useState(user?.fullname ?? "");
  const [email, setEmail] = useState(user?.email ?? "");
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");

  const saveProfile = async (event: FormEvent) => {
    event.preventDefault();
    setError("");
    setMessage("");
    try {
      await updateProfile({
        fullname,
        ...(email !== user?.email ? { email, currentPassword } : {}),
      });
      setMessage("Profile updated.");
      setCurrentPassword("");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Unable to update profile.");
    }
  };

  const savePassword = async (event: FormEvent) => {
    event.preventDefault();
    setError("");
    setMessage("");
    if (newPassword.length < 8 || newPassword !== confirmPassword) {
      setError("Use at least 8 characters and make both new passwords match.");
      return;
    }
    try {
      await changePassword({ currentPassword, newPassword });
      navigate("/login", { replace: true });
    } catch (err) {
      setError(err instanceof Error ? err.message : "Unable to change password.");
    }
  };

  return (
    <div className="flex-1 p-4 sm:p-6 lg:p-8 overflow-y-auto">
      <PageHeader
        breadcrumbs={[
          { label: "Dashboard", onClick: () => navigate("/dashboard") },
          { label: "Profile" },
        ]}
        title="My Profile"
      />

      {message && <p className="mb-5 rounded-xl border border-green-200 bg-green-50 px-4 py-3 text-sm text-green-700">{message}</p>}
      {error && <p className="mb-5 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">{error}</p>}

      <div className="flex w-full flex-col gap-4">
        <form onSubmit={saveProfile} className="flex w-full flex-col gap-5 rounded-xl border border-gray-200 bg-white p-4 sm:p-6">
          <h2 className="text-base font-semibold text-gray-800">Profile Details</h2>
          <label className="block text-sm font-semibold text-gray-800">
            Full Name
            <input
              id="txt_ProfileFullname"
              className="mt-1 w-full rounded-lg border border-gray-200 bg-white px-3.5 py-2.5 text-sm outline-none transition focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100"
              value={fullname}
              onChange={(e) => setFullname(e.target.value)}
            />
          </label>
          <label className="block text-sm font-semibold text-gray-800">
            Employee ID
            <input
              id="txt_ProfileEmployeeId"
              className="mt-1 w-full cursor-not-allowed rounded-lg border border-gray-200 bg-gray-100 px-3.5 py-2.5 text-sm text-gray-500 outline-none"
              value={user?.employeeId ?? "-"}
              readOnly
              disabled
            />
          </label>
          <label className="block text-sm font-semibold text-gray-800">
            Email
            <input
              id="txt_ProfileEmail"
              className="mt-1 w-full rounded-lg border border-gray-200 bg-white px-3.5 py-2.5 text-sm outline-none transition focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
            />
          </label>
          {email !== user?.email && (
            <label className="block text-sm font-semibold text-gray-800">
              Current Password
              <input
                id="txt_ProfileCurrentPassword"
                className="mt-1 w-full rounded-lg border border-gray-200 bg-white px-3.5 py-2.5 text-sm outline-none transition focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100"
                type="password"
                value={currentPassword}
                onChange={(e) => setCurrentPassword(e.target.value)}
              />
            </label>
          )}
          <div className="flex w-full justify-stretch sm:justify-end">
            <Button type="submit" id="btn_SaveProfile" className="w-full sm:w-auto">Save Profile</Button>
          </div>
        </form>
        <form onSubmit={savePassword} className="flex w-full flex-col gap-5 rounded-xl border border-gray-200 bg-white p-4 sm:p-6">
          <h2 className="text-base font-semibold text-gray-800">Change Password</h2>
          <label className="block text-sm font-semibold text-gray-800">
            Current Password
            <input
              id="txt_ChangeCurrentPassword"
              className="mt-1 w-full rounded-lg border border-gray-200 bg-white px-3.5 py-2.5 text-sm outline-none transition focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100"
              type="password"
              value={currentPassword}
              onChange={(e) => setCurrentPassword(e.target.value)}
            />
          </label>
          <label className="block text-sm font-semibold text-gray-800">
            New Password
            <input
              id="txt_NewPassword"
              className="mt-1 w-full rounded-lg border border-gray-200 bg-white px-3.5 py-2.5 text-sm outline-none transition focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100"
              type="password"
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
            />
          </label>
          <label className="block text-sm font-semibold text-gray-800">
            Confirm New Password
            <input
              id="txt_ConfirmNewPassword"
              className="mt-1 w-full rounded-lg border border-gray-200 bg-white px-3.5 py-2.5 text-sm outline-none transition focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100"
              type="password"
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
            />
          </label>
          <p className="text-xs leading-5 text-gray-500">Changing your password signs you out of every device.</p>
          <div className="flex w-full justify-stretch sm:justify-end">
            <Button type="submit" id="btn_ChangePassword" className="w-full sm:w-auto">Change Password</Button>
          </div>
        </form>
      </div>
    </div>
  );
}
