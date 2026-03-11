'use client';

import { useNotifications } from '@/lib/contexts/NotificationContext';
import { useAuth } from '@/lib/contexts/AuthContext';
import { useRouter } from 'next/navigation';
import { useEffect } from 'react';
import AppLayout from '@/components/AppLayout';

export default function NotificationsPage() {
    const { user, isLoading } = useAuth();
    const { notifications, unreadCount, markAsRead, markAllAsRead } = useNotifications();
    const router = useRouter();

    useEffect(() => {
        if (!isLoading && !user) {
            router.push('/login');
        }
    }, [user, isLoading, router]);

    if (isLoading) {
        return (
            <AppLayout showRightSidebar={false}>
                <div className="flex items-center justify-center py-20">
                    <div className="w-10 h-10 border-4 border-[var(--primary)]/30 border-t-[var(--primary)] rounded-full animate-spin"></div>
                </div>
            </AppLayout>
        );
    }

    if (!user) return null;

    const getNotificationIcon = (type: string) => {
        switch (type) {
            case 'answer': return { icon: 'chat', color: 'text-green-500', bg: 'bg-green-500/20' };
            case 'comment': return { icon: 'comment', color: 'text-blue-500', bg: 'bg-blue-500/20' };
            case 'vote': return { icon: 'thumb_up', color: 'text-yellow-500', bg: 'bg-yellow-500/20' };
            case 'accepted': return { icon: 'check_circle', color: 'text-green-500', bg: 'bg-green-500/20' };
            case 'mention': return { icon: 'alternate_email', color: 'text-purple-500', bg: 'bg-purple-500/20' };
            case 'follow': return { icon: 'person_add', color: 'text-pink-500', bg: 'bg-pink-500/20' };
            default: return { icon: 'notifications', color: 'text-[var(--text-muted)]', bg: 'bg-[var(--bg-tertiary)]' };
        }
    };

    const formatDate = (dateString: string) => {
        // Ensure UTC dates from backend are parsed correctly
        const normalized = dateString.endsWith('Z') || dateString.includes('+') ? dateString : dateString + 'Z';
        const date = new Date(normalized);
        const now = new Date();
        const diffMs = now.getTime() - date.getTime();
        const diffMins = Math.floor(diffMs / 60000);
        const diffHours = Math.floor(diffMs / 3600000);
        const diffDays = Math.floor(diffMs / 86400000);

        if (diffMins < 60) return `${diffMins}m ago`;
        if (diffHours < 24) return `${diffHours}h ago`;
        if (diffDays < 7) return `${diffDays}d ago`;
        return date.toLocaleDateString();
    };

    return (
        <AppLayout showRightSidebar={false}>
            {/* Header */}
            <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4 mb-6">
                <div>
                    <h1 className="text-2xl font-bold text-[var(--text-primary)] flex items-center gap-2">
                        <span className="material-symbols-outlined text-[var(--primary)]">notifications</span>
                        Notifications
                    </h1>
                    <p className="text-[var(--text-muted)]">
                        {unreadCount > 0 ? `${unreadCount} unread notifications` : 'All caught up!'}
                    </p>
                </div>
                {unreadCount > 0 && (
                    <button
                        onClick={markAllAsRead}
                        className="flex items-center gap-2 px-4 py-2 border border-[var(--border-color)] rounded-xl text-[var(--text-secondary)] hover:border-[var(--primary)] hover:text-[var(--primary)] transition"
                    >
                        <span className="material-symbols-outlined">done_all</span>
                        Mark all as read
                    </button>
                )}
            </div>

            {/* Notifications List */}
            {notifications.length > 0 ? (
                <div className="space-y-3">
                    {notifications.map((notification: any) => {
                        const iconStyle = getNotificationIcon(notification.type);
                        return (
                            <button
                                key={notification.notificationId}
                                onClick={() => !notification.isRead && markAsRead(notification.notificationId)}
                                className={`w-full text-left p-4 rounded-2xl border transition flex items-start gap-4 ${!notification.isRead
                                        ? 'bg-[var(--primary)]/5 border-[var(--primary)]/30'
                                        : 'bg-[var(--bg-secondary)] border-[var(--border-color)] hover:border-[var(--border-color)]'
                                    }`}
                            >
                                <div className={`w-10 h-10 rounded-xl ${iconStyle.bg} flex items-center justify-center flex-shrink-0`}>
                                    <span className={`material-symbols-outlined ${iconStyle.color}`}>{iconStyle.icon}</span>
                                </div>
                                <div className="flex-1 min-w-0">
                                    <p className={`text-[var(--text-primary)] ${!notification.isRead ? 'font-medium' : ''}`}>
                                        {notification.message}
                                    </p>
                                    <span className="text-sm text-[var(--text-muted)]">
                                        {formatDate(notification.createdDate)}
                                    </span>
                                </div>
                                {!notification.isRead && (
                                    <span className="px-2 py-1 text-xs font-medium bg-[var(--primary)] text-white rounded-lg">
                                        New
                                    </span>
                                )}
                            </button>
                        );
                    })}
                </div>
            ) : (
                <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-2xl p-12 text-center">
                    <span className="material-symbols-outlined text-5xl text-[var(--text-muted)] mb-4">notifications_off</span>
                    <h3 className="text-lg font-semibold text-[var(--text-primary)] mb-2">No notifications yet</h3>
                    <p className="text-[var(--text-muted)]">When you get notifications, they&apos;ll show up here</p>
                </div>
            )}
        </AppLayout>
    );
}
