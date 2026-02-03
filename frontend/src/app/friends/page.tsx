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
    const [activeTab, setActiveTab] = useState<'friends' | 'pending'>('friends');

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
                // Handle both array response and paginated response
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
                // Handle both array and paginated response, map to expected format
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

    const gradients = [
        'from-blue-500 to-cyan-500',
        'from-purple-500 to-pink-500',
        'from-green-500 to-emerald-500',
        'from-orange-500 to-red-500',
        'from-indigo-500 to-purple-500',
    ];

    if (!isAuthenticated) {
        return (
            <AppLayout>
                <div className="flex flex-col items-center justify-center py-20 text-center">
                    <div className="w-20 h-20 bg-[var(--bg-tertiary)] rounded-full flex items-center justify-center mb-6">
                        <i className="bi bi-lock text-4xl text-[var(--text-muted)]"></i>
                    </div>
                    <h2 className="text-2xl font-bold text-[var(--text-primary)] mb-3">Login Required</h2>
                    <p className="text-[var(--text-muted)] mb-6 max-w-md">
                        Please login to view and manage your friends.
                    </p>
                    <Link href="/login" className="px-6 py-3 bg-[var(--primary)] text-white rounded-xl font-medium hover:bg-[var(--primary-dark)] transition">
                        Login Now
                    </Link>
                </div>
            </AppLayout>
        );
    }

    return (
        <AppLayout>
            {/* Header */}
            <div className="mb-6">
                <h1 className="text-2xl font-bold text-[var(--text-primary)] flex items-center gap-2">
                    <i className="bi bi-people-fill text-[var(--primary)]"></i>
                    Friends
                </h1>
                <p className="text-[var(--text-muted)]">Manage your connections and friend requests</p>
            </div>

            {/* Tabs */}
            <div className="flex bg-[var(--bg-secondary)] rounded-xl p-1 border border-[var(--border-color)] mb-6 w-fit">
                <button
                    onClick={() => setActiveTab('friends')}
                    className={`flex items-center gap-2 px-5 py-2.5 rounded-lg font-medium text-sm transition ${activeTab === 'friends' ? 'bg-[var(--primary)] text-white' : 'text-[var(--text-muted)] hover:text-[var(--text-primary)]'}`}
                >
                    <i className="bi bi-people"></i>
                    Friends ({friends.length})
                </button>
                <button
                    onClick={() => setActiveTab('pending')}
                    className={`flex items-center gap-2 px-5 py-2.5 rounded-lg font-medium text-sm transition ${activeTab === 'pending' ? 'bg-[var(--primary)] text-white' : 'text-[var(--text-muted)] hover:text-[var(--text-primary)]'}`}
                >
                    <i className="bi bi-person-plus"></i>
                    Pending ({pendingRequests.length})
                    {pendingRequests.length > 0 && (
                        <span className="w-2 h-2 bg-red-500 rounded-full"></span>
                    )}
                </button>
            </div>

            {loading ? (
                <div className="flex items-center justify-center py-16">
                    <div className="w-10 h-10 border-4 border-[var(--primary)]/30 border-t-[var(--primary)] rounded-full animate-spin"></div>
                </div>
            ) : activeTab === 'friends' ? (
                /* Friends List */
                friends.length === 0 ? (
                    <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-2xl p-12 text-center">
                        <i className="bi bi-person-hearts text-5xl text-[var(--text-muted)] mb-4"></i>
                        <h3 className="text-lg font-semibold text-[var(--text-primary)] mb-2">No friends yet</h3>
                        <p className="text-[var(--text-muted)] mb-4">Start connecting with other developers!</p>
                        <Link href="/users" className="inline-flex items-center gap-2 px-5 py-2.5 bg-[var(--primary)] text-white rounded-xl font-medium hover:bg-[var(--primary-dark)] transition">
                            <i className="bi bi-search"></i>
                            Browse Users
                        </Link>
                    </div>
                ) : (
                    <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-4">
                        {friends.map((friend, index) => (
                            <div key={friend.userId} className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-2xl p-5 hover:border-[var(--primary)]/50 transition">
                                <div className="flex items-center gap-4 mb-4">
                                    <div className={`w-14 h-14 rounded-xl bg-gradient-to-br ${gradients[index % gradients.length]} flex items-center justify-center text-white text-xl font-bold relative`}>
                                        {friend.username.charAt(0).toUpperCase()}
                                        <span className="absolute -bottom-1 -right-1 w-4 h-4 bg-green-500 border-2 border-[var(--bg-secondary)] rounded-full"></span>
                                    </div>
                                    <div className="flex-1 min-w-0">
                                        <h3 className="font-semibold text-[var(--text-primary)] truncate">
                                            {friend.displayName || friend.username}
                                        </h3>
                                        <p className="text-sm text-[var(--text-muted)]">@{friend.username}</p>
                                    </div>
                                </div>
                                <div className="flex gap-2">
                                    <Link
                                        href={`/chat?user=${friend.userId}`}
                                        className="flex-1 py-2.5 text-center bg-[var(--primary)] text-white rounded-xl font-medium hover:bg-[var(--primary-dark)] transition text-sm"
                                    >
                                        <i className="bi bi-chat-dots mr-2"></i>
                                        Chat
                                    </Link>
                                    <Link
                                        href={`/users/${friend.userId}`}
                                        className="flex-1 py-2.5 text-center bg-[var(--bg-tertiary)] text-[var(--text-primary)] rounded-xl font-medium border border-[var(--border-color)] hover:border-[var(--primary)] transition text-sm"
                                    >
                                        <i className="bi bi-person mr-2"></i>
                                        Profile
                                    </Link>
                                </div>
                            </div>
                        ))}
                    </div>
                )
            ) : (
                /* Pending Requests */
                pendingRequests.length === 0 ? (
                    <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-2xl p-12 text-center">
                        <i className="bi bi-inbox text-5xl text-[var(--text-muted)] mb-4"></i>
                        <h3 className="text-lg font-semibold text-[var(--text-primary)] mb-2">No pending requests</h3>
                        <p className="text-[var(--text-muted)]">Check back later for new friend requests!</p>
                    </div>
                ) : (
                    <div className="space-y-3">
                        {pendingRequests.map((request, index) => (
                            <div key={request.requestId} className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-2xl p-5 hover:border-[var(--primary)]/30 transition">
                                <div className="flex items-center gap-4">
                                    <div className={`w-14 h-14 rounded-xl bg-gradient-to-br ${gradients[index % gradients.length]} flex items-center justify-center text-white text-xl font-bold shrink-0`}>
                                        {request.sender.username.charAt(0).toUpperCase()}
                                    </div>
                                    <div className="flex-1 min-w-0">
                                        <h3 className="font-semibold text-[var(--text-primary)]">
                                            {request.sender.displayName || request.sender.username}
                                        </h3>
                                        <p className="text-sm text-[var(--text-muted)]">@{request.sender.username} wants to be your friend</p>
                                    </div>
                                    <div className="flex gap-2 shrink-0">
                                        <button
                                            onClick={() => handleAccept(request.requestId)}
                                            className="px-4 py-2 bg-green-500 text-white rounded-xl font-medium hover:bg-green-600 transition text-sm"
                                        >
                                            <i className="bi bi-check-lg mr-1"></i>
                                            Accept
                                        </button>
                                        <button
                                            onClick={() => handleDecline(request.requestId)}
                                            className="px-4 py-2 bg-[var(--bg-tertiary)] text-[var(--text-muted)] rounded-xl font-medium hover:bg-red-500/20 hover:text-red-400 transition text-sm"
                                        >
                                            <i className="bi bi-x-lg"></i>
                                        </button>
                                    </div>
                                </div>
                            </div>
                        ))}
                    </div>
                )
            )}
        </AppLayout>
    );
}
