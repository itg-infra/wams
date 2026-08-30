"use client";

import { Bell } from "lucide-react";
import { useEffect, useRef, useState } from "react";
import { useNotificationStore } from "../master_data/store/listNotificationStore";
import { formatRelativeTime } from "./format/formatRelativeTime";
import { useNotificationController } from "../master_data/controller/listNotificationController";
import type { ListNotification } from "../types/listNotifications";

interface Props {
  notifications: ListNotification[];
  unreadCount: number;
  onViewAll?: () => void;
  onMarkAllRead?: () => void;
  onNotificationClick?: (notification: ListNotification) => void;
}

export default function NotificationDropdown({
  unreadCount,
  onMarkAllRead,
  onNotificationClick,
}: Props) {
  const [open, setOpen] = useState(false);

  const dropdownRef = useRef<HTMLDivElement>(null);
  const observerRef = useRef<HTMLDivElement>(null);

  const {
    notifications,
    hasMore,
    loadingMore,
    unreadOnly,
    fetchNotifications,
    loadMoreNotifications,
    setUnreadOnly,
  } = useNotificationStore();

  const { handleNotificationClick } = useNotificationController();

  useEffect(() => {
    if (open && notifications.length === 0) {
      fetchNotifications(unreadOnly);
    }
  }, [open]);

  useEffect(() => {
    if (!open) return;

    const observer = new IntersectionObserver(
      (entries) => {
        const first = entries[0];

        if (first.isIntersecting && hasMore && !loadingMore) {
          loadMoreNotifications();
        }
      },
      {
        threshold: 0.1,
      },
    );

    const current = observerRef.current;

    if (current) {
      observer.observe(current);
    }

    return () => {
      if (current) {
        observer.unobserve(current);
      }

      observer.disconnect();
    };
  }, [open, hasMore, loadingMore, loadMoreNotifications]);

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (
        dropdownRef.current &&
        !dropdownRef.current.contains(event.target as Node)
      ) {
        setOpen(false);
      }
    };

    document.addEventListener("mousedown", handleClickOutside);

    return () => {
      document.removeEventListener("mousedown", handleClickOutside);
    };
  }, []);

  const handleUnreadToggle = async () => {
    await setUnreadOnly(!unreadOnly);
  };

  return (
    <div ref={dropdownRef} className="relative">
      <button
        id="btn_Notification"
        onClick={() => setOpen(!open)}
        className="relative w-9 h-9 rounded-full border border-gray-200 flex items-center justify-center hover:bg-gray-50 transition"
      >
        <Bell className="w-4 h-4 text-gray-500" />

        {unreadCount > 0 && (
          <span className="absolute -top-1 -right-1 min-w-4.5 h-4.5 px-1 bg-red-500 text-white text-[10px] rounded-full flex items-center justify-center font-semibold">
            {unreadCount > 99 ? "99+" : unreadCount}
          </span>
        )}
      </button>

      {open && (
        <div
          id="lsb_Notification"
          className="
    fixed
    top-16
    left-2
    right-2

    sm:absolute
    sm:left-auto
    sm:right-0
    sm:top-11
    sm:w-95

    bg-white
    border border-gray-200
    rounded-2xl
    shadow-xl
    z-50
    overflow-hidden
  "
        >
          <div className="sticky top-0 bg-white z-10 px-4 py-3 border-b">
            <div className="flex items-center justify-between">
              <div>
                <h3 className="text-sm font-semibold text-gray-800">
                  Notifications
                </h3>

                <p className="text-xs text-gray-500">
                  {unreadCount} unread notifications
                </p>
              </div>

              {unreadCount > 0 && (
                <button
                  onClick={onMarkAllRead}
                  className="text-xs font-medium text-indigo-600 hover:text-indigo-700"
                >
                  Mark all read
                </button>
              )}
            </div>

            {/* FILTER UNREAD */}
            <div className="mt-2">
              <button
                onClick={handleUnreadToggle}
                className={`text-xs font-medium ${
                  unreadOnly ? "text-indigo-600" : "text-gray-500"
                }`}
              >
                {unreadOnly ? "Showing unread only" : "Show unread only"}
              </button>
            </div>
          </div>

          <div className="max-h-[70vh] sm:max-h-105 overflow-y-auto">
            {notifications.length === 0 ? (
              <div className="py-10 text-center">
                <p className="text-sm text-gray-500">No notifications</p>
              </div>
            ) : (
              <>
                {notifications.map((item) => (
                  <button
                    key={item.id}
                    onClick={async () => {
                      await handleNotificationClick(item);

                      onNotificationClick?.(item);
                    }}
                    className="w-full text-left px-4 py-3 border-b hover:bg-gray-50 transition"
                  >
                    <div className="flex gap-3">
                      <div className="pt-1">
                        {item.status === "unread" && (
                          <div className="w-2 h-2 rounded-full bg-blue-500" />
                        )}
                      </div>

                      <div className="flex-1 min-w-0">
                        <h4 className="text-sm font-medium text-gray-800 line-clamp-1">
                          {item.title}
                        </h4>

                        <p className="text-xs text-gray-500 mt-1 line-clamp-2">
                          {item.message}
                        </p>

                        <span className="block mt-2 text-[11px] text-gray-400">
                          {formatRelativeTime(item.createdAt)}
                        </span>
                      </div>
                    </div>
                  </button>
                ))}

                {/* SENTINEL */}
                <div ref={observerRef} className="h-1" />

                {loadingMore && (
                  <div className="py-3 text-center text-xs text-gray-500">
                    Loading more...
                  </div>
                )}
              </>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
