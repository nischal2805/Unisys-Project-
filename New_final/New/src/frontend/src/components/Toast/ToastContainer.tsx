import React from 'react';
import { Toast } from '../../hooks/useToast';
import './ToastContainer.css';

interface ToastContainerProps {
  toasts: Toast[];
  onRemove: (id: string) => void;
}

const getToastIcon = (type: Toast['type']): string => {
  switch (type) {
    case 'success':
      return 'OK';
    case 'error':
      return 'X';
    case 'warning':
      return '!';
    case 'info':
      return 'i';
    default:
      return 'i';
  }
};

const ToastContainer: React.FC<ToastContainerProps> = ({ toasts, onRemove }) => {
  if (toasts.length === 0) return null;

  return (
    <div className="toast-container" role="region" aria-label="Notifications">
      {toasts.map((toast) => (
        <div
          key={toast.id}
          className={`toast toast-${toast.type}`}
          role="alert"
          aria-live="polite"
        >
          <span className="toast-icon">{getToastIcon(toast.type)}</span>
          <div className="toast-content">
            <div className="toast-title">{toast.title}</div>
            <div className="toast-message">{toast.message}</div>
          </div>
          <button
            className="toast-close"
            onClick={() => onRemove(toast.id)}
            aria-label="Close notification"
          >
            ×
          </button>
        </div>
      ))}
    </div>
  );
};

export default ToastContainer;
