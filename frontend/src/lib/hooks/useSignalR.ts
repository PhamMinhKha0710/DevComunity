'use client';

import { useEffect, useRef, useState, useCallback } from 'react';
import * as signalR from '@microsoft/signalr';
import { useAuth } from '@/lib/contexts/AuthContext';

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5122';

export interface SignalRConnection {
    connection: signalR.HubConnection | null;
    connectionState: signalR.HubConnectionState;
    error: Error | null;
}

export function useSignalR(hubName: string): SignalRConnection {
    const { user } = useAuth();
    const connectionRef = useRef<signalR.HubConnection | null>(null);
    const [connectionState, setConnectionState] = useState<signalR.HubConnectionState>(
        signalR.HubConnectionState.Disconnected
    );
    const [error, setError] = useState<Error | null>(null);

    useEffect(() => {
        // Only connect if user is authenticated
        if (!user) {
            return;
        }

        const token = typeof window !== 'undefined' ? localStorage.getItem('accessToken') : null;

        const connection = new signalR.HubConnectionBuilder()
            .withUrl(`${API_BASE_URL}/hubs/${hubName}`, {
                accessTokenFactory: () => token || '',
                skipNegotiation: true,
                transport: signalR.HttpTransportType.WebSockets,
            })
            .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
            .configureLogging(signalR.LogLevel.Information)
            .build();

        connectionRef.current = connection;

        // Connection state change handlers
        connection.onreconnecting((error) => {
            console.log(`SignalR reconnecting to ${hubName}:`, error);
            setConnectionState(signalR.HubConnectionState.Reconnecting);
        });

        connection.onreconnected((connectionId) => {
            console.log(`SignalR reconnected to ${hubName}:`, connectionId);
            setConnectionState(signalR.HubConnectionState.Connected);
        });

        connection.onclose((error) => {
            console.log(`SignalR connection closed for ${hubName}:`, error);
            setConnectionState(signalR.HubConnectionState.Disconnected);
            if (error) setError(error);
        });

        // Start connection
        const startConnection = async () => {
            try {
                await connection.start();
                console.log(`SignalR connected to ${hubName}`);
                setConnectionState(signalR.HubConnectionState.Connected);
                setError(null);
            } catch (err) {
                console.error(`SignalR connection error for ${hubName}:`, err);
                setError(err as Error);
                setConnectionState(signalR.HubConnectionState.Disconnected);
            }
        };

        startConnection();

        return () => {
            if (connection.state === signalR.HubConnectionState.Connected) {
                connection.stop();
            }
        };
    }, [hubName, user]);

    return {
        connection: connectionRef.current,
        connectionState,
        error,
    };
}

// Hook for Notification Hub
export function useNotifications() {
    const { connection, connectionState } = useSignalR('notifications');
    const [notifications, setNotifications] = useState<Notification[]>([]);
    const [unreadCount, setUnreadCount] = useState(0);

    useEffect(() => {
        if (!connection) return;

        connection.on('ReceiveNotification', (notification: Notification) => {
            setNotifications(prev => [notification, ...prev]);
            setUnreadCount(prev => prev + 1);
        });

        connection.on('NotificationRead', (notificationId: number) => {
            setNotifications(prev =>
                prev.map(n => n.notificationId === notificationId ? { ...n, isRead: true } : n)
            );
            setUnreadCount(prev => Math.max(0, prev - 1));
        });

        return () => {
            connection.off('ReceiveNotification');
            connection.off('NotificationRead');
        };
    }, [connection]);

    const markAsRead = useCallback(async (notificationId: number) => {
        if (connection && connectionState === signalR.HubConnectionState.Connected) {
            await connection.invoke('MarkAsRead', notificationId);
        }
    }, [connection, connectionState]);

    return { notifications, unreadCount, markAsRead, connectionState };
}

// Hook for Chat Hub
export function useChat(conversationId?: number) {
    const { connection, connectionState } = useSignalR('chat');
    const [messages, setMessages] = useState<ChatMessage[]>([]);
    const [typingUsers, setTypingUsers] = useState<string[]>([]);

    useEffect(() => {
        if (!connection) return;

        connection.on('ReceiveMessage', (message: ChatMessage) => {
            setMessages(prev => [...prev, message]);
        });

        connection.on('UserTyping', (userId: string, username: string) => {
            setTypingUsers(prev => [...new Set([...prev, username])]);
            setTimeout(() => {
                setTypingUsers(prev => prev.filter(u => u !== username));
            }, 3000);
        });

        connection.on('MessageRead', (messageId: number) => {
            setMessages(prev =>
                prev.map(m => m.messageId === messageId ? { ...m, isRead: true } : m)
            );
        });

        // Join conversation if specified
        if (conversationId) {
            connection.invoke('JoinConversation', conversationId);
        }

        return () => {
            connection.off('ReceiveMessage');
            connection.off('UserTyping');
            connection.off('MessageRead');
            if (conversationId) {
                connection.invoke('LeaveConversation', conversationId);
            }
        };
    }, [connection, conversationId]);

    const sendMessage = useCallback(async (content: string, receiverId?: number) => {
        if (connection && connectionState === signalR.HubConnectionState.Connected) {
            await connection.invoke('SendMessage', conversationId, receiverId, content);
        }
    }, [connection, connectionState, conversationId]);

    const sendTyping = useCallback(async () => {
        if (connection && connectionState === signalR.HubConnectionState.Connected && conversationId) {
            await connection.invoke('SendTyping', conversationId);
        }
    }, [connection, connectionState, conversationId]);

    return { messages, typingUsers, sendMessage, sendTyping, connectionState };
}

// Hook for Question Hub (real-time updates)
export function useQuestionUpdates(questionId?: number) {
    const { connection, connectionState } = useSignalR('question');
    const [voteUpdate, setVoteUpdate] = useState<VoteUpdate | null>(null);
    const [newAnswer, setNewAnswer] = useState<Answer | null>(null);
    const [newComment, setNewComment] = useState<Comment | null>(null);

    useEffect(() => {
        if (!connection) return;

        connection.on('VoteUpdated', (update: VoteUpdate) => {
            setVoteUpdate(update);
        });

        connection.on('NewAnswer', (answer: Answer) => {
            setNewAnswer(answer);
        });

        connection.on('NewComment', (comment: Comment) => {
            setNewComment(comment);
        });

        // Join question room if specified
        if (questionId) {
            connection.invoke('JoinQuestion', questionId);
        }

        return () => {
            connection.off('VoteUpdated');
            connection.off('NewAnswer');
            connection.off('NewComment');
            if (questionId) {
                connection.invoke('LeaveQuestion', questionId);
            }
        };
    }, [connection, questionId]);

    return { voteUpdate, newAnswer, newComment, connectionState };
}

// Types
interface Notification {
    notificationId: number;
    message: string;
    type: string;
    isRead: boolean;
    createdAt: string;
    link?: string;
}

interface ChatMessage {
    messageId: number;
    content: string;
    senderId: number;
    senderName: string;
    createdAt: string;
    isRead: boolean;
}

interface VoteUpdate {
    targetType: 'question' | 'answer';
    targetId: number;
    newScore: number;
}

interface Answer {
    answerId: number;
    body: string;
    authorUsername: string;
    createdAt: string;
}

interface Comment {
    commentId: number;
    body: string;
    authorUsername: string;
    createdAt: string;
    parentType: 'question' | 'answer';
    parentId: number;
}
