import { useEffect } from "react";
import { useNotificationStore } from "../store/listNotificationStore";
import type { ListNotification } from "../../types/listNotifications";

export const useNotificationController = () => {
  const {
    notifications,
    loading,
    loadingMore,

    page,
    totalPages,

    hasMore,
    unreadOnly,

    fetchNotifications,
    loadMoreNotifications,
    setUnreadOnly,
    markAsRead,
    markAllAsRead,

    connectStream,
    disconnectStream,
  } = useNotificationStore();

  useEffect(() => {
    fetchNotifications(false);

    connectStream();

    return () => {
      disconnectStream();
    };
  }, []);

  const handleMarkAllRead = async () => {
    await markAllAsRead();
  };

   const handleNotificationClick = async (notification: ListNotification) => {
     if (notification.status === "unread") {
       await markAsRead(notification.id);
     }

     return notification;
   };

  return {
    notifications,

    loading,
    loadingMore,

    page,
    totalPages,

    hasMore,

    unreadOnly,

    refresh: () => fetchNotifications(unreadOnly),

    loadMoreNotifications,

    setUnreadOnly,

    handleNotificationClick,

    handleMarkAllRead,
  };
};
