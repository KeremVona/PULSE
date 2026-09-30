import { describe, it, expect } from 'vitest';
import { makeAppStore } from '@/store';
import {
  addNotification,
  dismissNotification,
  clearAllNotifications,
  makeInitialNotificationState
} from '@/store/slices/notificationSlice';

describe('notificationSlice State Management', () => {
  it('should initialize with empty notifications list', () => {
    const madeInitialState = makeInitialNotificationState();
    expect(madeInitialState.notifications).toHaveLength(0);
  });

  it('should add notification toast with auto-generated id', () => {
    const madeStore = makeAppStore();

    madeStore.dispatch(
      addNotification({
        type: 'warning',
        title: 'Physiological Boundary Warning',
        message: 'Resting BP exceeds 180 mmHg.',
        details: ['Systolic: 185 mmHg', 'Diastolic: 110 mmHg']
      })
    );

    const madeNotifications = madeStore.getState().notification.notifications;
    expect(madeNotifications).toHaveLength(1);
    expect(madeNotifications[0].title).toBe('Physiological Boundary Warning');
    expect(madeNotifications[0].type).toBe('warning');
    expect(madeNotifications[0].id).toContain('notif-');
    expect(madeNotifications[0].details).toHaveLength(2);
  });

  it('should dismiss notification by id', () => {
    const madeStore = makeAppStore();

    madeStore.dispatch(
      addNotification({
        type: 'info',
        title: 'Notice 1',
        message: 'First test notice'
      })
    );
    madeStore.dispatch(
      addNotification({
        type: 'success',
        title: 'Notice 2',
        message: 'Second test notice'
      })
    );

    const madeInitialNotifs = madeStore.getState().notification.notifications;
    expect(madeInitialNotifs).toHaveLength(2);

    const targetId = madeInitialNotifs[0].id;
    madeStore.dispatch(dismissNotification(targetId));

    const madeRemainingNotifs = madeStore.getState().notification.notifications;
    expect(madeRemainingNotifs).toHaveLength(1);
    expect(madeRemainingNotifs[0].title).toBe('Notice 2');
  });

  it('should clear all notifications', () => {
    const madeStore = makeAppStore();

    madeStore.dispatch(addNotification({ type: 'info', title: 'A', message: 'Msg A' }));
    madeStore.dispatch(addNotification({ type: 'error', title: 'B', message: 'Msg B' }));
    expect(madeStore.getState().notification.notifications).toHaveLength(2);

    madeStore.dispatch(clearAllNotifications());
    expect(madeStore.getState().notification.notifications).toHaveLength(0);
  });
});
