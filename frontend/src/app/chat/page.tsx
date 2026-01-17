'use client';

import { useEffect, useState, useCallback } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useAuth } from '@/lib/contexts/AuthContext';
import type { Conversation, ChatMessage } from '@/types';
import apiClient from '@/lib/api/client';

export default function ChatPage() {
    const { user, isLoading: authLoading } = useAuth();
    const router = useRouter();
    const [conversations, setConversations] = useState<Conversation[]>([]);
    const [selectedConversation, setSelectedConversation] = useState<number | null>(null);
    const [messages, setMessages] = useState<ChatMessage[]>([]);
    const [newMessage, setNewMessage] = useState('');
    const [isLoading, setIsLoading] = useState(true);
    const [lastMessageTime, setLastMessageTime] = useState<string | null>(null);

    useEffect(() => {
        if (!authLoading && !user) {
            router.push('/login');
        } else if (user) {
            fetchConversations();
        }
    }, [user, authLoading, router]);

    // Polling for new messages every 3 seconds
    useEffect(() => {
        if (!selectedConversation) return;

        const pollInterval = setInterval(() => {
            fetchNewMessages();
        }, 3000);

        return () => clearInterval(pollInterval);
    }, [selectedConversation, lastMessageTime]);

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
            // Immediately fetch to show our message
            fetchMessages(selectedConversation);
        } catch (error) {
            console.error('Failed to send message:', error);
        }
    };

    const formatTime = (dateString: string) => {
        const date = new Date(dateString);
        return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    };

    if (authLoading || isLoading) {
        return (
            <div className="container py-5 text-center">
                <div className="spinner-border text-primary" role="status">
                    <span className="visually-hidden">Loading...</span>
                </div>
            </div>
        );
    }

    if (!user) return null;

    return (
        <div className="container-fluid py-0" style={{ height: 'calc(100vh - 160px)' }}>
            <div className="row h-100">
                {/* Conversations Sidebar */}
                <div className="col-md-4 col-lg-3 border-end h-100 d-flex flex-column">
                    <div className="p-3 border-bottom d-flex justify-content-between align-items-center">
                        <h5 className="mb-0 fw-bold">
                            <i className="bi bi-chat-dots me-2"></i>Messages
                        </h5>
                        <button className="btn btn-sm btn-primary rounded-circle">
                            <i className="bi bi-plus"></i>
                        </button>
                    </div>

                    <div className="flex-grow-1 overflow-auto">
                        {conversations.length > 0 ? (
                            <div className="list-group list-group-flush">
                                {conversations.map((conv) => (
                                    <button
                                        key={conv.conversationId}
                                        onClick={() => selectConversation(conv.conversationId)}
                                        className={`list-group-item list-group-item-action p-3 border-0 ${selectedConversation === conv.conversationId ? 'active' : ''
                                            }`}
                                    >
                                        <div className="d-flex w-100 justify-content-between align-items-start">
                                            <div className="d-flex">
                                                <div className="position-relative me-3">
                                                    {conv.participants.length > 0 ? (
                                                        <img
                                                            src={conv.participants[0].profilePicture || '/images/default-avatar.png'}
                                                            className="rounded-circle"
                                                            width="48"
                                                            height="48"
                                                            alt=""
                                                        />
                                                    ) : (
                                                        <div className="rounded-circle bg-secondary d-flex align-items-center justify-content-center" style={{ width: 48, height: 48 }}>
                                                            <i className="bi bi-person text-white"></i>
                                                        </div>
                                                    )}
                                                    {conv.unreadCount > 0 && (
                                                        <span className="position-absolute top-0 start-100 translate-middle badge rounded-pill bg-danger">
                                                            {conv.unreadCount}
                                                        </span>
                                                    )}
                                                </div>
                                                <div>
                                                    <h6 className="mb-1 fw-semibold">
                                                        {conv.title || conv.participants.map(p => p.username).join(', ')}
                                                    </h6>
                                                    <p className="mb-0 small text-truncate" style={{ maxWidth: '150px' }}>
                                                        {conv.lastMessage || 'No messages yet'}
                                                    </p>
                                                </div>
                                            </div>
                                            <small className="text-muted">{formatTime(conv.lastActivityAt)}</small>
                                        </div>
                                    </button>
                                ))}
                            </div>
                        ) : (
                            <div className="text-center py-5 text-muted">
                                <i className="bi bi-chat-square-dots fs-1 mb-3"></i>
                                <p>No conversations yet</p>
                            </div>
                        )}
                    </div>
                </div>

                {/* Chat Area */}
                <div className="col-md-8 col-lg-9 h-100 d-flex flex-column">
                    {selectedConversation ? (
                        <>
                            {/* Chat Header */}
                            <div className="p-3 border-bottom bg-white">
                                <div className="d-flex align-items-center">
                                    {conversations.find(c => c.conversationId === selectedConversation)?.participants[0] && (
                                        <img
                                            src={conversations.find(c => c.conversationId === selectedConversation)?.participants[0].profilePicture || '/images/default-avatar.png'}
                                            className="rounded-circle me-3"
                                            width="40"
                                            height="40"
                                            alt=""
                                        />
                                    )}
                                    <div>
                                        <h6 className="mb-0 fw-bold">
                                            {conversations.find(c => c.conversationId === selectedConversation)?.title ||
                                                conversations.find(c => c.conversationId === selectedConversation)?.participants.map(p => p.username).join(', ')}
                                        </h6>
                                        <small className="text-success">
                                            <i className="bi bi-circle-fill me-1" style={{ fontSize: '0.5rem' }}></i>Online
                                        </small>
                                    </div>
                                </div>
                            </div>

                            {/* Messages */}
                            <div className="flex-grow-1 overflow-auto p-4 bg-light" id="messagesContainer">
                                {messages.map((msg) => (
                                    <div
                                        key={msg.messageId}
                                        className={`d-flex mb-3 ${msg.senderId === user.userId ? 'justify-content-end' : 'justify-content-start'}`}
                                    >
                                        {msg.senderId !== user.userId && (
                                            <img
                                                src={msg.senderAvatar || '/images/default-avatar.png'}
                                                className="rounded-circle me-2 align-self-end"
                                                width="32"
                                                height="32"
                                                alt=""
                                            />
                                        )}
                                        <div
                                            className={`p-3 rounded-4 ${msg.senderId === user.userId
                                                    ? 'bg-primary text-white'
                                                    : 'bg-white border'
                                                }`}
                                            style={{ maxWidth: '70%' }}
                                        >
                                            <p className="mb-1">{msg.content}</p>
                                            <small className={msg.senderId === user.userId ? 'text-white-50' : 'text-muted'}>
                                                {formatTime(msg.sentAt)}
                                            </small>
                                        </div>
                                    </div>
                                ))}
                            </div>

                            {/* Message Input */}
                            <div className="p-3 border-top bg-white">
                                <div className="input-group">
                                    <input
                                        type="text"
                                        className="form-control rounded-pill border-end-0"
                                        placeholder="Type a message..."
                                        value={newMessage}
                                        onChange={(e) => setNewMessage(e.target.value)}
                                        onKeyPress={(e) => e.key === 'Enter' && sendMessage()}
                                    />
                                    <button
                                        className="btn btn-primary rounded-pill ms-2"
                                        onClick={sendMessage}
                                        disabled={!newMessage.trim()}
                                    >
                                        <i className="bi bi-send"></i>
                                    </button>
                                </div>
                            </div>
                        </>
                    ) : (
                        <div className="flex-grow-1 d-flex flex-column align-items-center justify-content-center text-muted">
                            <i className="bi bi-chat-square-dots fs-1 mb-3"></i>
                            <h5>Select a conversation</h5>
                            <p>Choose a conversation from the list or start a new one</p>
                        </div>
                    )}
                </div>
            </div>
        </div>
    );
}
