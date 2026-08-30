// NotificationStream.tsx

import { useEffect } from "react";
import { useNotificationStore } from "../master_data/store/listNotificationStore";

export default function NotificationStream() {
  const connectStream = useNotificationStore((state) => state.connectStream);

  const disconnectStream = useNotificationStore(
    (state) => state.disconnectStream,
  );

  useEffect(() => {
    connectStream();

    return () => {
      disconnectStream();
    };
  }, []);

  return null;
}
