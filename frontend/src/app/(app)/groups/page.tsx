'use client';

import { useState } from 'react';
import Link from 'next/link';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import AppLayout from '@/components/AppLayout';
import { useAuth } from '@/lib/contexts/AuthContext';
import { groupsApi } from '@/lib/api/groups.api';
import { authorInitial } from '@/lib/utils';

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

const gradients = [
    'from-teal-700 via-teal-600 to-emerald-500',
    'from-slate-800 via-slate-700 to-slate-600',
    'from-cyan-700 via-cyan-600 to-blue-500',
    'from-violet-700 via-purple-600 to-fuchsia-500',
    'from-blue-700 via-indigo-600 to-violet-500',
];

export default function GroupsPage() {
    const { isAuthenticated } = useAuth();
    const queryClient = useQueryClient();
    const [activeTab, setActiveTab] = useState('discover');
    const [search, setSearch] = useState('');
    const [showCreateModal, setShowCreateModal] = useState(false);
    const [newGroup, setNewGroup] = useState({ name: '', description: '', isPrivate: false });

    const { data: groupsData, isLoading: loading } = useQuery({
        queryKey: ['groups'],
        queryFn: () => groupsApi.list(),
    });

    const groups: Group[] = groupsData?.items || [];

    const { data: myGroupsData } = useQuery({
        queryKey: ['groups', 'my'],
        queryFn: () => groupsApi.getMyGroups(),
        enabled: isAuthenticated,
    });

    const myGroups: Group[] = (() => {
        const data = myGroupsData;
        return Array.isArray(data) ? data : (data?.items || []);
    })();

    const createGroupMutation = useMutation({
        mutationFn: (data: { name: string; description: string; isPrivate: boolean }) => groupsApi.create(data),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['groups'] });
            setShowCreateModal(false);
            setNewGroup({ name: '', description: '', isPrivate: false });
        },
    });

    const joinGroupMutation = useMutation({
        mutationFn: (groupId: number) => groupsApi.join(groupId),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['groups'] });
            queryClient.invalidateQueries({ queryKey: ['newsfeed'] });
        },
    });

    const handleCreateGroup = (e: React.FormEvent) => {
        e.preventDefault();
        createGroupMutation.mutate(newGroup);
    };

    const handleJoinGroup = (groupId: number) => {
        joinGroupMutation.mutate(groupId);
    };

    const isMember = (groupId: number) => myGroups.some(g => g.groupId === groupId);

    const formatMembers = (count: number) => {
        if (count >= 1000) return `${(count / 1000).toFixed(1)}k`;
        return count.toLocaleString();
    };

    const filteredGroups = groups.filter(g =>
        g.name.toLowerCase().includes(search.toLowerCase()) ||
        (g.description || '').toLowerCase().includes(search.toLowerCase())
    );

    const trendingTags = ['#Web3', '#OpenSource', '#RemoteWork', '#LLM', '#React19'];

    const tabs = [
        { key: 'discover', label: 'Discover' },
        { key: 'my', label: 'My Groups' },
        { key: 'trending', label: 'Trending' },
        { key: 'invites', label: 'Invites', badge: 0 },
    ];

    return (
        <AppLayout showRightSidebar={false}>
            <div className="grid grid-cols-1 xl:grid-cols-12 gap-8">
                {/* Main Content */}
                <div className="xl:col-span-8">
                    {/* Hero */}
                    <div className="mb-6">
                        <h1 className="text-2xl sm:text-4xl font-black text-slate-900 dark:text-white tracking-tight mb-2">Communities</h1>
                        <p className="text-slate-500 dark:text-slate-400 text-base">
                            Connect with tech enthusiasts, share knowledge, and grow your professional network in specialized tech groups.
                        </p>
                    </div>

                    {/* Create Group Button */}
                    {isAuthenticated && (
                        <button
                            onClick={() => setShowCreateModal(true)}
                            className="mb-6 flex items-center gap-2 px-6 py-3 bg-[var(--primary)] text-white rounded-xl font-bold text-sm hover:bg-[var(--primary)]/90 transition shadow-lg shadow-[var(--primary)]/20"
                        >
                            <span className="material-symbols-outlined text-sm">add</span>
                            Create Group
                        </button>
                    )}

                    {/* Search */}
                    <div className="relative w-full mb-6">
                        <span className="material-symbols-outlined absolute left-4 top-1/2 -translate-y-1/2 text-slate-400">search</span>
                        <input
                            type="text"
                            value={search}
                            onChange={(e) => setSearch(e.target.value)}
                            placeholder="Search for tech groups (e.g. React, AI, Web3)..."
                            className="w-full pl-12 pr-4 py-3 bg-slate-50 dark:bg-slate-800/50 border border-slate-200 dark:border-slate-800 rounded-xl focus:ring-2 focus:ring-[var(--primary)]/20 placeholder:text-slate-400 text-slate-900 dark:text-white"
                        />
                    </div>

                    {/* Tabs */}
                    <div className="flex border-b border-slate-200 dark:border-slate-800 mb-8 overflow-x-auto">
                        {tabs.map((tab) => (
                            <button
                                key={tab.key}
                                onClick={() => setActiveTab(tab.key)}
                                className={`px-4 sm:px-6 py-3 text-sm font-bold transition-colors border-b-2 whitespace-nowrap ${activeTab === tab.key
                                    ? 'text-[var(--primary)] border-[var(--primary)]'
                                    : 'text-slate-500 border-transparent hover:text-[var(--primary)]'
                                    }`}
                            >
                                {tab.label}
                                {tab.badge !== undefined && tab.badge > 0 && (
                                    <span className="ml-1.5 px-1.5 py-0.5 bg-red-500 text-white text-[10px] rounded-full leading-none">{tab.badge}</span>
                                )}
                            </button>
                        ))}
                    </div>

                    {/* Discover Tab */}
                    {activeTab === 'discover' && (
                        <>
                            <div className="flex items-center justify-between mb-6">
                                <h3 className="text-xl font-bold text-slate-900 dark:text-white">Recommended for you</h3>
                                <button className="text-[var(--primary)] text-sm font-semibold hover:underline">View all</button>
                            </div>

                            {loading ? (
                                <div className="flex items-center justify-center py-16">
                                    <div className="w-10 h-10 border-4 border-[var(--primary)]/30 border-t-[var(--primary)] rounded-full animate-spin"></div>
                                </div>
                            ) : filteredGroups.length === 0 ? (
                                <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-2xl p-12 text-center">
                                    <span className="material-symbols-outlined text-5xl text-slate-300 mb-4 block">groups</span>
                                    <h3 className="text-lg font-semibold text-slate-900 dark:text-white mb-2">No groups found</h3>
                                    <p className="text-slate-500">Be the first to create a group!</p>
                                </div>
                            ) : (
                                <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4 mb-10">
                                    {filteredGroups.map((group, index) => (
                                        <div
                                            key={group.groupId}
                                            className="bg-white dark:bg-slate-900 rounded-xl overflow-hidden border border-slate-200 dark:border-slate-800 hover:shadow-lg transition-all group"
                                        >
                                            {/* Cover */}
                                            <div className={`h-28 bg-gradient-to-br ${gradients[index % gradients.length]} relative p-3`}>
                                                <span className={`absolute top-3 right-3 text-[10px] font-bold px-2 py-0.5 rounded-full uppercase ${group.isPrivate
                                                    ? 'bg-black/30 text-white backdrop-blur-sm'
                                                    : 'bg-white/20 text-white backdrop-blur-sm'
                                                    }`}>
                                                    {group.isPrivate && <span className="material-symbols-outlined text-[10px] mr-0.5 align-middle">lock</span>}
                                                    {group.isPrivate ? 'Private' : 'Public'}
                                                </span>
                                                <div className="absolute bottom-3 left-3 text-white font-black text-lg drop-shadow-md">
                                                    {authorInitial(group.name)}
                                                </div>
                                            </div>
                                            {/* Content */}
                                            <div className="p-4">
                                                <Link href={`/groups/${group.groupId}`}>
                                                    <h4 className="font-bold text-slate-900 dark:text-white group-hover:text-[var(--primary)] transition-colors mb-1">
                                                        {group.name}
                                                    </h4>
                                                </Link>
                                                <p className="text-slate-500 text-xs line-clamp-2 mb-3">
                                                    {group.description || 'No description available'}
                                                </p>
                                                <div className="flex items-center justify-between">
                                                    <div className="flex items-center gap-1 text-slate-400 text-xs">
                                                        <span className="material-symbols-outlined text-sm">person</span>
                                                        <span className="font-medium">{formatMembers(group.memberCount)} members</span>
                                                    </div>
                                                    {isAuthenticated && !isMember(group.groupId) ? (
                                                        <button
                                                            onClick={() => handleJoinGroup(group.groupId)}
                                                            className="px-3 py-1 text-[var(--primary)] border border-[var(--primary)] rounded-lg text-xs font-bold hover:bg-[var(--primary)] hover:text-white transition-all"
                                                        >
                                                            {group.isPrivate ? 'Apply' : 'Join'}
                                                        </button>
                                                    ) : isMember(group.groupId) ? (
                                                        <Link
                                                            href={`/groups/${group.groupId}`}
                                                            className="px-3 py-1 text-[var(--primary)] border border-[var(--primary)] rounded-lg text-xs font-bold hover:bg-[var(--primary)] hover:text-white transition-all"
                                                        >
                                                            Open
                                                        </Link>
                                                    ) : null}
                                                </div>
                                            </div>
                                        </div>
                                    ))}
                                </div>
                            )}

                            {/* Your Groups Section */}
                            {myGroups.length > 0 && (
                                <>
                                    <div className="flex items-center justify-between mb-4">
                                        <h3 className="text-xl font-bold text-slate-900 dark:text-white">Your Groups</h3>
                                        <span className="text-slate-400 text-sm">{myGroups.length} active groups</span>
                                    </div>
                                    <div className="bg-white dark:bg-slate-900 rounded-xl border border-slate-200 dark:border-slate-800 divide-y divide-slate-100 dark:divide-slate-800">
                                        {myGroups.map((group) => (
                                            <Link
                                                key={group.groupId}
                                                href={`/groups/${group.groupId}`}
                                                className="flex items-center justify-between p-4 hover:bg-slate-50 dark:hover:bg-slate-800/50 transition-colors group"
                                            >
                                                <div className="flex items-center gap-4">
                                                    <div className="w-12 h-12 rounded-xl bg-gradient-to-br from-blue-500 to-indigo-500 flex items-center justify-center text-white font-bold text-sm">
                                                        {authorInitial(group.name)}
                                                    </div>
                                                    <div>
                                                        <h5 className="font-bold text-slate-900 dark:text-white text-sm group-hover:text-[var(--primary)] transition-colors">
                                                            {group.name}
                                                        </h5>
                                                        <p className="text-xs text-slate-500">
                                                            {formatMembers(group.memberCount)} members
                                                        </p>
                                                    </div>
                                                </div>
                                                <span className="material-symbols-outlined text-slate-400 group-hover:text-[var(--primary)] transition-colors">chevron_right</span>
                                            </Link>
                                        ))}
                                    </div>
                                </>
                            )}
                        </>
                    )}

                    {/* My Groups Tab */}
                    {activeTab === 'my' && (
                        myGroups.length === 0 ? (
                            <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-2xl p-12 text-center">
                                <span className="material-symbols-outlined text-5xl text-slate-300 mb-4 block">groups</span>
                                <h3 className="text-lg font-semibold text-slate-900 dark:text-white mb-2">No groups joined yet</h3>
                                <p className="text-slate-500">Discover and join groups to see them here!</p>
                            </div>
                        ) : (
                            <div className="bg-white dark:bg-slate-900 rounded-xl border border-slate-200 dark:border-slate-800 divide-y divide-slate-100 dark:divide-slate-800">
                                {myGroups.map((group) => (
                                    <Link
                                        key={group.groupId}
                                        href={`/groups/${group.groupId}`}
                                        className="flex items-center justify-between p-4 hover:bg-slate-50 dark:hover:bg-slate-800/50 transition-colors group"
                                    >
                                        <div className="flex items-center gap-4">
                                            <div className="w-12 h-12 rounded-xl bg-gradient-to-br from-blue-500 to-indigo-500 flex items-center justify-center text-white font-bold text-sm">
                                                {authorInitial(group.name)}
                                            </div>
                                            <div>
                                                <h5 className="font-bold text-slate-900 dark:text-white text-sm">{group.name}</h5>
                                                <p className="text-xs text-slate-500">{formatMembers(group.memberCount)} members</p>
                                            </div>
                                        </div>
                                        <span className="material-symbols-outlined text-slate-400">chevron_right</span>
                                    </Link>
                                ))}
                            </div>
                        )
                    )}

                    {/* Trending / Invites Tabs */}
                    {(activeTab === 'trending' || activeTab === 'invites') && (
                        <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-2xl p-12 text-center">
                            <span className="material-symbols-outlined text-5xl text-slate-300 mb-4 block">
                                {activeTab === 'trending' ? 'trending_up' : 'mail'}
                            </span>
                            <h3 className="text-lg font-semibold text-slate-900 dark:text-white mb-2">
                                {activeTab === 'trending' ? 'Trending Groups' : 'Group Invites'}
                            </h3>
                            <p className="text-slate-500">Coming soon!</p>
                        </div>
                    )}
                </div>

                {/* Right Sidebar */}
                <div className="hidden xl:flex xl:col-span-4 flex-col gap-6">
                    {/* Trending Topics */}
                    <div className="bg-white dark:bg-slate-900 rounded-2xl p-5 border border-slate-200 dark:border-slate-800">
                        <h3 className="font-bold text-slate-900 dark:text-white mb-4">Trending Topics</h3>
                        <div className="flex flex-wrap gap-2">
                            {trendingTags.map((tag) => (
                                <span
                                    key={tag}
                                    className="px-3 py-1.5 bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300 rounded-full text-xs font-semibold hover:bg-[var(--primary)]/10 hover:text-[var(--primary)] cursor-pointer transition"
                                >
                                    {tag}
                                </span>
                            ))}
                        </div>
                    </div>

                    {/* Suggested People */}
                    <div className="bg-white dark:bg-slate-900 rounded-2xl p-5 border border-slate-200 dark:border-slate-800">
                        <h3 className="font-bold text-slate-900 dark:text-white mb-4">Suggested People</h3>
                        <div className="space-y-4">
                            {[
                                { name: 'Sarah Jenkins', role: 'CTO at TechFlow' },
                                { name: 'Markus Webb', role: 'Backend Architect' },
                                { name: 'Linh Nguyen', role: 'ML Engineer' },
                            ].map((person) => (
                                <div key={person.name} className="flex items-center justify-between">
                                    <div className="flex items-center gap-3">
                                        <div className="h-10 w-10 rounded-full bg-gradient-to-br from-slate-600 to-slate-800 flex items-center justify-center text-white text-xs font-bold">
                                            {authorInitial(person.name)}
                                        </div>
                                        <div>
                                            <p className="text-sm font-bold text-slate-900 dark:text-white">{person.name}</p>
                                            <p className="text-xs text-slate-500">{person.role}</p>
                                        </div>
                                    </div>
                                    <button className="text-[var(--primary)] text-xs font-bold hover:underline">Follow</button>
                                </div>
                            ))}
                        </div>
                    </div>
                </div>
            </div>

            {/* Create Group Modal */}
            {showCreateModal && (
                <div className="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50 p-4">
                    <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-2xl p-6 w-full max-w-md shadow-2xl">
                        <div className="flex items-center justify-between mb-6">
                            <h2 className="text-xl font-bold text-slate-900 dark:text-white">Create New Group</h2>
                            <button onClick={() => setShowCreateModal(false)} className="p-2 hover:bg-slate-100 dark:hover:bg-slate-800 rounded-lg transition">
                                <span className="material-symbols-outlined text-slate-400">close</span>
                            </button>
                        </div>
                        <form onSubmit={handleCreateGroup}>
                            <div className="mb-4">
                                <label className="block text-sm font-medium text-slate-600 dark:text-slate-400 mb-2">Group Name</label>
                                <input
                                    type="text"
                                    value={newGroup.name}
                                    onChange={(e) => setNewGroup({ ...newGroup, name: e.target.value })}
                                    className="w-full px-4 py-3 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-slate-900 dark:text-white focus:border-[var(--primary)] focus:ring-2 focus:ring-[var(--primary)]/20 transition"
                                    placeholder="e.g. React Developers"
                                    required
                                />
                            </div>
                            <div className="mb-4">
                                <label className="block text-sm font-medium text-slate-600 dark:text-slate-400 mb-2">Description</label>
                                <textarea
                                    value={newGroup.description}
                                    onChange={(e) => setNewGroup({ ...newGroup, description: e.target.value })}
                                    className="w-full px-4 py-3 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-slate-900 dark:text-white focus:border-[var(--primary)] focus:ring-2 focus:ring-[var(--primary)]/20 transition resize-none"
                                    rows={3}
                                    placeholder="What is this group about?"
                                />
                            </div>
                            <div className="mb-6">
                                <label className="flex items-center gap-3 cursor-pointer">
                                    <input
                                        type="checkbox"
                                        checked={newGroup.isPrivate}
                                        onChange={(e) => setNewGroup({ ...newGroup, isPrivate: e.target.checked })}
                                        className="w-5 h-5 rounded border-slate-300 dark:border-slate-600 text-[var(--primary)] focus:ring-[var(--primary)]"
                                    />
                                    <span className="text-slate-600 dark:text-slate-400">Private Group (Invite only)</span>
                                </label>
                            </div>
                            <div className="flex gap-3">
                                <button
                                    type="button"
                                    onClick={() => setShowCreateModal(false)}
                                    className="flex-1 py-3 bg-slate-100 dark:bg-slate-800 text-slate-900 dark:text-white rounded-xl font-bold hover:bg-slate-200 dark:hover:bg-slate-700 transition"
                                >
                                    Cancel
                                </button>
                                <button
                                    type="submit"
                                    className="flex-1 py-3 bg-[var(--primary)] text-white rounded-xl font-bold hover:bg-[var(--primary)]/90 transition"
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
