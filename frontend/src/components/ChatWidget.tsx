'use client';

import { useEffect, useState, useRef, useCallback, useMemo } from 'react';
import * as signalR from '@microsoft/signalr';
import { useAuth } from '@/lib/contexts/AuthContext';
import apiClient from '@/lib/api/client';

/**
 * ============================================================
 * CHAT WIDGET WITH WEBSOCKET (SIGNALR) - LIKE FACEBOOK/INSTAGRAM
 * ============================================================
 * 
 * Kiến trúc realtime:
 * - WebSocket connection luôn mở (không polling)
 * - Server push message tức thời
 * - Typing indicator realtime
 * - Seen/Delivered status
 * - Auto-reconnect khi mất kết nối
 */

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5122';

// ==================== TYPES ====================
interface Participant {
    userId: number;
    username: string;
    displayName?: string;
    profilePicture?: string;
}

interface Conversation {
    conversationId: number;
    title?: string;
    participants: Participant[];
    lastMessagePreview?: string;
    lastMessageDate?: string;
    unreadCount: number;
}

interface ChatMessage {
    messageId: number | string;
    conversationId: number;
    senderId: number;
    senderUsername?: string;
    senderProfilePicture?: string;
    content: string;
    sentDate: string;
    isRead: boolean;
    status?: 'sending' | 'sent' | 'delivered' | 'read';
}

interface User {
    userId: number;
    username: string;
    displayName?: string;
    profilePicture?: string;
}

interface TypingUser {
    userId: string;
    isTyping: boolean;
}

// ==================== MAIN COMPONENT ====================
export default function ChatWidget() {
    // ========== AUTH & STATE ==========
    const { user, isLoading: authLoading } = useAuth();
    const [isOpen, setIsOpen] = useState(false);
    const [isMinimized, setIsMinimized] = useState(false);
    const [activeView, setActiveView] = useState<'list' | 'chat' | 'new'>('list');

    // Conversations & Messages
    const [conversations, setConversations] = useState<Conversation[]>([]);
    const [selectedConversation, setSelectedConversation] = useState<Conversation | null>(null);
    const [messages, setMessages] = useState<ChatMessage[]>([]);
    const [newMessage, setNewMessage] = useState('');
    const [isLoading, setIsLoading] = useState(false);

    // New conversation
    const [users, setUsers] = useState<User[]>([]);
    const [searchUser, setSearchUser] = useState('');
    const [selectedUser, setSelectedUser] = useState<User | null>(null);

    // Realtime features
    const [totalUnread, setTotalUnread] = useState(0);
    const [typingUsers, setTypingUsers] = useState<string[]>([]);
    const [connectionState, setConnectionState] = useState<'connecting' | 'connected' | 'disconnected'>('disconnected');
    const [onlineUsers, setOnlineUsers] = useState<Set<string>>(new Set());
    const [notification, setNotification] = useState<{ message: string; sender: string } | null>(null);

    // Refs
    const messagesEndRef = useRef<HTMLDivElement>(null);
    const connectionRef = useRef<signalR.HubConnection | null>(null);
    const presenceConnectionRef = useRef<signalR.HubConnection | null>(null);
    const typingTimeoutRef = useRef<NodeJS.Timeout | null>(null);
    const selectedConversationRef = useRef<Conversation | null>(null);

    // ========== WEBSOCKET CONNECTION ==========

    /**
     * Khởi tạo SignalR WebSocket connection
     * - Auto-reconnect với exponential backoff
     * - JWT authentication qua access_token query param
     */
    useEffect(() => {
        // Connect when user is logged in (not just when widget is open)
        // This ensures messages are received even when chat widget is closed
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
            .withAutomaticReconnect([0, 2000, 5000, 10000, 30000]) // Reconnect intervals
            .configureLogging(signalR.LogLevel.Warning)
            .build();

        connectionRef.current = connection;

        // ===== EVENT HANDLERS =====

        // Nhận tin nhắn mới realtime
        connection.on('ReceiveMessage', (message: ChatMessage) => {
            console.log('📩 RECEIVED MESSAGE via WebSocket:', {
                messageId: message.messageId,
                conversationId: message.conversationId,
                senderId: message.senderId,
                myUserId: user.userId,
                content: message.content?.substring(0, 50)
            });

            // SKIP messages from myself - optimistic update already added them
            // This prevents duplicate: optimistic (tempId) + server (real messageId)
            if (message.senderId === user.userId) {
                console.log('⏭️ Skipping own message (already shown via optimistic update)');
                // Just update the temp message with real messageId if needed
                setMessages(prev => prev.map(m =>
                    (m.status === 'sending' || m.status === 'sent') &&
                        m.content === message.content &&
                        m.senderId === message.senderId
                        ? { ...message, status: 'delivered' }
                        : m
                ));
                return;
            }

            // Use REF to get current conversation (fixes closure bug)
            const currentConv = selectedConversationRef.current;

            // Nếu message thuộc conversation đang mở, thêm vào messages
            if (currentConv && message.conversationId === currentConv.conversationId) {
                setMessages(prev => {
                    // Tránh duplicate
                    if (prev.some(m => m.messageId === message.messageId)) return prev;
                    console.log('✅ Adding message from other user');
                    return [...prev, { ...message, status: 'delivered' }];
                });
            } else {
                console.log('ℹ️ Message for different conversation, updating list...');
            }

            // Cập nhật unread nếu không đang mở conversation đó  
            if (!currentConv || message.conversationId !== currentConv.conversationId) {
                setTotalUnread(prev => prev + 1);
            }
            // Refresh conversation list để cập nhật last message
            fetchConversations();
        });

        // Người khác đang gõ
        connection.on('UserTyping', (data: TypingUser) => {
            if (data.isTyping) {
                setTypingUsers(prev => [...new Set([...prev, data.userId])]);
            } else {
                setTypingUsers(prev => prev.filter(u => u !== data.userId));
            }
            // Auto clear typing sau 3s
            setTimeout(() => {
                setTypingUsers(prev => prev.filter(u => u !== data.userId));
            }, 3000);
        });

        // Tin nhắn đã được đọc
        connection.on('MessagesRead', (data: { userId: string; lastMessageId: number }) => {
            setMessages(prev => prev.map(m =>
                typeof m.messageId === 'number' && m.messageId <= data.lastMessageId ? { ...m, status: 'read', isRead: true } : m
            ));
        });

        // Notification khi có tin nhắn mới từ conversation khác
        connection.on('NewMessageNotification', (data: { conversationId: number; messagePreview: string; senderName: string }) => {
            console.log('🔔 New message notification:', data);
            // Chỉ hiển thị nếu không đang mở conversation đó
            if (!selectedConversation || selectedConversation.conversationId !== data.conversationId) {
                setNotification({
                    message: data.messagePreview,
                    sender: data.senderName || 'Someone'
                });
                // Play notification sound
                try {
                    const audio = new Audio('/notification.mp3');
                    audio.volume = 0.3;
                    audio.play().catch(() => { }); // Ignore if audio fails
                } catch { }
            }
            // Refresh conversations để cập nhật unread count
            fetchConversations();
        });

        // Connection state handlers
        connection.onreconnecting(() => {
            console.log('🔄 SignalR reconnecting...');
            setConnectionState('connecting');
        });

        connection.onreconnected(() => {
            console.log('✅ SignalR reconnected');
            setConnectionState('connected');
            // Rejoin conversation nếu đang mở
            if (selectedConversation) {
                connection.invoke('JoinConversation', selectedConversation.conversationId);
            }
        });

        connection.onclose(() => {
            console.log('❌ SignalR disconnected');
            setConnectionState('disconnected');
        });

        let isMounted = true;

        // Start connection
        connection.start()
            .then(() => {
                if (!isMounted) {
                    connection.stop();
                    console.log('SignalR connection stopped immediately due to unmount');
                    return;
                }
                console.log('✅ SignalR connected to ChatHub');
                setConnectionState('connected');
            })
            .catch(err => {
                console.error('❌ SignalR connection error:', err);
                if (isMounted) {
                    setConnectionState('disconnected');
                }
            });

        return () => {
            isMounted = false;
            if (connection.state === signalR.HubConnectionState.Connected) {
                connection.stop().catch(console.error);
            }
        };
    }, [user]); // Removed isOpen dependency - stay connected when logged in

    // ========== PRESENCE HUB CONNECTION ==========
    /**
     * PresenceHub để theo dõi user online/offline
     * - Nhận sự kiện UserOnline/UserOffline từ server
     * - GetOnlineUsers() khi kết nối để lấy danh sách ban đầu
     */
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

        // User đăng nhập online
        presenceConnection.on('UserOnline', (userId: string) => {
            console.log('🟢 User online:', userId);
            setOnlineUsers(prev => new Set([...prev, userId]));
        });

        // User offline
        presenceConnection.on('UserOffline', (userId: string) => {
            console.log('🔴 User offline:', userId);
            setOnlineUsers(prev => {
                const newSet = new Set(prev);
                newSet.delete(userId);
                return newSet;
            });
        });

        let isMounted = true;

        presenceConnection.start()
            .then(async () => {
                if (!isMounted) {
                    presenceConnection.stop();
                    return;
                }
                console.log('✅ PresenceHub connected');
                // Lấy danh sách users đang online
                try {
                    const onlineList = await presenceConnection.invoke<string[]>('GetOnlineUsers');
                    console.log('👥 Online users:', onlineList);
                    if (isMounted) {
                        setOnlineUsers(new Set(onlineList));
                    }
                } catch (err) {
                    console.error('Failed to get online users:', err);
                }
            })
            .catch(err => console.error('❌ PresenceHub connection error:', err));

        return () => {
            isMounted = false;
            if (presenceConnection.state === signalR.HubConnectionState.Connected) {
                presenceConnection.stop().catch(console.error);
            }
        };
    }, [user]);

    // ========== NOTIFICATION HANDLING ==========
    // Auto-hide notification after 4 seconds
    useEffect(() => {
        if (notification) {
            const timer = setTimeout(() => setNotification(null), 4000);
            return () => clearTimeout(timer);
        }
    }, [notification]);

    // ========== SYNC REF WITH STATE ==========
    // Keep selectedConversationRef in sync with state (fixes closure bug in event handlers)
    useEffect(() => {
        selectedConversationRef.current = selectedConversation;
        console.log('🔄 selectedConversationRef updated:', selectedConversation?.conversationId);
    }, [selectedConversation]);

    // Join/Leave conversation khi chọn
    useEffect(() => {
        const connection = connectionRef.current;
        if (!connection || connectionState !== 'connected') return;

        if (selectedConversation) {
            connection.invoke('JoinConversation', selectedConversation.conversationId)
                .then(() => console.log(`📍 Joined conversation ${selectedConversation.conversationId}`))
                .catch(console.error);
        }

        return () => {
            if (selectedConversation && connection.state === signalR.HubConnectionState.Connected) {
                connection.invoke('LeaveConversation', selectedConversation.conversationId)
                    .catch(console.error);
            }
        };
    }, [selectedConversation, connectionState]);

    // Auto-scroll
    useEffect(() => {
        messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
    }, [messages]);

    // Fetch conversations khi mở
    useEffect(() => {
        if (user && isOpen) {
            fetchConversations();
        }
    }, [user, isOpen]);

    // ========== API CALLS ==========

    const fetchConversations = async () => {
        try {
            setIsLoading(true);
            const response = await apiClient.get('/chat/conversations');
            // API returns paginated response: { items: [...], totalCount, page, pageSize }
            const convs = response.data?.items || response.data || [];
            const convList = Array.isArray(convs) ? convs : [];
            setConversations(convList);
            const unread = convList.reduce((acc: number, c: Conversation) => acc + (c.unreadCount || 0), 0);
            setTotalUnread(unread);
        } catch (error) {
            console.error('Failed to fetch conversations:', error);
            setConversations([]);
        } finally {
            setIsLoading(false);
        }
    };

    const fetchMessages = async (conversationId: number, silent = false) => {
        try {
            if (!silent) setIsLoading(true);
            const response = await apiClient.get(`/chat/conversations/${conversationId}/messages`);
            const msgs = response.data?.items || response.data?.messages || [];
            setMessages(Array.isArray(msgs) ? msgs.map((m: ChatMessage) => ({ ...m, status: 'delivered' })) : []);
            // Mark as read
            await apiClient.put(`/chat/conversations/${conversationId}/read`);
        } catch (error) {
            if (!silent) console.error('Failed to fetch messages:', error);
        } finally {
            if (!silent) setIsLoading(false);
        }
    };

    // ========== REALTIME ACTIONS ==========

    /**
     * Gửi tin nhắn qua WebSocket
     * Fallback to REST API nếu WebSocket không available
     */
    const sendMessage = async () => {
        if (!newMessage.trim() || !selectedConversation) return;

        const tempId = `temp_${Date.now()}`;
        const content = newMessage.trim();

        // Optimistic update - hiển thị ngay tin nhắn
        const optimisticMessage: ChatMessage = {
            messageId: tempId,
            conversationId: selectedConversation.conversationId,
            senderId: user!.userId,
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
                // ✅ Gửi qua WebSocket (instant)
                await connection.invoke('SendMessage', selectedConversation.conversationId, content);
                // Update status
                setMessages(prev => prev.map(m =>
                    m.messageId === tempId ? { ...m, status: 'sent' } : m
                ));
            } else {
                // Fallback to REST API
                await apiClient.post(`/chat/conversations/${selectedConversation.conversationId}/messages`, { content });
                setMessages(prev => prev.map(m =>
                    m.messageId === tempId ? { ...m, status: 'sent' } : m
                ));
            }
        } catch (error) {
            console.error('Failed to send message:', error);
            // Remove optimistic message on error
            setMessages(prev => prev.filter(m => m.messageId !== tempId));
            setNewMessage(content); // Restore message
        }
    };

    /**
     * Gửi typing indicator
     * Debounced để không spam
     */
    const sendTyping = useCallback(() => {
        const connection = connectionRef.current;
        if (!connection || !selectedConversation || connectionState !== 'connected') return;

        // Clear previous timeout
        if (typingTimeoutRef.current) {
            clearTimeout(typingTimeoutRef.current);
        }

        // Send typing = true
        connection.invoke('Typing', selectedConversation.conversationId, true).catch(console.error);

        // Auto stop typing after 2s
        typingTimeoutRef.current = setTimeout(() => {
            connection.invoke('Typing', selectedConversation.conversationId, false).catch(console.error);
        }, 2000);
    }, [selectedConversation, connectionState]);

    /**
     * Bắt đầu conversation mới
     */
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
            setSearchUser('');
            setActiveView('chat');
            setSelectedConversation(newConv);
            fetchMessages(newConv.conversationId);
            fetchConversations();
        } catch (error) {
            console.error('Failed to start conversation:', error);
        }
    };

    const searchUsers = async (query: string) => {
        if (query.length < 2) {
            setUsers([]);
            return;
        }
        try {
            const response = await apiClient.get(`/users?search=${encodeURIComponent(query)}&pageSize=5`);
            const userList = response.data?.items || [];
            setUsers(userList.filter((u: User) => u.userId !== user?.userId));
        } catch (error) {
            console.error('Failed to search users:', error);
        }
    };

    // ========== HANDLERS ==========

    const handleConversationClick = (conv: Conversation) => {
        setSelectedConversation(conv);
        setActiveView('chat');
        fetchMessages(conv.conversationId);
    };

    const handleBackToList = () => {
        setActiveView('list');
        setSelectedConversation(null);
        setMessages([]);
        fetchConversations();
    };

    const handleNewChat = () => {
        setActiveView('new');
        setSelectedUser(null);
        setSearchUser('');
        setUsers([]);
    };

    const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        setNewMessage(e.target.value);
        sendTyping(); // Send typing indicator
    };

    const handleKeyPress = (e: React.KeyboardEvent) => {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            if (activeView === 'new' && selectedUser) {
                startNewConversation();
            } else {
                sendMessage();
            }
        }
    };

    // ========== HELPERS ==========

    const formatTime = (dateString: string) => {
        if (!dateString) return '';
        const date = new Date(dateString);
        if (isNaN(date.getTime())) return '';

        const now = new Date();
        const diffDays = Math.floor((now.getTime() - date.getTime()) / (1000 * 60 * 60 * 24));

        if (diffDays === 0) {
            return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
        } else if (diffDays === 1) {
            return 'Yesterday';
        } else if (diffDays < 7) {
            return date.toLocaleDateString([], { weekday: 'short' });
        }
        return date.toLocaleDateString([], { month: 'short', day: 'numeric' });
    };

    const getParticipantName = (conv: Conversation) => {
        if (conv.title) return conv.title;
        if (selectedUser && (!conv.participants || conv.participants.length === 0)) {
            return selectedUser.displayName || selectedUser.username;
        }
        const otherParticipants = conv.participants?.filter(p => p.userId !== user?.userId) || [];
        if (otherParticipants.length === 0) {
            return selectedUser?.displayName || selectedUser?.username || 'New Chat';
        }
        return otherParticipants.map(p => p.displayName || p.username).join(', ');
    };

    const getParticipantAvatar = (conv: Conversation) => {
        const otherParticipant = conv.participants?.find(p => p.userId !== user?.userId);
        return otherParticipant?.profilePicture || '/images/default-avatar.png';
    };

    // Check if participant is online
    const isParticipantOnline = (conv: Conversation): boolean => {
        const otherParticipant = conv.participants?.find(p => p.userId !== user?.userId);
        if (!otherParticipant) return false;
        return onlineUsers.has(String(otherParticipant.userId));
    };

    // Get other participant's userId for online check
    const getOtherParticipantId = (conv: Conversation): number | null => {
        const otherParticipant = conv.participants?.find(p => p.userId !== user?.userId);
        return otherParticipant?.userId || null;
    };

    const getMessageStatusIcon = (status?: string) => {
        switch (status) {
            case 'sending': return '○';
            case 'sent': return '✓';
            case 'delivered': return '✓✓';
            case 'read': return '✓✓';
            default: return '';
        }
    };

    // Message grouping for Instagram-style bubbles
    const getMessageGroupPosition = (messages: ChatMessage[], index: number): 'single' | 'group-first' | 'group-middle' | 'group-last' => {
        const msg = messages[index];
        const prevMsg = messages[index - 1];
        const nextMsg = messages[index + 1];
        
        const sameSenderAsPrev = prevMsg && prevMsg.senderId === msg.senderId;
        const sameSenderAsNext = nextMsg && nextMsg.senderId === msg.senderId;
        
        // Check time gap (5 minutes)
        const TIME_GAP = 5 * 60 * 1000;
        const prevTimeDiff = prevMsg ? new Date(msg.sentDate).getTime() - new Date(prevMsg.sentDate).getTime() : Infinity;
        const nextTimeDiff = nextMsg ? new Date(nextMsg.sentDate).getTime() - new Date(msg.sentDate).getTime() : Infinity;
        
        const groupWithPrev = sameSenderAsPrev && prevTimeDiff < TIME_GAP;
        const groupWithNext = sameSenderAsNext && nextTimeDiff < TIME_GAP;
        
        if (!groupWithPrev && !groupWithNext) return 'single';
        if (!groupWithPrev && groupWithNext) return 'group-first';
        if (groupWithPrev && groupWithNext) return 'group-middle';
        return 'group-last';
    };

    const shouldShowAvatar = (messages: ChatMessage[], index: number): boolean => {
        const pos = getMessageGroupPosition(messages, index);
        return pos === 'single' || pos === 'group-last';
    };

    const shouldShowTime = (messages: ChatMessage[], index: number): boolean => {
        const pos = getMessageGroupPosition(messages, index);
        return pos === 'single' || pos === 'group-last';
    };

    // ========== RENDER ==========

    if (authLoading || !user) {
        return null;
    }

    return (
        <>
            {/* ========== FLOATING BUTTON ========== */}
            <button
                onClick={() => setIsOpen(!isOpen)}
                className="chat-widget-button"
                title="Messages"
            >
                <span className="material-symbols-outlined">{isOpen ? 'close' : 'chat_bubble'}</span>
                {!isOpen && totalUnread > 0 && (
                    <span className="chat-widget-badge">{totalUnread > 9 ? '9+' : totalUnread}</span>
                )}
            </button>

            {/* ========== CHAT POPUP ========== */}
            {isOpen && (
                <div className={`chat-widget-popup ${isMinimized ? 'minimized' : ''}`}>
                    {/* Header */}
                    <div className="chat-widget-header">
                        <div className="d-flex align-items-center">
                            {activeView !== 'list' && (
                                <button className="btn btn-sm btn-link text-white p-0 me-2" onClick={handleBackToList}>
                                    <span className="material-symbols-outlined">arrow_back</span>
                                </button>
                            )}
                            <h6 className="mb-0 fw-bold text-white">
                                {activeView === 'list' && 'Messages'}
                                {activeView === 'chat' && (selectedConversation ? getParticipantName(selectedConversation) : 'Chat')}
                                {activeView === 'new' && 'New Message'}
                            </h6>
                            {/* Connection status indicator */}
                            <span className={`ms-2 connection-dot ${connectionState}`} title={connectionState}></span>
                        </div>
                        <div className="d-flex gap-2">
                            {activeView === 'list' && (
                                <button className="btn btn-sm btn-link text-white p-0" onClick={handleNewChat} title="New message">
                                    <span className="material-symbols-outlined">edit</span>
                                </button>
                            )}
                            <button className="btn btn-sm btn-link text-white p-0" onClick={() => setIsMinimized(!isMinimized)}>
                                <span className="material-symbols-outlined">{isMinimized ? 'expand_less' : 'remove'}</span>
                            </button>
                        </div>
                    </div>

                    {/* Body */}
                    {!isMinimized && (
                        <div className="chat-widget-body">
                            {isLoading && (
                                <div className="text-center py-4">
                                    <div className="spinner-border spinner-border-sm text-primary" role="status"></div>
                                </div>
                            )}

                            {/* CONVERSATION LIST */}
                            {activeView === 'list' && !isLoading && (
                                <div className="chat-widget-conversations">
                                    {conversations.length === 0 ? (
                                        <div className="text-center py-4 text-muted">
                                            <span className="material-symbols-outlined text-3xl mb-2 block">chat</span>
                                            <p className="small mb-2">No conversations yet</p>
                                            <button className="btn btn-sm btn-primary" onClick={handleNewChat}>Start a chat</button>
                                        </div>
                                    ) : (
                                        conversations.map(conv => (
                                            <div key={conv.conversationId} className="chat-widget-conv-item" onClick={() => handleConversationClick(conv)}>
                                                <div className="avatar-wrapper">
                                                    <img src={getParticipantAvatar(conv)} alt="" className="chat-widget-avatar" />
                                                    {isParticipantOnline(conv) && (
                                                        <span className="online-indicator" title="Online"></span>
                                                    )}
                                                </div>
                                                <div className="chat-widget-conv-info">
                                                    <div className="d-flex justify-content-between align-items-center">
                                                        <span className="fw-semibold small">{getParticipantName(conv)}</span>
                                                        <span className="text-muted" style={{ fontSize: '10px' }}>{formatTime(conv.lastMessageDate || '')}</span>
                                                    </div>
                                                    <div className="d-flex align-items-center">
                                                        <p className="text-muted small mb-0 text-truncate flex-grow-1">{conv.lastMessagePreview || 'Start chatting...'}</p>
                                                        {isParticipantOnline(conv) && (
                                                            <span className="online-text ms-1">Online</span>
                                                        )}
                                                    </div>
                                                </div>
                                                {conv.unreadCount > 0 && (<span className="badge bg-primary rounded-pill ms-2">{conv.unreadCount}</span>)}
                                            </div>
                                        ))
                                    )}
                                </div>
                            )}

                            {/* CHAT VIEW */}
                            {activeView === 'chat' && !isLoading && (
                                <>
                                    <div className="chat-widget-messages">
                                        {messages.length === 0 ? (
                                            <div className="text-center text-muted py-4">
                                                <span className="material-symbols-outlined text-2xl mb-2 block">chat</span>
                                                <p className="small">No messages yet. Say hi!</p>
                                            </div>
                                        ) : (
                                            messages.map((msg, idx) => {
                                                const groupPos = getMessageGroupPosition(messages, idx);
                                                const showAvatar = shouldShowAvatar(messages, idx);
                                                const showTime = shouldShowTime(messages, idx);
                                                const isSent = msg.senderId === user.userId;
                                                
                                                return (
                                                    <div 
                                                        key={msg.messageId} 
                                                        className={`chat-widget-message ${isSent ? 'sent' : 'received'} ${groupPos}`}
                                                        style={{ marginBottom: groupPos === 'group-first' || groupPos === 'group-middle' ? '2px' : '12px' }}
                                                    >
                                                        {!isSent && (
                                                            <div style={{ width: '28px', height: '28px', visibility: showAvatar ? 'visible' : 'hidden' }}>
                                                                <img 
                                                                    src={msg.senderProfilePicture || '/images/default-avatar.png'} 
                                                                    alt="" 
                                                                    className="chat-widget-msg-avatar" 
                                                                />
                                                            </div>
                                                        )}
                                                        <div className={`chat-widget-bubble ${msg.status === 'sending' ? 'sending' : ''}`}>
                                                            <p className="mb-0">{msg.content}</p>
                                                            {showTime && (
                                                                <span className="chat-widget-time">
                                                                    {formatTime(msg.sentDate)}
                                                                    {isSent && (
                                                                        <span className={`ms-1 status-icon ${msg.status === 'read' ? 'read' : ''}`}>
                                                                            {getMessageStatusIcon(msg.status)}
                                                                        </span>
                                                                    )}
                                                                </span>
                                                            )}
                                                        </div>
                                                    </div>
                                                );
                                            })
                                        )}
                                        {/* Typing indicator */}
                                        {typingUsers.length > 0 && (
                                            <div className="typing-indicator">
                                                <span className="dot"></span>
                                                <span className="dot"></span>
                                                <span className="dot"></span>
                                            </div>
                                        )}
                                        <div ref={messagesEndRef} />
                                    </div>

                                    <div className="chat-widget-input">
                                        <input
                                            type="text"
                                            placeholder="Type a message..."
                                            value={newMessage}
                                            onChange={handleInputChange}
                                            onKeyPress={handleKeyPress}
                                        />
                                        <button onClick={sendMessage} disabled={!newMessage.trim()}>
                                            <span className="material-symbols-outlined">send</span>
                                        </button>
                                    </div>
                                </>
                            )}

                            {/* NEW CHAT VIEW */}
                            {activeView === 'new' && !isLoading && (
                                <>
                                    <div className="chat-widget-new-chat">
                                        <div className="p-2 border-bottom">
                                            <div className="input-group input-group-sm">
                                                <span className="input-group-text bg-transparent border-end-0"><span className="material-symbols-outlined">search</span></span>
                                                <input
                                                    type="text"
                                                    className="form-control border-start-0"
                                                    placeholder="Search users..."
                                                    value={searchUser}
                                                    onChange={(e) => { setSearchUser(e.target.value); searchUsers(e.target.value); }}
                                                />
                                            </div>
                                        </div>
                                        {selectedUser && (
                                            <div className="p-2 bg-light border-bottom d-flex align-items-center">
                                                <span className="badge bg-primary d-flex align-items-center gap-1">
                                                    {selectedUser.displayName || selectedUser.username}
                                                    <button className="btn-close btn-close-white ms-1" style={{ fontSize: '8px' }} onClick={() => setSelectedUser(null)}></button>
                                                </span>
                                            </div>
                                        )}
                                        {!selectedUser && users.length > 0 && (
                                            <div className="chat-widget-user-results">
                                                {users.map(u => (
                                                    <div key={u.userId} className="chat-widget-user-item" onClick={() => { setSelectedUser(u); setSearchUser(''); setUsers([]); }}>
                                                        <img src={u.profilePicture || '/images/default-avatar.png'} alt="" className="chat-widget-avatar small" />
                                                        <div>
                                                            <div className="fw-semibold small">{u.displayName || u.username}</div>
                                                            <div className="text-muted" style={{ fontSize: '11px' }}>@{u.username}</div>
                                                        </div>
                                                    </div>
                                                ))}
                                            </div>
                                        )}
                                        {!selectedUser && searchUser.length > 1 && users.length === 0 && (
                                            <div className="text-center text-muted py-3"><p className="small mb-0">No users found</p></div>
                                        )}
                                    </div>
                                    {selectedUser && (
                                        <div className="chat-widget-input">
                                            <input type="text" placeholder="Type your message..." value={newMessage} onChange={handleInputChange} onKeyPress={handleKeyPress} />
                                            <button onClick={startNewConversation} disabled={!newMessage.trim()}><span className="material-symbols-outlined">send</span></button>
                                        </div>
                                    )}
                                </>
                            )}
                        </div>
                    )}
                </div>
            )}

            {/* ========== NOTIFICATION TOAST ========== */}
            {notification && (
                <div className="chat-notification-toast" onClick={() => { setNotification(null); setIsOpen(true); }}>
                    <div className="toast-icon">
                        <span className="material-symbols-outlined">chat</span>
                    </div>
                    <div className="toast-content">
                        <div className="toast-sender">{notification.sender}</div>
                        <div className="toast-message">{notification.message}</div>
                    </div>
                    <button className="toast-close" onClick={(e) => { e.stopPropagation(); setNotification(null); }}>
                        <span className="material-symbols-outlined">close</span>
                    </button>
                </div>
            )}

            {/* ========== STYLES ========== */}
            <style jsx>{`
                .chat-widget-button {
                    position: fixed; bottom: 24px; right: 24px; width: 56px; height: 56px;
                    border-radius: 50%; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                    border: none; color: white; font-size: 24px; cursor: pointer;
                    box-shadow: 0 4px 20px rgba(102, 126, 234, 0.4); transition: all 0.3s ease; z-index: 9999;
                    display: flex; align-items: center; justify-content: center;
                }
                .chat-widget-button:hover { transform: scale(1.1); box-shadow: 0 6px 25px rgba(102, 126, 234, 0.5); }
                .chat-widget-badge {
                    position: absolute; top: -4px; right: -4px; min-width: 20px; height: 20px; padding: 0 6px;
                    border-radius: 10px; background: #ff4757; color: white; font-size: 11px; font-weight: 600;
                    display: flex; align-items: center; justify-content: center; border: 2px solid white;
                }
                .chat-widget-popup {
                    position: fixed; bottom: 90px; right: 24px; width: 340px; height: 480px;
                    background: white; border-radius: 16px; box-shadow: 0 10px 40px rgba(0, 0, 0, 0.15);
                    z-index: 9998; display: flex; flex-direction: column; overflow: hidden; animation: slideUp 0.3s ease;
                }
                .chat-widget-popup.minimized { height: auto; }
                @keyframes slideUp { from { opacity: 0; transform: translateY(20px); } to { opacity: 1; transform: translateY(0); } }
                .chat-widget-header {
                    padding: 14px 16px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                    display: flex; justify-content: space-between; align-items: center;
                }
                .connection-dot {
                    width: 8px; height: 8px; border-radius: 50%; display: inline-block;
                }
                .connection-dot.connected { background: #2ecc71; }
                .connection-dot.connecting { background: #f1c40f; animation: pulse 1s infinite; }
                .connection-dot.disconnected { background: #e74c3c; }
                @keyframes pulse { 0%, 100% { opacity: 1; } 50% { opacity: 0.5; } }
                .chat-widget-body { flex: 1; display: flex; flex-direction: column; overflow: hidden; }
                .chat-widget-conversations { flex: 1; overflow-y: auto; }
                .chat-widget-conv-item {
                    display: flex; align-items: center; padding: 12px 16px; cursor: pointer;
                    transition: background 0.2s; border-bottom: 1px solid #f0f0f0;
                }
                .chat-widget-conv-item:hover { background: #f8f9fa; }
                .chat-widget-avatar { width: 44px; height: 44px; border-radius: 50%; object-fit: cover; margin-right: 12px; }
                .chat-widget-avatar.small { width: 36px; height: 36px; }
                .chat-widget-conv-info { flex: 1; min-width: 0; }
                .chat-widget-messages { flex: 1; overflow-y: auto; padding: 12px; background: #f8f9fa; }
                .chat-widget-message { display: flex; align-items: flex-end; margin-bottom: 12px; gap: 8px; }
                .chat-widget-message.sent { flex-direction: row-reverse; }
                .chat-widget-msg-avatar { width: 28px; height: 28px; border-radius: 50%; object-fit: cover; }
                .chat-widget-bubble {
                    max-width: 70%; padding: 10px 14px; border-radius: 18px; font-size: 14px; line-height: 1.4;
                }
                .chat-widget-bubble.sending { opacity: 0.7; }
                .chat-widget-message.sent .chat-widget-bubble {
                    background: linear-gradient(135deg, #833ab4 0%, #fd1d1d 50%, #fcb045 100%); 
                    color: white; 
                    border-bottom-right-radius: 4px;
                    box-shadow: 0 2px 12px rgba(131, 58, 180, 0.3);
                }
                .chat-widget-message.received .chat-widget-bubble {
                    background: white; color: #333; border: 1px solid #e0e0e0; border-bottom-left-radius: 4px;
                    box-shadow: 0 1px 4px rgba(0,0,0,0.05);
                }
                /* Message grouping styles - Instagram style */
                .chat-widget-message.sent.group-first .chat-widget-bubble { border-radius: 18px 18px 4px 18px; }
                .chat-widget-message.sent.group-middle .chat-widget-bubble { border-radius: 18px 4px 4px 18px; }
                .chat-widget-message.sent.group-last .chat-widget-bubble { border-radius: 18px 4px 18px 18px; }
                .chat-widget-message.received.group-first .chat-widget-bubble { border-radius: 18px 18px 18px 4px; }
                .chat-widget-message.received.group-middle .chat-widget-bubble { border-radius: 4px 18px 18px 4px; }
                .chat-widget-message.received.group-last .chat-widget-bubble { border-radius: 4px 18px 18px 18px; }
                .chat-widget-message.sent.single .chat-widget-bubble { border-radius: 18px 18px 4px 18px; }
                .chat-widget-message.received.single .chat-widget-bubble { border-radius: 18px 18px 18px 4px; }
                .chat-widget-time { display: block; font-size: 10px; margin-top: 4px; opacity: 0.7; }
                .status-icon { font-size: 10px; }
                .status-icon.read { color: #3b82f6; }
                .chat-widget-input {
                    display: flex; gap: 8px; padding: 12px; background: white; border-top: 1px solid #e0e0e0;
                }
                .chat-widget-input input {
                    flex: 1; padding: 10px 16px; border: 1px solid #e0e0e0; border-radius: 24px;
                    font-size: 14px; outline: none; transition: border-color 0.2s;
                }
                .chat-widget-input input:focus { border-color: #667eea; }
                .chat-widget-input button {
                    width: 40px; height: 40px; border-radius: 50%;
                    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                    border: none; color: white; cursor: pointer; transition: opacity 0.2s;
                }
                .chat-widget-input button:disabled { opacity: 0.5; cursor: not-allowed; }
                .chat-widget-new-chat { flex: 1; overflow-y: auto; }
                .chat-widget-user-results { max-height: 200px; overflow-y: auto; }
                .chat-widget-user-item {
                    display: flex; align-items: center; padding: 10px 16px; cursor: pointer; gap: 10px; transition: background 0.2s;
                }
                .chat-widget-user-item:hover { background: #f8f9fa; }
                /* Typing indicator */
                .typing-indicator {
                    display: flex; gap: 4px; padding: 10px 14px; background: white; border-radius: 18px;
                    width: fit-content; border: 1px solid #e0e0e0;
                }
                .typing-indicator .dot {
                    width: 8px; height: 8px; border-radius: 50%; background: #667eea;
                    animation: typingBounce 1.4s infinite ease-in-out both;
                }
                .typing-indicator .dot:nth-child(1) { animation-delay: -0.32s; }
                .typing-indicator .dot:nth-child(2) { animation-delay: -0.16s; }
                @keyframes typingBounce {
                    0%, 80%, 100% { transform: scale(0); opacity: 0.5; }
                    40% { transform: scale(1); opacity: 1; }
                }
                /* Dark mode */
                :global([data-theme="dark"]) .chat-widget-popup { background: #1e293b; color: #e2e8f0; }
                :global([data-theme="dark"]) .chat-widget-conv-item { border-color: #334155; }
                :global([data-theme="dark"]) .chat-widget-conv-item:hover { background: #334155; }
                :global([data-theme="dark"]) .chat-widget-messages { background: #0f172a; }
                :global([data-theme="dark"]) .chat-widget-message.received .chat-widget-bubble { background: #334155; color: #e2e8f0; border-color: #475569; }
                :global([data-theme="dark"]) .chat-widget-input { background: #1e293b; border-color: #334155; }
                :global([data-theme="dark"]) .chat-widget-input input { background: #0f172a; border-color: #334155; color: #e2e8f0; }
                :global([data-theme="dark"]) .typing-indicator { background: #334155; border-color: #475569; }
                @media (max-width: 480px) {
                    .chat-widget-popup { width: calc(100vw - 32px); right: 16px; bottom: 80px; height: 60vh; }
                    .chat-widget-button { right: 16px; bottom: 16px; width: 52px; height: 52px; }
                }

                /* Avatar wrapper for online indicator */
                .avatar-wrapper {
                    position: relative;
                    flex-shrink: 0;
                }
                .online-indicator {
                    position: absolute;
                    bottom: 2px;
                    right: 2px;
                    width: 12px;
                    height: 12px;
                    background: #22c55e;
                    border-radius: 50%;
                    border: 2px solid white;
                    animation: pulse 2s infinite;
                }
                @keyframes pulse {
                    0%, 100% { box-shadow: 0 0 0 0 rgba(34, 197, 94, 0.4); }
                    50% { box-shadow: 0 0 0 4px rgba(34, 197, 94, 0); }
                }
                .online-text {
                    font-size: 10px;
                    color: #22c55e;
                    font-weight: 500;
                    white-space: nowrap;
                }

                /* Notification toast */
                .chat-notification-toast {
                    position: fixed;
                    bottom: 90px;
                    right: 24px;
                    background: white;
                    border-radius: 12px;
                    box-shadow: 0 8px 32px rgba(0,0,0,0.15);
                    padding: 12px 16px;
                    display: flex;
                    align-items: center;
                    gap: 12px;
                    max-width: 320px;
                    cursor: pointer;
                    z-index: 10000;
                    animation: slideIn 0.3s ease-out;
                    transition: all 0.2s ease;
                }
                .chat-notification-toast:hover {
                    transform: translateY(-2px);
                    box-shadow: 0 12px 40px rgba(0,0,0,0.2);
                }
                @keyframes slideIn {
                    from { opacity: 0; transform: translateX(100%); }
                    to { opacity: 1; transform: translateX(0); }
                }
                .toast-icon {
                    width: 40px;
                    height: 40px;
                    border-radius: 50%;
                    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    color: white;
                    font-size: 18px;
                    flex-shrink: 0;
                }
                .toast-content {
                    flex: 1;
                    min-width: 0;
                }
                .toast-sender {
                    font-weight: 600;
                    font-size: 14px;
                    color: #1e293b;
                }
                .toast-message {
                    font-size: 13px;
                    color: #64748b;
                    white-space: nowrap;
                    overflow: hidden;
                    text-overflow: ellipsis;
                }
                .toast-close {
                    background: none;
                    border: none;
                    color: #94a3b8;
                    font-size: 18px;
                    cursor: pointer;
                    padding: 4px;
                    transition: color 0.2s;
                }
                .toast-close:hover { color: #475569; }

                /* Dark mode for new elements */
                :global([data-theme="dark"]) .online-indicator { border-color: #1e293b; }
                :global([data-theme="dark"]) .chat-notification-toast { background: #1e293b; }
                :global([data-theme="dark"]) .toast-sender { color: #e2e8f0; }
                :global([data-theme="dark"]) .toast-message { color: #94a3b8; }
                :global([data-theme="dark"]) .toast-close { color: #64748b; }
                :global([data-theme="dark"]) .toast-close:hover { color: #94a3b8; }
            `}</style>
        </>
    );
}
