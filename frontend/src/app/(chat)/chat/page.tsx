'use client';

import { useState, useRef, useEffect, useCallback, Suspense } from 'react';
import { useRouter } from 'next/navigation';
import { HubConnectionState } from '@microsoft/signalr';
import { useAuth } from '@/lib/contexts/AuthContext';
import { useHub } from '@/lib/signalr/useHub';
import type { Conversation, MessageReaction } from '@/types';
import apiClient from '@/lib/api/client';
import { chatApi } from '@/lib/api/chat.api';
import ModernNavbar from '@/components/ModernNavbar';
import MediaLightbox from '@/components/chat/MediaLightbox';
import ConversationList from '@/components/chat/ConversationList';
import MessageList from '@/components/chat/MessageList';
import MessageInput from '@/components/chat/MessageInput';
import NewConversationModal from '@/components/chat/NewConversationModal';
import type { RealtimeMessage, ConnectionStatus } from '@/components/chat/types';

function ChatContent() {
    const { user, isLoading: authLoading } = useAuth();
    const router = useRouter();
    const chatHub = useHub('chat');
    const presenceHub = useHub('presence');

    const [conversations, setConversations] = useState<Conversation[]>([]);
    const [selectedConversation, setSelectedConversation] = useState<number | null>(null);
    const [messages, setMessages] = useState<RealtimeMessage[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [typingUsers, setTypingUsers] = useState<string[]>([]);
    const [onlineUsers, setOnlineUsers] = useState<Set<string>>(new Set());
    const [showNewChat, setShowNewChat] = useState(false);
    const [replyingTo, setReplyingTo] = useState<RealtimeMessage | null>(null);
    const [lightboxOpen, setLightboxOpen] = useState(false);
    const [lightboxMedia, setLightboxMedia] = useState<{ url: string; type: 'image' | 'video'; fileName?: string }>({ url: '', type: 'image' });

    const messagesEndRef = useRef<HTMLDivElement>(null);
    const typingTimeoutRef = useRef<NodeJS.Timeout | null>(null);
    const selectedConvRef = useRef(selectedConversation);
    const userRef = useRef(user);

    useEffect(() => { selectedConvRef.current = selectedConversation; }, [selectedConversation]);
    useEffect(() => { userRef.current = user; }, [user]);

    // ── Data fetching ──

    const fetchConversations = useCallback(async () => {
        try {
            const data = await chatApi.getConversations();
            setConversations(Array.isArray(data) ? data : (data as { items: Conversation[] }).items || []);
        } catch (err) { console.error('Failed to fetch conversations:', err); }
        finally { setIsLoading(false); }
    }, []);

    const fetchMessages = useCallback(async (convId: number) => {
        try {
            const data = await chatApi.getMessages(convId);
            setMessages(((data as { items: RealtimeMessage[] }).items || []).map((m: RealtimeMessage) => ({ ...m, status: 'delivered' as const })));
            await chatApi.markAsRead(convId);
        } catch (err) { console.error('Failed to fetch messages:', err); }
    }, []);

    // ── Chat hub events ──

    useEffect(() => {
        if (chatHub.connectionState !== HubConnectionState.Connected) return;

        const onReceiveMessage = (msg: RealtimeMessage) => {
            if (msg.senderId === userRef.current?.userId) {
                setMessages(prev => prev.map(m =>
                    (m.status === 'sending' || m.status === 'sent') && m.content === msg.content && m.senderId === msg.senderId
                        ? { ...msg, status: 'delivered' } : m
                ));
                return;
            }
            if (selectedConvRef.current && msg.conversationId === selectedConvRef.current) {
                setMessages(prev => prev.some(m => m.messageId === msg.messageId) ? prev : [...prev, { ...msg, status: 'delivered' }]);
            }
            fetchConversations();
        };

        const onTyping = (data: { userId: string; isTyping: boolean }) => {
            setTypingUsers(prev => data.isTyping ? [...new Set([...prev, data.userId])] : prev.filter(u => u !== data.userId));
            setTimeout(() => setTypingUsers(prev => prev.filter(u => u !== data.userId)), 3000);
        };

        const onMessagesRead = (data: { userId: string; lastMessageId: number }) => {
            setMessages(prev => prev.map(m =>
                typeof m.messageId === 'number' && m.messageId <= data.lastMessageId ? { ...m, status: 'read', isRead: true } : m
            ));
        };

        const onReceiveReaction = (r: { messageId: number; userId: number; username: string; profilePicture?: string; reactionType: string; createdAt: string }) => {
            setMessages(prev => prev.map(m => {
                if (m.messageId !== r.messageId) return m;
                const existing = m.reactions || [];
                const idx = existing.findIndex(x => x.userId === r.userId);
                const updated: MessageReaction[] = idx >= 0
                    ? existing.map((x, i) => i === idx ? { ...x, reactionType: r.reactionType, createdAt: r.createdAt } : x)
                    : [...existing, { messageReactionId: Date.now(), userId: r.userId, username: r.username, profilePicture: r.profilePicture, reactionType: r.reactionType, createdAt: r.createdAt }];
                return { ...m, reactions: updated };
            }));
        };

        const onRemoveReaction = (data: { messageId: number; userId: number }) => {
            setMessages(prev => prev.map(m =>
                m.messageId === data.messageId ? { ...m, reactions: (m.reactions || []).filter(r => r.userId !== data.userId) } : m
            ));
        };

        chatHub.on('ReceiveMessage', onReceiveMessage);
        chatHub.on('UserTyping', onTyping);
        chatHub.on('MessagesRead', onMessagesRead);
        chatHub.on('ReceiveReaction', onReceiveReaction);
        chatHub.on('RemoveReaction', onRemoveReaction);
        return () => {
            chatHub.off('ReceiveMessage', onReceiveMessage);
            chatHub.off('UserTyping', onTyping);
            chatHub.off('MessagesRead', onMessagesRead);
            chatHub.off('ReceiveReaction', onReceiveReaction);
            chatHub.off('RemoveReaction', onRemoveReaction);
        };
    }, [chatHub.connectionState, fetchConversations]);

    // ── Presence hub events ──

    useEffect(() => {
        if (presenceHub.connectionState !== HubConnectionState.Connected) return;
        presenceHub.invoke('GetOnlineUsers')
            .then((list: unknown) => setOnlineUsers(new Set(list as string[])))
            .catch(console.error);

        const onOnline = (id: unknown) => setOnlineUsers(prev => new Set([...prev, String(id)]));
        const onOffline = (id: unknown) => { setOnlineUsers(prev => { const s = new Set(prev); s.delete(String(id)); return s; }); };
        presenceHub.on('UserOnline', onOnline);
        presenceHub.on('UserOffline', onOffline);
        return () => { presenceHub.off('UserOnline', onOnline); presenceHub.off('UserOffline', onOffline); };
    }, [presenceHub.connectionState]);

    // ── Join / leave conversation ──

    useEffect(() => {
        if (chatHub.connectionState !== HubConnectionState.Connected || !selectedConversation) return;
        chatHub.invoke('JoinConversation', selectedConversation).catch(console.error);
        return () => { chatHub.invoke('LeaveConversation', selectedConversation).catch(() => {}); };
    }, [selectedConversation, chatHub.connectionState]);

    // ── Auth guard & init ──

    useEffect(() => {
        if (!authLoading && !user) router.push('/login');
        else if (user) fetchConversations();
    }, [user, authLoading, router, fetchConversations]);

    useEffect(() => { messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' }); }, [messages]);

    // ── Callbacks for child components ──

    const selectConversation = (id: number) => { setSelectedConversation(id); setShowNewChat(false); fetchMessages(id); };

    const handleSendMessage = async (content: string) => {
        if (!selectedConversation || !user) return;
        const replyToId = replyingTo?.messageId;
        const replyInfo = replyingTo;
        const tempId = Date.now();

        const optimistic: RealtimeMessage = {
            messageId: tempId, conversationId: selectedConversation, senderId: user.userId,
            senderUsername: user.username, content, sentDate: new Date().toISOString(),
            isRead: false, status: 'sending', replyToMessageId: replyToId,
            replyToMessage: replyInfo ? { messageId: replyInfo.messageId, senderId: replyInfo.senderId, senderUsername: replyInfo.senderUsername || '', content: replyInfo.content } : undefined,
        };
        setMessages(prev => [...prev, optimistic]);
        setReplyingTo(null);

        try {
            if (chatHub.connectionState === HubConnectionState.Connected) {
                await chatHub.invoke('SendMessage', selectedConversation, content, replyToId || null);
            } else {
                await apiClient.post(`/chat/conversations/${selectedConversation}/messages`, { content, replyToMessageId: replyToId });
            }
            setMessages(prev => prev.map(m => m.messageId === tempId ? { ...m, status: 'sent' } : m));
            fetchConversations();
        } catch (err) {
            console.error('Failed to send:', err);
            setMessages(prev => prev.filter(m => m.messageId !== tempId));
            setReplyingTo(replyInfo);
            throw err;
        }
    };

    const handleSendMedia = async (upload: { url: string; fileName: string; fileSize: number; messageType: string }, caption: string) => {
        if (!selectedConversation || !user) return;
        const replyToId = replyingTo?.messageId;
        const replyInfo = replyingTo;
        const tempId = Date.now();

        const optimistic: RealtimeMessage = {
            messageId: tempId, conversationId: selectedConversation, senderId: user.userId,
            senderUsername: user.username, content: caption,
            messageType: upload.messageType as 'image' | 'video' | 'audio' | 'file',
            attachmentUrl: upload.url, attachmentFileName: upload.fileName, attachmentSize: upload.fileSize,
            sentDate: new Date().toISOString(), isRead: false, status: 'sending',
            replyToMessageId: replyToId,
            replyToMessage: replyInfo ? { messageId: replyInfo.messageId, senderId: replyInfo.senderId, senderUsername: replyInfo.senderUsername || '', content: replyInfo.content } : undefined,
        };
        setMessages(prev => [...prev, optimistic]);
        setReplyingTo(null);

        try {
            if (chatHub.connectionState === HubConnectionState.Connected) {
                await chatHub.invoke('SendMediaMessage', selectedConversation, upload.messageType, upload.url, upload.fileName, upload.fileSize, caption || null, replyToId || null);
            }
            setMessages(prev => prev.map(m => m.messageId === tempId ? { ...m, status: 'sent' } : m));
            fetchConversations();
        } catch (err) {
            console.error('Failed to send media:', err);
            throw err;
        }
    };

    const handleToggleReaction = async (messageId: number, reactionType: string) => {
        if (chatHub.connectionState !== HubConnectionState.Connected || !selectedConversation) return;
        const msg = messages.find(m => m.messageId === messageId);
        const existing = msg?.reactions?.find(r => r.userId === user?.userId);
        try {
            if (existing?.reactionType === reactionType) await chatHub.invoke('RemoveReaction', selectedConversation, messageId);
            else await chatHub.invoke('AddReaction', selectedConversation, messageId, reactionType);
        } catch (err) { console.error('Reaction failed:', err); }
    };

    const handleTyping = useCallback(() => {
        if (chatHub.connectionState !== HubConnectionState.Connected || !selectedConvRef.current) return;
        if (typingTimeoutRef.current) clearTimeout(typingTimeoutRef.current);
        chatHub.invoke('Typing', selectedConvRef.current, true).catch(console.error);
        typingTimeoutRef.current = setTimeout(() => {
            chatHub.invoke('Typing', selectedConvRef.current, false).catch(console.error);
        }, 2000);
    }, [chatHub.connectionState]);

    const handleConversationCreated = (convId: number) => {
        setShowNewChat(false);
        setSelectedConversation(convId);
        fetchMessages(convId);
        fetchConversations();
    };

    const handleDeleteConversation = async (convId: number) => {
        if (!window.confirm('Xóa đoạn chat này khỏi hộp thoại của bạn? Đoạn chat vẫn còn ở phía người kia.')) {
            return;
        }
        try {
            await chatApi.deleteConversation(convId);
            setConversations(prev => prev.filter(c => c.conversationId !== convId));
            if (selectedConversation === convId) {
                setSelectedConversation(null);
                setMessages([]);
            }
        } catch (err) {
            console.error('Failed to delete conversation:', err);
        }
    };

    // ── Derived ──

    const connectionStatus: ConnectionStatus =
        chatHub.connectionState === HubConnectionState.Connected ? 'connected'
            : [HubConnectionState.Connecting, HubConnectionState.Reconnecting].includes(chatHub.connectionState) ? 'connecting'
                : 'disconnected';

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
                <ConversationList
                    conversations={conversations}
                    selectedConversationId={selectedConversation}
                    currentUser={user}
                    onlineUsers={onlineUsers}
                    connectionStatus={connectionStatus}
                    onSelectConversation={selectConversation}
                    onNewChat={() => { setShowNewChat(true); setSelectedConversation(null); }}
                    onDeleteConversation={handleDeleteConversation}
                />
                <div className="flex-1 flex flex-col">
                    {showNewChat ? (
                        <NewConversationModal
                            currentUserId={user.userId}
                            onConversationCreated={handleConversationCreated}
                            onCancel={() => setShowNewChat(false)}
                        />
                    ) : selectedConversation ? (
                        <>
                            <MessageList
                                messages={messages}
                                currentUser={user}
                                otherParticipant={otherParticipant}
                                selectedConversation={selectedConv}
                                typingUsers={typingUsers}
                                onlineUsers={onlineUsers}
                                onReply={setReplyingTo}
                                onToggleReaction={handleToggleReaction}
                                onOpenLightbox={(url, type, fileName) => { setLightboxMedia({ url, type, fileName }); setLightboxOpen(true); }}
                                messagesEndRef={messagesEndRef}
                            />
                            <MessageInput
                                replyingTo={replyingTo}
                                onCancelReply={() => setReplyingTo(null)}
                                onSend={handleSendMessage}
                                onSendMedia={handleSendMedia}
                                onTyping={handleTyping}
                            />
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
