'use client';

import { useNotifications } from '@/lib/contexts/NotificationContext';
import { useAuth } from '@/lib/contexts/AuthContext';
import { useRouter } from 'next/navigation';
import { useEffect } from 'react';

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
            <div className="container py-5 text-center">
                <div className="spinner-border text-primary" role="status">
                    <span className="visually-hidden">Loading...</span>
                </div>
            </div>
        );
    }

    if (!user) return null;

    const getNotificationIcon = (type: string) => {
        switch (type) {
            case 'answer': return 'bi-chat-left-text text-success';
            case 'comment': return 'bi-chat-dots text-info';
            case 'vote': return 'bi-hand-thumbs-up text-warning';
            case 'accepted': return 'bi-check-circle text-success';
            case 'mention': return 'bi-at text-primary';
            default: return 'bi-bell text-secondary';
        }
    };

    return (
        <div className="container py-4">
            <div className="row justify-content-center">
                <div className="col-lg-8">
                    <div className="d-flex justify-content-between align-items-center mb-4">
                        <div>
                            <h2 className="fw-bold mb-1">Notifications</h2>
                            <p className="text-muted mb-0">
                                {unreadCount > 0 ? `${unreadCount} unread notifications` : 'All caught up!'}
                            </p>
                        </div>
                        {unreadCount > 0 && (
                            <button className="btn btn-outline-primary rounded-pill" onClick={markAllAsRead}>
                                <i className="bi bi-check-all me-2"></i>Mark all as read
                            </button>
                        )}
                    </div>

                    <div className="card border-0 shadow-sm rounded-4">
                        {notifications.length > 0 ? (
                            <div className="list-group list-group-flush">
                                {notifications.map((notification) => (
                                    <div
                                        key={notification.notificationId}
                                        className={`list-group-item p-3 border-0 border-bottom ${!notification.isRead ? 'bg-light' : ''}`}
                                        onClick={() => !notification.isRead && markAsRead(notification.notificationId)}
                                        style={{ cursor: 'pointer' }}
                                    >
                                        <div className="d-flex">
                                            <div className="me-3">
                                                <i className={`bi ${getNotificationIcon(notification.type)} fs-4`}></i>
                                            </div>
                                            <div className="flex-grow-1">
                                                <p className="mb-1">{notification.message}</p>
                                                <small className="text-muted">
                                                    {new Date(notification.createdAt).toLocaleDateString('en-US', {
                                                        month: 'short',
                                                        day: 'numeric',
                                                        hour: '2-digit',
                                                        minute: '2-digit'
                                                    })}
                                                </small>
                                            </div>
                                            {!notification.isRead && (
                                                <span className="badge bg-primary rounded-pill">New</span>
                                            )}
                                        </div>
                                    </div>
                                ))}
                            </div>
                        ) : (
                            <div className="card-body text-center py-5">
                                <i className="bi bi-bell-slash fs-1 text-muted mb-3"></i>
                                <h5>No notifications yet</h5>
                                <p className="text-muted">When you get notifications, they'll show up here</p>
                            </div>
                        )}
                    </div>
                </div>
            </div>
        </div>
    );
}
