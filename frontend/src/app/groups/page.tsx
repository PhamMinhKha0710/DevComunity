'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import AppLayout from '@/components/AppLayout';
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

const categoryFilters = ['All', 'Frontend', 'Backend', 'DevOps', 'AI/ML', 'Mobile'];
const gradients = [
    'from-purple-600 to-pink-500',
    'from-blue-600 to-cyan-500',
    'from-green-600 to-emerald-500',
    'from-orange-500 to-red-500',
    'from-indigo-600 to-purple-500',
];

export default function GroupsPage() {
    const { isAuthenticated } = useAuth();
    const [groups, setGroups] = useState<Group[]>([]);
    const [myGroups, setMyGroups] = useState<Group[]>([]);
    const [loading, setLoading] = useState(true);
    const [activeTab, setActiveTab] = useState<'all' | 'my'>('all');
    const [activeCategory, setActiveCategory] = useState('All');
    const [showCreateModal, setShowCreateModal] = useState(false);
    const [newGroup, setNewGroup] = useState({ name: '', description: '', isPrivate: false });

    const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5122';
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
    const isMember = (groupId: number) => myGroups.some(g => g.groupId === groupId);

    return (
        <AppLayout showRightSidebar={false}>
            {/* Hero */}
            <div className="text-center mb-8">
                <h1 className="text-3xl font-bold text-[var(--text-primary)] mb-2">
                    Discover Developer Groups
                </h1>
                <p className="text-[var(--text-muted)]">Find your community, collaborate, and grow.</p>
            </div>

            {/* Search & Filters */}
            <div className="flex flex-col sm:flex-row gap-4 mb-6">
                <div className="flex-1 relative">
                    <i className="bi bi-search absolute left-4 top-1/2 -translate-y-1/2 text-[var(--text-muted)]"></i>
                    <input
                        type="text"
                        placeholder="Search for groups, topics, or technologies..."
                        className="w-full pl-11 pr-4 py-3 bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-xl text-[var(--text-primary)] placeholder-[var(--text-muted)] focus:border-[var(--primary)] transition"
                    />
                </div>
                {isAuthenticated && (
                    <button
                        onClick={() => setShowCreateModal(true)}
                        className="px-5 py-3 bg-[var(--primary)] text-white rounded-xl font-medium hover:bg-[var(--primary-dark)] transition flex items-center gap-2 shrink-0"
                    >
                        <i className="bi bi-plus-circle"></i>
                        Create Group
                    </button>
                )}
            </div>

            {/* Tabs & Category Filters */}
            <div className="flex flex-wrap items-center gap-3 mb-6">
                {isAuthenticated && (
                    <div className="flex bg-[var(--bg-secondary)] rounded-xl p-1 border border-[var(--border-color)]">
                        <button
                            onClick={() => setActiveTab('all')}
                            className={`px-4 py-2 rounded-lg font-medium text-sm transition ${activeTab === 'all' ? 'bg-[var(--primary)] text-white' : 'text-[var(--text-muted)] hover:text-[var(--text-primary)]'}`}
                        >
                            All Groups
                        </button>
                        <button
                            onClick={() => setActiveTab('my')}
                            className={`px-4 py-2 rounded-lg font-medium text-sm transition ${activeTab === 'my' ? 'bg-[var(--primary)] text-white' : 'text-[var(--text-muted)] hover:text-[var(--text-primary)]'}`}
                        >
                            My Groups
                        </button>
                    </div>
                )}
                <div className="flex gap-2 flex-wrap">
                    {categoryFilters.map((cat) => (
                        <button
                            key={cat}
                            onClick={() => setActiveCategory(cat)}
                            className={`px-3 py-1.5 rounded-lg text-sm font-medium transition ${activeCategory === cat ? 'bg-[var(--primary)]/20 text-[var(--primary)] border border-[var(--primary)]/50' : 'bg-[var(--bg-tertiary)] text-[var(--text-muted)] border border-transparent hover:border-[var(--border-color)]'}`}
                        >
                            {cat}
                        </button>
                    ))}
                </div>
            </div>

            {/* Groups Grid */}
            {loading ? (
                <div className="flex items-center justify-center py-16">
                    <div className="w-10 h-10 border-4 border-[var(--primary)]/30 border-t-[var(--primary)] rounded-full animate-spin"></div>
                </div>
            ) : displayGroups.length === 0 ? (
                <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-2xl p-12 text-center">
                    <i className="bi bi-collection text-5xl text-[var(--text-muted)] mb-4"></i>
                    <h3 className="text-lg font-semibold text-[var(--text-primary)] mb-2">
                        {activeTab === 'my' ? 'No groups joined yet' : 'No groups available'}
                    </h3>
                    <p className="text-[var(--text-muted)]">
                        {activeTab === 'my' ? 'Join or create your first group!' : 'Be the first to create a group!'}
                    </p>
                </div>
            ) : (
                <div className="grid md:grid-cols-2 lg:grid-cols-3 gap-4">
                    {displayGroups.map((group, index) => (
                        <div key={group.groupId} className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-2xl overflow-hidden hover:border-[var(--primary)]/50 transition group">
                            {/* Gradient Header */}
                            <div className={`h-24 bg-gradient-to-br ${gradients[index % gradients.length]} p-4`}>
                                <div className="flex items-start justify-between">
                                    <div className="w-12 h-12 bg-white/20 backdrop-blur rounded-xl flex items-center justify-center text-white text-xl font-bold">
                                        {group.name.charAt(0).toUpperCase()}
                                    </div>
                                    {group.isPrivate && (
                                        <span className="px-2 py-1 bg-black/20 backdrop-blur rounded-lg text-white text-xs flex items-center gap-1">
                                            <i className="bi bi-lock"></i> Private
                                        </span>
                                    )}
                                </div>
                            </div>
                            {/* Content */}
                            <div className="p-4">
                                <Link href={`/groups/${group.groupId}`} className="font-bold text-[var(--text-primary)] hover:text-[var(--primary)] text-lg">
                                    {group.name}
                                </Link>
                                <div className="flex items-center gap-2 text-sm text-[var(--text-muted)] mt-1 mb-3">
                                    <i className="bi bi-people"></i>
                                    <span>{group.memberCount} members</span>
                                </div>
                                <p className="text-sm text-[var(--text-secondary)] line-clamp-2 mb-4">
                                    {group.description || 'No description available'}
                                </p>
                                {isAuthenticated && !isMember(group.groupId) ? (
                                    <button
                                        onClick={() => handleJoinGroup(group.groupId)}
                                        className="w-full py-2.5 bg-[var(--primary)] text-white rounded-xl font-medium hover:bg-[var(--primary-dark)] transition"
                                    >
                                        Join Group
                                    </button>
                                ) : isMember(group.groupId) ? (
                                    <Link
                                        href={`/groups/${group.groupId}`}
                                        className="block w-full py-2.5 text-center bg-[var(--bg-tertiary)] text-[var(--text-primary)] rounded-xl font-medium border border-[var(--border-color)] hover:border-[var(--primary)] transition"
                                    >
                                        View Group
                                    </Link>
                                ) : null}
                            </div>
                        </div>
                    ))}
                </div>
            )}

            {/* Create Group Modal */}
            {showCreateModal && (
                <div className="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50 p-4">
                    <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-2xl p-6 w-full max-w-md shadow-2xl">
                        <div className="flex items-center justify-between mb-6">
                            <h2 className="text-xl font-bold text-[var(--text-primary)]">Create New Group</h2>
                            <button onClick={() => setShowCreateModal(false)} className="p-2 hover:bg-[var(--bg-hover)] rounded-lg transition">
                                <i className="bi bi-x-lg text-[var(--text-muted)]"></i>
                            </button>
                        </div>
                        <form onSubmit={handleCreateGroup}>
                            <div className="mb-4">
                                <label className="block text-sm font-medium text-[var(--text-secondary)] mb-2">Group Name</label>
                                <input
                                    type="text"
                                    value={newGroup.name}
                                    onChange={(e) => setNewGroup({ ...newGroup, name: e.target.value })}
                                    className="w-full px-4 py-3 bg-[var(--bg-tertiary)] border border-[var(--border-color)] rounded-xl text-[var(--text-primary)] focus:border-[var(--primary)] transition"
                                    required
                                />
                            </div>
                            <div className="mb-4">
                                <label className="block text-sm font-medium text-[var(--text-secondary)] mb-2">Description</label>
                                <textarea
                                    value={newGroup.description}
                                    onChange={(e) => setNewGroup({ ...newGroup, description: e.target.value })}
                                    className="w-full px-4 py-3 bg-[var(--bg-tertiary)] border border-[var(--border-color)] rounded-xl text-[var(--text-primary)] focus:border-[var(--primary)] transition resize-none"
                                    rows={3}
                                />
                            </div>
                            <div className="mb-6">
                                <label className="flex items-center gap-3 cursor-pointer">
                                    <input
                                        type="checkbox"
                                        checked={newGroup.isPrivate}
                                        onChange={(e) => setNewGroup({ ...newGroup, isPrivate: e.target.checked })}
                                        className="w-5 h-5 rounded border-[var(--border-color)] text-[var(--primary)]"
                                    />
                                    <span className="text-[var(--text-secondary)]">Private Group (Invite only)</span>
                                </label>
                            </div>
                            <div className="flex gap-3">
                                <button
                                    type="button"
                                    onClick={() => setShowCreateModal(false)}
                                    className="flex-1 py-3 bg-[var(--bg-tertiary)] text-[var(--text-primary)] rounded-xl font-medium hover:bg-[var(--bg-hover)] transition"
                                >
                                    Cancel
                                </button>
                                <button
                                    type="submit"
                                    className="flex-1 py-3 bg-[var(--primary)] text-white rounded-xl font-medium hover:bg-[var(--primary-dark)] transition"
                                >
                                    Create Group
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            )}
        </AppLayout>
    );
}
