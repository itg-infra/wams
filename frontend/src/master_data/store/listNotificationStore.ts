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

let notificationEventSource: ReturnType<typeof createNotificationStream> = null;
let reconnectTimer: ReturnType<typeof setTimeout> | null = null;
let reconnectAttempt = 0;
let reconnectEnabled = false;

const reconnectDelay = () => Math.min(1_000 * 2 ** reconnectAttempt++, 30_000);

const clearReconnectTimer = () => {
  if (reconnectTimer) clearTimeout(reconnectTimer);
  reconnectTimer = null;
};

const toListNotification = (
  notification: NotificationStreamResponse,
): ListNotification => ({
  id: notification.Id,
  type: notification.Type,
  title: notification.Title,
  message: notification.Message,
  referenceType: notification.ReferenceType,
  referenceId: notification.ReferenceId,
  status: notification.Status,
  createdAt: notification.CreatedAt,
  readAt: notification.ReadAt,
  recipientUserId: notification.RecipientUserId,
  actorUserId: notification.ActorUserId,
  route: notification.Route,
});

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
    const { notifications, unreadOnly } = get();

    if (unreadOnly && notification.Status !== "unread") return;
    if (notifications.some((item) => item.id === notification.Id)) return;

    set((state) => {
      return {
        notifications: [toListNotification(notification), ...state.notifications],
        notificationStream: notification,
      };
    });

    showNotificationToast(notification);
  },

  connectStream: () => {
    reconnectEnabled = true;
    if (notificationEventSource || reconnectTimer) return;

    notificationEventSource = createNotificationStream(
      (notification: NotificationStreamResponse) => {
        reconnectAttempt = 0;
        get().addNotification(notification);
      },
      () => {
        notificationEventSource = null;
        if (!reconnectEnabled || reconnectTimer) return;

        reconnectTimer = setTimeout(() => {
          reconnectTimer = null;
          get().connectStream();
        }, reconnectDelay());
      },
      () => {
        reconnectAttempt = 0;
      },
    );

    if (!notificationEventSource) reconnectEnabled = false;
  },

  disconnectStream: () => {
    reconnectEnabled = false;
    reconnectAttempt = 0;
    clearReconnectTimer();
    notificationEventSource?.close();

    notificationEventSource = null;
  },

  reset: () => {
    reconnectEnabled = false;
    reconnectAttempt = 0;
    clearReconnectTimer();
    notificationEventSource?.close();

    notificationEventSource = null;

    set({
      notifications: [],
      notificationStream: null,
      page: 1,
      totalPages: 1,
      hasMore: true,
    });
  },
}));
