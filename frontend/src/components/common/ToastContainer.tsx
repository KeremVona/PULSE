import React, { useEffect } from 'react';
import { useAppDispatch, useAppSelector } from '@/store/hooks';
import { dismissNotification } from '@/store/slices/notificationSlice';
import { CheckCircle2, AlertTriangle, AlertCircle, Info, X } from 'lucide-react';
import { makeClassName } from '@/utils/cn';
import { ToastNotification } from '@/types/api';

const ToastItem: React.FC<{ notification: ToastNotification }> = ({ notification }) => {
  const dispatch = useAppDispatch();

  useEffect(() => {
    if (notification.durationMs && notification.durationMs > 0) {
      const timer = setTimeout(() => {
        dispatch(dismissNotification(notification.id));
      }, notification.durationMs);
      return () => clearTimeout(timer);
    }
  }, [notification, dispatch]);

  const icons = {
    success: <CheckCircle2 className="w-5 h-5 text-emerald-500 shrink-0" />,
    warning: <AlertTriangle className="w-5 h-5 text-amber-500 shrink-0" />,
    error: <AlertCircle className="w-5 h-5 text-rose-500 shrink-0" />,
    info: <Info className="w-5 h-5 text-sky-500 shrink-0" />
  };

  const borders = {
    success: 'border-emerald-200 bg-white shadow-emerald-900/5',
    warning: 'border-amber-200 bg-white shadow-amber-900/5',
    error: 'border-rose-200 bg-white shadow-rose-900/5',
    info: 'border-sky-200 bg-white shadow-sky-900/5'
  };

  return (
    <div
      role="alert"
      className={makeClassName(
        'w-96 rounded-xl border p-4 shadow-lg flex items-start gap-3 transition-all animate-in fade-in slide-in-from-top-2 duration-200',
        borders[notification.type]
      )}
    >
      {icons[notification.type]}
      <div className="flex-1 min-w-0">
        <h4 className="text-sm font-semibold text-slate-900 truncate">
          {notification.title}
        </h4>
        <p className="text-xs text-slate-600 mt-0.5 leading-relaxed">
          {notification.message}
        </p>

        {notification.details && notification.details.length > 0 && (
          <ul className="mt-2 text-xs text-rose-700 bg-rose-50/70 p-2 rounded border border-rose-100 list-disc list-inside space-y-1">
            {notification.details.map((detail, idx) => (
              <li key={idx} className="truncate">{detail}</li>
            ))}
          </ul>
        )}
      </div>

      <button
        type="button"
        onClick={() => dispatch(dismissNotification(notification.id))}
        className="text-slate-400 hover:text-slate-600 transition-colors p-1 -mr-1 -mt-1 rounded-md"
        aria-label="Dismiss alert"
      >
        <X className="w-4 h-4" />
      </button>
    </div>
  );
};

export const ToastContainer: React.FC = () => {
  const notifications = useAppSelector((state) => state.notification.notifications);

  if (notifications.length === 0) return null;

  return (
    <div
      aria-live="polite"
      className="fixed top-4 right-4 z-50 flex flex-col gap-2.5 pointer-events-auto max-h-screen overflow-y-auto pr-1"
    >
      {notifications.map((item) => (
        <ToastItem key={item.id} notification={item} />
      ))}
    </div>
  );
};
