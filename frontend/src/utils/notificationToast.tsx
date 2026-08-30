import toast from "react-hot-toast";
import { NotificationToast } from "../components/notificationToast";
import type { NotificationStreamResponse } from "../types/notificationStream";

export const showNotificationToast = (
  notification: NotificationStreamResponse,
) => {
  toast.custom(
    (t) => (
      <div
        className={`transition-all duration-300 ${
          t.visible ? "translate-x-0 opacity-100" : "translate-x-8 opacity-0"
        }`}
      >
        <NotificationToast
          notification={notification}
          onDismiss={() => toast.dismiss(t.id)}
        />
      </div>
    ),
    {
      duration: 5000,
      position: "top-right",
    },
  );
};
