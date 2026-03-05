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

const getBadge = (rep: number) => {
    if (rep >= 20000) return { label: 'Elite', style: 'bg-[var(--primary)]/10 text-[var(--primary)]', icon: 'star' };
    if (rep >= 10000) return { label: 'Expert', style: 'bg-[var(--primary)]/10 text-[var(--primary)]', icon: 'verified' };
    if (rep >= 5000) return { label: 'Pro', style: 'bg-[var(--primary)]/10 text-[var(--primary)]', icon: 'verified' };
    if (rep >= 2000) return { label: 'Moderator', style: 'bg-slate-100 dark:bg-slate-800 text-slate-500', icon: 'verified_user' };
    if (rep >= 1000) return { label: 'Editor', style: 'bg-slate-100 dark:bg-slate-800 text-slate-500', icon: 'military_tech' };
    if (rep >= 100) return { label: 'Rising', style: 'bg-slate-100 dark:bg-slate-800 text-slate-500', icon: 'trending_up' };
    return { label: 'Member', style: 'bg-slate-100 dark:bg-slate-800 text-slate-500', icon: 'shield' };
};

const formatRep = (rep: number) => {
    if (rep >= 1000) return `${(rep / 1000).toFixed(1)}k`;
    return rep.toLocaleString();
};

export default function UsersPage() {
    const { user: currentUser } = useAuth();
    const [users, setUsers] = useState<User[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [search, setSearch] = useState('');
    const [sortBy, setSortBy] = useState('reputation');
    const [timeFilter, setTimeFilter] = useState('month');
    const [followingIds, setFollowingIds] = useState<Set<number>>(new Set());
    const [followLoading, setFollowLoading] = useState<number | null>(null);
    const [page, setPage] = useState(1);
    const perPage = 8;

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

    const totalPages = Math.ceil(filteredUsers.length / perPage);
    const paginatedUsers = filteredUsers.slice((page - 1) * perPage, page * perPage);

    const getPageNumbers = () => {
        const pages: (number | '...')[] = [];
        if (totalPages <= 5) {
            for (let i = 1; i <= totalPages; i++) pages.push(i);
        } else {
            pages.push(1);
            if (page > 3) pages.push('...');
            for (let i = Math.max(2, page - 1); i <= Math.min(totalPages - 1, page + 1); i++) pages.push(i);
            if (page < totalPages - 2) pages.push('...');
            pages.push(totalPages);
        }
        return pages;
    };

    const timeFilters = [
        { key: 'week', label: 'Week' },
        { key: 'month', label: 'Month' },
        { key: 'all', label: 'All Time' },
    ];

    return (
        <AppLayout showRightSidebar={false}>
            {/* Title & Filter Section */}
            <div className="flex flex-col md:flex-row justify-between items-end gap-6 mb-8">
                <div className="flex flex-col gap-1">
                    <h3 className="text-3xl font-black text-slate-900 dark:text-white tracking-tight">Top Contributors</h3>
                    <p className="text-slate-500">Discover and connect with the most active members of SocialTechsy.</p>
                </div>
                <div className="bg-slate-100 dark:bg-slate-800 p-1 rounded-xl flex gap-1">
                    {timeFilters.map((tf) => (
                        <button
                            key={tf.key}
                            onClick={() => setTimeFilter(tf.key)}
                            className={`px-4 py-2 rounded-lg text-sm font-semibold transition-all ${timeFilter === tf.key
                                ? 'bg-white dark:bg-slate-700 text-[var(--primary)] shadow-sm'
                                : 'text-slate-500 hover:text-slate-700 dark:hover:text-slate-200'
                                }`}
                        >
                            {tf.label}
                        </button>
                    ))}
                </div>
            </div>

            {/* Search */}
            <div className="mb-8">
                <div className="relative max-w-sm">
                    <span className="material-symbols-outlined absolute left-3 top-1/2 -translate-y-1/2 text-slate-400">search</span>
                    <input
                        type="text"
                        value={search}
                        onChange={(e) => { setSearch(e.target.value); setPage(1); }}
                        placeholder="Search members..."
                        className="w-full pl-10 pr-4 py-2 bg-slate-100 dark:bg-slate-800 border-none rounded-xl focus:ring-2 focus:ring-[var(--primary)] text-sm placeholder:text-slate-500 text-slate-900 dark:text-white"
                    />
                </div>
            </div>

            {/* User Grid */}
            {isLoading ? (
                <div className="flex items-center justify-center py-16">
                    <div className="w-10 h-10 border-4 border-[var(--primary)]/30 border-t-[var(--primary)] rounded-full animate-spin"></div>
                </div>
            ) : paginatedUsers.length === 0 ? (
                <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-2xl p-12 text-center">
                    <span className="material-symbols-outlined text-5xl text-slate-300 mb-4 block">group</span>
                    <h3 className="text-lg font-semibold text-slate-900 dark:text-white mb-2">No users found</h3>
                    <p className="text-slate-500">Try a different search term</p>
                </div>
            ) : (
                <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
                    {paginatedUsers.map((user) => {
                        const badge = getBadge(user.reputationPoints);
                        return (
                            <Link
                                key={user.userId}
                                href={`/users/${user.userId}`}
                                className="bg-white dark:bg-slate-900 p-6 rounded-2xl border border-slate-200 dark:border-slate-800 hover:shadow-xl hover:-translate-y-1 transition-all group"
                            >
                                {/* Top: Avatar + Badge */}
                                <div className="flex items-start justify-between mb-4">
                                    <div className="w-16 h-16 rounded-2xl overflow-hidden shadow-lg group-hover:ring-4 ring-[var(--primary)]/10 transition-all">
                                        {user.profilePicture ? (
                                            <img src={user.profilePicture} alt={user.displayName || user.username} className="w-full h-full object-cover" />
                                        ) : (
                                            <div className="w-full h-full bg-gradient-to-br from-blue-500 via-indigo-500 to-purple-500 flex items-center justify-center text-white text-2xl font-bold">
                                                {(user.displayName || user.username || '?').substring(0, 2).toUpperCase()}
                                            </div>
                                        )}
                                    </div>
                                    <div className="flex flex-col items-end">
                                        <span className={`${badge.style} text-[10px] font-black px-2 py-1 rounded-full uppercase tracking-widest`}>
                                            {badge.label}
                                        </span>
                                        <div className="flex items-center gap-1 mt-2 text-[var(--primary)]">
                                            <span className="material-symbols-outlined text-base">{badge.icon}</span>
                                            <span className="text-xs font-bold">{formatRep(user.reputationPoints)}</span>
                                        </div>
                                    </div>
                                </div>

                                {/* Name & Bio */}
                                <h4 className="text-lg font-bold text-slate-900 dark:text-white mb-1">
                                    {user.displayName || user.username}
                                </h4>
                                <p className="text-slate-500 text-sm mb-4 line-clamp-2">
                                    @{user.username} • Joined {new Date(user.createdDate).toLocaleDateString('en-US', { month: 'short', year: 'numeric' })}
                                </p>

                                {/* Follow Button (stop propagation to prevent link navigation) */}
                                {currentUser && currentUser.userId !== user.userId && (
                                    <button
                                        onClick={(e) => {
                                            e.preventDefault();
                                            e.stopPropagation();
                                            followingIds.has(user.userId) ? handleUnfollow(user.userId) : handleFollow(user.userId);
                                        }}
                                        disabled={followLoading === user.userId}
                                        className={`w-full font-bold py-2 rounded-lg text-xs transition-all mt-2 ${followingIds.has(user.userId)
                                            ? 'bg-slate-100 dark:bg-slate-800 text-slate-900 dark:text-white hover:bg-slate-200'
                                            : 'bg-[var(--primary)] text-white hover:bg-[var(--primary)]/90'
                                            }`}
                                    >
                                        {followLoading === user.userId ? (
                                            <div className="w-4 h-4 border-2 border-current border-t-transparent rounded-full animate-spin mx-auto"></div>
                                        ) : followingIds.has(user.userId) ? 'Following' : 'Follow'}
                                    </button>
                                )}
                            </Link>
                        );
                    })}
                </div>
            )}

            {/* Pagination Footer */}
            {totalPages > 0 && !isLoading && (
                <div className="mt-12 flex items-center justify-between border-t border-slate-200 dark:border-slate-800 pt-6">
                    <p className="text-sm text-slate-500 font-medium">
                        Showing <span className="text-slate-900 dark:text-white">{(page - 1) * perPage + 1} - {Math.min(page * perPage, filteredUsers.length)}</span> of {filteredUsers.length.toLocaleString()} users
                    </p>
                    {totalPages > 1 && (
                        <div className="flex gap-2">
                            <button
                                onClick={() => setPage(p => Math.max(1, p - 1))}
                                disabled={page === 1}
                                className="px-3 py-1 rounded-lg border border-slate-200 dark:border-slate-800 text-slate-500 hover:bg-slate-50 dark:hover:bg-slate-800 transition-colors disabled:opacity-30"
                            >
                                <span className="material-symbols-outlined text-sm align-middle">chevron_left</span>
                            </button>
                            {getPageNumbers().map((p, i) =>
                                p === '...' ? (
                                    <span key={`dots-${i}`} className="px-2 text-slate-400">...</span>
                                ) : (
                                    <button
                                        key={p}
                                        onClick={() => setPage(p as number)}
                                        className={`px-3 py-1 rounded-lg text-sm font-bold transition-colors ${page === p
                                            ? 'bg-[var(--primary)] text-white'
                                            : 'border border-slate-200 dark:border-slate-800 text-slate-500 hover:bg-slate-50 dark:hover:bg-slate-800'
                                            }`}
                                    >
                                        {p}
                                    </button>
                                )
                            )}
                            <button
                                onClick={() => setPage(p => Math.min(totalPages, p + 1))}
                                disabled={page === totalPages}
                                className="px-3 py-1 rounded-lg border border-slate-200 dark:border-slate-800 text-slate-500 hover:bg-slate-50 dark:hover:bg-slate-800 transition-colors disabled:opacity-30"
                            >
                                <span className="material-symbols-outlined text-sm align-middle">chevron_right</span>
                            </button>
                        </div>
                    )}
                </div>
            )}
        </AppLayout>
    );
}