'use client';

import { createContext, useContext, useEffect, ReactNode, useCallback } from 'react';
import { useAuth } from './AuthContext';
import { useNotificationStore } from '@/lib/stores/notificationStore';
import { useHub } from '@/lib/signalr/useHub';
import { HubConnectionState } from '@microsoft/signalr';
import apiClient from '@/lib/api/client';
import toast from 'react-hot-toast';

interface NotificationContextType {
    notifications: any[];
    unreadCount: number;
    isConnected: boolean;
    markAsRead: (notificationId: number) => Promise<void>;
    markAllAsRead: () => Promise<void>;
    clearNotification: (notificationId: number) => void;
}

const NotificationContext = createContext<NotificationContextType | undefined>(undefined);

export function NotificationProvider({ children }: { children: ReactNode }) {
    const { user } = useAuth();
    const store = useNotificationStore();
    const hub = useHub('notifications');

    useEffect(() => {
        if (!user) {
            store.setNotifications([]);
            return;
        }

        const fetchNotifications = async () => {
            try {
                const response = await apiClient.get('/Notifications');
                const data = response.data;
                const notifs = Array.isArray(data) ? data : (data.items || []);
                store.setNotifications(notifs);
            } catch (error) {
                console.error('Failed to fetch notifications:', error);
            }
        };

        fetchNotifications();
    }, [user]);

    useEffect(() => {
        if (!user) return;

        const handleReceive = (notification: any) => {
            store.addNotification(notification);

            toast(notification.message, {
                icon: notification.type === 'message' ? '💬'
                    : notification.type === 'friend_request' ? '👥'
                    : notification.type === 'like' ? '❤️'
                    : '🔔',
                duration: 4000,
                style: { borderRadius: '10px', background: '#333', color: '#fff' },
            });

            if (typeof Notification !== 'undefined' && Notification.permission === 'granted') {
                new Notification('SocialTechsy', {
                    body: notification.message,
                    icon: '/images/favicon.ico',
                });
            }
        };

        const handleRead = (notificationId: number) => {
            store.markAsRead(notificationId);
        };

        const handleAllRead = () => {
            store.markAllAsRead();
        };

        hub.on('ReceiveNotification', handleReceive);
        hub.on('NotificationRead', handleRead);
        hub.on('AllNotificationsRead', handleAllRead);

        return () => {
            hub.off('ReceiveNotification', handleReceive);
            hub.off('NotificationRead', handleRead);
            hub.off('AllNotificationsRead', handleAllRead);
        };
    }, [user, hub]);

    const markAsRead = useCallback(async (notificationId: number) => {
        try {
            await hub.invoke('MarkAsRead', notificationId);
        } catch {
            try {
                await apiClient.put(`/Notifications/${notificationId}/read`);
                store.markAsRead(notificationId);
            } catch (err) {
                console.error('Failed to mark read via API:', err);
            }
        }
    }, [hub]);

    const markAllAsRead = useCallback(async () => {
        try {
            await hub.invoke('MarkAllAsRead');
        } catch (err) {
            console.error('Failed to mark all notifications as read:', err);
        }
    }, [hub]);

    const isConnected = hub.connectionState === HubConnectionState.Connected;

    return (
        <NotificationContext.Provider
            value={{
                notifications: store.notifications,
                unreadCount: store.unreadCount,
                isConnected,
                markAsRead,
                markAllAsRead,
                clearNotification: store.clearNotification,
            }}
        >
            {children}
        </NotificationContext.Provider>
    );
}

export function useNotifications() {
    const context = useContext(NotificationContext);
    if (context === undefined) {
        throw new Error('useNotifications must be used within a NotificationProvider');
    }
    return context;
}
