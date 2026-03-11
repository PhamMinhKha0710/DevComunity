import { create } from 'zustand';

interface Notification {
    notificationId: number;
    message: string;
    type: string;
    isRead: boolean;
    createdDate: string;
    link?: string;
    fromUserId?: number;
    fromUserName?: string;
}

interface NotificationState {
    notifications: Notification[];
    unreadCount: number;
    setNotifications: (notifications: Notification[]) => void;
    addNotification: (notification: Notification) => void;
    markAsRead: (notificationId: number) => void;
    markAllAsRead: () => void;
    clearNotification: (notificationId: number) => void;
}

export const useNotificationStore = create<NotificationState>((set) => ({
    notifications: [],
    unreadCount: 0,

    setNotifications: (notifications) => set({
        notifications,
        unreadCount: notifications.filter(n => !n.isRead).length,
    }),

    addNotification: (notification) => set((state) => ({
        notifications: [notification, ...state.notifications],
        unreadCount: state.unreadCount + 1,
    })),

    markAsRead: (notificationId) => set((state) => ({
        notifications: state.notifications.map(n =>
            n.notificationId === notificationId ? { ...n, isRead: true } : n
        ),
        unreadCount: Math.max(0, state.unreadCount - 1),
    })),

    markAllAsRead: () => set((state) => ({
        notifications: state.notifications.map(n => ({ ...n, isRead: true })),
        unreadCount: 0,
    })),

    clearNotification: (notificationId) => set((state) => ({
        notifications: state.notifications.filter(n => n.notificationId !== notificationId),
    })),
}));
