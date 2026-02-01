'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import MainLayout from '@/components/MainLayout';
import { useAuth } from '@/lib/contexts/AuthContext';

interface Friend {
    friendshipId: number;
    userId: number;
    username: string;
    displayName: string;
    profilePicture: string | null;
    status: string;
    createdAt: string;
}

interface FriendRequest {
    friendshipId: number;
    requesterId: number;
    requesterName: string;
    createdAt: string;
}

export default function FriendsPage() {
    const { isAuthenticated } = useAuth();
    const [friends, setFriends] = useState<Friend[]>([]);
    const [pendingRequests, setPendingRequests] = useState<FriendRequest[]>([]);
    const [loading, setLoading] = useState(true);
    const [activeTab, setActiveTab] = useState<'friends' | 'pending' | 'followers'>('friends');

    const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5164';
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
                setFriends(data || []);
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
                setPendingRequests(data || []);
            }
        } catch (error) {
            console.error('Error fetching pending requests:', error);
        }
    };

    const handleAcceptRequest = async (friendshipId: number) => {
        try {
            const response = await fetch(`${API_BASE_URL}/api/Friendship/${friendshipId}/accept`, {
                method: 'POST',
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

    const handleRejectRequest = async (friendshipId: number) => {
        try {
            const response = await fetch(`${API_BASE_URL}/api/Friendship/${friendshipId}/reject`, {
                method: 'POST',
                headers: { Authorization: `Bearer ${getToken()}` }
            });
            if (response.ok) {
                fetchPendingRequests();
            }
        } catch (error) {
            console.error('Error rejecting request:', error);
        }
    };

    if (!isAuthenticated) {
        return (
            <MainLayout>
                <div className="text-center py-20">
                    <i className="bi bi-lock text-6xl text-gray-400 mb-4"></i>
                    <h2 className="text-2xl font-bold text-gray-600 dark:text-gray-300 mb-4">
                        Login Required
                    </h2>
                    <p className="text-gray-500 mb-6">Please login to view your friends</p>
                    <Link href="/login" className="px-6 py-3 bg-orange-500 text-white rounded-lg hover:bg-orange-600 transition">
                        Login Now
                    </Link>
                </div>
            </MainLayout>
        );
    }

    return (
        <MainLayout>
            <div className="max-w-3xl mx-auto">
                {/* Header */}
                <div className="mb-6">
                    <h1 className="text-3xl font-bold text-gray-900 dark:text-white mb-2">
                        <i className="bi bi-person-hearts text-orange-500 mr-2"></i>
                        Friends
                    </h1>
                    <p className="text-gray-600 dark:text-gray-400">
                        Manage your connections and friend requests
                    </p>
                </div>

                {/* Tabs */}
                <div className="flex gap-2 mb-6">
                    <button
                        onClick={() => setActiveTab('friends')}
                        className={`px-4 py-2 rounded-lg font-medium transition flex items-center gap-2 ${activeTab === 'friends'
                            ? 'bg-orange-500 text-white'
                            : 'bg-white dark:bg-slate-800 text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-slate-700'
                            }`}
                    >
                        <i className="bi bi-people"></i>
                        Friends ({friends.length})
                    </button>
                    <button
                        onClick={() => setActiveTab('pending')}
                        className={`px-4 py-2 rounded-lg font-medium transition flex items-center gap-2 ${activeTab === 'pending'
                            ? 'bg-orange-500 text-white'
                            : 'bg-white dark:bg-slate-800 text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-slate-700'
                            }`}
                    >
                        <i className="bi bi-person-plus"></i>
                        Pending ({pendingRequests.length})
                        {pendingRequests.length > 0 && (
                            <span className="ml-1 px-2 py-0.5 bg-red-500 text-white text-xs rounded-full">
                                {pendingRequests.length}
                            </span>
                        )}
                    </button>
                </div>

                {/* Content */}
                {loading ? (
                    <div className="text-center py-10">
                        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-orange-500 mx-auto"></div>
                    </div>
                ) : activeTab === 'friends' ? (
                    friends.length === 0 ? (
                        <div className="text-center py-16 bg-white dark:bg-slate-800 rounded-xl border border-gray-200 dark:border-slate-700">
                            <i className="bi bi-person-x text-6xl text-gray-400 mb-4"></i>
                            <h3 className="text-xl font-semibold text-gray-600 dark:text-gray-300 mb-2">
                                No friends yet
                            </h3>
                            <p className="text-gray-500 mb-4">
                                Start connecting with other developers!
                            </p>
                            <Link href="/users" className="px-4 py-2 bg-orange-500 text-white rounded-lg hover:bg-orange-600 transition">
                                Browse Users
                            </Link>
                        </div>
                    ) : (
                        <div className="space-y-3">
                            {friends.map((friend) => (
                                <div key={friend.friendshipId} className="bg-white dark:bg-slate-800 rounded-xl p-4 border border-gray-200 dark:border-slate-700 flex items-center justify-between">
                                    <div className="flex items-center gap-3">
                                        <div className="w-12 h-12 bg-gradient-to-br from-blue-500 to-purple-500 rounded-full flex items-center justify-center text-white font-bold text-lg">
                                            {friend.username?.charAt(0).toUpperCase() || 'U'}
                                        </div>
                                        <div>
                                            <Link href={`/users/${friend.userId}`} className="font-semibold text-gray-900 dark:text-white hover:text-orange-500">
                                                {friend.displayName || friend.username}
                                            </Link>
                                            <p className="text-sm text-gray-500">@{friend.username}</p>
                                        </div>
                                    </div>
                                    <div className="flex gap-2">
                                        <Link href={`/chat?user=${friend.userId}`} className="px-3 py-1.5 border border-gray-300 dark:border-slate-600 text-gray-600 dark:text-gray-300 rounded-lg hover:bg-gray-50 dark:hover:bg-slate-700 transition">
                                            <i className="bi bi-chat-dots"></i>
                                        </Link>
                                        <Link href={`/users/${friend.userId}`} className="px-3 py-1.5 border border-orange-500 text-orange-500 rounded-lg hover:bg-orange-50 dark:hover:bg-slate-700 transition">
                                            View Profile
                                        </Link>
                                    </div>
                                </div>
                            ))}
                        </div>
                    )
                ) : (
                    pendingRequests.length === 0 ? (
                        <div className="text-center py-16 bg-white dark:bg-slate-800 rounded-xl border border-gray-200 dark:border-slate-700">
                            <i className="bi bi-inbox text-6xl text-gray-400 mb-4"></i>
                            <h3 className="text-xl font-semibold text-gray-600 dark:text-gray-300 mb-2">
                                No pending requests
                            </h3>
                            <p className="text-gray-500">
                                You're all caught up!
                            </p>
                        </div>
                    ) : (
                        <div className="space-y-3">
                            {pendingRequests.map((request) => (
                                <div key={request.friendshipId} className="bg-white dark:bg-slate-800 rounded-xl p-4 border border-gray-200 dark:border-slate-700 flex items-center justify-between">
                                    <div className="flex items-center gap-3">
                                        <div className="w-12 h-12 bg-gradient-to-br from-green-500 to-teal-500 rounded-full flex items-center justify-center text-white font-bold text-lg">
                                            {request.requesterName?.charAt(0).toUpperCase() || 'U'}
                                        </div>
                                        <div>
                                            <Link href={`/users/${request.requesterId}`} className="font-semibold text-gray-900 dark:text-white hover:text-orange-500">
                                                {request.requesterName}
                                            </Link>
                                            <p className="text-sm text-gray-500">
                                                Sent {new Date(request.createdAt).toLocaleDateString()}
                                            </p>
                                        </div>
                                    </div>
                                    <div className="flex gap-2">
                                        <button
                                            onClick={() => handleAcceptRequest(request.friendshipId)}
                                            className="px-4 py-1.5 bg-green-500 text-white rounded-lg hover:bg-green-600 transition"
                                        >
                                            Accept
                                        </button>
                                        <button
                                            onClick={() => handleRejectRequest(request.friendshipId)}
                                            className="px-4 py-1.5 bg-gray-200 dark:bg-slate-600 text-gray-700 dark:text-gray-200 rounded-lg hover:bg-gray-300 dark:hover:bg-slate-500 transition"
                                        >
                                            Decline
                                        </button>
                                    </div>
                                </div>
                            ))}
                        </div>
                    )
                )}
            </div>
        </MainLayout>
    );
}
