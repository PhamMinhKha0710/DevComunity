'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import MainLayout from '@/components/MainLayout';
import { useAuth } from '@/lib/contexts/AuthContext';

interface Group {
    groupId: number;
    name: string;
    description: string;
    isPrivate: boolean;
    memberCount: number;
    createdAt: string;
    creator: {
        userId: number;
        username: string;
    };
}

export default function GroupsPage() {
    const { isAuthenticated } = useAuth();
    const [groups, setGroups] = useState<Group[]>([]);
    const [myGroups, setMyGroups] = useState<Group[]>([]);
    const [loading, setLoading] = useState(true);
    const [activeTab, setActiveTab] = useState<'all' | 'my'>('all');
    const [showCreateModal, setShowCreateModal] = useState(false);
    const [newGroup, setNewGroup] = useState({ name: '', description: '', isPrivate: false });

    const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5164';
    const getToken = () => localStorage.getItem('accessToken');

    useEffect(() => {
        fetchGroups();
        if (isAuthenticated) {
            fetchMyGroups();
        }
    }, [isAuthenticated]);

    const fetchGroups = async () => {
        setLoading(true);
        try {
            const response = await fetch(`${API_BASE_URL}/api/Groups`);
            if (response.ok) {
                const data = await response.json();
                setGroups(data.items || []);
            } else {
                setGroups([]);
            }
        } catch (error) {
            console.error('Error fetching groups:', error);
            setGroups([]);
        } finally {
            setLoading(false);
        }
    };

    const fetchMyGroups = async () => {
        try {
            const response = await fetch(`${API_BASE_URL}/api/Groups/my`, {
                headers: { Authorization: `Bearer ${getToken()}` }
            });
            if (response.ok) {
                const data = await response.json();
                setMyGroups(data.items || []);
            }
        } catch (error) {
            console.error('Error fetching my groups:', error);
        }
    };

    const handleCreateGroup = async (e: React.FormEvent) => {
        e.preventDefault();
        try {
            const response = await fetch(`${API_BASE_URL}/api/Groups`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    Authorization: `Bearer ${getToken()}`
                },
                body: JSON.stringify(newGroup)
            });
            if (response.ok) {
                const createdGroup = await response.json();
                setGroups([createdGroup, ...groups]);
                setMyGroups([createdGroup, ...myGroups]);
                setShowCreateModal(false);
                setNewGroup({ name: '', description: '', isPrivate: false });
            }
        } catch (error) {
            console.error('Error creating group:', error);
        }
    };

    const handleJoinGroup = async (groupId: number) => {
        try {
            const response = await fetch(`${API_BASE_URL}/api/Groups/${groupId}/join`, {
                method: 'POST',
                headers: { Authorization: `Bearer ${getToken()}` }
            });
            if (response.ok) {
                fetchGroups();
                fetchMyGroups();
            }
        } catch (error) {
            console.error('Error joining group:', error);
        }
    };

    const displayGroups = activeTab === 'my' ? myGroups : groups;

    return (
        <MainLayout>
            <div className="max-w-4xl mx-auto">
                {/* Header */}
                <div className="flex justify-between items-center mb-6">
                    <div>
                        <h1 className="text-3xl font-bold text-gray-900 dark:text-white mb-2">
                            <i className="bi bi-people-fill text-orange-500 mr-2"></i>
                            Groups
                        </h1>
                        <p className="text-gray-600 dark:text-gray-400">
                            Join groups to connect with like-minded developers
                        </p>
                    </div>
                    {isAuthenticated && (
                        <button
                            onClick={() => setShowCreateModal(true)}
                            className="px-4 py-2 bg-orange-500 text-white rounded-lg hover:bg-orange-600 transition flex items-center gap-2"
                        >
                            <i className="bi bi-plus-circle"></i>
                            Create Group
                        </button>
                    )}
                </div>

                {/* Tabs */}
                {isAuthenticated && (
                    <div className="flex gap-2 mb-6">
                        <button
                            onClick={() => setActiveTab('all')}
                            className={`px-4 py-2 rounded-lg font-medium transition ${activeTab === 'all'
                                ? 'bg-orange-500 text-white'
                                : 'bg-white dark:bg-slate-800 text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-slate-700'
                                }`}
                        >
                            All Groups
                        </button>
                        <button
                            onClick={() => setActiveTab('my')}
                            className={`px-4 py-2 rounded-lg font-medium transition ${activeTab === 'my'
                                ? 'bg-orange-500 text-white'
                                : 'bg-white dark:bg-slate-800 text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-slate-700'
                                }`}
                        >
                            My Groups
                        </button>
                    </div>
                )}

                {/* Groups Grid */}
                {loading ? (
                    <div className="text-center py-10">
                        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-orange-500 mx-auto"></div>
                    </div>
                ) : displayGroups.length === 0 ? (
                    <div className="text-center py-16 bg-white dark:bg-slate-800 rounded-xl border border-gray-200 dark:border-slate-700">
                        <i className="bi bi-people text-6xl text-gray-400 mb-4"></i>
                        <h3 className="text-xl font-semibold text-gray-600 dark:text-gray-300 mb-2">
                            {activeTab === 'my' ? 'No groups yet' : 'No groups available'}
                        </h3>
                        <p className="text-gray-500">
                            {activeTab === 'my' ? 'Join or create your first group!' : 'Be the first to create a group!'}
                        </p>
                    </div>
                ) : (
                    <div className="grid md:grid-cols-2 gap-4">
                        {displayGroups.map((group) => (
                            <div key={group.groupId} className="bg-white dark:bg-slate-800 rounded-xl shadow-sm p-5 border border-gray-200 dark:border-slate-700 hover:shadow-md transition">
                                <div className="flex items-start justify-between mb-3">
                                    <div className="flex items-center gap-3">
                                        <div className="w-12 h-12 bg-gradient-to-br from-purple-500 to-pink-500 rounded-xl flex items-center justify-center text-white text-xl font-bold">
                                            {group.name.charAt(0).toUpperCase()}
                                        </div>
                                        <div>
                                            <Link href={`/groups/${group.groupId}`} className="font-bold text-lg text-gray-900 dark:text-white hover:text-orange-500">
                                                {group.name}
                                            </Link>
                                            <div className="flex items-center gap-2 text-sm text-gray-500">
                                                <i className="bi bi-people"></i>
                                                <span>{group.memberCount} members</span>
                                                {group.isPrivate && (
                                                    <span className="px-2 py-0.5 bg-gray-100 dark:bg-slate-700 rounded text-xs">
                                                        <i className="bi bi-lock"></i> Private
                                                    </span>
                                                )}
                                            </div>
                                        </div>
                                    </div>
                                </div>
                                <p className="text-gray-600 dark:text-gray-400 text-sm mb-4 line-clamp-2">
                                    {group.description || 'No description'}
                                </p>
                                {isAuthenticated && !myGroups.some(g => g.groupId === group.groupId) && (
                                    <button
                                        onClick={() => handleJoinGroup(group.groupId)}
                                        className="w-full py-2 border border-orange-500 text-orange-500 rounded-lg hover:bg-orange-50 dark:hover:bg-slate-700 transition"
                                    >
                                        Join Group
                                    </button>
                                )}
                                {myGroups.some(g => g.groupId === group.groupId) && (
                                    <Link
                                        href={`/groups/${group.groupId}`}
                                        className="block w-full py-2 text-center bg-gray-100 dark:bg-slate-700 text-gray-600 dark:text-gray-300 rounded-lg hover:bg-gray-200 dark:hover:bg-slate-600 transition"
                                    >
                                        View Group
                                    </Link>
                                )}
                            </div>
                        ))}
                    </div>
                )}

                {/* Create Group Modal */}
                {showCreateModal && (
                    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
                        <div className="bg-white dark:bg-slate-800 rounded-xl p-6 w-full max-w-md mx-4 shadow-xl">
                            <h2 className="text-xl font-bold text-gray-900 dark:text-white mb-4">Create New Group</h2>
                            <form onSubmit={handleCreateGroup}>
                                <div className="mb-4">
                                    <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                                        Group Name
                                    </label>
                                    <input
                                        type="text"
                                        value={newGroup.name}
                                        onChange={(e) => setNewGroup({ ...newGroup, name: e.target.value })}
                                        className="w-full px-3 py-2 border border-gray-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-900 text-gray-900 dark:text-white"
                                        required
                                    />
                                </div>
                                <div className="mb-4">
                                    <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                                        Description
                                    </label>
                                    <textarea
                                        value={newGroup.description}
                                        onChange={(e) => setNewGroup({ ...newGroup, description: e.target.value })}
                                        className="w-full px-3 py-2 border border-gray-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-900 text-gray-900 dark:text-white"
                                        rows={3}
                                    />
                                </div>
                                <div className="mb-4">
                                    <label className="flex items-center gap-2">
                                        <input
                                            type="checkbox"
                                            checked={newGroup.isPrivate}
                                            onChange={(e) => setNewGroup({ ...newGroup, isPrivate: e.target.checked })}
                                            className="rounded"
                                        />
                                        <span className="text-sm text-gray-700 dark:text-gray-300">Private Group</span>
                                    </label>
                                </div>
                                <div className="flex gap-3">
                                    <button
                                        type="button"
                                        onClick={() => setShowCreateModal(false)}
                                        className="flex-1 py-2 border border-gray-300 dark:border-slate-600 text-gray-700 dark:text-gray-300 rounded-lg hover:bg-gray-50 dark:hover:bg-slate-700 transition"
                                    >
                                        Cancel
                                    </button>
                                    <button
                                        type="submit"
                                        className="flex-1 py-2 bg-orange-500 text-white rounded-lg hover:bg-orange-600 transition"
                                    >
                                        Create
                                    </button>
                                </div>
                            </form>
                        </div>
                    </div>
                )}
            </div>
        </MainLayout>
    );
}
