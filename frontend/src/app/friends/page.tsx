'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import AppLayout from '@/components/AppLayout';
import { useAuth } from '@/lib/contexts/AuthContext';

interface Friend {
    userId: number;
    username: string;
    displayName: string;
    profilePicture: string | null;
    friendshipDate: string;
}

interface FriendRequest {
    requestId: number;
    sender: {
        userId: number;
        username: string;
        displayName: string;
        profilePicture: string | null;
    };
    createdAt: string;
}

export default function FriendsPage() {
    const { isAuthenticated } = useAuth();
    const [friends, setFriends] = useState<Friend[]>([]);
    const [pendingRequests, setPendingRequests] = useState<FriendRequest[]>([]);
    const [loading, setLoading] = useState(true);
    const [activeTab, setActiveTab] = useState('connected');
    const [search, setSearch] = useState('');

    const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5122';
    const getToken = () => localStorage.getItem('accessToken');

    useEffect(() => {
        if (isAuthenticated) {
            fetchFriends();
            fetchPendingRequests();
        } else {
            setLoading(false);
        }
    }, [isAuthenticated]);

    const fetchFriends = async () => {
        try {
            const response = await fetch(`${API_BASE_URL}/api/Friendship/friends`, {
                headers: { Authorization: `Bearer ${getToken()}` }
            });
            if (response.ok) {
                const data = await response.json();
                const friendList = Array.isArray(data) ? data : (data.items || []);
                setFriends(friendList);
            }
        } catch (error) {
            console.error('Error fetching friends:', error);
        } finally {
            setLoading(false);
        }
    };

    const fetchPendingRequests = async () => {
        try {
            const response = await fetch(`${API_BASE_URL}/api/Friendship/pending`, {
                headers: { Authorization: `Bearer ${getToken()}` }
            });
            if (response.ok) {
                const data = await response.json();
                const requests = Array.isArray(data) ? data : (data.items || []);
                setPendingRequests(requests.map((r: any) => ({
                    requestId: r.friendshipId,
                    sender: r.requester,
                    createdAt: r.createdAt
                })));
            }
        } catch (error) {
            console.error('Error fetching pending requests:', error);
        }
    };

    const handleAccept = async (requestId: number) => {
        try {
            const response = await fetch(`${API_BASE_URL}/api/Friendship/accept/${requestId}`, {
                method: 'PUT',
                headers: { Authorization: `Bearer ${getToken()}` }
            });
            if (response.ok) {
                fetchFriends();
                fetchPendingRequests();
            }
        } catch (error) {
            console.error('Error accepting request:', error);
        }
    };

    const handleDecline = async (requestId: number) => {
        try {
            const response = await fetch(`${API_BASE_URL}/api/Friendship/reject/${requestId}`, {
                method: 'PUT',
                headers: { Authorization: `Bearer ${getToken()}` }
            });
            if (response.ok) {
                fetchPendingRequests();
            }
        } catch (error) {
            console.error('Error declining request:', error);
        }
    };

    const formatDate = (dateString: string) => {
        const date = new Date(dateString);
        const now = new Date();
        const diffMs = now.getTime() - date.getTime();
        const diffMins = Math.floor(diffMs / 60000);
        const diffHours = Math.floor(diffMs / 3600000);
        const diffDays = Math.floor(diffMs / 86400000);
        if (diffMins < 60) return `Active ${diffMins}m ago`;
        if (diffHours < 24) return `Active ${diffHours}h ago`;
        if (diffDays < 7) return `Active ${diffDays}d ago`;
        return `Active ${date.toLocaleDateString()}`;
    };

    const filteredFriends = friends.filter(f =>
        f.username.toLowerCase().includes(search.toLowerCase()) ||
        (f.displayName || '').toLowerCase().includes(search.toLowerCase())
    );

    const tabs = [
        { key: 'connected', label: 'Connected', count: friends.length },
        { key: 'pending', label: 'Pending', count: pendingRequests.length },
        { key: 'suggestions', label: 'Suggestions', count: null },
    ];

    if (!isAuthenticated) {
        return (
            <AppLayout showRightSidebar={false}>
                <div className="flex flex-col items-center justify-center py-20 text-center">
                    <div className="w-20 h-20 bg-slate-100 dark:bg-slate-800 rounded-full flex items-center justify-center mb-6">
                        <span className="material-symbols-outlined text-4xl text-slate-400">lock</span>
                    </div>
                    <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-3">Login Required</h2>
                    <p className="text-slate-500 mb-6 max-w-md">
                        Please login to view and manage your connections.
                    </p>
                    <Link href="/login" className="px-6 py-3 bg-[var(--primary)] text-white rounded-xl font-bold hover:bg-[var(--primary)]/90 transition">
                        Login Now
                    </Link>
                </div>
            </AppLayout>
        );
    }

    return (
        <AppLayout showRightSidebar={false}>
            <div className="grid grid-cols-1 xl:grid-cols-12 gap-8">
                {/* Main Content */}
                <div className="xl:col-span-8">
                    {/* Header */}
                    <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-8">
                        <div>
                            <h1 className="text-3xl font-black text-slate-900 dark:text-white tracking-tight">Connected Developers</h1>
                            <p className="text-slate-500 text-sm mt-1">Manage your professional network and connections.</p>
                        </div>
                        <Link
                            href="/users"
                            className="flex items-center justify-center gap-2 px-4 py-2 bg-slate-100 dark:bg-slate-800 text-slate-900 dark:text-white font-bold rounded-xl text-sm transition hover:bg-slate-200 dark:hover:bg-slate-700"
                        >
                            <span className="material-symbols-outlined text-lg">person_add</span>
                            Find People
                        </Link>
                    </div>

                    {/* Search */}
                    <div className="relative w-full mb-6">
                        <span className="material-symbols-outlined absolute left-4 top-1/2 -translate-y-1/2 text-slate-400">search</span>
                        <input
                            type="text"
                            value={search}
                            onChange={(e) => setSearch(e.target.value)}
                            placeholder="Search connections by name or role..."
                            className="w-full pl-12 pr-4 py-3 bg-slate-50 dark:bg-slate-800/50 border-none rounded-xl focus:ring-2 focus:ring-[var(--primary)]/20 placeholder:text-slate-400 text-slate-900 dark:text-white"
                        />
                    </div>

                    {/* Tabs */}
                    <div className="flex border-b border-slate-200 dark:border-slate-800 mb-6">
                        {tabs.map((tab) => (
                            <button
                                key={tab.key}
                                onClick={() => setActiveTab(tab.key)}
                                className={`px-6 py-4 border-b-2 font-bold text-sm transition-colors ${activeTab === tab.key
                                    ? 'border-[var(--primary)] text-[var(--primary)]'
                                    : 'border-transparent text-slate-500 hover:text-slate-700 dark:hover:text-slate-300'
                                    }`}
                            >
                                {tab.label}
                                {tab.count !== null && (
                                    <span className="ml-1 opacity-60 text-xs font-normal">({tab.count})</span>
                                )}
                            </button>
                        ))}
                    </div>

                    {/* Connected Tab */}
                    {activeTab === 'connected' && (
                        loading ? (
                            <div className="flex items-center justify-center py-16">
                                <div className="w-10 h-10 border-4 border-[var(--primary)]/30 border-t-[var(--primary)] rounded-full animate-spin"></div>
                            </div>
                        ) : filteredFriends.length === 0 ? (
                            <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-2xl p-12 text-center">
                                <span className="material-symbols-outlined text-5xl text-slate-300 mb-4 block">group</span>
                                <h3 className="text-lg font-semibold text-slate-900 dark:text-white mb-2">
                                    {search ? 'No matching connections' : 'No connections yet'}
                                </h3>
                                <p className="text-slate-500 mb-4">
                                    {search ? 'Try a different search term' : 'Start connecting with other developers!'}
                                </p>
                                {!search && (
                                    <Link href="/users" className="inline-flex items-center gap-2 px-5 py-2.5 bg-[var(--primary)] text-white rounded-xl font-bold text-sm">
                                        <span className="material-symbols-outlined text-sm">search</span> Browse Users
                                    </Link>
                                )}
                            </div>
                        ) : (
                            <div className="space-y-4">
                                {filteredFriends.map((friend) => (
                                    <div
                                        key={friend.userId}
                                        className="group flex items-center justify-between p-4 rounded-2xl border border-slate-100 dark:border-slate-800 hover:shadow-md hover:border-[var(--primary)]/20 transition-all bg-white dark:bg-slate-900"
                                    >
                                        <div className="flex items-center gap-4">
                                            <div className="relative">
                                                <div className="h-14 w-14 rounded-full overflow-hidden">
                                                    {friend.profilePicture ? (
                                                        <img src={friend.profilePicture} alt="" className="h-full w-full object-cover" />
                                                    ) : (
                                                        <div className="h-full w-full bg-gradient-to-br from-blue-500 to-purple-500 flex items-center justify-center text-white text-xl font-bold">
                                                            {(friend.displayName || friend.username || '?').charAt(0).toUpperCase()}
                                                        </div>
                                                    )}
                                                </div>
                                                <span className="absolute bottom-0 right-0 h-4 w-4 rounded-full border-2 border-white dark:border-slate-900 bg-green-500"></span>
                                            </div>
                                            <div>
                                                <Link href={`/users/${friend.userId}`}>
                                                    <h3 className="font-bold text-slate-900 dark:text-white group-hover:text-[var(--primary)] transition-colors">
                                                        {friend.displayName || friend.username}
                                                    </h3>
                                                </Link>
                                                <p className="text-sm text-slate-500">@{friend.username}</p>
                                                <p className="text-xs text-slate-400 mt-1 flex items-center gap-1">
                                                    <span className="material-symbols-outlined text-xs">history</span>
                                                    {formatDate(friend.friendshipDate)}
                                                </p>
                                            </div>
                                        </div>
                                        <Link
                                            href={`/chat?user=${friend.userId}`}
                                            className="flex items-center gap-2 px-5 py-2.5 bg-[var(--primary)]/10 text-[var(--primary)] font-bold rounded-xl text-sm hover:bg-[var(--primary)] hover:text-white transition-all"
                                        >
                                            <span className="material-symbols-outlined text-lg">chat_bubble</span>
                                            Message
                                        </Link>
                                    </div>
                                ))}
                            </div>
                        )
                    )}

                    {/* Pending Tab */}
                    {activeTab === 'pending' && (
                        pendingRequests.length === 0 ? (
                            <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-2xl p-12 text-center">
                                <span className="material-symbols-outlined text-5xl text-slate-300 mb-4 block">inbox</span>
                                <h3 className="text-lg font-semibold text-slate-900 dark:text-white mb-2">No pending requests</h3>
                                <p className="text-slate-500">Check back later for new connection requests!</p>
                            </div>
                        ) : (
                            <div className="space-y-4">
                                {pendingRequests.map((request) => (
                                    <div
                                        key={request.requestId}
                                        className="group flex items-center justify-between p-4 rounded-2xl border border-slate-100 dark:border-slate-800 hover:shadow-md hover:border-[var(--primary)]/20 transition-all bg-white dark:bg-slate-900"
                                    >
                                        <div className="flex items-center gap-4">
                                            <div className="relative">
                                                <div className="h-14 w-14 rounded-full overflow-hidden">
                                                    {request.sender.profilePicture ? (
                                                        <img src={request.sender.profilePicture} alt="" className="h-full w-full object-cover" />
                                                    ) : (
                                                        <div className="h-full w-full bg-gradient-to-br from-indigo-500 to-purple-500 flex items-center justify-center text-white text-xl font-bold">
                                                            {(request.sender.displayName || request.sender.username || '?').charAt(0).toUpperCase()}
                                                        </div>
                                                    )}
                                                </div>
                                            </div>
                                            <div>
                                                <h3 className="font-bold text-slate-900 dark:text-white group-hover:text-[var(--primary)] transition-colors">
                                                    {request.sender.displayName || request.sender.username}
                                                </h3>
                                                <p className="text-sm text-slate-500">@{request.sender.username} wants to connect</p>
                                            </div>
                                        </div>
                                        <div className="flex gap-2">
                                            <button
                                                onClick={() => handleAccept(request.requestId)}
                                                className="flex items-center gap-1 px-4 py-2.5 bg-[var(--primary)] text-white font-bold rounded-xl text-sm hover:bg-[var(--primary)]/90 transition"
                                            >
                                                <span className="material-symbols-outlined text-sm">check</span> Accept
                                            </button>
                                            <button
                                                onClick={() => handleDecline(request.requestId)}
                                                className="flex items-center gap-1 px-4 py-2.5 bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-300 font-bold rounded-xl text-sm hover:bg-red-50 hover:text-red-500 dark:hover:bg-red-900/20 transition"
                                            >
                                                <span className="material-symbols-outlined text-sm">close</span> Decline
                                            </button>
                                        </div>
                                    </div>
                                ))}
                            </div>
                        )
                    )}

                    {/* Suggestions Tab */}
                    {activeTab === 'suggestions' && (
                        <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-2xl p-12 text-center">
                            <span className="material-symbols-outlined text-5xl text-slate-300 mb-4 block">person_search</span>
                            <h3 className="text-lg font-semibold text-slate-900 dark:text-white mb-2">Connection Suggestions</h3>
                            <p className="text-slate-500 mb-4">Browse our community to find developers to connect with.</p>
                            <Link href="/users" className="inline-flex items-center gap-2 px-5 py-2.5 bg-[var(--primary)] text-white rounded-xl font-bold text-sm">
                                <span className="material-symbols-outlined text-sm">explore</span> Explore Users
                            </Link>
                        </div>
                    )}
                </div>

                {/* Right Sidebar */}
                <div className="hidden xl:flex xl:col-span-4 flex-col gap-6">
                    {/* People You May Know */}
                    <div className="bg-white dark:bg-slate-900 rounded-2xl p-5 border border-slate-200 dark:border-slate-800">
                        <div className="flex items-center justify-between mb-4">
                            <h3 className="font-bold text-slate-900 dark:text-white">People You May Know</h3>
                            <Link href="/users" className="text-[var(--primary)] text-xs font-bold hover:underline">View All</Link>
                        </div>
                        <div className="space-y-4">
                            {[
                                { name: 'David Miller', role: 'Go Developer' },
                                { name: 'Sophia Zhang', role: 'Data Scientist' },
                                { name: 'Liam Patel', role: 'Product Designer' },
                            ].map((person) => (
                                <div key={person.name} className="flex items-center justify-between">
                                    <div className="flex items-center gap-3">
                                        <div className="h-10 w-10 rounded-full bg-gradient-to-br from-blue-500 to-indigo-500 flex items-center justify-center text-white text-sm font-bold">
                                            {person.name.charAt(0)}
                                        </div>
                                        <div className="min-w-0">
                                            <p className="text-sm font-bold text-slate-900 dark:text-white truncate">{person.name}</p>
                                            <p className="text-xs text-slate-500 truncate">{person.role}</p>
                                        </div>
                                    </div>
                                    <button className="text-[var(--primary)] hover:bg-[var(--primary)]/10 p-1.5 rounded-lg transition">
                                        <span className="material-symbols-outlined text-xl">person_add</span>
                                    </button>
                                </div>
                            ))}
                        </div>
                    </div>

                    {/* Network Growth */}
                    <div className="bg-white dark:bg-slate-900 rounded-2xl p-5 border border-slate-200 dark:border-slate-800">
                        <h3 className="font-bold text-slate-900 dark:text-white mb-4">Network Growth</h3>
                        <div className="flex items-end gap-2 mb-4">
                            <span className="text-3xl font-black text-slate-900 dark:text-white">+{friends.length}</span>
                            <span className="text-xs text-green-500 font-bold pb-1 flex items-center">
                                <span className="material-symbols-outlined text-xs">trending_up</span>
                                This month
                            </span>
                        </div>
                        {/* Simple Bar Graph */}
                        <div className="flex items-end gap-1.5 h-16 w-full">
                            <div className="flex-1 bg-slate-100 dark:bg-slate-800 h-1/2 rounded-t-sm"></div>
                            <div className="flex-1 bg-slate-100 dark:bg-slate-800 h-2/3 rounded-t-sm"></div>
                            <div className="flex-1 bg-[var(--primary)]/20 h-3/4 rounded-t-sm"></div>
                            <div className="flex-1 bg-[var(--primary)]/40 h-1/2 rounded-t-sm"></div>
                            <div className="flex-1 bg-[var(--primary)]/60 h-2/3 rounded-t-sm"></div>
                            <div className="flex-1 bg-[var(--primary)] h-full rounded-t-sm"></div>
                            <div className="flex-1 bg-[var(--primary)]/80 h-3/4 rounded-t-sm"></div>
                        </div>
                        <div className="flex justify-between mt-2">
                            <span className="text-[10px] text-slate-400 uppercase font-bold">Week 1</span>
                            <span className="text-[10px] text-slate-400 uppercase font-bold">Week 4</span>
                        </div>
                        <div className="mt-4 pt-4 border-t border-slate-100 dark:border-slate-800 flex justify-between items-center">
                            <div className="text-xs text-slate-500">Total Connections</div>
                            <div className="text-xs font-bold text-slate-900 dark:text-white">{friends.length}</div>
                        </div>
                    </div>

                    {/* Event Card */}
                    <div className="bg-[var(--primary)] rounded-2xl p-5 text-white relative overflow-hidden">
                        <div className="relative z-10">
                            <h4 className="font-bold text-lg mb-1">Tech Mixer 2026</h4>
                            <p className="text-white/80 text-sm mb-4">Join 200+ local developers this Friday.</p>
                            <button className="w-full py-2 bg-white text-[var(--primary)] font-bold rounded-xl text-sm transition hover:bg-slate-100">RSVP Now</button>
                        </div>
                        <div className="absolute -right-4 -bottom-4 opacity-10">
                            <span className="material-symbols-outlined text-[120px]">groups</span>
                        </div>
                    </div>
                </div>
            </div>
        </AppLayout>
    );
}
