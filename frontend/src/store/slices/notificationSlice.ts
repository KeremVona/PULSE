import { createSlice, PayloadAction } from '@reduxjs/toolkit';
import { ToastNotification, ToastType } from '@/types/api';

export interface NotificationState {
  notifications: ToastNotification[];
}

export function makeInitialNotificationState(): NotificationState {
  return {
    notifications: []
  };
}

export function makeNotificationId(): string {
  return 'notif-' + Math.random().toString(36).substring(2, 9) + '-' + Date.now();
}

export const notificationSlice = createSlice({
  name: 'notification',
  initialState: makeInitialNotificationState(),
  reducers: {
    addNotification: (
      state,
      action: PayloadAction<{
        type: ToastType;
        title: string;
        message: string;
        details?: string[];
        durationMs?: number;
      }>
    ) => {
      const madeNotification: ToastNotification = {
        id: makeNotificationId(),
        type: action.payload.type,
        title: action.payload.title,
        message: action.payload.message,
        details: action.payload.details,
        durationMs: action.payload.durationMs ?? 6000
      };
      state.notifications.push(madeNotification);
    },
    dismissNotification: (state, action: PayloadAction<string>) => {
      state.notifications = state.notifications.filter((n) => n.id !== action.payload);
    },
    clearAllNotifications: (state) => {
      state.notifications = [];
    }
  }
});

export const { addNotification, dismissNotification, clearAllNotifications } = notificationSlice.actions;
export default notificationSlice.reducer;
