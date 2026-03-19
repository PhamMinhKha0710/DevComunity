'use client';

import { createContext, useContext, useEffect, ReactNode, useCallback } from 'react';
import { useAuth } from './AuthContext';
import { useNotificationStore } from '@/lib/stores/notificationStore';
import { useHub } from '@/lib/signalr/useHub';
import { HubConnectionState } from '@microsoft/signalr';
import { useQueryClient } from '@tanstack/react-query';
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
    const queryClient = useQueryClient();

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

        const handleFriendRequestReceived = (data: any) => {
            const fromName = data.fromUser?.displayName || data.fromUser?.username || 'Someone';
            toast.success(`Bạn có lời mời kết bạn từ ${fromName}`, {
                icon: '👥',
                duration: 4000,
                style: { borderRadius: '10px', background: '#333', color: '#fff' },
            });
            queryClient.invalidateQueries({ queryKey: ['friends'] });
            queryClient.invalidateQueries({ queryKey: ['friendRequests'] });
        };

        const handleFriendRequestAccepted = (data: any) => {
            const byName = data.acceptedBy?.displayName || data.acceptedBy?.username || 'Someone';
            toast.success(`${byName} đã chấp nhận lời mời kết bạn`, {
                icon: '✅',
                duration: 4000,
                style: { borderRadius: '10px', background: '#333', color: '#fff' },
            });
            queryClient.invalidateQueries({ queryKey: ['friends'] });
            queryClient.invalidateQueries({ queryKey: ['friendRequests'] });
        };

        const handleFriendRequestRejected = () => {
            toast('Lời mời kết bạn đã bị từ chối', {
                icon: '❌',
                duration: 3000,
                style: { borderRadius: '10px', background: '#333', color: '#fff' },
            });
            queryClient.invalidateQueries({ queryKey: ['friendRequests'] });
        };

        const handleRead = (notificationId: number) => {
            store.markAsRead(notificationId);
        };

        const handleAllRead = () => {
            store.markAllAsRead();
        };

        const handleReputationChanged = (data: { newScore: number, delta: number, reason: string }) => {
            const isGain = data.delta > 0;
            const sign = isGain ? '+' : '';
            toast(`Danh tiếng ${sign}${data.delta} điểm (${data.reason})`, {
                icon: isGain ? '🚀' : '📉',
                duration: 4000,
                style: { 
                    borderRadius: '12px', 
                    background: isGain ? 'linear-gradient(to right, #10b981, #059669)' : 'linear-gradient(to right, #ef4444, #dc2626)', 
                    color: '#fff', 
                    fontWeight: 'bold',
                    boxShadow: '0 4px 12px rgba(0,0,0,0.15)'
                },
            });
            queryClient.invalidateQueries({ queryKey: ['user'] });
        };

        const handleBadgeEarned = (data: { badgeName: string; badgeType: string; description?: string }) => {
            const badgeEmoji = data.badgeType === 'gold' ? '🥇' : data.badgeType === 'silver' ? '🥈' : '🥉';
            toast.success(`${badgeEmoji} Bạn đã đạt huy hiệu mới: ${data.badgeName}!`, {
                icon: badgeEmoji,
                duration: 6000,
                style: { 
                    borderRadius: '12px', 
                    background: 'linear-gradient(to right, #6366f1, #8b5cf6)', 
                    color: '#fff', 
                    fontWeight: 'bold',
                    boxShadow: '0 4px 12px rgba(99, 102, 241, 0.4)'
                },
            });
            queryClient.invalidateQueries({ queryKey: ['user'] });
            queryClient.invalidateQueries({ queryKey: ['badges'] });
        };

        hub.on('ReceiveNotification', handleReceive);
        hub.on('NotificationRead', handleRead);
        hub.on('AllNotificationsRead', handleAllRead);
        hub.on('FriendRequestReceived', handleFriendRequestReceived);
        hub.on('FriendRequestAccepted', handleFriendRequestAccepted);
        hub.on('FriendRequestRejected', handleFriendRequestRejected);
        hub.on('ReputationChanged', handleReputationChanged);
        hub.on('BadgeEarned', handleBadgeEarned);

        return () => {
            hub.off('ReceiveNotification', handleReceive);
            hub.off('NotificationRead', handleRead);
            hub.off('AllNotificationsRead', handleAllRead);
            hub.off('FriendRequestReceived', handleFriendRequestReceived);
            hub.off('FriendRequestAccepted', handleFriendRequestAccepted);
            hub.off('FriendRequestRejected', handleFriendRequestRejected);
            hub.off('ReputationChanged', handleReputationChanged);
            hub.off('BadgeEarned', handleBadgeEarned);
        };
    }, [user, hub, queryClient]);

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
