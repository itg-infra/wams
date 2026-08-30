import { useState } from "react";
import {
  LogOut,
  ChevronDown,
  PanelLeftOpen,
  PanelLeftClose,
} from "lucide-react";
import { useDashboardController } from "../controllers/dashboard/dashboardController";
import Sidebar from "../components/sidebar/sidebar";

import { useSidebar } from "../hook/useSideBar";
import { Outlet, useLocation, useNavigate } from "react-router-dom";
import { useCompanyStore } from "../store/companyStore";
import { companyLogoMap, defaultLogo } from "../assets/companyLogo";
import NotificationDropdown from "../components/notificationDropDown";
import { useNotificationController } from "../master_data/controller/listNotificationController";
import { LogoutDialog } from "../components/logOutDialog";
import { WarehouseSelector } from "../components/warehouseSelector";
import PermissionGuard from "../components/guards/permissionGuard";
import type { ListNotification } from "../types/listNotifications";

export default function HomeDashboardScreen() {
  const {
    showLogoutDialog,
    isLogoutLoading,
    displayName,
    initials,
    handleOpenLogout,
    handleCancelLogout,
    handleConfirmLogout,
  } = useDashboardController();

  // const { notifications, loadMoreNotifications, hasMore } =
  const { notifications, handleMarkAllRead, handleNotificationClick } = useNotificationController();

  // const unreadCount = notifications.filter(
  //   (item) => item.status === "unread",
  // ).length;

  const sidebarItems = useSidebar();

  const navigate = useNavigate();
  const location = useLocation();

  const activePage = location.pathname;

  const handleNavigate = (path: string) => {
    navigate(path);
  };

  const selectedCompanyId = useCompanyStore((s) => s.selectedCompanyId);
  const logo = companyLogoMap[selectedCompanyId ?? -1] ?? defaultLogo;

  const [sidebarCollapsed, setSidebarCollapsed] = useState(false);

  const [mobileSidebarOpen, setMobileSidebarOpen] = useState(false);

  return (
    <div className="flex min-h-screen  bg-[#F8F8F8]">
      <LogoutDialog
        open={showLogoutDialog}
        isLoading={isLogoutLoading}
        onConfirm={handleConfirmLogout}
        onCancel={handleCancelLogout}
      />

      {/* SIDEBAR */}
      {/* Desktop Sidebar */}
      <div className="hidden md:block">
        <Sidebar
          items={sidebarItems}
          activePage={activePage}
          onNavigate={handleNavigate}
          collapsed={sidebarCollapsed}
          onToggleCollapse={() => setSidebarCollapsed((prev) => !prev)}
          header={
            <div className="flex items-center justify-center py-2">
              <img
                src={logo}
                alt="Company Logo"
                className={`object-contain transition-all duration-300 ${
                  sidebarCollapsed ? "h-8 w-8" : "h-10 w-auto"
                }`}
              />
            </div>
          }
          bottomItems={
            <button
              id="btn_SignOut"
              onClick={handleOpenLogout}
              className="w-full flex items-center gap-2.5 px-3 py-2 rounded-lg text-sm text-gray-500 hover:bg-red-50 hover:text-red-500 transition-all duration-150"
            >
              <LogOut className="w-4 h-4" />
              {!sidebarCollapsed && <span>Sign Out</span>}
            </button>
          }
        />
      </div>

      {/* Mobile Sidebar */}
      {mobileSidebarOpen && (
        <>
          <div
            className="fixed inset-0 bg-black/40 z-40 md:hidden"
            onClick={() => setMobileSidebarOpen(false)}
          />

          <div className="fixed left-0 top-0 h-full z-50 md:hidden">
            <Sidebar
              items={sidebarItems}
              activePage={activePage}
              onNavigate={(path) => {
                handleNavigate(path);
                setMobileSidebarOpen(false);
              }}
              collapsed={false}
              onToggleCollapse={() => {}}
              header={
                <div className="flex items-center justify-center py-2">
                  <img
                    src={logo}
                    alt="Company Logo"
                    className="h-10 w-auto object-contain"
                  />
                </div>
              }
              bottomItems={
                <button
                  id="btn_SignOutMobile"
                  onClick={handleOpenLogout}
                  className="w-full flex items-center gap-2.5 px-3 py-2 rounded-lg text-sm text-gray-500 hover:bg-red-50 hover:text-red-500 transition-all duration-150"
                >
                  <LogOut className="w-4 h-4" />
                  <span>Sign Out</span>
                </button>
              }
            />
          </div>
        </>
      )}

      {/* MAIN */}
      <div className="flex-1 flex flex-col min-h-screen overflow-hidden">
        {/* TOPBAR */}
        <div className="flex items-center px-6 py-3 bg-white border-b border-gray-100 sticky top-0 z-10">
          <button
            id="btn_SidebarToggle"
            onClick={() => {
              if (window.innerWidth < 768) {
                setMobileSidebarOpen(true);
              } else {
                setSidebarCollapsed((prev) => !prev);
              }
            }}
            className="p-2 mr-2 rounded-lg border border-gray-200 hover:bg-gray-50 transition"
          >
            {sidebarCollapsed ? (
              <PanelLeftOpen className="w-5 h-5" />
            ) : (
              <PanelLeftClose className="w-5 h-5" />
            )}
          </button>
          <div className="flex items-center justify-between w-full gap-3">
            <PermissionGuard permission="user.warehouse.read">
              <div className="shrink-0">
                <WarehouseSelector />
              </div>
            </PermissionGuard>

            {/* Right */}
            <div className="flex items-center gap-3">
              {/* NotificationDropdown renders its own <button id="btn_Notification">.
                  Wrapping it in another <button> nests interactive elements, which is
                  invalid HTML and swallows the inner click. */}
              <div className="w-9 h-9 rounded-full border border-gray-200 flex items-center justify-center hover:bg-gray-50 transition">
                <NotificationDropdown
                  notifications={notifications}
                  unreadCount={
                    notifications.filter((n) => n.status === "unread").length
                  }
                  onMarkAllRead={() => {
                    handleMarkAllRead();
                  }}
                  onNotificationClick={async (
                    notification: ListNotification,
                  ) => {
                    await handleNotificationClick(notification); 
                    navigate(notification.route);

                  }}
                />
              </div>
              <div className="relative group">
                <button
                  id="btn_UserMenu"
                  className="flex items-center gap-2 border border-gray-200 rounded-full pl-1 pr-3 py-1 hover:bg-gray-50 transition"
                >
                  <div className="w-7 h-7 bg-indigo-600 rounded-full flex items-center justify-center text-white text-xs font-bold">
                    {initials}
                  </div>
                  <span className="hidden sm:block text-sm font-medium text-gray-700">
                    {displayName}
                  </span>
                  <ChevronDown className="hidden sm:block w-3.5 h-3.5 text-gray-400" />
                </button>
                <div
                  id="lsb_UserMenu"
                  className="absolute right-0 top-11 w-44 bg-white border rounded-xl shadow-lg opacity-0 invisible group-hover:opacity-100 group-hover:visible transition-all duration-200 z-50"
                >
                  <div className="px-4 py-3 border-b">
                    <p className="text-xs font-semibold text-gray-800 truncate">
                      {displayName}
                    </p>
                  </div>
                  <button
                    id="btn_Logout"
                    onClick={handleOpenLogout}
                    className="w-full flex items-center gap-2 px-4 py-3 text-sm text-red-500 hover:bg-red-50 rounded-b-xl transition"
                  >
                    <LogOut className="w-4 h-4" />
                    Logout
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* PAGE CONTENT */}
        <Outlet />
      </div>
    </div>
  );
}
