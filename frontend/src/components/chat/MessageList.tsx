'use client';

import { useState, useMemo, type RefObject } from 'react';
import { Virtuoso } from 'react-virtuoso';
import type { Conversation, ConversationParticipant, User } from '@/types';
import { formatTime, buildMessageGroups, getParticipantName, isParticipantOnline, authorInitial } from './types';
import type { RealtimeMessage } from './types';
import MessageBubble from './MessageBubble';
import TypingIndicator from './TypingIndicator';

interface MessageListProps {
    messages: RealtimeMessage[];
    currentUser: User;
    otherParticipant?: ConversationParticipant;
    selectedConversation?: Conversation;
    typingUsers: string[];
    onlineUsers: Set<string>;
    onReply: (msg: RealtimeMessage) => void;
    onToggleReaction: (messageId: number, type: string) => void;
    onOpenLightbox: (url: string, type: 'image' | 'video', fileName?: string) => void;
    onAudioCall?: () => void;
    onVideoCall?: () => void;
    onInfoClick?: () => void;
    messagesEndRef: RefObject<HTMLDivElement | null>;
}

export default function MessageList({
    messages, currentUser, otherParticipant, selectedConversation,
    typingUsers, onlineUsers, onReply, onToggleReaction, onOpenLightbox,
    onAudioCall, onVideoCall, onInfoClick, messagesEndRef,
}: MessageListProps) {
    const [showReactionPicker, setShowReactionPicker] = useState<number | null>(null);
    const groupedMessages = useMemo(() => buildMessageGroups(messages), [messages]);

    const participantOnline = selectedConversation
        ? isParticipantOnline(selectedConversation, currentUser.userId, onlineUsers)
        : false;

    const handleToggleReaction = (messageId: number, type: string) => {
        setShowReactionPicker(null);
        onToggleReaction(messageId, type);
    };

    return (
        <>
            {/* Chat Header */}
            <header className="flex h-20 items-center justify-between border-b border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 px-6 shrink-0">
                <div className="flex items-center gap-4">
                    <div className="relative">
                        <div className="size-11 rounded-full overflow-hidden bg-gradient-to-br from-blue-500 to-indigo-500 flex items-center justify-center text-white font-bold">
                            {otherParticipant?.profilePicture ? (
                                <img src={otherParticipant.profilePicture} alt="" className="w-full h-full object-cover" />
                            ) : (
                                selectedConversation ? authorInitial(getParticipantName(selectedConversation, currentUser.userId)) : '?'
                            )}
                        </div>
                        {participantOnline && (
                            <div className="absolute bottom-0 right-0 size-3.5 rounded-full bg-emerald-500 border-2 border-white dark:border-slate-900"></div>
                        )}
                    </div>
                    <div>
                        <h2 className="text-slate-900 dark:text-white text-base font-bold">
                            {selectedConversation ? getParticipantName(selectedConversation, currentUser.userId) : 'Chat'}
                        </h2>
                        {participantOnline ? (
                            <p className="text-emerald-600 dark:text-emerald-400 text-xs font-semibold">Active now</p>
                        ) : (
                            <p className="text-slate-500 text-xs">Offline</p>
                        )}
                    </div>
                </div>
                <div className="flex items-center gap-2">
                    <button onClick={onAudioCall} className="flex size-10 items-center justify-center rounded-full hover:bg-slate-100 dark:hover:bg-slate-800 text-slate-600 dark:text-slate-400 transition-colors" title="Gọi thoại">
                        <span className="material-symbols-outlined">call</span>
                    </button>
                    <button onClick={onVideoCall} className="flex size-10 items-center justify-center rounded-full hover:bg-slate-100 dark:hover:bg-slate-800 text-slate-600 dark:text-slate-400 transition-colors" title="Gọi video">
                        <span className="material-symbols-outlined">videocam</span>
                    </button>
                    <button onClick={onInfoClick} className="flex size-10 items-center justify-center rounded-full hover:bg-slate-100 dark:hover:bg-slate-800 text-slate-600 dark:text-slate-400 transition-colors" title="Thông tin">
                        <span className="material-symbols-outlined">info</span>
                    </button>
                </div>
            </header>

            {/* Messages */}
            <div className="flex-1 bg-slate-50 dark:bg-slate-950">
                <Virtuoso
                    data={groupedMessages}
                    followOutput="smooth"
                    initialTopMostItemIndex={groupedMessages.length > 0 ? groupedMessages.length - 1 : 0}
                    className="h-full"
                    itemContent={(groupIdx, group) => {
                        const isCallGroup = group.messages.length === 1 && group.messages[0].messageType === 'call';
                        return (
                        <div className="max-w-3xl mx-auto px-6 py-0.5 animate-fadeIn">
                            {group.showTime && (
                                <div className="flex justify-center my-4">
                                    <span className="px-4 py-1 rounded-full bg-slate-200 dark:bg-slate-800 text-slate-500 dark:text-slate-400 text-[10px] font-bold uppercase tracking-wider">
                                        {group.timeLabel}
                                    </span>
                                </div>
                            )}

                            {isCallGroup ? (
                                <div className="flex justify-center my-2">
                                    <MessageBubble
                                        message={group.messages[0]}
                                        isSent={false}
                                        isFirst={true}
                                        isLast={true}
                                        groupLength={1}
                                        currentUserId={currentUser.userId}
                                        showReactionPicker={false}
                                        onReply={onReply}
                                        onToggleReaction={handleToggleReaction}
                                        onShowReactionPicker={setShowReactionPicker}
                                        onOpenLightbox={onOpenLightbox}
                                        isCallEvent
                                    />
                                </div>
                            ) : (
                            <div className={`flex ${group.senderId === currentUser.userId ? 'flex-row-reverse' : ''} items-start gap-3`}>
                                {group.senderId !== currentUser.userId ? (
                                    <div className={`size-9 rounded-full overflow-hidden shrink-0 mt-1 ${group.showAvatar ? 'visible' : 'invisible'}`}>
                                        {otherParticipant?.profilePicture ? (
                                            <img src={otherParticipant.profilePicture} alt="" className="w-full h-full object-cover" />
                                        ) : (
                                            <div className="w-full h-full bg-gradient-to-br from-blue-400 to-indigo-500 flex items-center justify-center text-white text-xs font-semibold">
                                                {authorInitial(otherParticipant?.displayName || otherParticipant?.username)}
                                            </div>
                                        )}
                                    </div>
                                ) : (
                                    <div className={`size-9 rounded-full overflow-hidden shrink-0 mt-1 ${group.showAvatar ? 'visible' : 'invisible'}`}>
                                        <div className="w-full h-full bg-gradient-to-br from-blue-500 to-indigo-500 flex items-center justify-center text-white text-xs font-semibold">
                                            {authorInitial(currentUser.displayName || currentUser.username) || 'U'}
                                        </div>
                                    </div>
                                )}

                                <div className={`flex flex-col ${group.senderId === currentUser.userId ? 'items-end' : 'items-start'} gap-1 max-w-[70%] w-full`}>
                                    {group.messages.map((msg, msgIdx) => (
                                        <MessageBubble
                                            key={msg.messageId}
                                            message={msg}
                                            isSent={msg.senderId === currentUser.userId}
                                            isFirst={msgIdx === 0}
                                            isLast={msgIdx === group.messages.length - 1}
                                            groupLength={group.messages.length}
                                            currentUserId={currentUser.userId}
                                            showReactionPicker={showReactionPicker === msg.messageId}
                                            onReply={onReply}
                                            onToggleReaction={handleToggleReaction}
                                            onShowReactionPicker={setShowReactionPicker}
                                            onOpenLightbox={onOpenLightbox}
                                        />
                                    ))}

                                    {group.showAvatar && (
                                        <div className={`flex items-center gap-1.5 mt-1 px-1 ${group.senderId === currentUser.userId ? 'flex-row-reverse' : ''}`}>
                                            <p className="text-[10px] text-slate-500">
                                                {formatTime(group.messages[group.messages.length - 1].sentDate)}
                                            </p>
                                            {group.senderId === currentUser.userId && (
                                                <span className={`material-symbols-outlined text-[14px] ${group.messages[group.messages.length - 1].status === 'read' ? 'text-[var(--primary)]' : 'text-slate-400'}`}>
                                                    done_all
                                                </span>
                                            )}
                                        </div>
                                    )}
                                </div>
                            </div>
                            )}
                        </div>
                        );
                    }}
                    components={{
                        Footer: () => (
                            <div className="max-w-3xl mx-auto px-6 pb-4">
                                {typingUsers.length > 0 && <TypingIndicator otherParticipant={otherParticipant} />}
                                {messages.length > 0 &&
                                    messages[messages.length - 1].senderId === currentUser.userId &&
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
                                                        {authorInitial(otherParticipant?.displayName || otherParticipant?.username)}
                                                    </div>
                                                )}
                                            </div>
                                        </div>
                                    )}
                                <div ref={messagesEndRef} />
                            </div>
                        ),
                    }}
                />
            </div>
        </>
    );
}
