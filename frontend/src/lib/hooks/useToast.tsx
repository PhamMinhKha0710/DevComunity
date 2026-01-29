'use client';

import { useState, useCallback, createContext, useContext, ReactNode } from 'react';

type ToastType = 'success' | 'error' | 'info' | 'warning';

interface Toast {
    id: string;
    title: string;
    message: string;
    type: ToastType;
}

interface ToastContextType {
    toasts: Toast[];
    showToast: (title: string, message: string, type?: ToastType) => void;
    hideToast: (id: string) => void;
}

const ToastContext = createContext<ToastContextType | undefined>(undefined);

/**
 * Toast provider component - wrap your app with this
 * Converted from public/js/vote-handler.js showToast/hideToast functions
 */
export function ToastProvider({ children }: { children: ReactNode }) {
    const [toasts, setToasts] = useState<Toast[]>([]);

    const showToast = useCallback((title: string, message: string, type: ToastType = 'info') => {
        const id = `toast-${Date.now()}`;
        const newToast: Toast = { id, title, message, type };

        setToasts(prev => [...prev, newToast]);

        // Auto-hide after 5 seconds
        setTimeout(() => {
            hideToast(id);
        }, 5000);
    }, []);

    const hideToast = useCallback((id: string) => {
        setToasts(prev => prev.filter(toast => toast.id !== id));
    }, []);

    return (
        <ToastContext.Provider value={{ toasts, showToast, hideToast }}>
            {children}
            {/* Toast Container */}
            <div
                className="position-fixed top-0 end-0 p-3"
                style={{ zIndex: 1080 }}
            >
                {toasts.map(toast => (
                    <div
                        key={toast.id}
                        className={`toast show toast-${toast.type}`}
                        role="alert"
                        aria-live="assertive"
                        aria-atomic="true"
                    >
                        <div className="toast-header">
                            <strong className="me-auto">
                                {toast.type === 'success' && <i className="bi bi-check-circle-fill text-success me-2" />}
                                {toast.type === 'error' && <i className="bi bi-exclamation-circle-fill text-danger me-2" />}
                                {toast.type === 'warning' && <i className="bi bi-exclamation-triangle-fill text-warning me-2" />}
                                {toast.type === 'info' && <i className="bi bi-info-circle-fill text-info me-2" />}
                                {toast.title}
                            </strong>
                            <button
                                type="button"
                                className="btn-close"
                                onClick={() => hideToast(toast.id)}
                                aria-label="Close"
                            />
                        </div>
                        <div className="toast-body">{toast.message}</div>
                    </div>
                ))}
            </div>
        </ToastContext.Provider>
    );
}

/**
 * Hook to use toast notifications
 */
export function useToast() {
    const context = useContext(ToastContext);
    if (!context) {
        throw new Error('useToast must be used within a ToastProvider');
    }
    return context;
}

export default useToast;
