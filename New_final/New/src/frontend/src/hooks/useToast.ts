import { useState, useCallback } from 'react';

export type ToastType = 'success' | 'error' | 'warning' | 'info';

export interface Toast {
  id: string;
  type: ToastType;
  title: string;
  message: string;
  duration?: number;
}

interface UseToastReturn {
  toasts: Toast[];
  addToast: (toast: Omit<Toast, 'id'>) => void;
  removeToast: (id: string) => void;
  clearToasts: () => void;
  showSuccess: (title: string, message: string) => void;
  showError: (title: string, message: string) => void;
  showWarning: (title: string, message: string) => void;
  showInfo: (title: string, message: string) => void;
}

/**
 * Custom hook for managing toast notifications
 */
export const useToast = (): UseToastReturn => {
  const [toasts, setToasts] = useState<Toast[]>([]);

  const addToast = useCallback((toast: Omit<Toast, 'id'>) => {
    const id = crypto.randomUUID();
    const newToast: Toast = {
      ...toast,
      id,
      duration: toast.duration ?? 5000,
    };

    setToasts((prev) => [...prev, newToast]);

    // Auto-remove toast after duration
    if (newToast.duration && newToast.duration > 0) {
      setTimeout(() => {
        removeToast(id);
      }, newToast.duration);
    }
  }, []);

  const removeToast = useCallback((id: string) => {
    setToasts((prev) => prev.filter((toast) => toast.id !== id));
  }, []);

  const clearToasts = useCallback(() => {
    setToasts([]);
  }, []);

  const showSuccess = useCallback(
    (title: string, message: string) => {
      addToast({ type: 'success', title, message });
    },
    [addToast]
  );

  const showError = useCallback(
    (title: string, message: string) => {
      addToast({ type: 'error', title, message, duration: 8000 }); // Errors stay longer
    },
    [addToast]
  );

  const showWarning = useCallback(
    (title: string, message: string) => {
      addToast({ type: 'warning', title, message });
    },
    [addToast]
  );

  const showInfo = useCallback(
    (title: string, message: string) => {
      addToast({ type: 'info', title, message });
    },
    [addToast]
  );

  return {
    toasts,
    addToast,
    removeToast,
    clearToasts,
    showSuccess,
    showError,
    showWarning,
    showInfo,
  };
};

/**
 * Parse API error and return user-friendly message
 */
export const parseApiError = (error: any): { title: string; message: string } => {
  // Handle Axios error
  if (error.response) {
    const status = error.response.status;
    const data = error.response.data;

    // RFC 7807 Problem Details format
    if (data?.title && data?.detail) {
      return {
        title: data.title,
        message: data.detail,
      };
    }

    // Standard error response
    switch (status) {
      case 400:
        return {
          title: 'Invalid Request',
          message: data?.message || 'The request was invalid. Please check your input.',
        };
      case 401:
        return {
          title: 'Unauthorized',
          message: 'You are not authorized to perform this action.',
        };
      case 403:
        return {
          title: 'Forbidden',
          message: 'You do not have permission to access this resource.',
        };
      case 404:
        return {
          title: 'Not Found',
          message: data?.message || 'The requested resource was not found.',
        };
      case 409:
        return {
          title: 'Conflict',
          message: data?.message || 'The operation could not be completed due to a conflict.',
        };
      case 422:
        return {
          title: 'Validation Error',
          message: data?.message || 'The provided data failed validation.',
        };
      case 429:
        return {
          title: 'Rate Limited',
          message: 'Too many requests. Please wait a moment and try again.',
        };
      case 500:
        return {
          title: 'Server Error',
          message: 'An unexpected server error occurred. Please try again later.',
        };
      case 502:
        return {
          title: 'Service Unavailable',
          message: 'The server is temporarily unavailable. Please try again.',
        };
      case 503:
        return {
          title: 'Service Unavailable',
          message: 'The service is temporarily unavailable. Please try again.',
        };
      case 504:
        return {
          title: 'Gateway Timeout',
          message: 'The request timed out. Please try again.',
        };
      default:
        return {
          title: 'Error',
          message: data?.message || `An error occurred (${status})`,
        };
    }
  }

  // Network error
  if (error.request) {
    return {
      title: 'Network Error',
      message: 'Unable to connect to the server. Please check your internet connection.',
    };
  }

  // Circuit breaker open
  if (error.message?.includes('Circuit breaker')) {
    return {
      title: 'Service Temporarily Unavailable',
      message: 'The service is experiencing issues. Please wait a moment and try again.',
    };
  }

  // Generic error
  return {
    title: 'Error',
    message: error.message || 'An unexpected error occurred.',
  };
};

export default useToast;
