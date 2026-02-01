'use client';

import { useEffect, useState, useRef } from 'react';
import { useRouter } from 'next/navigation';
import { useAuth } from '@/lib/contexts/AuthContext';
import type { Conversation, ChatMessage } from '@/types';
import apiClient from '@/lib/api/client';
import ModernNavbar from '@/components/ModernNavbar';

export default function ChatPage() {
    const { user, isLoading: authLoading } = useAuth();
    const router = useRouter();
    const [conversations, setConversations] = useState<Conversation[]>([]);
    const [selectedConversation, setSelectedConversation] = useState<number | null>(null);
    const [messages, setMessages] = useState<ChatMessage[]>([]);
    const [newMessage, setNewMessage] = useState('');
    const [isLoading, setIsLoading] = useState(true);
    const [lastMessageTime, setLastMessageTime] = useState<string | null>(null);
    const messagesEndRef = useRef<HTMLDivElement>(null);

    useEffect(() => {
        if (!authLoading && !user) {
            router.push('/login');
        } else if (user) {
            fetchConversations();
        }
    }, [user, authLoading, router]);

    useEffect(() => {
        if (!selectedConversation) return;
        const pollInterval = setInterval(() => {
            fetchNewMessages();
        }, 3000);
        return () => clearInterval(pollInterval);
    }, [selectedConversation, lastMessageTime]);

    useEffect(() => {
        messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
    }, [messages]);

    const fetchConversations = async () => {
        try {
            const response = await apiClient.get<{ conversations: Conversation[] }>('/chat/conversations');
            setConversations(response.data.conversations || []);
        } catch (error) {
            console.error('Failed to fetch conversations:', error);
        } finally {
            setIsLoading(false);
        }
    };

    const fetchMessages = async (conversationId: number) => {
        try {
            const response = await apiClient.get<{ messages: ChatMessage[] }>(`/chat/conversations/${conversationId}/messages`);
            const msgs = response.data.messages || [];
            setMessages(msgs);
            if (msgs.length > 0) {
                setLastMessageTime(msgs[msgs.length - 1].sentAt);
            }
        } catch (error) {
            console.error('Failed to fetch messages:', error);
        }
    };

    const fetchNewMessages = async () => {
        if (!selectedConversation || !lastMessageTime) return;
        try {
            const response = await apiClient.get<{ messages: ChatMessage[] }>(
                `/chat/conversations/${selectedConversation}/messages/new?since=${encodeURIComponent(lastMessageTime)}`
            );
            const newMsgs = response.data.messages || [];
            if (newMsgs.length > 0) {
                setMessages(prev => [...prev, ...newMsgs]);
                setLastMessageTime(newMsgs[newMsgs.length - 1].sentAt);
            }
        } catch (error) {
            // Silent fail for polling
        }
    };

    const selectConversation = (conversationId: number) => {
        setSelectedConversation(conversationId);
        fetchMessages(conversationId);
    };

    const sendMessage = async () => {
        if (!newMessage.trim() || !selectedConversation) return;
        try {
            await apiClient.post(`/chat/conversations/${selectedConversation}/messages`, {
                content: newMessage
            });
            setNewMessage('');
            fetchMessages(selectedConversation);
        } catch (error) {
            console.error('Failed to send message:', error);
        }
    };

    const formatTime = (dateString: string) => {
        const date = new Date(dateString);
        return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
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
                    {/* Sidebar Header */}
                    <div className="p-4 border-b border-[var(--border-color)]">
                        <div className="flex items-center justify-between">
                            <h2 className="text-lg font-bold text-[var(--text-primary)] flex items-center gap-2">
                                <i className="bi bi-chat-dots-fill text-[var(--primary)]"></i>
                                Messages
                            </h2>
                            <button className="w-8 h-8 rounded-lg bg-[var(--primary)] text-white flex items-center justify-center hover:bg-[var(--primary-dark)] transition">
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
                                            {conv.participants[0]?.username?.charAt(0).toUpperCase() || '?'}
                                        </div>
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
                                                {conv.title || conv.participants.map(p => p.username).join(', ')}
                                            </span>
                                            <span className="text-xs text-[var(--text-muted)] whitespace-nowrap">
                                                {formatTime(conv.lastActivityAt)}
                                            </span>
                                        </div>
                                        <p className="text-sm text-[var(--text-muted)] truncate mt-0.5">
                                            {conv.lastMessage || 'No messages yet'}
                                        </p>
                                    </div>
                                </button>
                            ))
                        ) : (
                            <div className="flex flex-col items-center justify-center h-full text-center p-8">
                                <i className="bi bi-chat-square-dots text-5xl text-[var(--text-muted)] mb-4"></i>
                                <p className="text-[var(--text-muted)]">No conversations yet</p>
                            </div>
                        )}
                    </div>
                </div>

                {/* Chat Area */}
                <div className="flex-1 flex flex-col">
                    {selectedConversation ? (
                        <>
                            {/* Chat Header */}
                            <div className="p-4 border-b border-[var(--border-color)] bg-[var(--bg-secondary)]">
                                <div className="flex items-center gap-3">
                                    <div className="w-10 h-10 rounded-full bg-gradient-to-br from-purple-500 to-pink-500 flex items-center justify-center text-white font-bold">
                                        {selectedConv?.participants[0]?.username?.charAt(0).toUpperCase() || '?'}
                                    </div>
                                    <div>
                                        <h3 className="font-semibold text-[var(--text-primary)]">
                                            {selectedConv?.title || selectedConv?.participants.map(p => p.username).join(', ')}
                                        </h3>
                                        <span className="text-xs text-green-500 flex items-center gap-1">
                                            <span className="w-2 h-2 rounded-full bg-green-500"></span>
                                            Online
                                        </span>
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
                                                {msg.senderName?.charAt(0).toUpperCase() || '?'}
                                            </div>
                                        )}
                                        <div
                                            className={`max-w-[70%] px-4 py-3 rounded-2xl ${msg.senderId === user.userId
                                                ? 'bg-[var(--primary)] text-white rounded-tr-sm'
                                                : 'bg-[var(--bg-secondary)] text-[var(--text-primary)] border border-[var(--border-color)] rounded-tl-sm'
                                                }`}
                                        >
                                            <p className="break-words">{msg.content}</p>
                                            <span className={`text-xs mt-1 block ${msg.senderId === user.userId ? 'text-white/70' : 'text-[var(--text-muted)]'
                                                }`}>
                                                {formatTime(msg.sentAt)}
                                            </span>
                                        </div>
                                    </div>
                                ))}
                                <div ref={messagesEndRef} />
                            </div>

                            {/* Message Input */}
                            <div className="p-4 border-t border-[var(--border-color)] bg-[var(--bg-secondary)]">
                                <div className="flex items-center gap-3">
                                    <input
                                        type="text"
                                        placeholder="Type a message..."
                                        value={newMessage}
                                        onChange={(e) => setNewMessage(e.target.value)}
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
