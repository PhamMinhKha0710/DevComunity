'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import apiClient from '@/lib/api/client';
import { useAuth } from '@/lib/contexts/AuthContext';
import AppLayout from '@/components/AppLayout';

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
    const { user: currentUser } = useAuth();
    const [users, setUsers] = useState<User[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [search, setSearch] = useState('');
    const [sortBy, setSortBy] = useState('reputation');
    const [followingIds, setFollowingIds] = useState<Set<number>>(new Set());
    const [followLoading, setFollowLoading] = useState<number | null>(null);
    const [friendStatus, setFriendStatus] = useState<Record<number, { areFriends: boolean; requestPending: boolean; isSentByMe: boolean }>>({});
    const [friendLoading, setFriendLoading] = useState<number | null>(null);

    useEffect(() => {
        fetchUsers();
    }, [sortBy]);

    useEffect(() => {
        if (currentUser) {
            fetchFollowing();
        }
    }, [currentUser]);

    const fetchFollowing = async () => {
        if (!currentUser) return;
        try {
            const response = await apiClient.get<any[]>(`/Follow/following/${currentUser.userId}`);
            const ids = new Set(response.data.map((f: any) => f.userId));
            setFollowingIds(ids);
        } catch (error) {
            console.error('Failed to fetch following list:', error);
        }
    };

    const handleFollow = async (targetId: number) => {
        if (!currentUser) return;
        setFollowLoading(targetId);
        try {
            await apiClient.post(`/Follow/${targetId}`);
            setFollowingIds(prev => new Set(prev).add(targetId));
        } catch (error) {
            console.error('Failed to follow user:', error);
        } finally {
            setFollowLoading(null);
        }
    };

    const handleUnfollow = async (targetId: number) => {
        if (!currentUser) return;
        setFollowLoading(targetId);
        try {
            await apiClient.delete(`/Follow/${targetId}`);
            setFollowingIds(prev => {
                const next = new Set(prev);
                next.delete(targetId);
                return next;
            });
        } catch (error) {
            console.error('Failed to unfollow user:', error);
        } finally {
            setFollowLoading(null);
        }
    };

    const checkFriendStatus = async (targetId: number) => {
        if (!currentUser) return;
        try {
            const response = await apiClient.get<{ areFriends: boolean; requestPending: boolean; isSentByMe: boolean }>(`/Friendship/check/${targetId}`);
            setFriendStatus(prev => ({ ...prev, [targetId]: response.data }));
        } catch {
            // Ignore errors
        }
    };

    const handleAddFriend = async (targetId: number) => {
        if (!currentUser) return;
        setFriendLoading(targetId);
        try {
            await apiClient.post(`/Friendship/request/${targetId}`);
            setFriendStatus(prev => ({ ...prev, [targetId]: { areFriends: false, requestPending: true, isSentByMe: true } }));
        } catch (error) {
            console.error('Failed to send friend request:', error);
        } finally {
            setFriendLoading(null);
        }
    };

    useEffect(() => {
        if (currentUser && users.length > 0) {
            users.forEach(user => {
                if (user.userId !== currentUser.userId) {
                    checkFriendStatus(user.userId);
                }
            });
        }
    }, [currentUser, users]);

    const fetchUsers = async () => {
        try {
            const response = await apiClient.get<any>(`/users?sortBy=${sortBy}`);
            const data = response.data;
            if (Array.isArray(data)) {
                setUsers(data);
            } else if (data && Array.isArray(data.items)) {
                setUsers(data.items);
            } else {
                setUsers([]);
            }
        } catch (error) {
            console.error('Failed to fetch users:', error);
            setUsers([]);
        } finally {
            setIsLoading(false);
        }
    };

    const filteredUsers = users.filter(user =>
        user.username.toLowerCase().includes(search.toLowerCase()) ||
        (user.displayName || '').toLowerCase().includes(search.toLowerCase())
    );

    const sortOptions = [
        { key: 'reputation', label: 'Reputation', icon: 'bi-award' },
        { key: 'newest', label: 'New Users', icon: 'bi-person-plus' },
    ];

    return (
        <AppLayout>
            {/* Header */}
            <div className="mb-6">
                <h1 className="text-2xl font-bold text-[var(--text-primary)] flex items-center gap-2">
                    <i className="bi bi-people-fill text-[var(--primary)]"></i>
                    Community Members
                </h1>
                <p className="text-[var(--text-muted)]">Connect with developers in our community</p>
            </div>

            {/* Search & Filters */}
            <div className="flex flex-col sm:flex-row gap-4 mb-6">
                <div className="relative flex-1">
                    <i className="bi bi-search absolute left-4 top-1/2 -translate-y-1/2 text-[var(--text-muted)]"></i>
                    <input
                        type="text"
                        placeholder="Search users..."
                        value={search}
                        onChange={(e) => setSearch(e.target.value)}
                        className="w-full pl-11 pr-4 py-3 bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-xl text-[var(--text-primary)] placeholder-[var(--text-muted)] focus:border-[var(--primary)] focus:ring-2 focus:ring-[var(--primary)]/20 transition"
                    />
                </div>
                <div className="flex gap-2">
                    {sortOptions.map((opt) => (
                        <button
                            key={opt.key}
                            onClick={() => setSortBy(opt.key)}
                            className={`flex items-center gap-2 px-4 py-2 rounded-xl font-medium text-sm transition ${sortBy === opt.key
                                ? 'bg-[var(--primary)] text-white'
                                : 'bg-[var(--bg-secondary)] text-[var(--text-muted)] border border-[var(--border-color)] hover:border-[var(--primary)]'
                                }`}
                        >
                            <i className={`bi ${opt.icon}`}></i>
                            {opt.label}
                        </button>
                    ))}
                </div>
            </div>

            {/* Users Grid */}
            {isLoading ? (
                <div className="flex items-center justify-center py-16">
                    <div className="w-10 h-10 border-4 border-[var(--primary)]/30 border-t-[var(--primary)] rounded-full animate-spin"></div>
                </div>
            ) : filteredUsers.length === 0 ? (
                <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-2xl p-12 text-center">
                    <i className="bi bi-people text-5xl text-[var(--text-muted)] mb-4"></i>
                    <h3 className="text-lg font-semibold text-[var(--text-primary)] mb-2">No users found</h3>
                    <p className="text-[var(--text-muted)]">Try a different search term</p>
                </div>
            ) : (
                <div className="grid sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
                    {filteredUsers.map((user) => (
                        <div
                            key={user.userId}
                            className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-2xl p-5 text-center hover:border-[var(--primary)]/50 transition"
                        >
                            {/* Avatar */}
                            <Link href={`/users/${user.userId}`}>
                                <div className="w-20 h-20 rounded-full bg-gradient-to-br from-purple-500 via-pink-500 to-orange-500 p-0.5 mx-auto mb-3">
                                    <div className="w-full h-full rounded-full bg-[var(--bg-secondary)] flex items-center justify-center text-2xl font-bold text-[var(--text-primary)]">
                                        {user.displayName?.charAt(0).toUpperCase() || user.username?.charAt(0).toUpperCase() || '?'}
                                    </div>
                                </div>
                            </Link>

                            <Link href={`/users/${user.userId}`} className="font-semibold text-[var(--text-primary)] hover:text-[var(--primary)] transition">
                                {user.displayName || user.username}
                            </Link>
                            <p className="text-sm text-[var(--text-muted)] mb-3">@{user.username}</p>

                            {/* Stats */}
                            <div className="flex justify-center gap-4 text-center mb-4">
                                <div>
                                    <span className="block text-lg font-bold text-[var(--primary)]">{user.reputationPoints}</span>
                                    <span className="text-xs text-[var(--text-muted)]">reputation</span>
                                </div>
                                <div className="w-px bg-[var(--border-color)]"></div>
                                <div>
                                    <span className="block text-lg font-bold text-[var(--text-primary)]">{user.questionCount || 0}</span>
                                    <span className="text-xs text-[var(--text-muted)]">questions</span>
                                </div>
                            </div>

                            {/* Follow Button */}
                            {currentUser && currentUser.userId !== user.userId && (
                                <div className="space-y-2">
                                    <button
                                        onClick={() => followingIds.has(user.userId) ? handleUnfollow(user.userId) : handleFollow(user.userId)}
                                        disabled={followLoading === user.userId}
                                        className={`w-full py-2 rounded-xl font-medium text-sm transition ${followingIds.has(user.userId)
                                            ? 'border border-[var(--primary)] text-[var(--primary)] hover:bg-[var(--primary)]/10'
                                            : 'bg-[var(--primary)] text-white hover:bg-[var(--primary-dark)]'
                                            }`}
                                    >
                                        {followLoading === user.userId ? (
                                            <div className="w-4 h-4 border-2 border-current border-t-transparent rounded-full animate-spin mx-auto"></div>
                                        ) : followingIds.has(user.userId) ? (
                                            <><i className="bi bi-person-check mr-1"></i> Following</>
                                        ) : (
                                            <><i className="bi bi-person-plus mr-1"></i> Follow</>
                                        )}
                                    </button>

                                    {/* Add Friend Button */}
                                    {(() => {
                                        const status = friendStatus[user.userId];
                                        if (status?.areFriends) {
                                            return (
                                                <button disabled className="w-full py-2 rounded-xl font-medium text-sm bg-green-500/20 text-green-500 border border-green-500/30">
                                                    <i className="bi bi-people-fill mr-1"></i> Friends
                                                </button>
                                            );
                                        }
                                        if (status?.requestPending) {
                                            return (
                                                <button disabled className="w-full py-2 rounded-xl font-medium text-sm bg-yellow-500/20 text-yellow-600 border border-yellow-500/30">
                                                    <i className="bi bi-hourglass-split mr-1"></i>
                                                    {status.isSentByMe ? 'Request Sent' : 'Respond to Request'}
                                                </button>
                                            );
                                        }
                                        return (
                                            <button
                                                onClick={() => handleAddFriend(user.userId)}
                                                disabled={friendLoading === user.userId}
                                                className="w-full py-2 rounded-xl font-medium text-sm border border-[var(--border-color)] text-[var(--text-muted)] hover:border-blue-500 hover:text-blue-500 transition"
                                            >
                                                {friendLoading === user.userId ? (
                                                    <div className="w-4 h-4 border-2 border-current border-t-transparent rounded-full animate-spin mx-auto"></div>
                                                ) : (
                                                    <><i className="bi bi-person-plus-fill mr-1"></i> Add Friend</>
                                                )}
                                            </button>
                                        );
                                    })()}
                                </div>
                            )}
                        </div>
                    ))}
                </div>
            )}
        </AppLayout>
    );
}
