import { create } from "zustand";

import type {
  ListNotification,
  ListNotificationParams,
} from "../../types/listNotifications";

import {
  getNotifications,
  markNotificationAsRead,
  createNotificationStream,
  markAllNotificationsAsRead,
} from "../../api/services/notif/listNotificationService";
import { showNotificationToast } from "../../utils/notificationToast";
import type { NotificationStreamResponse } from "../../types/notificationStream";
interface NotificationState {
  notifications: ListNotification[];
  notificationStream: NotificationStreamResponse | null;

  loading: boolean;
  loadingMore: boolean;

  page: number;
  totalPages: number;

  unreadOnly: boolean;

  hasMore: boolean;

  fetchNotifications: (unreadOnly?: boolean) => Promise<void>;

  loadMoreNotifications: () => Promise<void>;

  setUnreadOnly: (unreadOnly: boolean) => Promise<void>;

  markAsRead: (notificationId: number) => Promise<void>;

  connectStream: () => void;

  disconnectStream: () => void;

  markAllAsRead: () => Promise<void>;

  addNotification: (notification: NotificationStreamResponse) => void;

  reset: () => void;
}

let notificationEventSource: EventSource | null = null;

export const useNotificationStore = create<NotificationState>((set, get) => ({
  notifications: [],
  notificationStream: null,

  loading: false,
  loadingMore: false,

  page: 1,
  totalPages: 1,

  unreadOnly: false,

  hasMore: true,

  markAllAsRead: async () => {
    try {
      await markAllNotificationsAsRead();

      set((state) => ({
        notifications: state.notifications.map(
          (notification): ListNotification =>
            notification.status === "unread"
              ? {
                  ...notification,
                  status: "read",
                  readAt: new Date().toISOString(),
                }
              : notification,
        ),
      }));
    } catch (error) {
      console.error(error);
    }
  },

  fetchNotifications: async (unreadOnly = false) => {
    set({
      loading: true,
    });

    try {
      const params: ListNotificationParams = {
        unreadOnly,
        page: 1,
        limit: 20,
      };

      const response = await getNotifications(params);

      set({
        notifications: response.data,
        page: response.meta.page,
        totalPages: response.meta.totalPages,
        hasMore: response.meta.page < response.meta.totalPages,
        unreadOnly,
      });
    } finally {
      set({
        loading: false,
      });
    }
  },

  loadMoreNotifications: async () => {
    const { page, totalPages, loadingMore, unreadOnly, notifications } = get();

    if (loadingMore) return;

    if (page >= totalPages) return;

    set({
      loadingMore: true,
    });

    try {
      const nextPage = page + 1;

      const response = await getNotifications({
        unreadOnly,
        page: nextPage,
        limit: 20,
      });

      const merged = [...notifications, ...response.data];

      const uniqueNotifications = Array.from(
        new Map(
          merged.map((notification) => [notification.id, notification]),
        ).values(),
      );

      set({
        notifications: uniqueNotifications,
        page: response.meta.page,
        totalPages: response.meta.totalPages,
        hasMore: response.meta.page < response.meta.totalPages,
      });
    } finally {
      set({
        loadingMore: false,
      });
    }
  },

  setUnreadOnly: async (unreadOnly: boolean) => {
    await get().fetchNotifications(unreadOnly);
  },

  markAsRead: async (notificationId: number) => {
    try {
      await markNotificationAsRead(notificationId);

      set((state) => ({
        notifications: state.notifications.map(
          (notification): ListNotification =>
            notification.id === notificationId
              ? {
                  ...notification,
                  status: "read",
                  readAt: new Date().toISOString(),
                }
              : notification,
        ),
      }));
    } catch (error) {
      console.error(error);
    }
  },

  addNotification: (notification: NotificationStreamResponse) => {
    const { unreadOnly } = get();

    if (unreadOnly && notification.Status !== "unread") return;

    showNotificationToast(notification);

    set((state) => {
      // ✅ state di sini selalu fresh/terbaru
      const exists = state.notifications.some(
        (item) => item.id === notification.Id,
      );

      if (exists) return state;

      showNotificationToast(notification);

      return {
        notificationStream: notification,
      };
    });
  },

  connectStream: () => {
    if (notificationEventSource) {
      return;
    }

    notificationEventSource = createNotificationStream(
      (notification: NotificationStreamResponse) => {
        get().addNotification(notification);
      },
      () => {
        notificationEventSource = null;
      },
    );
  },

  disconnectStream: () => {
    notificationEventSource?.close();

    notificationEventSource = null;
  },

  reset: () => {
    notificationEventSource?.close();

    notificationEventSource = null;

    set({
      notifications: [],
      page: 1,
      totalPages: 1,
      hasMore: true,
    });
  },
}));