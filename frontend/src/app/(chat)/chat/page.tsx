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
import ConfirmDialog from '@/components/chat/ConfirmDialog';
import ConversationInfoPanel from '@/components/chat/ConversationInfoPanel';
import CallOverlay from '@/components/chat/CallOverlay';
import { useWebRTC } from '@/lib/webrtc/useWebRTC';
import type { CallType } from '@/lib/webrtc/useWebRTC';
import type { RealtimeMessage, ConnectionStatus } from '@/components/chat/types';

function ChatContent() {
    const { user, isLoading: authLoading } = useAuth();
    const router = useRouter();
    const chatHub = useHub('chat');
    const presenceHub = useHub('presence');
    const callHub = useHub('call');
    const webrtc = useWebRTC();

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
    const [deleteConfirmId, setDeleteConfirmId] = useState<number | null>(null);
    const [showInfoPanel, setShowInfoPanel] = useState(false);

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

    const presenceHubRef = useRef(presenceHub);
    presenceHubRef.current = presenceHub;

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

    useEffect(() => {
        const onVisibilityChange = () => {
            if (document.visibilityState === 'visible' && presenceHubRef.current.connectionState === HubConnectionState.Connected) {
                presenceHubRef.current.invoke('GetOnlineUsers')
                    .then((list: unknown) => setOnlineUsers(new Set(list as string[])))
                    .catch(console.error);
            }
        };
        document.addEventListener('visibilitychange', onVisibilityChange);
        return () => document.removeEventListener('visibilitychange', onVisibilityChange);
    }, []);

    const selectedConv = conversations.find(c => c.conversationId === selectedConversation);
    const otherParticipant = selectedConv?.participants?.find(p => p.userId !== user?.userId);

    const webrtcRef = useRef(webrtc);
    useEffect(() => { webrtcRef.current = webrtc; });

    // ── Call hub events ──

    useEffect(() => {
        if (callHub.connectionState !== HubConnectionState.Connected) return;

        webrtcRef.current.setIceCandidateSender((peerId: string, candidate: string) => {
            callHub.invoke('SendIceCandidate', peerId, candidate).catch(console.error);
        });

        const onIncomingCall = (data: { callerId: string; callerName: string; callType: string }) => {
            webrtcRef.current.handleIncomingCall(data.callerId, data.callerName, data.callType as CallType);
        };

        const onCallAccepted = () => {
            webrtcRef.current.handleCallAccepted((peerId: string, sdp: string) => {
                callHub.invoke('SendOffer', peerId, sdp).catch(console.error);
            });
        };

        const onCallRejected = () => {
            webrtcRef.current.handleCallRejected();
        };

        const onCallEnded = () => {
            const info = webrtcRef.current.callInfo;
            const state = webrtcRef.current.callState;
            const convId = selectedConvRef.current;
            if (state === 'incoming' && convId && userRef.current) {
                chatApi.logCallEvent(convId, {
                    callEventType: 'missed',
                    callType: info?.callType ?? 'audio',
                }).then(() => fetchMessages(convId)).catch(console.error);
            }
            webrtcRef.current.handleCallEnded();
        };

        const onReceiveOffer = (data: { callerId: string; sdp: string }) => {
            webrtcRef.current.handleReceiveOffer(data.callerId, data.sdp, (peerId: string, sdp: string) => {
                callHub.invoke('SendAnswer', peerId, sdp).catch(console.error);
            });
        };

        const onReceiveAnswer = (data: { answererId: string; sdp: string }) => {
            webrtcRef.current.handleReceiveAnswer(data.sdp);
        };

        const onReceiveIceCandidate = (data: { senderId: string; candidate: string }) => {
            webrtcRef.current.handleReceiveIceCandidate(data.candidate);
        };

        callHub.on('IncomingCall', onIncomingCall);
        callHub.on('CallAccepted', onCallAccepted);
        callHub.on('CallRejected', onCallRejected);
        callHub.on('CallEnded', onCallEnded);
        callHub.on('ReceiveOffer', onReceiveOffer);
        callHub.on('ReceiveAnswer', onReceiveAnswer);
        callHub.on('ReceiveIceCandidate', onReceiveIceCandidate);

        return () => {
            callHub.off('IncomingCall', onIncomingCall);
            callHub.off('CallAccepted', onCallAccepted);
            callHub.off('CallRejected', onCallRejected);
            callHub.off('CallEnded', onCallEnded);
            callHub.off('ReceiveOffer', onReceiveOffer);
            callHub.off('ReceiveAnswer', onReceiveAnswer);
            callHub.off('ReceiveIceCandidate', onReceiveIceCandidate);
        };
    }, [callHub.connectionState, fetchMessages]);

    const initiateCall = useCallback(async (type: CallType) => {
        if (!otherParticipant || callHub.connectionState !== HubConnectionState.Connected) return;
        const peerId = String(otherParticipant.userId);
        const peerName = otherParticipant.displayName || otherParticipant.username;
        const callerName = user?.displayName || user?.username || 'User';

        await webrtc.startCall(peerId, peerName, type);
        callHub.invoke('InitiateCall', peerId, type, callerName).catch(console.error);
    }, [otherParticipant, callHub.connectionState, user, webrtc]);

    const handleAcceptCall = useCallback(async () => {
        if (!webrtc.callInfo || callHub.connectionState !== HubConnectionState.Connected) return;
        await webrtc.acceptCall();
        callHub.invoke('AcceptCall', webrtc.callInfo.peerId).catch(console.error);
    }, [webrtc, callHub.connectionState]);

    const logCallEvent = useCallback((eventType: string, callType?: string, durationSeconds?: number) => {
        if (!selectedConversation || !user) return;
        chatApi.logCallEvent(selectedConversation, {
            callEventType: eventType,
            callType: callType ?? 'audio',
            durationSeconds,
        }).then(() => fetchMessages(selectedConversation)).catch(console.error);
    }, [selectedConversation, user, fetchMessages]);

    const handleRejectCall = useCallback(() => {
        if (!webrtc.callInfo || callHub.connectionState !== HubConnectionState.Connected) return;
        logCallEvent('rejected', webrtc.callInfo.callType);
        callHub.invoke('RejectCall', webrtc.callInfo.peerId).catch(console.error);
        webrtc.endCall();
    }, [webrtc, callHub.connectionState, logCallEvent]);

    const handleEndCall = useCallback(() => {
        if (!webrtc.callInfo || callHub.connectionState !== HubConnectionState.Connected) return;
        const eventType = webrtc.callState === 'connected' ? 'ended' : 'cancelled';
        const duration = webrtc.callState === 'connected' ? webrtc.callDuration : undefined;
        logCallEvent(eventType, webrtc.callInfo.callType, duration);
        callHub.invoke('EndCall', webrtc.callInfo.peerId).catch(console.error);
        webrtc.endCall();
    }, [webrtc, callHub.connectionState, logCallEvent]);


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

    const handleDeleteConversation = (convId: number) => {
        setDeleteConfirmId(convId);
    };

    const confirmDeleteConversation = async () => {
        if (deleteConfirmId === null) return;
        const convId = deleteConfirmId;
        setDeleteConfirmId(null);
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
                            <div className="flex flex-1 overflow-hidden">
                                <div className="flex flex-1 flex-col">
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
                                        onAudioCall={() => initiateCall('audio')}
                                        onVideoCall={() => initiateCall('video')}
                                        onInfoClick={() => setShowInfoPanel(prev => !prev)}
                                        messagesEndRef={messagesEndRef}
                                    />
                                    <MessageInput
                                        replyingTo={replyingTo}
                                        onCancelReply={() => setReplyingTo(null)}
                                        onSend={handleSendMessage}
                                        onSendMedia={handleSendMedia}
                                        onTyping={handleTyping}
                                    />
                                </div>
                                {showInfoPanel && selectedConv && (
                                    <ConversationInfoPanel
                                        conversation={selectedConv}
                                        otherParticipant={otherParticipant}
                                        isOnline={otherParticipant ? onlineUsers.has(String(otherParticipant.userId)) : false}
                                        messages={messages}
                                        onClose={() => setShowInfoPanel(false)}
                                    />
                                )}
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
            <MediaLightbox
                isOpen={lightboxOpen}
                onClose={() => setLightboxOpen(false)}
                mediaUrl={lightboxMedia.url}
                mediaType={lightboxMedia.type}
                fileName={lightboxMedia.fileName}
            />
            <ConfirmDialog
                isOpen={deleteConfirmId !== null}
                message="Bạn có chắc chắn muốn xóa đoạn chat này?"
                confirmLabel="Xóa"
                onConfirm={confirmDeleteConversation}
                onCancel={() => setDeleteConfirmId(null)}
            />
            {webrtc.callState !== 'idle' && webrtc.callInfo && (
                <CallOverlay
                    callState={webrtc.callState}
                    callType={webrtc.callInfo.callType}
                    peerName={webrtc.callInfo.peerName}
                    localStream={webrtc.localStream}
                    remoteStream={webrtc.remoteStream}
                    isMuted={webrtc.isMuted}
                    isCameraOff={webrtc.isCameraOff}
                    callDuration={webrtc.callDuration}
                    onAccept={handleAcceptCall}
                    onReject={handleRejectCall}
                    onEnd={handleEndCall}
                    onToggleMute={webrtc.toggleMute}
                    onToggleCamera={webrtc.toggleCamera}
                />
            )}
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
