'use client';

import { useState } from 'react';
import type { Conversation } from '@/types';
import type { ConnectionStatus } from './types';
import ConversationItem from './ConversationItem';

interface ConversationListProps {
    conversations: Conversation[];
    selectedConversationId: number | null;
    currentUser: { userId: number; username: string };
    onlineUsers: Set<string>;
    connectionStatus: ConnectionStatus;
    onSelectConversation: (id: number) => void;
    onNewChat: () => void;
}

export default function ConversationList({
    conversations, selectedConversationId, currentUser, onlineUsers,
    connectionStatus, onSelectConversation, onNewChat,
}: ConversationListProps) {
    const [searchFilter, setSearchFilter] = useState('');

    const statusColor = connectionStatus === 'connected'
        ? 'bg-emerald-500'
        : connectionStatus === 'connecting'
            ? 'bg-yellow-500 animate-pulse'
            : 'bg-red-500';

    return (
        <aside className="flex w-80 flex-col border-r border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 shrink-0">
            <div className="p-4 space-y-4">
                <div className="flex items-center gap-3">
                    <div className="relative">
                        <div className="size-10 rounded-full bg-gradient-to-br from-blue-500 to-indigo-500 flex items-center justify-center text-white font-bold text-sm">
                            {currentUser.username?.charAt(0).toUpperCase() || 'U'}
                        </div>
                        <div className={`absolute bottom-0 right-0 size-3 rounded-full border-2 border-white dark:border-slate-900 ${statusColor}`}></div>
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
                    onClick={onNewChat}
                    className="w-full bg-[var(--primary)] hover:bg-[var(--primary)]/90 text-white rounded-xl py-2.5 text-sm font-bold flex items-center justify-center gap-2 transition-all"
                >
                    <span className="material-symbols-outlined text-[20px]">edit_square</span>
                    New Message
                </button>
                <div className="relative">
                    <div className="absolute inset-y-0 left-0 flex items-center pl-3 pointer-events-none text-slate-400">
                        <span className="material-symbols-outlined text-[20px]">search</span>
                    </div>
                    <input
                        className="block w-full pl-10 pr-3 py-2 bg-slate-100 dark:bg-slate-800 border-none rounded-xl text-xs focus:ring-1 focus:ring-[var(--primary)]"
                        placeholder="Search conversations..."
                        type="text"
                        value={searchFilter}
                        onChange={(e) => setSearchFilter(e.target.value)}
                    />
                </div>
            </div>
            <div className="flex-1 overflow-y-auto">
                <div className="flex flex-col">
                    {conversations.length > 0 ? (
                        conversations.map((conv) => (
                            <ConversationItem
                                key={conv.conversationId}
                                conversation={conv}
                                isActive={selectedConversationId === conv.conversationId}
                                currentUserId={currentUser.userId}
                                onlineUsers={onlineUsers}
                                onSelect={onSelectConversation}
                            />
                        ))
                    ) : (
                        <div className="flex flex-col items-center justify-center h-full text-center p-8">
                            <span className="material-symbols-outlined text-5xl text-slate-300 mb-4">chat</span>
                            <p className="text-slate-500">No conversations yet</p>
                            <button
                                onClick={onNewChat}
                                className="mt-4 px-4 py-2 bg-[var(--primary)] text-white rounded-xl text-sm font-bold hover:bg-[var(--primary)]/90 transition"
                            >
                                Start a chat
                            </button>
                        </div>
                    )}
                </div>
            </div>
        </aside>
    );
}
