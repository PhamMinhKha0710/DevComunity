'use client';

import { createContext, useContext, useState, useEffect, ReactNode, useCallback } from 'react';
import * as signalR from '@microsoft/signalr';
import { useAuth } from './AuthContext';

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5122';

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

interface NotificationContextType {
    notifications: Notification[];
    unreadCount: number;
    isConnected: boolean;
    markAsRead: (notificationId: number) => Promise<void>;
    markAllAsRead: () => Promise<void>;
    clearNotification: (notificationId: number) => void;
}

const NotificationContext = createContext<NotificationContextType | undefined>(undefined);

export function NotificationProvider({ children }: { children: ReactNode }) {
    const { user } = useAuth();
    const [notifications, setNotifications] = useState<Notification[]>([]);
    const [unreadCount, setUnreadCount] = useState(0);
    const [connection, setConnection] = useState<signalR.HubConnection | null>(null);
    const [isConnected, setIsConnected] = useState(false);

    // Fetch initial notifications
    useEffect(() => {
        if (!user) {
            setNotifications([]);
            setUnreadCount(0);
            return;
        }

        const fetchNotifications = async () => {
            try {
                const token = localStorage.getItem('accessToken');
                if (!token) return;

                const response = await fetch(`${API_BASE_URL}/api/Notifications`, {
                    headers: {
                        'Authorization': `Bearer ${token}`
                    }
                });

                if (response.ok) {
                    const data = await response.json();
                    const notifs = Array.isArray(data) ? data : (data.items || []);
                    setNotifications(notifs);
                    setUnreadCount(notifs.filter((n: Notification) => !n.isRead).length);
                }
            } catch (error) {
                console.error('Failed to fetch notifications:', error);
            }
        };

        fetchNotifications();
    }, [user]);

    useEffect(() => {
        if (!user) return;

        const token = typeof window !== 'undefined' ? localStorage.getItem('accessToken') : null;

        const newConnection = new signalR.HubConnectionBuilder()
            .withUrl(`${API_BASE_URL}/hubs/notifications`, {
                accessTokenFactory: () => token || '',
                skipNegotiation: true,
                transport: signalR.HttpTransportType.WebSockets,
            })
            .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
            .configureLogging(signalR.LogLevel.Warning)
            .build();

        // Event handlers
        newConnection.on('ReceiveNotification', (notification: Notification) => {
            setNotifications(prev => [notification, ...prev]);
            setUnreadCount(prev => prev + 1);

            // Show browser notification if permitted
            if (Notification.permission === 'granted') {
                new Notification('SocialTechsy', {
                    body: notification.message,
                    icon: '/images/favicon.ico',
                });
            }
        });

        newConnection.on('NotificationRead', (notificationId: number) => {
            setNotifications(prev =>
                prev.map(n => n.notificationId === notificationId ? { ...n, isRead: true } : n)
            );
            setUnreadCount(prev => Math.max(0, prev - 1));
        });

        newConnection.on('AllNotificationsRead', () => {
            setNotifications(prev => prev.map(n => ({ ...n, isRead: true })));
            setUnreadCount(0);
        });

        newConnection.onreconnected(() => {
            console.log('Notification hub reconnected');
            setIsConnected(true);
        });

        newConnection.onclose(() => {
            console.log('Notification hub disconnected');
            setIsConnected(false);
        });

        let isMounted = true;

        // Start connection
        newConnection.start()
            .then(() => {
                if (!isMounted) {
                    newConnection.stop();
                    console.log('Notification hub connection stopped immediately due to unmount');
                    return;
                }
                console.log('Notification hub connected');
                setIsConnected(true);
                setConnection(newConnection);
            })
            .catch(err => {
                console.error('Notification hub connection error:', err);
            });

        return () => {
            isMounted = false;
            if (newConnection.state === signalR.HubConnectionState.Connected) {
                newConnection.stop().catch(console.error);
            }
        };
    }, [user]);

    const markAsRead = useCallback(async (notificationId: number) => {
        if (connection && connection.state === signalR.HubConnectionState.Connected) {
            try {
                await connection.invoke('MarkAsRead', notificationId);
            } catch (err) {
                console.error('Failed to mark notification as read:', err);
            }
        } else {
            // Fallback to API if SignalR is not connected
            try {
                const token = localStorage.getItem('accessToken');
                await fetch(`${API_BASE_URL}/api/Notifications/${notificationId}/read`, {
                    method: 'PUT',
                    headers: { 'Authorization': `Bearer ${token}` }
                });

                // Update local state
                setNotifications(prev =>
                    prev.map(n => n.notificationId === notificationId ? { ...n, isRead: true } : n)
                );
                setUnreadCount(prev => Math.max(0, prev - 1));
            } catch (err) {
                console.error('Failed to mark read via API:', err);
            }
        }
    }, [connection]);

    const markAllAsRead = useCallback(async () => {
        if (connection && connection.state === signalR.HubConnectionState.Connected) {
            try {
                await connection.invoke('MarkAllAsRead');
            } catch (err) {
                console.error('Failed to mark all notifications as read:', err);
            }
        }
    }, [connection]);

    const clearNotification = useCallback((notificationId: number) => {
        setNotifications(prev => prev.filter(n => n.notificationId !== notificationId));
    }, []);

    return (
        <NotificationContext.Provider
            value={{
                notifications,
                unreadCount,
                isConnected,
                markAsRead,
                markAllAsRead,
                clearNotification,
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
