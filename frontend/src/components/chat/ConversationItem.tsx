'use client';

import type { Conversation } from '@/types';
import { formatTime, getParticipantName, getParticipantAvatar, isParticipantOnline, decodeHtmlEntities, formatCallPreview, authorInitial } from './types';

interface ConversationItemProps {
    conversation: Conversation;
    isActive: boolean;
    currentUserId: number;
    onlineUsers: Set<string>;
    onSelect: (id: number) => void;
    onDelete: (id: number) => void;
}

export default function ConversationItem({
    conversation: conv,
    isActive,
    currentUserId,
    onlineUsers,
    onSelect,
    onDelete,
}: ConversationItemProps) {
    const isOnline = isParticipantOnline(conv, currentUserId, onlineUsers);
    const hasUnread = conv.unreadCount > 0;
    const name = getParticipantName(conv, currentUserId);
    const avatar = getParticipantAvatar(conv, currentUserId);

    return (
        <div
            className={`flex items-center gap-3 px-4 py-4 cursor-pointer transition-colors ${
                isActive ? 'bg-[var(--primary)]/5 border-l-4 border-[var(--primary)]' : 'hover:bg-slate-50 dark:hover:bg-slate-800 border-l-4 border-transparent'
            }`}
            onClick={() => onSelect(conv.conversationId)}
        >
            <div className="relative shrink-0">
                <div className="size-12 rounded-full overflow-hidden bg-gradient-to-br from-blue-500 to-indigo-500 flex items-center justify-center text-white font-bold">
                    {avatar ? (
                        <img src={avatar} alt="" className="w-full h-full object-cover" />
                    ) : (
                        authorInitial(name)
                    )}
                </div>
                {isOnline && (
                    <div className="absolute bottom-0 right-0 size-3.5 rounded-full bg-emerald-500 border-2 border-white dark:border-slate-900"></div>
                )}
            </div>
            <div className="flex flex-col min-w-0 flex-1 text-left">
                <div className="flex justify-between items-baseline">
                    <p className={`text-sm truncate ${hasUnread ? 'font-bold text-slate-900 dark:text-white' : 'font-medium text-slate-900 dark:text-white'}`}>
                        {name}
                    </p>
                    <p className={`text-[10px] ${hasUnread ? 'text-[var(--primary)] font-bold' : 'text-slate-500'}`}>
                        {formatTime(conv.lastMessageDate)}
                    </p>
                </div>
                <div className="flex justify-between items-center gap-2">
                    <p className={`text-xs truncate ${isActive ? 'text-[var(--primary)] font-semibold' : hasUnread ? 'text-slate-900 dark:text-white font-bold' : 'text-slate-500'}`}>
                        {conv.lastMessagePreview
                            ? conv.lastMessagePreview.trimStart().startsWith('{')
                                ? formatCallPreview(conv.lastMessagePreview)
                                : decodeHtmlEntities(conv.lastMessagePreview)
                            : 'No messages yet'}
                    </p>
                    {hasUnread && (
                        <span className="size-4 flex items-center justify-center bg-[var(--primary)] text-white text-[10px] rounded-full shrink-0">
                            {conv.unreadCount}
                        </span>
                    )}
                </div>
            </div>
            <button
                type="button"
                onClick={(e) => {
                    e.stopPropagation();
                    onDelete(conv.conversationId);
                }}
                className="ml-2 text-slate-400 hover:text-red-500 transition-colors"
                title="Xóa đoạn chat này khỏi hộp thoại của bạn"
            >
                <span className="material-symbols-outlined text-[18px]">delete</span>
            </button>
        </div>
    );
}
