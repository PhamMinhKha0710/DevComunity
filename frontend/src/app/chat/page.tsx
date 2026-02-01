'use client';

import { useEffect, useState, useRef, useCallback } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import * as signalR from '@microsoft/signalr';
import { useAuth } from '@/lib/contexts/AuthContext';
import type { Conversation, ChatMessage } from '@/types';
import apiClient from '@/lib/api/client';
import ModernNavbar from '@/components/ModernNavbar';

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5122';

// Extended types for realtime features
interface RealtimeMessage extends ChatMessage {
    status?: 'sending' | 'sent' | 'delivered' | 'read';
}

interface User {
    userId: number;
    username: string;
    displayName?: string;
    profilePicture?: string;
}

export default function ChatPage() {
    const { user, isLoading: authLoading } = useAuth();
    const router = useRouter();
    const searchParams = useSearchParams();

    // State
    const [conversations, setConversations] = useState<Conversation[]>([]);
    const [selectedConversation, setSelectedConversation] = useState<number | null>(null);
    const [messages, setMessages] = useState<RealtimeMessage[]>([]);
    const [newMessage, setNewMessage] = useState('');
    const [isLoading, setIsLoading] = useState(true);

    // Realtime features
    const [connectionState, setConnectionState] = useState<'connecting' | 'connected' | 'disconnected'>('disconnected');
    const [typingUsers, setTypingUsers] = useState<string[]>([]);
    const [onlineUsers, setOnlineUsers] = useState<Set<string>>(new Set());

    // New conversation
    const [showNewChat, setShowNewChat] = useState(false);
    const [searchUser, setSearchUser] = useState('');
    const [userResults, setUserResults] = useState<User[]>([]);
    const [selectedUser, setSelectedUser] = useState<User | null>(null);

    // Refs
    const messagesEndRef = useRef<HTMLDivElement>(null);
    const connectionRef = useRef<signalR.HubConnection | null>(null);
    const presenceConnectionRef = useRef<signalR.HubConnection | null>(null);
    const typingTimeoutRef = useRef<NodeJS.Timeout | null>(null);
    const selectedConversationRef = useRef<number | null>(null);

    // ========== SIGNALR CHAT HUB ==========
    useEffect(() => {
        if (!user) return;

        const token = typeof window !== 'undefined' ? localStorage.getItem('accessToken') : null;
        if (!token) return;

        setConnectionState('connecting');

        const connection = new signalR.HubConnectionBuilder()
            .withUrl(`${API_BASE_URL}/hubs/chat`, {
                accessTokenFactory: () => token,
                skipNegotiation: true,
                transport: signalR.HttpTransportType.WebSockets,
            })
            .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
            .configureLogging(signalR.LogLevel.Warning)
            .build();

        connectionRef.current = connection;

        // Receive message realtime
        connection.on('ReceiveMessage', (message: RealtimeMessage) => {
            console.log('📩 RECEIVED MESSAGE via WebSocket:', message);

            // Skip own messages - optimistic update already shows them
            if (message.senderId === user.userId) {
                setMessages(prev => prev.map(m =>
                    (m.status === 'sending' || m.status === 'sent') &&
                        m.content === message.content &&
                        m.senderId === message.senderId
                        ? { ...message, status: 'delivered' }
                        : m
                ));
                return;
            }

            const currentConvId = selectedConversationRef.current;
            if (currentConvId && message.conversationId === currentConvId) {
                setMessages(prev => {
                    if (prev.some(m => m.messageId === message.messageId)) return prev;
                    return [...prev, { ...message, status: 'delivered' }];
                });
            }

            // Refresh conversations list
            fetchConversations();
        });

        // Typing indicator
        connection.on('UserTyping', (data: { userId: string; isTyping: boolean }) => {
            if (data.isTyping) {
                setTypingUsers(prev => [...new Set([...prev, data.userId])]);
            } else {
                setTypingUsers(prev => prev.filter(u => u !== data.userId));
            }
            setTimeout(() => {
                setTypingUsers(prev => prev.filter(u => u !== data.userId));
            }, 3000);
        });

        // Messages read
        connection.on('MessagesRead', (data: { userId: string; lastMessageId: number }) => {
            setMessages(prev => prev.map(m =>
                typeof m.messageId === 'number' && m.messageId <= data.lastMessageId
                    ? { ...m, status: 'read', isRead: true }
                    : m
            ));
        });

        // Connection handlers
        connection.onreconnecting(() => setConnectionState('connecting'));
        connection.onreconnected(() => {
            setConnectionState('connected');
            if (selectedConversationRef.current) {
                connection.invoke('JoinConversation', selectedConversationRef.current);
            }
        });
        connection.onclose(() => setConnectionState('disconnected'));

        connection.start()
            .then(() => {
                console.log('✅ ChatHub connected');
                setConnectionState('connected');
            })
            .catch(err => {
                console.error('❌ ChatHub error:', err);
                setConnectionState('disconnected');
            });

        return () => {
            connection.stop();
        };
    }, [user]);

    // ========== PRESENCE HUB ==========
    useEffect(() => {
        if (!user) return;

        const token = typeof window !== 'undefined' ? localStorage.getItem('accessToken') : null;
        if (!token) return;

        const presenceConnection = new signalR.HubConnectionBuilder()
            .withUrl(`${API_BASE_URL}/hubs/presence`, {
                accessTokenFactory: () => token,
                skipNegotiation: true,
                transport: signalR.HttpTransportType.WebSockets,
            })
            .withAutomaticReconnect([0, 2000, 5000, 10000])
            .configureLogging(signalR.LogLevel.Warning)
            .build();

        presenceConnectionRef.current = presenceConnection;

        presenceConnection.on('UserOnline', (userId: string) => {
            setOnlineUsers(prev => new Set([...prev, userId]));
        });

        presenceConnection.on('UserOffline', (userId: string) => {
            setOnlineUsers(prev => {
                const newSet = new Set(prev);
                newSet.delete(userId);
                return newSet;
            });
        });

        presenceConnection.start()
            .then(async () => {
                console.log('✅ PresenceHub connected');
                try {
                    const onlineList = await presenceConnection.invoke<string[]>('GetOnlineUsers');
                    setOnlineUsers(new Set(onlineList));
                } catch (err) {
                    console.error('Failed to get online users:', err);
                }
            })
            .catch(err => console.error('❌ PresenceHub error:', err));

        return () => {
            presenceConnection.stop();
        };
    }, [user]);

    // ========== SYNC REF ==========
    useEffect(() => {
        selectedConversationRef.current = selectedConversation;
    }, [selectedConversation]);

    // ========== JOIN/LEAVE CONVERSATION ==========
    useEffect(() => {
        const connection = connectionRef.current;
        if (!connection || connectionState !== 'connected') return;

        if (selectedConversation) {
            connection.invoke('JoinConversation', selectedConversation)
                .then(() => console.log(`📍 Joined conversation ${selectedConversation}`))
                .catch(console.error);
        }

        return () => {
            if (selectedConversation && connection.state === signalR.HubConnectionState.Connected) {
                connection.invoke('LeaveConversation', selectedConversation).catch(console.error);
            }
        };
    }, [selectedConversation, connectionState]);

    // ========== AUTH CHECK ==========
    useEffect(() => {
        if (!authLoading && !user) {
            router.push('/login');
        } else if (user) {
            fetchConversations();
        }
    }, [user, authLoading, router]);

    // Auto scroll
    useEffect(() => {
        messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
    }, [messages]);

    // ========== API CALLS ==========
    const fetchConversations = async () => {
        try {
            const response = await apiClient.get<Conversation[]>('/chat/conversations');
            setConversations(Array.isArray(response.data) ? response.data : []);
        } catch (error) {
            console.error('Failed to fetch conversations:', error);
        } finally {
            setIsLoading(false);
        }
    };

    const fetchMessages = async (conversationId: number) => {
        try {
            const response = await apiClient.get<{ items: ChatMessage[], totalCount: number }>(`/chat/conversations/${conversationId}/messages`);
            const msgs = response.data.items || [];
            setMessages(msgs.map(m => ({ ...m, status: 'delivered' as const })));
            // Mark as read
            await apiClient.put(`/chat/conversations/${conversationId}/read`);
        } catch (error) {
            console.error('Failed to fetch messages:', error);
        }
    };

    // ========== SEND MESSAGE (WebSocket) ==========
    const sendMessage = async () => {
        if (!newMessage.trim() || !selectedConversation) return;

        const tempId = `temp_${Date.now()}`;
        const content = newMessage.trim();

        // Optimistic update
        const optimisticMessage: RealtimeMessage = {
            messageId: parseInt(tempId.replace('temp_', '')),
            conversationId: selectedConversation,
            senderId: user!.userId,
            senderUsername: user!.username,
            content,
            sentDate: new Date().toISOString(),
            isRead: false,
            status: 'sending'
        };

        setMessages(prev => [...prev, optimisticMessage]);
        setNewMessage('');

        try {
            const connection = connectionRef.current;

            if (connection && connection.state === signalR.HubConnectionState.Connected) {
                // Send via WebSocket
                await connection.invoke('SendMessage', selectedConversation, content);
                setMessages(prev => prev.map(m =>
                    m.messageId === optimisticMessage.messageId ? { ...m, status: 'sent' } : m
                ));
            } else {
                // Fallback to REST
                await apiClient.post(`/chat/conversations/${selectedConversation}/messages`, { content });
                setMessages(prev => prev.map(m =>
                    m.messageId === optimisticMessage.messageId ? { ...m, status: 'sent' } : m
                ));
            }
            fetchConversations(); // Update last message
        } catch (error) {
            console.error('Failed to send message:', error);
            setMessages(prev => prev.filter(m => m.messageId !== optimisticMessage.messageId));
            setNewMessage(content);
        }
    };

    // ========== TYPING INDICATOR ==========
    const sendTyping = useCallback(() => {
        const connection = connectionRef.current;
        if (!connection || !selectedConversation || connectionState !== 'connected') return;

        if (typingTimeoutRef.current) clearTimeout(typingTimeoutRef.current);

        connection.invoke('Typing', selectedConversation, true).catch(console.error);

        typingTimeoutRef.current = setTimeout(() => {
            connection.invoke('Typing', selectedConversation, false).catch(console.error);
        }, 2000);
    }, [selectedConversation, connectionState]);

    // ========== NEW CONVERSATION ==========
    const searchUsers = async (query: string) => {
        if (query.length < 2) {
            setUserResults([]);
            return;
        }
        try {
            const response = await apiClient.get(`/users?search=${encodeURIComponent(query)}&pageSize=5`);
            const users = response.data?.items || [];
            setUserResults(users.filter((u: User) => u.userId !== user?.userId));
        } catch (error) {
            console.error('Failed to search users:', error);
        }
    };

    const startNewConversation = async () => {
        if (!selectedUser || !newMessage.trim()) return;

        try {
            const response = await apiClient.post('/chat/conversations', {
                recipientId: selectedUser.userId,
                initialMessage: newMessage.trim()
            });

            const newConv = response.data;
            setNewMessage('');
            setSelectedUser(null);
            setShowNewChat(false);
            setSearchUser('');
            setSelectedConversation(newConv.conversationId);
            fetchMessages(newConv.conversationId);
            fetchConversations();
        } catch (error) {
            console.error('Failed to start conversation:', error);
        }
    };

    // ========== HANDLERS ==========
    const selectConversation = (conversationId: number) => {
        setSelectedConversation(conversationId);
        setShowNewChat(false);
        fetchMessages(conversationId);
    };

    const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        setNewMessage(e.target.value);
        sendTyping();
    };

    // ========== HELPERS ==========
    const formatTime = (dateString: string | null | undefined) => {
        if (!dateString) return '';
        try {
            const date = new Date(dateString);
            if (isNaN(date.getTime())) return '';
            const now = new Date();
            const diff = now.getTime() - date.getTime();
            const diffDays = Math.floor(diff / (1000 * 60 * 60 * 24));

            if (diffDays === 0) {
                return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
            } else if (diffDays === 1) {
                return 'Yesterday';
            } else if (diffDays < 7) {
                return date.toLocaleDateString([], { weekday: 'short' });
            } else {
                return date.toLocaleDateString([], { month: 'short', day: 'numeric' });
            }
        } catch {
            return '';
        }
    };

    const getParticipantName = (conv: Conversation) => {
        if (conv.title) return conv.title;
        const others = conv.participants?.filter(p => p.userId !== user?.userId) || [];
        return others.map(p => p.displayName || p.username).join(', ') || 'New Chat';
    };

    const isParticipantOnline = (conv: Conversation) => {
        const other = conv.participants?.find(p => p.userId !== user?.userId);
        return other ? onlineUsers.has(String(other.userId)) : false;
    };

    const getStatusIcon = (status?: string) => {
        switch (status) {
            case 'sending': return '○';
            case 'sent': return '✓';
            case 'delivered': return '✓✓';
            case 'read': return '✓✓';
            default: return '';
        }
    };

    const selectedConv = conversations.find(c => c.conversationId === selectedConversation);

    if (authLoading || isLoading) {
        return (
            <>
                <ModernNavbar />
                <div className="flex items-center justify-center min-h-screen bg-[var(--bg-primary)]">
                    <div className="w-10 h-10 border-4 border-[var(--primary)]/30 border-t-[var(--primary)] rounded-full animate-spin"></div>
                </div>
            </>
        );
    }

    if (!user) return null;

    return (
        <>
            <ModernNavbar />
            <div className="flex h-[calc(100vh-64px)] bg-[var(--bg-primary)]">
                {/* Conversations Sidebar */}
                <div className="w-80 bg-[var(--bg-secondary)] border-r border-[var(--border-color)] flex flex-col">
                    {/* Header */}
                    <div className="p-4 border-b border-[var(--border-color)]">
                        <div className="flex items-center justify-between">
                            <h2 className="text-lg font-bold text-[var(--text-primary)] flex items-center gap-2">
                                <i className="bi bi-chat-dots-fill text-[var(--primary)]"></i>
                                Messages
                                {/* Connection indicator */}
                                <span className={`w-2 h-2 rounded-full ${connectionState === 'connected' ? 'bg-green-500' :
                                        connectionState === 'connecting' ? 'bg-yellow-500 animate-pulse' :
                                            'bg-red-500'
                                    }`} title={connectionState}></span>
                            </h2>
                            <button
                                onClick={() => { setShowNewChat(true); setSelectedConversation(null); }}
                                className="w-8 h-8 rounded-lg bg-[var(--primary)] text-white flex items-center justify-center hover:bg-[var(--primary-dark)] transition"
                            >
                                <i className="bi bi-plus"></i>
                            </button>
                        </div>
                    </div>

                    {/* Conversation List */}
                    <div className="flex-1 overflow-y-auto">
                        {conversations.length > 0 ? (
                            conversations.map((conv) => (
                                <button
                                    key={conv.conversationId}
                                    onClick={() => selectConversation(conv.conversationId)}
                                    className={`w-full p-4 flex items-start gap-3 border-b border-[var(--border-color)] transition ${selectedConversation === conv.conversationId
                                            ? 'bg-[var(--primary)]/10'
                                            : 'hover:bg-[var(--bg-tertiary)]'
                                        }`}
                                >
                                    {/* Avatar */}
                                    <div className="relative">
                                        <div className="w-12 h-12 rounded-full bg-gradient-to-br from-purple-500 to-pink-500 flex items-center justify-center text-white font-bold">
                                            {getParticipantName(conv).charAt(0).toUpperCase()}
                                        </div>
                                        {/* Online indicator */}
                                        {isParticipantOnline(conv) && (
                                            <span className="absolute bottom-0 right-0 w-3 h-3 bg-green-500 border-2 border-white rounded-full"></span>
                                        )}
                                        {conv.unreadCount > 0 && (
                                            <span className="absolute -top-1 -right-1 min-w-[20px] h-5 flex items-center justify-center px-1.5 text-xs font-bold bg-red-500 text-white rounded-full">
                                                {conv.unreadCount}
                                            </span>
                                        )}
                                    </div>

                                    {/* Info */}
                                    <div className="flex-1 min-w-0 text-left">
                                        <div className="flex items-center justify-between gap-2">
                                            <span className="font-semibold text-[var(--text-primary)] truncate">
                                                {getParticipantName(conv)}
                                            </span>
                                            <span className="text-xs text-[var(--text-muted)] whitespace-nowrap">
                                                {formatTime(conv.lastMessageDate)}
                                            </span>
                                        </div>
                                        <div className="flex items-center gap-1">
                                            <p className="text-sm text-[var(--text-muted)] truncate mt-0.5 flex-1">
                                                {conv.lastMessagePreview || 'No messages yet'}
                                            </p>
                                            {isParticipantOnline(conv) && (
                                                <span className="text-xs text-green-500">Online</span>
                                            )}
                                        </div>
                                    </div>
                                </button>
                            ))
                        ) : (
                            <div className="flex flex-col items-center justify-center h-full text-center p-8">
                                <i className="bi bi-chat-square-dots text-5xl text-[var(--text-muted)] mb-4"></i>
                                <p className="text-[var(--text-muted)]">No conversations yet</p>
                                <button
                                    onClick={() => setShowNewChat(true)}
                                    className="mt-4 px-4 py-2 bg-[var(--primary)] text-white rounded-lg hover:bg-[var(--primary-dark)] transition"
                                >
                                    Start a chat
                                </button>
                            </div>
                        )}
                    </div>
                </div>

                {/* Chat Area */}
                <div className="flex-1 flex flex-col">
                    {showNewChat ? (
                        /* New Chat View */
                        <div className="flex-1 flex flex-col bg-[var(--bg-tertiary)]">
                            <div className="p-4 border-b border-[var(--border-color)] bg-[var(--bg-secondary)]">
                                <h3 className="font-semibold text-[var(--text-primary)]">New Message</h3>
                            </div>

                            <div className="p-4 border-b border-[var(--border-color)] bg-[var(--bg-secondary)]">
                                <div className="relative">
                                    <i className="bi bi-search absolute left-3 top-1/2 -translate-y-1/2 text-[var(--text-muted)]"></i>
                                    <input
                                        type="text"
                                        placeholder="Search users..."
                                        value={searchUser}
                                        onChange={(e) => { setSearchUser(e.target.value); searchUsers(e.target.value); }}
                                        className="w-full pl-10 pr-4 py-2 bg-[var(--bg-tertiary)] border border-[var(--border-color)] rounded-lg text-[var(--text-primary)]"
                                    />
                                </div>

                                {selectedUser && (
                                    <div className="mt-2 flex items-center gap-2">
                                        <span className="px-3 py-1 bg-[var(--primary)] text-white rounded-full text-sm flex items-center gap-2">
                                            {selectedUser.displayName || selectedUser.username}
                                            <button onClick={() => setSelectedUser(null)} className="hover:opacity-80">×</button>
                                        </span>
                                    </div>
                                )}

                                {!selectedUser && userResults.length > 0 && (
                                    <div className="mt-2 bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-lg overflow-hidden">
                                        {userResults.map(u => (
                                            <button
                                                key={u.userId}
                                                onClick={() => { setSelectedUser(u); setSearchUser(''); setUserResults([]); }}
                                                className="w-full p-3 flex items-center gap-3 hover:bg-[var(--bg-tertiary)] text-left"
                                            >
                                                <div className="w-10 h-10 rounded-full bg-gradient-to-br from-blue-500 to-purple-500 flex items-center justify-center text-white font-bold">
                                                    {(u.displayName || u.username).charAt(0).toUpperCase()}
                                                </div>
                                                <div>
                                                    <div className="font-semibold text-[var(--text-primary)]">{u.displayName || u.username}</div>
                                                    <div className="text-sm text-[var(--text-muted)]">@{u.username}</div>
                                                </div>
                                            </button>
                                        ))}
                                    </div>
                                )}
                            </div>

                            <div className="flex-1"></div>

                            {selectedUser && (
                                <div className="p-4 border-t border-[var(--border-color)] bg-[var(--bg-secondary)]">
                                    <div className="flex items-center gap-3">
                                        <input
                                            type="text"
                                            placeholder="Type a message..."
                                            value={newMessage}
                                            onChange={(e) => setNewMessage(e.target.value)}
                                            onKeyDown={(e) => e.key === 'Enter' && startNewConversation()}
                                            className="flex-1 px-4 py-3 bg-[var(--bg-tertiary)] border border-[var(--border-color)] rounded-xl text-[var(--text-primary)]"
                                        />
                                        <button
                                            onClick={startNewConversation}
                                            disabled={!newMessage.trim()}
                                            className="w-12 h-12 rounded-xl bg-[var(--primary)] text-white flex items-center justify-center hover:bg-[var(--primary-dark)] disabled:opacity-50 transition"
                                        >
                                            <i className="bi bi-send-fill"></i>
                                        </button>
                                    </div>
                                </div>
                            )}
                        </div>
                    ) : selectedConversation ? (
                        <>
                            {/* Chat Header */}
                            <div className="p-4 border-b border-[var(--border-color)] bg-[var(--bg-secondary)]">
                                <div className="flex items-center gap-3">
                                    <div className="relative">
                                        <div className="w-10 h-10 rounded-full bg-gradient-to-br from-purple-500 to-pink-500 flex items-center justify-center text-white font-bold">
                                            {selectedConv ? getParticipantName(selectedConv).charAt(0).toUpperCase() : '?'}
                                        </div>
                                        {selectedConv && isParticipantOnline(selectedConv) && (
                                            <span className="absolute bottom-0 right-0 w-3 h-3 bg-green-500 border-2 border-white rounded-full"></span>
                                        )}
                                    </div>
                                    <div>
                                        <h3 className="font-semibold text-[var(--text-primary)]">
                                            {selectedConv ? getParticipantName(selectedConv) : 'Chat'}
                                        </h3>
                                        {selectedConv && isParticipantOnline(selectedConv) ? (
                                            <span className="text-xs text-green-500 flex items-center gap-1">
                                                <span className="w-2 h-2 rounded-full bg-green-500"></span>
                                                Online
                                            </span>
                                        ) : (
                                            <span className="text-xs text-[var(--text-muted)]">Offline</span>
                                        )}
                                    </div>
                                </div>
                            </div>

                            {/* Messages */}
                            <div className="flex-1 overflow-y-auto p-6 space-y-4 bg-[var(--bg-tertiary)]">
                                {messages.map((msg) => (
                                    <div
                                        key={msg.messageId}
                                        className={`flex ${msg.senderId === user.userId ? 'justify-end' : 'justify-start'}`}
                                    >
                                        {msg.senderId !== user.userId && (
                                            <div className="w-8 h-8 rounded-full bg-gradient-to-br from-blue-500 to-purple-500 flex items-center justify-center text-white text-sm font-bold mr-2 flex-shrink-0">
                                                {msg.senderUsername?.charAt(0).toUpperCase() || '?'}
                                            </div>
                                        )}
                                        <div
                                            className={`max-w-[70%] px-4 py-3 rounded-2xl ${msg.senderId === user.userId
                                                    ? `bg-[var(--primary)] text-white rounded-tr-sm ${msg.status === 'sending' ? 'opacity-70' : ''}`
                                                    : 'bg-[var(--bg-secondary)] text-[var(--text-primary)] border border-[var(--border-color)] rounded-tl-sm'
                                                }`}
                                        >
                                            <p className="break-words">{msg.content}</p>
                                            <span className={`text-xs mt-1 block ${msg.senderId === user.userId ? 'text-white/70' : 'text-[var(--text-muted)]'
                                                }`}>
                                                {formatTime(msg.sentDate)}
                                                {msg.senderId === user.userId && (
                                                    <span className={`ml-1 ${msg.status === 'read' ? 'text-blue-300' : ''}`}>
                                                        {getStatusIcon(msg.status)}
                                                    </span>
                                                )}
                                            </span>
                                        </div>
                                    </div>
                                ))}

                                {/* Typing indicator */}
                                {typingUsers.length > 0 && (
                                    <div className="flex items-center gap-2 text-[var(--text-muted)]">
                                        <div className="flex gap-1">
                                            <span className="w-2 h-2 bg-[var(--text-muted)] rounded-full animate-bounce"></span>
                                            <span className="w-2 h-2 bg-[var(--text-muted)] rounded-full animate-bounce" style={{ animationDelay: '0.1s' }}></span>
                                            <span className="w-2 h-2 bg-[var(--text-muted)] rounded-full animate-bounce" style={{ animationDelay: '0.2s' }}></span>
                                        </div>
                                        <span className="text-sm">typing...</span>
                                    </div>
                                )}

                                <div ref={messagesEndRef} />
                            </div>

                            {/* Message Input */}
                            <div className="p-4 border-t border-[var(--border-color)] bg-[var(--bg-secondary)]">
                                <div className="flex items-center gap-3">
                                    <input
                                        type="text"
                                        placeholder="Type a message..."
                                        value={newMessage}
                                        onChange={handleInputChange}
                                        onKeyDown={(e) => e.key === 'Enter' && sendMessage()}
                                        className="flex-1 px-4 py-3 bg-[var(--bg-tertiary)] border border-[var(--border-color)] rounded-xl text-[var(--text-primary)] placeholder-[var(--text-muted)] focus:border-[var(--primary)] focus:ring-2 focus:ring-[var(--primary)]/20 transition"
                                    />
                                    <button
                                        onClick={sendMessage}
                                        disabled={!newMessage.trim()}
                                        className="w-12 h-12 rounded-xl bg-[var(--primary)] text-white flex items-center justify-center hover:bg-[var(--primary-dark)] disabled:opacity-50 transition"
                                    >
                                        <i className="bi bi-send-fill"></i>
                                    </button>
                                </div>
                            </div>
                        </>
                    ) : (
                        <div className="flex-1 flex flex-col items-center justify-center text-center bg-[var(--bg-tertiary)]">
                            <i className="bi bi-chat-square-dots text-6xl text-[var(--text-muted)] mb-4"></i>
                            <h3 className="text-xl font-semibold text-[var(--text-primary)] mb-2">Select a conversation</h3>
                            <p className="text-[var(--text-muted)]">Choose a conversation from the list or start a new one</p>
                        </div>
                    )}
                </div>
            </div>
        </>
    );
}
