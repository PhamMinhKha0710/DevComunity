'use client';

import { useState } from 'react';
import apiClient from '@/lib/api/client';
import type { SearchUser } from './types';
import { authorInitial } from './types';

interface NewConversationModalProps {
    currentUserId: number;
    onConversationCreated: (conversationId: number) => void;
    onCancel: () => void;
}

export default function NewConversationModal({ currentUserId, onConversationCreated }: NewConversationModalProps) {
    const [searchUser, setSearchUser] = useState('');
    const [userResults, setUserResults] = useState<SearchUser[]>([]);
    const [selectedUser, setSelectedUser] = useState<SearchUser | null>(null);
    const [message, setMessage] = useState('');

    const searchUsers = async (query: string) => {
        if (query.length < 2) { setUserResults([]); return; }
        try {
            const response = await apiClient.get(`/users?search=${encodeURIComponent(query)}&pageSize=5`);
            const users = response.data?.items || [];
            setUserResults(users.filter((u: SearchUser) => u.userId !== currentUserId));
        } catch (err) {
            console.error('Failed to search users:', err);
        }
    };

    const startConversation = async () => {
        if (!selectedUser || !message.trim()) return;
        try {
            const response = await apiClient.post('/chat/conversations', {
                recipientId: selectedUser.userId,
                initialMessage: message.trim(),
            });
            onConversationCreated(response.data.conversationId);
        } catch (err) {
            console.error('Failed to start conversation:', err);
        }
    };

    return (
        <div className="flex-1 flex flex-col bg-slate-50 dark:bg-slate-950">
            <div className="p-4 border-b border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900">
                <h3 className="font-bold text-slate-900 dark:text-white">New Message</h3>
            </div>
            <div className="p-4 border-b border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900">
                <div className="relative">
                    <span className="material-symbols-outlined absolute left-3 top-1/2 -translate-y-1/2 text-slate-400">search</span>
                    <input
                        type="text"
                        placeholder="Search users..."
                        value={searchUser}
                        onChange={(e) => { setSearchUser(e.target.value); searchUsers(e.target.value); }}
                        className="w-full pl-10 pr-4 py-2 bg-slate-100 dark:bg-slate-800 border-none rounded-xl text-sm text-slate-900 dark:text-white focus:ring-1 focus:ring-[var(--primary)]"
                    />
                </div>
                {selectedUser && (
                    <div className="mt-2 flex items-center gap-2">
                        <span className="px-3 py-1 bg-[var(--primary)] text-white rounded-full text-sm flex items-center gap-2">
                            {selectedUser.displayName || selectedUser.username}
                            <button onClick={() => setSelectedUser(null)} className="hover:opacity-80">×</button>
                        </span>
                    </div>
                )}
                {!selectedUser && userResults.length > 0 && (
                    <div className="mt-2 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl overflow-hidden">
                        {userResults.map(u => (
                            <button
                                key={u.userId}
                                onClick={() => { setSelectedUser(u); setSearchUser(''); setUserResults([]); }}
                                className="w-full p-3 flex items-center gap-3 hover:bg-slate-50 dark:hover:bg-slate-800 text-left"
                            >
                                {u.profilePicture ? (
                                    <img src={u.profilePicture} alt="" className="w-10 h-10 rounded-full object-cover" />
                                ) : (
                                    <div className="w-10 h-10 rounded-full bg-gradient-to-br from-blue-500 to-indigo-500 flex items-center justify-center text-white font-bold text-sm">
                                        {authorInitial(u.displayName || u.username)}
                                    </div>
                                )}
                                <div>
                                    <div className="font-bold text-sm text-slate-900 dark:text-white">{u.displayName || u.username}</div>
                                    <div className="text-xs text-slate-500">@{u.username}</div>
                                </div>
                            </button>
                        ))}
                    </div>
                )}
            </div>
            <div className="flex-1"></div>
            {selectedUser && (
                <div className="p-4 border-t border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900">
                    <div className="flex items-end gap-3 bg-slate-100 dark:bg-slate-800 p-2 rounded-2xl">
                        <div className="flex-1">
                            <input
                                type="text"
                                placeholder="Type a message..."
                                value={message}
                                onChange={(e) => setMessage(e.target.value)}
                                onKeyDown={(e) => e.key === 'Enter' && startConversation()}
                                className="w-full bg-transparent border-none focus:ring-0 text-sm py-2 px-2 text-slate-900 dark:text-white"
                            />
                        </div>
                        <button
                            onClick={startConversation}
                            disabled={!message.trim()}
                            className="size-10 rounded-xl bg-[var(--primary)] text-white flex items-center justify-center disabled:opacity-50 shadow-lg shadow-[var(--primary)]/30"
                        >
                            <span className="material-symbols-outlined">send</span>
                        </button>
                    </div>
                </div>
            )}
        </div>
    );
}
