'use client';

import { useEffect, useState, useRef, useCallback, useMemo, Suspense } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import * as signalR from '@microsoft/signalr';
import { useAuth } from '@/lib/contexts/AuthContext';
import type { Conversation, ChatMessage, MessageReaction } from '@/types';
import apiClient from '@/lib/api/client';
import ModernNavbar from '@/components/ModernNavbar';
import ReactionPicker, { getReactionEmoji } from '@/components/ReactionPicker';
import MediaPicker from '@/components/chat/MediaPicker';
import MediaPreview from '@/components/chat/MediaPreview';
import MediaLightbox from '@/components/chat/MediaLightbox';

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5122';

// Extended types for realtime features
interface RealtimeMessage extends ChatMessage {
    status?: 'sending' | 'sent' | 'delivered' | 'read';
    senderAvatar?: string;
    senderDisplayName?: string;
}

interface User {
    userId: number;
    username: string;
    displayName?: string;
    profilePicture?: string;
}

// Message grouping types for Instagram-style UI
interface MessageGroup {
    senderId: number;
    messages: RealtimeMessage[];
    showAvatar: boolean;
    showTime: boolean;
    timeLabel: string;
}

function ChatContent() {
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

    // Reactions
    const [showReactionPicker, setShowReactionPicker] = useState<number | null>(null);

    // Reply
    const [replyingTo, setReplyingTo] = useState<RealtimeMessage | null>(null);

    // Media
    const [showMediaPicker, setShowMediaPicker] = useState(false);
    const [selectedFile, setSelectedFile] = useState<File | null>(null);
    const [filePreview, setFilePreview] = useState<string>('');
    const [fileMessageType, setFileMessageType] = useState<string>('');
    const [isUploading, setIsUploading] = useState(false);
    const [uploadProgress, setUploadProgress] = useState(0);

    // Lightbox
    const [lightboxOpen, setLightboxOpen] = useState(false);
    const [lightboxMedia, setLightboxMedia] = useState<{ url: string; type: 'image' | 'video'; fileName?: string }>({ url: '', type: 'image' });

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

        // Receive reaction (realtime)
        connection.on('ReceiveReaction', (reaction: { messageId: number; userId: number; username: string; profilePicture?: string; reactionType: string; createdAt: string }) => {
            console.log('📩 RECEIVED REACTION:', reaction);
            setMessages(prev => prev.map(m => {
                if (m.messageId !== reaction.messageId) return m;

                // Update or add reaction
                const existingReactions = m.reactions || [];
                const existingIdx = existingReactions.findIndex(r => r.userId === reaction.userId);

                let newReactions: MessageReaction[];
                if (existingIdx >= 0) {
                    // Update existing
                    newReactions = existingReactions.map((r, i) =>
                        i === existingIdx
                            ? { ...r, reactionType: reaction.reactionType, createdAt: reaction.createdAt }
                            : r
                    );
                } else {
                    // Add new
                    newReactions = [...existingReactions, {
                        messageReactionId: Date.now(),
                        userId: reaction.userId,
                        username: reaction.username,
                        profilePicture: reaction.profilePicture,
                        reactionType: reaction.reactionType,
                        createdAt: reaction.createdAt
                    }];
                }

                return { ...m, reactions: newReactions };
            }));
        });

        // Remove reaction (realtime)
        connection.on('RemoveReaction', (data: { messageId: number; userId: number }) => {
            console.log('📩 REACTION REMOVED:', data);
            setMessages(prev => prev.map(m => {
                if (m.messageId !== data.messageId) return m;
                return {
                    ...m,
                    reactions: (m.reactions || []).filter(r => r.userId !== data.userId)
                };
            }));
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

        let isMounted = true;

        connection.start()
            .then(() => {
                if (!isMounted) {
                    connection.stop();
                    return;
                }
                console.log('✅ ChatHub connected');
                setConnectionState('connected');
            })
            .catch(err => {
                console.error('❌ ChatHub error:', err);
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

        let isMounted = true;

        presenceConnection.start()
            .then(async () => {
                if (!isMounted) {
                    presenceConnection.stop();
                    return;
                }
                console.log('✅ PresenceHub connected');
                try {
                    const onlineList = await presenceConnection.invoke<string[]>('GetOnlineUsers');
                    if (isMounted) {
                        setOnlineUsers(new Set(onlineList));
                    }
                } catch (err) {
                    console.error('Failed to get online users:', err);
                }
            })
            .catch(err => console.error('❌ PresenceHub error:', err));

        return () => {
            isMounted = false;
            if (presenceConnection.state === signalR.HubConnectionState.Connected) {
                presenceConnection.stop().catch(console.error);
            }
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
        const replyToId = replyingTo?.messageId;

        // Optimistic update
        const optimisticMessage: RealtimeMessage = {
            messageId: parseInt(tempId.replace('temp_', '')),
            conversationId: selectedConversation,
            senderId: user!.userId,
            senderUsername: user!.username,
            content,
            sentDate: new Date().toISOString(),
            isRead: false,
            status: 'sending',
            replyToMessageId: replyToId,
            replyToMessage: replyingTo ? {
                messageId: replyingTo.messageId,
                senderId: replyingTo.senderId,
                senderUsername: replyingTo.senderUsername || '',
                content: replyingTo.content
            } : undefined
        };

        setMessages(prev => [...prev, optimisticMessage]);
        setNewMessage('');
        setReplyingTo(null); // Clear reply after sending

        try {
            const connection = connectionRef.current;

            if (connection && connection.state === signalR.HubConnectionState.Connected) {
                // Send via WebSocket (with optional replyToMessageId)
                await connection.invoke('SendMessage', selectedConversation, content, replyToId || null);
                setMessages(prev => prev.map(m =>
                    m.messageId === optimisticMessage.messageId ? { ...m, status: 'sent' } : m
                ));
            } else {
                // Fallback to REST
                await apiClient.post(`/chat/conversations/${selectedConversation}/messages`, {
                    content,
                    replyToMessageId: replyToId
                });
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

    // ========== MEDIA HANDLING ==========
    const handleFileSelect = (file: File, preview: string, messageType: string) => {
        setSelectedFile(file);
        setFilePreview(preview);
        setFileMessageType(messageType);
    };

    const clearSelectedFile = () => {
        if (filePreview) {
            URL.revokeObjectURL(filePreview);
        }
        setSelectedFile(null);
        setFilePreview('');
        setFileMessageType('');
    };

    const sendMediaMessage = async () => {
        if (!selectedFile || !selectedConversation) return;

        setIsUploading(true);
        setUploadProgress(0);

        try {
            // Upload file first
            const formData = new FormData();
            formData.append('file', selectedFile);

            const uploadResponse = await apiClient.post('/media/upload', formData, {
                headers: { 'Content-Type': 'multipart/form-data' },
                onUploadProgress: (progressEvent) => {
                    const progress = progressEvent.total
                        ? Math.round((progressEvent.loaded * 100) / progressEvent.total)
                        : 0;
                    setUploadProgress(progress);
                }
            });

            const { url, fileName, fileSize, messageType } = uploadResponse.data;

            // Create optimistic message
            const tempId = `temp_${Date.now()}`;
            const caption = newMessage.trim();
            const replyToId = replyingTo?.messageId;

            const optimisticMessage: RealtimeMessage = {
                messageId: parseInt(tempId.replace('temp_', '')),
                conversationId: selectedConversation,
                senderId: user!.userId,
                senderUsername: user!.username,
                content: caption,
                messageType: messageType as 'image' | 'video' | 'audio' | 'file',
                attachmentUrl: url,
                attachmentFileName: fileName,
                attachmentSize: fileSize,
                sentDate: new Date().toISOString(),
                isRead: false,
                status: 'sending',
                replyToMessageId: replyToId,
                replyToMessage: replyingTo ? {
                    messageId: replyingTo.messageId,
                    senderId: replyingTo.senderId,
                    senderUsername: replyingTo.senderUsername || '',
                    content: replyingTo.content
                } : undefined
            };

            setMessages(prev => [...prev, optimisticMessage]);
            clearSelectedFile();
            setNewMessage('');
            setReplyingTo(null);

            // Send via WebSocket
            const connection = connectionRef.current;
            if (connection && connection.state === signalR.HubConnectionState.Connected) {
                await connection.invoke('SendMediaMessage',
                    selectedConversation,
                    messageType,
                    url,
                    fileName,
                    fileSize,
                    caption || null,
                    replyToId || null
                );
                setMessages(prev => prev.map(m =>
                    m.messageId === optimisticMessage.messageId ? { ...m, status: 'sent' } : m
                ));
            }

            fetchConversations();
        } catch (error) {
            console.error('Failed to send media message:', error);
        } finally {
            setIsUploading(false);
            setUploadProgress(0);
        }
    };

    const openLightbox = (url: string, type: 'image' | 'video', fileName?: string) => {
        setLightboxMedia({ url, type, fileName });
        setLightboxOpen(true);
    };

    const formatFileSize = (bytes?: number) => {
        if (!bytes) return '';
        const k = 1024;
        const sizes = ['B', 'KB', 'MB', 'GB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
    };

    // ========== REACTIONS ==========
    const addReaction = async (messageId: number, reactionType: string) => {
        const connection = connectionRef.current;
        if (!connection || !selectedConversation || connectionState !== 'connected') return;

        try {
            await connection.invoke('AddReaction', selectedConversation, messageId, reactionType);
            setShowReactionPicker(null);
        } catch (error) {
            console.error('Failed to add reaction:', error);
        }
    };

    const removeReaction = async (messageId: number) => {
        const connection = connectionRef.current;
        if (!connection || !selectedConversation || connectionState !== 'connected') return;

        try {
            await connection.invoke('RemoveReaction', selectedConversation, messageId);
        } catch (error) {
            console.error('Failed to remove reaction:', error);
        }
    };

    const toggleReaction = (messageId: number, reactionType: string) => {
        const message = messages.find(m => m.messageId === messageId);
        const userReaction = message?.reactions?.find(r => r.userId === user?.userId);

        if (userReaction?.reactionType === reactionType) {
            removeReaction(messageId);
        } else {
            addReaction(messageId, reactionType);
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

    // Format time label for message groups (Instagram style)
    const formatTimeLabel = (dateString: string) => {
        const date = new Date(dateString);
        const now = new Date();
        const diffMs = now.getTime() - date.getTime();
        const diffMins = Math.floor(diffMs / 60000);
        const diffHours = Math.floor(diffMs / 3600000);
        const diffDays = Math.floor(diffMs / 86400000);

        if (diffMins < 1) return 'Just now';
        if (diffMins < 60) return `${diffMins}m ago`;
        if (diffHours < 24) return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
        if (diffDays === 1) return `Yesterday ${date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}`;
        if (diffDays < 7) return `${date.toLocaleDateString([], { weekday: 'long' })} ${date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}`;
        return date.toLocaleDateString([], { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' });
    };

    // Group messages by sender and time (Instagram/FB style)
    const groupedMessages = useMemo(() => {
        const groups: MessageGroup[] = [];
        const TIME_GAP_THRESHOLD = 5 * 60 * 1000; // 5 minutes

        messages.forEach((msg, idx) => {
            const prevMsg = messages[idx - 1];
            const nextMsg = messages[idx + 1];

            const prevTime = prevMsg ? new Date(prevMsg.sentDate).getTime() : 0;
            const currTime = new Date(msg.sentDate).getTime();
            const nextTime = nextMsg ? new Date(nextMsg.sentDate).getTime() : 0;

            // Check if we need a time separator (gap > 5 min from previous)
            const showTimeLabel = !prevMsg || (currTime - prevTime > TIME_GAP_THRESHOLD);

            // Check if same sender as previous (for avatar grouping)
            const sameSenderAsPrev = prevMsg && prevMsg.senderId === msg.senderId && !showTimeLabel;

            // Check if same sender as next (for avatar grouping)
            const sameSenderAsNext = nextMsg && nextMsg.senderId === msg.senderId &&
                (nextTime - currTime <= TIME_GAP_THRESHOLD);

            // Show avatar only for the LAST message in a consecutive group from same sender
            const showAvatar = !sameSenderAsNext;

            // Create or add to group
            if (!sameSenderAsPrev || groups.length === 0) {
                groups.push({
                    senderId: msg.senderId,
                    messages: [msg],
                    showAvatar,
                    showTime: showTimeLabel,
                    timeLabel: showTimeLabel ? formatTimeLabel(msg.sentDate) : ''
                });
            } else {
                const lastGroup = groups[groups.length - 1];
                lastGroup.messages.push(msg);
                lastGroup.showAvatar = showAvatar;
            }
        });

        return groups;
    }, [messages]);

    const getParticipantName = (conv: Conversation) => {
        if (conv.title) return conv.title;
        const others = conv.participants?.filter(p => p.userId !== user?.userId) || [];
        return others.map(p => p.displayName || p.username).join(', ') || 'New Chat';
    };

    const getParticipantAvatar = (conv: Conversation) => {
        const other = conv.participants?.find(p => p.userId !== user?.userId);
        return other?.profilePicture;
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
    const otherParticipant = selectedConv?.participants?.find(p => p.userId !== user?.userId);

    if (authLoading || isLoading) {
        return (
            <>
                <ModernNavbar />
                <div className="flex items-center justify-center min-h-screen bg-slate-50 dark:bg-slate-950">
                    <div className="w-10 h-10 border-4 border-[var(--primary)]/30 border-t-[var(--primary)] rounded-full animate-spin"></div>
                </div>
            </>
        );
    }

    if (!user) return null;

    return (
        <>
            <ModernNavbar />
            <div className="flex h-[calc(100vh-64px)] overflow-hidden">
                {/* Conversations Sidebar */}
                <aside className="flex w-80 flex-col border-r border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 shrink-0">
                    <div className="p-4 space-y-4">
                        <div className="flex items-center gap-3">
                            <div className="relative">
                                <div className="size-10 rounded-full bg-gradient-to-br from-blue-500 to-indigo-500 flex items-center justify-center text-white font-bold text-sm">
                                    {user.username?.charAt(0).toUpperCase() || 'U'}
                                </div>
                                <div className={`absolute bottom-0 right-0 size-3 rounded-full border-2 border-white dark:border-slate-900 ${connectionState === 'connected' ? 'bg-emerald-500' : connectionState === 'connecting' ? 'bg-yellow-500 animate-pulse' : 'bg-red-500'}`}></div>
                            </div>
                            <div>
                                <h1 className="text-slate-900 dark:text-white text-sm font-bold">Active Chats</h1>
                                <p className="text-slate-500 text-xs font-medium">{onlineUsers.size} Online</p>
                            </div>
                        </div>
                        <div className="flex flex-col gap-1">
                            <div className="flex items-center gap-3 px-3 py-2 rounded-lg bg-[var(--primary)]/10 text-[var(--primary)]">
                                <span className="material-symbols-outlined text-[20px]">chat_bubble</span>
                                <p className="text-sm font-bold">All Messages</p>
                            </div>
                            <div className="flex items-center gap-3 px-3 py-2 text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800 rounded-lg cursor-pointer">
                                <span className="material-symbols-outlined text-[20px]">mail</span>
                                <p className="text-sm font-medium">Unread</p>
                            </div>
                            <div className="flex items-center gap-3 px-3 py-2 text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800 rounded-lg cursor-pointer">
                                <span className="material-symbols-outlined text-[20px]">archive</span>
                                <p className="text-sm font-medium">Archived</p>
                            </div>
                        </div>
                        <button
                            onClick={() => { setShowNewChat(true); setSelectedConversation(null); }}
                            className="w-full bg-[var(--primary)] hover:bg-[var(--primary)]/90 text-white rounded-xl py-2.5 text-sm font-bold flex items-center justify-center gap-2 transition-all"
                        >
                            <span className="material-symbols-outlined text-[20px]">edit_square</span>
                            New Message
                        </button>
                        <div className="relative">
                            <div className="absolute inset-y-0 left-0 flex items-center pl-3 pointer-events-none text-slate-400">
                                <span className="material-symbols-outlined text-[20px]">search</span>
                            </div>
                            <input className="block w-full pl-10 pr-3 py-2 bg-slate-100 dark:bg-slate-800 border-none rounded-xl text-xs focus:ring-1 focus:ring-[var(--primary)]" placeholder="Search conversations..." type="text" />
                        </div>
                    </div>
                    <div className="flex-1 overflow-y-auto">
                        <div className="flex flex-col">
                            {conversations.length > 0 ? (
                                conversations.map((conv) => {
                                    const isActive = selectedConversation === conv.conversationId;
                                    const isOnline = isParticipantOnline(conv);
                                    const hasUnread = conv.unreadCount > 0;
                                    return (
                                        <button
                                            key={conv.conversationId}
                                            onClick={() => selectConversation(conv.conversationId)}
                                            className={`flex items-center gap-3 px-4 py-4 cursor-pointer transition-colors ${
                                                isActive ? 'bg-[var(--primary)]/5 border-l-4 border-[var(--primary)]' : 'hover:bg-slate-50 dark:hover:bg-slate-800 border-l-4 border-transparent'
                                            }`}
                                        >
                                            <div className="relative shrink-0">
                                                <div className="size-12 rounded-full overflow-hidden bg-gradient-to-br from-blue-500 to-indigo-500 flex items-center justify-center text-white font-bold">
                                                    {getParticipantAvatar(conv) ? (
                                                        <img src={getParticipantAvatar(conv)!} alt="" className="w-full h-full object-cover" />
                                                    ) : (
                                                        getParticipantName(conv).charAt(0).toUpperCase()
                                                    )}
                                                </div>
                                                {isOnline && (
                                                    <div className="absolute bottom-0 right-0 size-3.5 rounded-full bg-emerald-500 border-2 border-white dark:border-slate-900"></div>
                                                )}
                                            </div>
                                            <div className="flex flex-col min-w-0 flex-1 text-left">
                                                <div className="flex justify-between items-baseline">
                                                    <p className={`text-sm truncate ${hasUnread ? 'font-bold text-slate-900 dark:text-white' : 'font-medium text-slate-900 dark:text-white'}`}>
                                                        {getParticipantName(conv)}
                                                    </p>
                                                    <p className={`text-[10px] ${hasUnread ? 'text-[var(--primary)] font-bold' : 'text-slate-500'}`}>
                                                        {formatTime(conv.lastMessageDate)}
                                                    </p>
                                                </div>
                                                <div className="flex justify-between items-center gap-2">
                                                    <p className={`text-xs truncate ${isActive ? 'text-[var(--primary)] font-semibold' : hasUnread ? 'text-slate-900 dark:text-white font-bold' : 'text-slate-500'}`}>
                                                        {conv.lastMessagePreview || 'No messages yet'}
                                                    </p>
                                                    {hasUnread && (
                                                        <span className="size-4 flex items-center justify-center bg-[var(--primary)] text-white text-[10px] rounded-full shrink-0">
                                                            {conv.unreadCount}
                                                        </span>
                                                    )}
                                                </div>
                                            </div>
                                        </button>
                                    );
                                })
                            ) : (
                                <div className="flex flex-col items-center justify-center h-full text-center p-8">
                                    <span className="material-symbols-outlined text-5xl text-slate-300 mb-4">chat</span>
                                    <p className="text-slate-500">No conversations yet</p>
                                    <button
                                        onClick={() => setShowNewChat(true)}
                                        className="mt-4 px-4 py-2 bg-[var(--primary)] text-white rounded-xl text-sm font-bold hover:bg-[var(--primary)]/90 transition"
                                    >
                                        Start a chat
                                    </button>
                                </div>
                            )}
                        </div>
                    </div>
                </aside>

                {/* Chat Area */}
                <div className="flex-1 flex flex-col">
                    {showNewChat ? (
                        <div className="flex-1 flex flex-col bg-slate-50 dark:bg-slate-950">
                            <div className="p-4 border-b border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900">
                                <h3 className="font-bold text-slate-900 dark:text-white">New Message</h3>
                            </div>
                            <div className="p-4 border-b border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900">
                                <div className="relative">
                                    <span className="material-symbols-outlined absolute left-3 top-1/2 -translate-y-1/2 text-slate-400">search</span>
                                    <input
                                        type="text"
                                        placeholder="Search users..."
                                        value={searchUser}
                                        onChange={(e) => { setSearchUser(e.target.value); searchUsers(e.target.value); }}
                                        className="w-full pl-10 pr-4 py-2 bg-slate-100 dark:bg-slate-800 border-none rounded-xl text-sm text-slate-900 dark:text-white focus:ring-1 focus:ring-[var(--primary)]"
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
                                    <div className="mt-2 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl overflow-hidden">
                                        {userResults.map(u => (
                                            <button
                                                key={u.userId}
                                                onClick={() => { setSelectedUser(u); setSearchUser(''); setUserResults([]); }}
                                                className="w-full p-3 flex items-center gap-3 hover:bg-slate-50 dark:hover:bg-slate-800 text-left"
                                            >
                                                <div className="w-10 h-10 rounded-full bg-gradient-to-br from-blue-500 to-indigo-500 flex items-center justify-center text-white font-bold text-sm">
                                                    {(u.displayName || u.username).charAt(0).toUpperCase()}
                                                </div>
                                                <div>
                                                    <div className="font-bold text-sm text-slate-900 dark:text-white">{u.displayName || u.username}</div>
                                                    <div className="text-xs text-slate-500">@{u.username}</div>
                                                </div>
                                            </button>
                                        ))}
                                    </div>
                                )}
                            </div>
                            <div className="flex-1"></div>
                            {selectedUser && (
                                <div className="p-4 border-t border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900">
                                    <div className="flex items-end gap-3 bg-slate-100 dark:bg-slate-800 p-2 rounded-2xl">
                                        <div className="flex-1">
                                            <input
                                                type="text"
                                                placeholder="Type a message..."
                                                value={newMessage}
                                                onChange={(e) => setNewMessage(e.target.value)}
                                                onKeyDown={(e) => e.key === 'Enter' && startNewConversation()}
                                                className="w-full bg-transparent border-none focus:ring-0 text-sm py-2 px-2 text-slate-900 dark:text-white"
                                            />
                                        </div>
                                        <button
                                            onClick={startNewConversation}
                                            disabled={!newMessage.trim()}
                                            className="size-10 rounded-xl bg-[var(--primary)] text-white flex items-center justify-center disabled:opacity-50 shadow-lg shadow-[var(--primary)]/30"
                                        >
                                            <span className="material-symbols-outlined">send</span>
                                        </button>
                                    </div>
                                </div>
                            )}
                        </div>
                    ) : selectedConversation ? (
                        <>
                            {/* Chat Header */}
                            <header className="flex h-20 items-center justify-between border-b border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 px-6 shrink-0">
                                <div className="flex items-center gap-4">
                                    <div className="relative">
                                        <div className="size-11 rounded-full overflow-hidden bg-gradient-to-br from-blue-500 to-indigo-500 flex items-center justify-center text-white font-bold">
                                            {otherParticipant?.profilePicture ? (
                                                <img src={otherParticipant.profilePicture} alt="" className="w-full h-full object-cover" />
                                            ) : (
                                                selectedConv ? getParticipantName(selectedConv).charAt(0).toUpperCase() : '?'
                                            )}
                                        </div>
                                        {selectedConv && isParticipantOnline(selectedConv) && (
                                            <div className="absolute bottom-0 right-0 size-3.5 rounded-full bg-emerald-500 border-2 border-white dark:border-slate-900"></div>
                                        )}
                                    </div>
                                    <div>
                                        <h2 className="text-slate-900 dark:text-white text-base font-bold">
                                            {selectedConv ? getParticipantName(selectedConv) : 'Chat'}
                                        </h2>
                                        {selectedConv && isParticipantOnline(selectedConv) ? (
                                            <p className="text-emerald-600 dark:text-emerald-400 text-xs font-semibold">Active now</p>
                                        ) : (
                                            <p className="text-slate-500 text-xs">Offline</p>
                                        )}
                                    </div>
                                </div>
                                <div className="flex items-center gap-2">
                                    <button className="flex size-10 items-center justify-center rounded-full hover:bg-slate-100 dark:hover:bg-slate-800 text-slate-600 dark:text-slate-400 transition-colors">
                                        <span className="material-symbols-outlined">call</span>
                                    </button>
                                    <button className="flex size-10 items-center justify-center rounded-full hover:bg-slate-100 dark:hover:bg-slate-800 text-slate-600 dark:text-slate-400 transition-colors">
                                        <span className="material-symbols-outlined">videocam</span>
                                    </button>
                                    <button className="flex size-10 items-center justify-center rounded-full hover:bg-slate-100 dark:hover:bg-slate-800 text-slate-600 dark:text-slate-400 transition-colors">
                                        <span className="material-symbols-outlined">info</span>
                                    </button>
                                </div>
                            </header>

                            {/* Messages */}
                            <div className="flex-1 overflow-y-auto p-6 space-y-6 bg-slate-50 dark:bg-slate-950">
                                <div className="max-w-3xl mx-auto space-y-1">
                                    {groupedMessages.map((group, groupIdx) => (
                                        <div key={groupIdx} className="animate-fadeIn">
                                            {/* Time separator */}
                                            {group.showTime && (
                                                <div className="flex justify-center my-4">
                                                    <span className="px-4 py-1 rounded-full bg-slate-200 dark:bg-slate-800 text-slate-500 dark:text-slate-400 text-[10px] font-bold uppercase tracking-wider">
                                                        {group.timeLabel}
                                                    </span>
                                                </div>
                                            )}

                                            {/* Messages from same sender */}
                                            <div className={`flex ${group.senderId === user.userId ? 'flex-row-reverse' : ''} items-start gap-3`}>
                                                {/* Avatar */}
                                                {group.senderId !== user.userId ? (
                                                    <div className={`size-9 rounded-full overflow-hidden shrink-0 mt-1 ${group.showAvatar ? 'visible' : 'invisible'}`}>
                                                        {otherParticipant?.profilePicture ? (
                                                            <img src={otherParticipant.profilePicture} alt="" className="w-full h-full object-cover" />
                                                        ) : (
                                                            <div className="w-full h-full bg-gradient-to-br from-blue-400 to-indigo-500 flex items-center justify-center text-white text-xs font-semibold">
                                                                {otherParticipant?.displayName?.charAt(0) || otherParticipant?.username?.charAt(0) || '?'}
                                                            </div>
                                                        )}
                                                    </div>
                                                ) : (
                                                    <div className={`size-9 rounded-full overflow-hidden shrink-0 mt-1 ${group.showAvatar ? 'visible' : 'invisible'}`}>
                                                        <div className="w-full h-full bg-gradient-to-br from-blue-500 to-indigo-500 flex items-center justify-center text-white text-xs font-semibold">
                                                            {user.username?.charAt(0).toUpperCase() || 'U'}
                                                        </div>
                                                    </div>
                                                )}

                                                {/* Message bubbles */}
                                                <div className={`flex flex-col ${group.senderId === user.userId ? 'items-end' : 'items-start'} gap-1 max-w-[70%]`}>
                                                    {group.messages.map((msg, msgIdx) => {
                                                        const isFirst = msgIdx === 0;
                                                        const isLast = msgIdx === group.messages.length - 1;
                                                        const isSent = msg.senderId === user.userId;

                                                        const getBorderRadius = () => {
                                                            if (isSent) {
                                                                if (group.messages.length === 1) return 'rounded-2xl rounded-tr-none';
                                                                if (isFirst) return 'rounded-2xl rounded-tr-none';
                                                                if (isLast) return 'rounded-2xl rounded-tr-none';
                                                                return 'rounded-2xl rounded-tr-none';
                                                            } else {
                                                                if (group.messages.length === 1) return 'rounded-2xl rounded-tl-none';
                                                                if (isFirst) return 'rounded-2xl rounded-tl-none';
                                                                if (isLast) return 'rounded-2xl rounded-tl-none';
                                                                return 'rounded-2xl rounded-tl-none';
                                                            }
                                                        };

                                                        const userReaction = msg.reactions?.find(r => r.userId === user.userId);
                                                        const groupedReactions = msg.reactions?.reduce((acc, r) => {
                                                            acc[r.reactionType] = (acc[r.reactionType] || 0) + 1;
                                                            return acc;
                                                        }, {} as Record<string, number>);

                                                        return (
                                                            <div
                                                                key={msg.messageId}
                                                                className={`group relative px-4 py-3 ${getBorderRadius()} ${isSent
                                                                        ? `bg-[var(--primary)] text-white shadow-md shadow-[var(--primary)]/20 ${msg.status === 'sending' ? 'opacity-70' : ''}`
                                                                        : 'bg-slate-200 dark:bg-slate-800 text-slate-800 dark:text-slate-200'
                                                                    } transition-all duration-200`}
                                                                onDoubleClick={() => toggleReaction(msg.messageId, 'like')}
                                                            >
                                                                {/* Quoted message (reply) */}
                                                                {msg.replyToMessage && (
                                                                    <div className={`mb-2 p-2 rounded-lg text-xs ${isSent
                                                                            ? 'bg-white/20 border-l-2 border-white/50'
                                                                            : 'bg-[var(--bg-tertiary)] border-l-2 border-[var(--primary)]'
                                                                        }`}>
                                                                        <div className={`font-semibold ${isSent ? 'text-white/90' : 'text-[var(--primary)]'}`}>
                                                                            {msg.replyToMessage.senderUsername}
                                                                        </div>
                                                                        <div className={`truncate ${isSent ? 'text-white/70' : 'text-[var(--text-muted)]'}`}>
                                                                            {msg.replyToMessage.content}
                                                                        </div>
                                                                    </div>
                                                                )}

                                                                {/* Media content */}
                                                                {msg.messageType === 'image' && msg.attachmentUrl && (
                                                                    <div
                                                                        className="mb-2 cursor-pointer"
                                                                        onClick={() => openLightbox(msg.attachmentUrl!, 'image', msg.attachmentFileName)}
                                                                    >
                                                                        <img
                                                                            src={msg.attachmentUrl}
                                                                            alt={msg.attachmentFileName || 'Image'}
                                                                            className="max-w-[200px] rounded-lg"
                                                                        />
                                                                    </div>
                                                                )}

                                                                {msg.messageType === 'video' && msg.attachmentUrl && (
                                                                    <div
                                                                        className="mb-2 cursor-pointer relative"
                                                                        onClick={() => openLightbox(msg.attachmentUrl!, 'video', msg.attachmentFileName)}
                                                                    >
                                                                        <video
                                                                            src={msg.attachmentUrl}
                                                                            className="max-w-[200px] rounded-lg"
                                                                        />
                                                                        <div className="absolute inset-0 flex items-center justify-center bg-black/30 rounded-lg">
                                                                            <span className="text-white text-3xl">▶️</span>
                                                                        </div>
                                                                    </div>
                                                                )}

                                                                {msg.messageType === 'audio' && msg.attachmentUrl && (
                                                                    <div className="mb-2">
                                                                        <audio
                                                                            src={msg.attachmentUrl}
                                                                            controls
                                                                            className="max-w-[200px]"
                                                                        />
                                                                        {msg.attachmentFileName && (
                                                                            <p className="text-xs mt-1 opacity-70">{msg.attachmentFileName}</p>
                                                                        )}
                                                                    </div>
                                                                )}

                                                                {msg.messageType === 'file' && msg.attachmentUrl && (
                                                                    <a
                                                                        href={msg.attachmentUrl}
                                                                        download={msg.attachmentFileName}
                                                                        target="_blank"
                                                                        rel="noopener noreferrer"
                                                                        className={`flex items-center gap-3 p-2 rounded-lg mb-2 border ${isSent ? 'bg-white/10 border-white/20 hover:bg-white/20' : 'bg-white dark:bg-slate-700 border-slate-300 dark:border-slate-600 hover:bg-slate-50 dark:hover:bg-slate-600'} transition`}
                                                                        onClick={(e) => e.stopPropagation()}
                                                                    >
                                                                        <div className={`size-10 rounded-lg flex items-center justify-center shrink-0 ${isSent ? 'bg-white/20' : 'bg-white dark:bg-slate-600'}`}>
                                                                            <span className={`material-symbols-outlined ${isSent ? 'text-white' : 'text-[var(--primary)]'}`}>description</span>
                                                                        </div>
                                                                        <div className="flex-1 min-w-0">
                                                                            <p className="text-sm font-bold truncate">{msg.attachmentFileName}</p>
                                                                            <p className="text-[10px] opacity-70">{formatFileSize(msg.attachmentSize)}</p>
                                                                        </div>
                                                                        <button className={`ml-auto ${isSent ? 'text-white/70 hover:text-white' : 'text-slate-400 hover:text-[var(--primary)]'} transition-colors`}>
                                                                            <span className="material-symbols-outlined">download</span>
                                                                        </button>
                                                                    </a>
                                                                )}

                                                                {/* Text content (caption for media or regular text) */}
                                                                {msg.content && (
                                                                    <p className="break-words text-sm leading-relaxed">{msg.content}</p>
                                                                )}

                                                                {/* Action buttons (appear on hover) */}
                                                                <div className={`absolute ${isSent ? '-left-16' : '-right-16'} top-1/2 -translate-y-1/2 flex gap-1 opacity-0 group-hover:opacity-100 transition-opacity`}>
                                                                    <button
                                                                        onClick={() => setReplyingTo(msg)}
                                                                        className="w-6 h-6 rounded-full bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 shadow-sm flex items-center justify-center text-xs hover:bg-slate-50 dark:hover:bg-slate-700"
                                                                        title="Reply"
                                                                    >
                                                                        ↩️
                                                                    </button>
                                                                    <button
                                                                        onClick={() => setShowReactionPicker(showReactionPicker === msg.messageId ? null : msg.messageId)}
                                                                        className="w-6 h-6 rounded-full bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 shadow-sm flex items-center justify-center text-sm hover:bg-slate-50 dark:hover:bg-slate-700"
                                                                        title="Add reaction"
                                                                    >
                                                                        {userReaction ? getReactionEmoji(userReaction.reactionType) : '😊'}
                                                                    </button>
                                                                </div>

                                                                {/* Reaction picker */}
                                                                {showReactionPicker === msg.messageId && (
                                                                    <div className={`absolute ${isSent ? 'right-0' : 'left-0'} -bottom-2 translate-y-full z-50`}>
                                                                        <ReactionPicker
                                                                            onReact={(type) => toggleReaction(msg.messageId, type)}
                                                                            onClose={() => setShowReactionPicker(null)}
                                                                            currentReaction={userReaction?.reactionType}
                                                                        />
                                                                    </div>
                                                                )}

                                                                {/* Display reactions */}
                                                                {groupedReactions && Object.keys(groupedReactions).length > 0 && (
                                                                    <div className={`absolute -bottom-3 ${isSent ? 'right-2' : 'left-2'} flex items-center gap-0.5 bg-white dark:bg-slate-800 rounded-full px-1.5 py-0.5 shadow-sm border border-slate-200 dark:border-slate-700`}>
                                                                        {Object.entries(groupedReactions).map(([type, count]) => (
                                                                            <span key={type} className="flex items-center text-xs">
                                                                                <span>{getReactionEmoji(type)}</span>
                                                                                {count > 1 && <span className="ml-0.5 text-[var(--text-muted)]">{count}</span>}
                                                                            </span>
                                                                        ))}
                                                                    </div>
                                                                )}
                                                            </div>
                                                        );
                                                    })}

                                                    {group.showAvatar && (
                                                        <div className={`flex items-center gap-1.5 mt-1 px-1 ${group.senderId === user.userId ? 'flex-row-reverse' : ''}`}>
                                                            <p className="text-[10px] text-slate-500">
                                                                {formatTime(group.messages[group.messages.length - 1].sentDate)}
                                                            </p>
                                                            {group.senderId === user.userId && (
                                                                <span className={`material-symbols-outlined text-[14px] ${group.messages[group.messages.length - 1].status === 'read' ? 'text-[var(--primary)]' : 'text-slate-400'}`}>
                                                                    done_all
                                                                </span>
                                                            )}
                                                        </div>
                                                    )}
                                                </div>
                                            </div>
                                        </div>
                                    ))}

                                    {/* Typing indicator - Instagram style */}
                                    {typingUsers.length > 0 && (
                                        <div className="flex items-start gap-3 mt-2 animate-fadeIn opacity-60">
                                            <div className="size-9 rounded-full overflow-hidden shrink-0 mt-1">
                                                {otherParticipant?.profilePicture ? (
                                                    <img src={otherParticipant.profilePicture} alt="" className="w-full h-full object-cover" />
                                                ) : (
                                                    <div className="w-full h-full bg-gradient-to-br from-blue-400 to-indigo-500 flex items-center justify-center text-white text-xs font-semibold">
                                                        {otherParticipant?.displayName?.charAt(0) || otherParticipant?.username?.charAt(0) || '?'}
                                                    </div>
                                                )}
                                            </div>
                                            <div className="bg-slate-200 dark:bg-slate-800 px-4 py-3 rounded-2xl rounded-tl-none flex gap-1">
                                                <span className="size-1.5 rounded-full bg-slate-500"></span>
                                                <span className="size-1.5 rounded-full bg-slate-500"></span>
                                                <span className="size-1.5 rounded-full bg-slate-500"></span>
                                            </div>
                                        </div>
                                    )}

                                    {/* Seen indicator - show avatar of reader */}
                                    {messages.length > 0 &&
                                        messages[messages.length - 1].senderId === user.userId &&
                                        messages[messages.length - 1].status === 'read' && (
                                            <div className="flex justify-end mt-1">
                                                <div className="flex items-center gap-1">
                                                    {otherParticipant?.profilePicture ? (
                                                        <img
                                                            src={otherParticipant.profilePicture}
                                                            alt="Seen"
                                                            className="w-4 h-4 rounded-full object-cover"
                                                            title={`Seen by ${otherParticipant.displayName || otherParticipant.username}`}
                                                        />
                                                    ) : (
                                                        <div
                                                            className="w-4 h-4 rounded-full bg-gradient-to-br from-blue-400 to-purple-500 flex items-center justify-center text-white text-[8px] font-semibold"
                                                            title={`Seen by ${otherParticipant?.displayName || otherParticipant?.username}`}
                                                        >
                                                            {otherParticipant?.displayName?.charAt(0) || otherParticipant?.username?.charAt(0) || '?'}
                                                        </div>
                                                    )}
                                                </div>
                                            </div>
                                        )}

                                    <div ref={messagesEndRef} />
                                </div>
                            </div>

                            {/* Message Input */}
                            <div className="border-t border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900">
                                {replyingTo && (
                                    <div className="px-4 py-2 bg-slate-50 dark:bg-slate-800 border-b border-slate-200 dark:border-slate-700 flex items-center gap-3 animate-slideUp">
                                        <div className="w-1 h-10 bg-[var(--primary)] rounded-full"></div>
                                        <div className="flex-1 min-w-0">
                                            <div className="text-xs font-semibold text-[var(--primary)]">
                                                Replying to {replyingTo.senderUsername}
                                            </div>
                                            <div className="text-sm text-slate-500 truncate">
                                                {replyingTo.content}
                                            </div>
                                        </div>
                                        <button
                                            onClick={() => setReplyingTo(null)}
                                            className="w-8 h-8 rounded-full hover:bg-slate-100 dark:hover:bg-slate-700 flex items-center justify-center text-slate-400 hover:text-slate-600 transition"
                                        >
                                            <span className="material-symbols-outlined">close</span>
                                        </button>
                                    </div>
                                )}

                                {/* Media preview */}
                                {selectedFile && (
                                    <div className="px-4 pt-3">
                                        <MediaPreview
                                            file={selectedFile}
                                            preview={filePreview}
                                            messageType={fileMessageType}
                                            onRemove={clearSelectedFile}
                                            isUploading={isUploading}
                                            uploadProgress={uploadProgress}
                                        />
                                    </div>
                                )}

                                <div className="p-4">
                                    <div className="flex items-end gap-3 bg-slate-100 dark:bg-slate-800 p-2 rounded-2xl relative">
                                        <div className="flex pb-1">
                                            <button
                                                onClick={() => setShowMediaPicker(!showMediaPicker)}
                                                className="flex size-9 items-center justify-center rounded-xl hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-500"
                                            >
                                                <span className="material-symbols-outlined">add_circle</span>
                                            </button>
                                            <button className="flex size-9 items-center justify-center rounded-xl hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-500">
                                                <span className="material-symbols-outlined">image</span>
                                            </button>
                                            <button className="flex size-9 items-center justify-center rounded-xl hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-500">
                                                <span className="material-symbols-outlined">attach_file</span>
                                            </button>
                                            <button className="flex size-9 items-center justify-center rounded-xl hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-500">
                                                <span className="material-symbols-outlined">mic</span>
                                            </button>
                                        </div>

                                        <MediaPicker
                                            isOpen={showMediaPicker}
                                            onClose={() => setShowMediaPicker(false)}
                                            onFileSelect={handleFileSelect}
                                        />

                                        <div className="flex-1">
                                            <input
                                                type="text"
                                                placeholder={selectedFile ? "Add a caption..." : (replyingTo ? `Reply to ${replyingTo.senderUsername}...` : "Type a message...")}
                                                value={newMessage}
                                                onChange={handleInputChange}
                                                onKeyDown={(e) => e.key === 'Enter' && (selectedFile ? sendMediaMessage() : sendMessage())}
                                                className="w-full bg-transparent border-none focus:ring-0 text-sm py-2 px-0 text-slate-900 dark:text-white placeholder-slate-400"
                                            />
                                        </div>
                                        <div className="flex pb-1 gap-1">
                                            <button className="flex size-9 items-center justify-center rounded-xl hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-500">
                                                <span className="material-symbols-outlined">mood</span>
                                            </button>
                                            <button
                                                onClick={selectedFile ? sendMediaMessage : sendMessage}
                                                disabled={selectedFile ? isUploading : !newMessage.trim()}
                                                className="flex size-10 items-center justify-center rounded-xl bg-[var(--primary)] text-white shadow-lg shadow-[var(--primary)]/30 disabled:opacity-50"
                                            >
                                                {isUploading ? (
                                                    <div className="w-5 h-5 border-2 border-white border-t-transparent rounded-full animate-spin"></div>
                                                ) : (
                                                    <span className="material-symbols-outlined">send</span>
                                                )}
                                            </button>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </>
                    ) : (
                        <div className="flex-1 flex flex-col items-center justify-center text-center bg-slate-50 dark:bg-slate-950">
                            <span className="material-symbols-outlined text-6xl text-slate-300 dark:text-slate-700 mb-4">chat</span>
                            <h3 className="text-xl font-bold text-slate-900 dark:text-white mb-2">Select a conversation</h3>
                            <p className="text-slate-500">Choose a conversation from the list or start a new one</p>
                        </div>
                    )}
                </div>
            </div>

            {/* Media Lightbox */}
            <MediaLightbox
                isOpen={lightboxOpen}
                onClose={() => setLightboxOpen(false)}
                mediaUrl={lightboxMedia.url}
                mediaType={lightboxMedia.type}
                fileName={lightboxMedia.fileName}
            />
        </>
    );
}

export default function ChatPage() {
    return (
        <Suspense fallback={
            <>
                <ModernNavbar />
                <div className="flex items-center justify-center min-h-screen bg-slate-50 dark:bg-slate-950">
                    <div className="w-10 h-10 border-4 border-[var(--primary)]/30 border-t-[var(--primary)] rounded-full animate-spin"></div>
                </div>
            </>
        }>
            <ChatContent />
        </Suspense>
    );
}
