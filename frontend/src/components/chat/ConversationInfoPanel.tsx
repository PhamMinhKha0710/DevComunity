'use client';

import { useMemo } from 'react';
import type { Conversation, ConversationParticipant } from '@/types';
import type { RealtimeMessage } from './types';

interface ConversationInfoPanelProps {
    conversation: Conversation;
    otherParticipant?: ConversationParticipant;
    isOnline: boolean;
    messages: RealtimeMessage[];
    onClose: () => void;
}

export default function ConversationInfoPanel({
    conversation, otherParticipant, isOnline, messages, onClose,
}: ConversationInfoPanelProps) {
    const sharedMedia = useMemo(() =>
        messages.filter(m => m.messageType === 'image' || m.messageType === 'video'),
        [messages]
    );

    const displayName = otherParticipant?.displayName || otherParticipant?.username || 'Unknown';

    return (
        <div className="w-80 border-l border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 flex flex-col shrink-0 overflow-y-auto">
            <header className="flex items-center justify-between px-5 py-4 border-b border-slate-200 dark:border-slate-800">
                <h3 className="font-bold text-slate-900 dark:text-white text-sm">Thông tin</h3>
                <button
                    onClick={onClose}
                    className="size-8 flex items-center justify-center rounded-full hover:bg-slate-100 dark:hover:bg-slate-800 text-slate-500 transition-colors"
                >
                    <span className="material-symbols-outlined text-xl">close</span>
                </button>
            </header>

            <div className="flex flex-col items-center py-6 px-5">
                <div className="size-20 rounded-full overflow-hidden bg-gradient-to-br from-blue-500 to-indigo-500 flex items-center justify-center text-white text-2xl font-bold">
                    {otherParticipant?.profilePicture ? (
                        <img src={otherParticipant.profilePicture} alt="" className="w-full h-full object-cover" />
                    ) : (
                        displayName.charAt(0).toUpperCase()
                    )}
                </div>
                <h4 className="mt-3 font-bold text-slate-900 dark:text-white">{displayName}</h4>
                <div className="flex items-center gap-1.5 mt-1">
                    <div className={`size-2 rounded-full ${isOnline ? 'bg-emerald-500' : 'bg-slate-400'}`} />
                    <span className={`text-xs font-medium ${isOnline ? 'text-emerald-600 dark:text-emerald-400' : 'text-slate-500'}`}>
                        {isOnline ? 'Đang hoạt động' : 'Ngoại tuyến'}
                    </span>
                </div>
            </div>

            <div className="px-5 pb-4">
                <div className="flex items-center gap-3 text-sm text-slate-500">
                    <span className="material-symbols-outlined text-base">calendar_today</span>
                    <span>
                        Tạo lúc{' '}
                        {conversation.createdDate
                            ? new Date(conversation.createdDate).toLocaleDateString('vi-VN', { day: 'numeric', month: 'long', year: 'numeric' })
                            : 'Không rõ'}
                    </span>
                </div>
            </div>

            {conversation.participants && conversation.participants.length > 2 && (
                <div className="px-5 pb-4">
                    <h5 className="text-xs font-bold uppercase tracking-wider text-slate-500 mb-3">Thành viên ({conversation.participants.length})</h5>
                    <div className="space-y-2">
                        {conversation.participants.map(p => (
                            <div key={p.userId} className="flex items-center gap-2.5">
                                <div className="size-8 rounded-full overflow-hidden bg-gradient-to-br from-blue-400 to-indigo-500 flex items-center justify-center text-white text-xs font-semibold shrink-0">
                                    {p.profilePicture ? (
                                        <img src={p.profilePicture} alt="" className="w-full h-full object-cover" />
                                    ) : (
                                        (p.displayName || p.username).charAt(0).toUpperCase()
                                    )}
                                </div>
                                <span className="text-sm text-slate-700 dark:text-slate-300 truncate">{p.displayName || p.username}</span>
                            </div>
                        ))}
                    </div>
                </div>
            )}

            <div className="px-5 pb-5">
                <h5 className="text-xs font-bold uppercase tracking-wider text-slate-500 mb-3">Media đã chia sẻ ({sharedMedia.length})</h5>
                {sharedMedia.length > 0 ? (
                    <div className="grid grid-cols-3 gap-1.5">
                        {sharedMedia.slice(0, 9).map(m => (
                            <div key={m.messageId} className="aspect-square rounded-lg overflow-hidden bg-slate-100 dark:bg-slate-800">
                                {m.messageType === 'image' ? (
                                    <img src={m.attachmentUrl} alt="" className="w-full h-full object-cover" />
                                ) : (
                                    <div className="w-full h-full flex items-center justify-center">
                                        <span className="material-symbols-outlined text-slate-400">videocam</span>
                                    </div>
                                )}
                            </div>
                        ))}
                    </div>
                ) : (
                    <p className="text-sm text-slate-400">Chưa có media nào</p>
                )}
            </div>
        </div>
    );
}
