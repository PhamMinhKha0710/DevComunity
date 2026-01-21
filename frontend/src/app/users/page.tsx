'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import apiClient from '@/lib/api/client';

interface User {
    userId: number;
    username: string;
    displayName?: string;
    profilePicture?: string;
    reputationPoints: number;
    questionCount?: number;
    answerCount?: number;
    createdDate: string;
}

export default function UsersPage() {
    const [users, setUsers] = useState<User[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [search, setSearch] = useState('');
    const [sortBy, setSortBy] = useState('reputation');

    useEffect(() => {
        fetchUsers();
    }, [sortBy]);

    const fetchUsers = async () => {
        try {
            const response = await apiClient.get<User[]>(`/users?sortBy=${sortBy}`);
            setUsers(response.data || []);
        } catch (error) {
            console.error('Failed to fetch users:', error);
        } finally {
            setIsLoading(false);
        }
    };

    const filteredUsers = users.filter(user =>
        user.username.toLowerCase().includes(search.toLowerCase()) ||
        (user.displayName || '').toLowerCase().includes(search.toLowerCase())
    );

    return (
        <div className="min-h-screen bg-gray-50 dark:bg-slate-900 py-8">
            <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
                {/* Header */}
                <div className="mb-8">
                    <h1 className="text-3xl font-bold text-gray-900 dark:text-white mb-2">Users</h1>
                    <p className="text-gray-500">Connect with developers in our community</p>
                </div>

                {/* Search and Filters */}
                <div className="bg-white dark:bg-slate-800 rounded-xl shadow-sm p-4 mb-6">
                    <div className="flex flex-col md:flex-row gap-4">
                        {/* Search */}
                        <div className="flex-1 relative">
                            <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                                <i className="bi bi-search text-gray-400"></i>
                            </div>
                            <input
                                type="text"
                                className="w-full pl-10 pr-4 py-3 border border-gray-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-orange-500 focus:border-transparent transition"
                                placeholder="Search users..."
                                value={search}
                                onChange={(e) => setSearch(e.target.value)}
                            />
                        </div>
                        {/* Sort Buttons */}
                        <div className="flex gap-2">
                            <button
                                onClick={() => setSortBy('reputation')}
                                className={`inline-flex items-center px-4 py-2 rounded-full text-sm font-medium transition ${sortBy === 'reputation'
                                        ? 'bg-orange-500 text-white'
                                        : 'bg-gray-100 dark:bg-slate-700 text-gray-700 dark:text-gray-300 hover:bg-gray-200'
                                    }`}
                            >
                                <i className="bi bi-award mr-2"></i>Reputation
                            </button>
                            <button
                                onClick={() => setSortBy('newest')}
                                className={`inline-flex items-center px-4 py-2 rounded-full text-sm font-medium transition ${sortBy === 'newest'
                                        ? 'bg-orange-500 text-white'
                                        : 'bg-gray-100 dark:bg-slate-700 text-gray-700 dark:text-gray-300 hover:bg-gray-200'
                                    }`}
                            >
                                <i className="bi bi-person-plus mr-2"></i>New Users
                            </button>
                        </div>
                    </div>
                </div>

                {/* Users Grid */}
                {isLoading ? (
                    <div className="flex items-center justify-center py-12">
                        <div className="inline-block w-8 h-8 border-4 border-orange-500 border-t-transparent rounded-full animate-spin"></div>
                    </div>
                ) : filteredUsers.length > 0 ? (
                    <div className="grid sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4">
                        {filteredUsers.map((user) => (
                            <div key={user.userId} className="bg-white dark:bg-slate-800 rounded-xl shadow-sm p-6 text-center hover:shadow-md transition">
                                <Link href={`/users/${user.userId}`}>
                                    <img
                                        src={user.profilePicture || '/images/default-avatar.png'}
                                        className="w-20 h-20 rounded-full mx-auto mb-3 border-4 border-gray-100 dark:border-slate-700"
                                        alt={user.displayName || user.username}
                                    />
                                </Link>
                                <h3 className="font-bold text-gray-900 dark:text-white mb-1">
                                    <Link href={`/users/${user.userId}`} className="hover:text-orange-500 transition">
                                        {user.displayName || user.username}
                                    </Link>
                                </h3>
                                <p className="text-gray-500 text-sm mb-4">@{user.username}</p>
                                <div className="flex justify-center gap-4 text-center text-sm">
                                    <div>
                                        <div className="font-bold text-orange-500">{user.reputationPoints}</div>
                                        <div className="text-gray-400 text-xs">reputation</div>
                                    </div>
                                    <div className="border-l border-gray-200 dark:border-slate-600"></div>
                                    <div>
                                        <div className="font-bold text-gray-900 dark:text-white">{user.questionCount || 0}</div>
                                        <div className="text-gray-400 text-xs">questions</div>
                                    </div>
                                    <div className="border-l border-gray-200 dark:border-slate-600"></div>
                                    <div>
                                        <div className="font-bold text-gray-900 dark:text-white">{user.answerCount || 0}</div>
                                        <div className="text-gray-400 text-xs">answers</div>
                                    </div>
                                </div>
                            </div>
                        ))}
                    </div>
                ) : (
                    <div className="text-center py-12">
                        <div className="w-16 h-16 bg-gray-100 dark:bg-slate-700 rounded-full mx-auto mb-4 flex items-center justify-center">
                            <i className="bi bi-people text-3xl text-gray-400"></i>
                        </div>
                        <h3 className="text-lg font-medium text-gray-900 dark:text-white mb-2">No users found</h3>
                        <p className="text-gray-500">Try a different search term</p>
                    </div>
                )}
            </div>
        </div>
    );
}
