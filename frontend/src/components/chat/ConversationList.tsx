'use client';

import { useState, useMemo } from 'react';
import type { Conversation } from '@/types';
import type { ConnectionStatus } from './types';
import { authorInitial } from './types';
import ConversationItem from './ConversationItem';

type MessageFilter = 'all' | 'unread' | 'archived';

interface ConversationListProps {
    conversations: Conversation[];
    selectedConversationId: number | null;
    currentUser: { userId: number; username: string; displayName?: string; profilePicture?: string | null };
    onlineUsers: Set<string>;
    connectionStatus: ConnectionStatus;
    onSelectConversation: (id: number) => void;
    onNewChat: () => void;
    onDeleteConversation: (id: number) => void;
}

export default function ConversationList({
    conversations,
    selectedConversationId,
    currentUser,
    onlineUsers,
    connectionStatus,
    onSelectConversation,
    onNewChat,
    onDeleteConversation,
}: ConversationListProps) {
    const [searchFilter, setSearchFilter] = useState('');
    const [messageFilter, setMessageFilter] = useState<MessageFilter>('all');

    const filteredConversations = useMemo(() => {
        let filtered = conversations;

        // Apply message filter
        if (messageFilter === 'unread') {
            filtered = filtered.filter(c => c.unreadCount > 0);
        } else if (messageFilter === 'archived') {
            // Backend doesn't have archived field yet - show empty or placeholder
            filtered = [];
        }

        // Apply search filter
        if (searchFilter.trim()) {
            const search = searchFilter.toLowerCase();
            filtered = filtered.filter(c =>
                c.title?.toLowerCase().includes(search) ||
                c.participants.some(p =>
                    (p.displayName || p.username).toLowerCase().includes(search)
                )
            );
        }

        return filtered;
    }, [conversations, messageFilter, searchFilter]);

    const statusColor = connectionStatus === 'connected'
        ? 'bg-emerald-500'
        : connectionStatus === 'connecting'
            ? 'bg-yellow-500 animate-pulse'
            : 'bg-red-500';

    const renderFilterTab = (filter: MessageFilter, label: string, icon: string, isActive: boolean) => (
        <button
            onClick={() => setMessageFilter(filter)}
            className={`flex items-center gap-3 px-3 py-2 rounded-lg cursor-pointer transition-colors ${
                isActive
                    ? 'bg-[var(--primary)]/10 text-[var(--primary)]'
                    : 'text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800'
            }`}
        >
            <span className="material-symbols-outlined text-[20px]">{icon}</span>
            <p className={`text-sm font-${isActive ? 'bold' : 'medium'}`}>{label}</p>
        </button>
    );

    return (
        <aside className="flex w-80 flex-col border-r border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 shrink-0">
            <div className="p-4 space-y-4">
                <div className="flex items-center gap-3">
                    <div className="relative">
                        {currentUser.profilePicture ? (
                            <img
                                src={currentUser.profilePicture}
                                alt={currentUser.displayName || currentUser.username}
                                className="size-10 rounded-full object-cover border-2 border-slate-100 dark:border-slate-700"
                            />
                        ) : (
                            <div className="size-10 rounded-full bg-gradient-to-br from-blue-500 to-indigo-500 flex items-center justify-center text-white font-bold text-sm">
                                {authorInitial(currentUser.displayName || currentUser.username) || 'U'}
                            </div>
                        )}
                        <div className={`absolute bottom-0 right-0 size-3 rounded-full border-2 border-white dark:border-slate-900 ${statusColor}`}></div>
                    </div>
                    <div>
                        <h1 className="text-slate-900 dark:text-white text-sm font-bold">Active Chats</h1>
                        <p className="text-slate-500 text-xs font-medium">{onlineUsers.size} Online</p>
                    </div>
                </div>
                <div className="flex flex-col gap-1">
                    {renderFilterTab('all', 'All Messages', 'chat_bubble', messageFilter === 'all')}
                    {renderFilterTab('unread', 'Unread', 'mail', messageFilter === 'unread')}
                    {renderFilterTab('archived', 'Archived', 'archive', messageFilter === 'archived')}
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
                    {filteredConversations.length > 0 ? (
                        filteredConversations.map((conv) => (
                            <ConversationItem
                                key={conv.conversationId}
                                conversation={conv}
                                isActive={selectedConversationId === conv.conversationId}
                                currentUserId={currentUser.userId}
                                onlineUsers={onlineUsers}
                                onSelect={onSelectConversation}
                                onDelete={onDeleteConversation}
                            />
                        ))
                    ) : (
                        <div className="flex flex-col items-center justify-center h-full text-center p-8">
                            <span className="material-symbols-outlined text-5xl text-slate-300 mb-4">chat</span>
                            <p className="text-slate-500">
                                {messageFilter === 'archived'
                                    ? 'No archived conversations'
                                    : messageFilter === 'unread'
                                        ? 'No unread conversations'
                                        : 'No conversations yet'}
                            </p>
                            {messageFilter === 'all' && (
                                <button
                                    onClick={onNewChat}
                                    className="mt-4 px-4 py-2 bg-[var(--primary)] text-white rounded-xl text-sm font-bold hover:bg-[var(--primary)]/90 transition"
                                >
                                    Start a chat
                                </button>
                            )}
                        </div>
                    )}
                </div>
            </div>
        </aside>
    );
}
