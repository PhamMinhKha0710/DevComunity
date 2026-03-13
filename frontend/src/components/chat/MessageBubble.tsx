'use client';

import dynamic from 'next/dynamic';
import { formatFileSize } from './types';
import type { RealtimeMessage } from './types';

const REACTION_EMOJI_MAP: Record<string, string> = {
    like: '👍', love: '❤️', haha: '😂', wow: '😮', sad: '😢', angry: '😠',
};

function getReactionEmoji(type: string): string {
    return REACTION_EMOJI_MAP[type] || '👍';
}

const ReactionPicker = dynamic(() => import('@/components/ReactionPicker'), {
    loading: () => <div className="animate-pulse h-8 w-48 bg-slate-100 dark:bg-slate-800 rounded-full" />,
});

interface MessageBubbleProps {
    message: RealtimeMessage;
    isSent: boolean;
    isFirst: boolean;
    isLast: boolean;
    groupLength: number;
    currentUserId: number;
    showReactionPicker: boolean;
    onReply: (msg: RealtimeMessage) => void;
    onToggleReaction: (messageId: number, type: string) => void;
    onShowReactionPicker: (messageId: number | null) => void;
    onOpenLightbox: (url: string, type: 'image' | 'video', fileName?: string) => void;
    isCallEvent?: boolean;
}

function formatCallDuration(seconds: number): string {
    const m = Math.floor(seconds / 60);
    const s = seconds % 60;
    return m > 0 ? `${m}:${s.toString().padStart(2, '0')}` : `0:${s.toString().padStart(2, '0')}`;
}

function parseCallContent(content: string): { type: string; callType?: string; duration?: number } {
    try {
        const parsed = JSON.parse(content);
        return { type: parsed.type ?? 'ended', callType: parsed.callType, duration: parsed.duration };
    } catch {
        return { type: 'ended' };
    }
}

export default function MessageBubble({
    message: msg, isSent, currentUserId,
    showReactionPicker: pickerOpen, onReply, onToggleReaction,
    onShowReactionPicker, onOpenLightbox, isCallEvent,
}: MessageBubbleProps) {
    if (msg.messageType === 'call' || isCallEvent) {
        const { type, callType, duration } = parseCallContent(msg.content || '{}');
        const labels: Record<string, string> = {
            ended: duration != null ? `Cuộc gọi đã kết thúc • ${formatCallDuration(duration)}` : 'Cuộc gọi đã kết thúc',
            rejected: 'Cuộc gọi đã từ chối',
            missed: 'Cuộc gọi nhỡ',
            cancelled: 'Cuộc gọi đã hủy',
        };
        const label = labels[type] ?? 'Cuộc gọi';
        const icon = type === 'missed' ? 'call_missed' : type === 'ended' ? 'call_end' : 'phone_disabled';
        return (
            <div className="flex items-center justify-center gap-2 py-2 px-4 rounded-full bg-slate-200/80 dark:bg-slate-700/80 text-slate-600 dark:text-slate-300 text-sm">
                <span className="material-symbols-outlined text-base">{icon}</span>
                <span>{label}</span>
                {callType === 'video' && <span className="material-symbols-outlined text-xs">videocam</span>}
            </div>
        );
    }

    const borderRadius = isSent ? 'rounded-2xl rounded-tr-none' : 'rounded-2xl rounded-tl-none';

    const userReaction = msg.reactions?.find(r => r.userId === currentUserId);
    const groupedReactions = msg.reactions?.reduce((acc, r) => {
        acc[r.reactionType] = (acc[r.reactionType] || 0) + 1;
        return acc;
    }, {} as Record<string, number>);

    return (
        <div
            className={`group relative px-4 py-3 ${borderRadius} ${
                isSent
                    ? `bg-[var(--primary)] text-white shadow-md shadow-[var(--primary)]/20 ${msg.status === 'sending' ? 'opacity-70' : ''}`
                    : 'bg-slate-200 dark:bg-slate-800 text-slate-800 dark:text-slate-200'
            } transition-all duration-200`}
            onDoubleClick={() => onToggleReaction(msg.messageId, 'like')}
        >
            {msg.replyToMessage && (
                <div className={`mb-2 p-2 rounded-lg text-xs ${
                    isSent
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

            {msg.messageType === 'image' && msg.attachmentUrl && (
                <div className="mb-2 cursor-pointer" onClick={() => onOpenLightbox(msg.attachmentUrl!, 'image', msg.attachmentFileName)}>
                    <img src={msg.attachmentUrl} alt={msg.attachmentFileName || 'Image'} className="max-w-[200px] rounded-lg" />
                </div>
            )}

            {msg.messageType === 'video' && msg.attachmentUrl && (
                <div className="mb-2 cursor-pointer relative" onClick={() => onOpenLightbox(msg.attachmentUrl!, 'video', msg.attachmentFileName)}>
                    <video src={msg.attachmentUrl} className="max-w-[200px] rounded-lg" />
                    <div className="absolute inset-0 flex items-center justify-center bg-black/30 rounded-lg">
                        <span className="text-white text-3xl">▶️</span>
                    </div>
                </div>
            )}

            {msg.messageType === 'audio' && msg.attachmentUrl && (
                <div className="mb-2">
                    <audio src={msg.attachmentUrl} controls className="max-w-[200px]" />
                    {msg.attachmentFileName && <p className="text-xs mt-1 opacity-70">{msg.attachmentFileName}</p>}
                </div>
            )}

            {msg.messageType === 'file' && msg.attachmentUrl && (
                <a
                    href={msg.attachmentUrl}
                    download={msg.attachmentFileName}
                    target="_blank"
                    rel="noopener noreferrer"
                    className={`flex items-center gap-3 p-2 rounded-lg mb-2 border ${
                        isSent
                            ? 'bg-white/10 border-white/20 hover:bg-white/20'
                            : 'bg-white dark:bg-slate-700 border-slate-300 dark:border-slate-600 hover:bg-slate-50 dark:hover:bg-slate-600'
                    } transition`}
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

            {msg.content && <p className="break-words text-sm leading-relaxed">{msg.content}</p>}

            <div className={`absolute ${isSent ? '-left-16' : '-right-16'} top-1/2 -translate-y-1/2 flex gap-1 opacity-0 group-hover:opacity-100 transition-opacity`}>
                <button
                    onClick={() => onReply(msg)}
                    className="w-6 h-6 rounded-full bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 shadow-sm flex items-center justify-center text-xs hover:bg-slate-50 dark:hover:bg-slate-700"
                    title="Reply"
                >
                    ↩️
                </button>
                <button
                    onClick={() => onShowReactionPicker(pickerOpen ? null : msg.messageId)}
                    className="w-6 h-6 rounded-full bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 shadow-sm flex items-center justify-center text-sm hover:bg-slate-50 dark:hover:bg-slate-700"
                    title="Add reaction"
                >
                    {userReaction ? getReactionEmoji(userReaction.reactionType) : '😊'}
                </button>
            </div>

            {pickerOpen && (
                <div className={`absolute ${isSent ? 'right-0' : 'left-0'} -bottom-2 translate-y-full z-50`}>
                    <ReactionPicker
                        onReact={(type) => onToggleReaction(msg.messageId, type)}
                        onClose={() => onShowReactionPicker(null)}
                        currentReaction={userReaction?.reactionType}
                    />
                </div>
            )}

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
}
