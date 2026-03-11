'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { useQuery, useMutation } from '@tanstack/react-query';
import AppLayout from '@/components/AppLayout';
import { useAuth } from '@/lib/contexts/AuthContext';
import { tagsApi } from '@/lib/api/tags.api';

interface Tag {
    tagId: number;
    tagName: string;
    description?: string;
    questionCount: number;
}

interface TagPreference {
    tagId: number;
    tagName: string;
    isFollowed: boolean;
    isIgnored: boolean;
}

interface TagsResponse {
    items?: Tag[];
}

export default function TagPreferencesPage() {
    const { user, isLoading: authLoading } = useAuth();
    const router = useRouter();
    const [preferences, setPreferences] = useState<TagPreference[]>([]);
    const [search, setSearch] = useState('');
    const [activeTab, setActiveTab] = useState<'all' | 'followed' | 'ignored'>('all');
    const [actionLoading, setActionLoading] = useState<number | null>(null);

    useEffect(() => {
        if (!authLoading && !user) {
            router.push('/auth?mode=login');
        }
    }, [user, authLoading, router]);

    const { data: tagsData, isLoading: tagsLoading } = useQuery({
        queryKey: ['tags'],
        queryFn: () => tagsApi.list(),
        enabled: !!user,
    });

    const tags: Tag[] = (() => {
        const data = tagsData;
        if (Array.isArray(data)) return data;
        if (data && 'items' in data) return data.items || [];
        return [];
    })();

    const { data: prefsData, isLoading: prefsLoading } = useQuery({
        queryKey: ['tagPreferences'],
        queryFn: () => tagsApi.getPreferences(),
        enabled: !!user,
    });

    const isLoading = tagsLoading || prefsLoading;

    useEffect(() => {
        if (prefsData) {
            setPreferences(prefsData || []);
        }
    }, [prefsData]);

    const getPreference = (tagId: number) => {
        return preferences.find(p => p.tagId === tagId);
    };

    const followTagMutation = useMutation({
        mutationFn: (tagId: number) => tagsApi.follow(tagId),
        onSuccess: (_, tagId) => {
            setPreferences(prev => {
                const existing = prev.find(p => p.tagId === tagId);
                if (existing) {
                    return prev.map(p => p.tagId === tagId ? { ...p, isFollowed: true, isIgnored: false } : p);
                }
                const tag = tags.find(t => t.tagId === tagId);
                return [...prev, { tagId, tagName: tag?.tagName || '', isFollowed: true, isIgnored: false }];
            });
        },
        onSettled: () => setActionLoading(null),
    });

    const ignoreTagMutation = useMutation({
        mutationFn: (tagId: number) => tagsApi.ignore(tagId),
        onSuccess: (_, tagId) => {
            setPreferences(prev => {
                const existing = prev.find(p => p.tagId === tagId);
                if (existing) {
                    return prev.map(p => p.tagId === tagId ? { ...p, isFollowed: false, isIgnored: true } : p);
                }
                const tag = tags.find(t => t.tagId === tagId);
                return [...prev, { tagId, tagName: tag?.tagName || '', isFollowed: false, isIgnored: true }];
            });
        },
        onSettled: () => setActionLoading(null),
    });

    const removeTagPrefMutation = useMutation({
        mutationFn: (tagId: number) => tagsApi.removePreference(tagId),
        onSuccess: (_, tagId) => {
            setPreferences(prev => prev.filter(p => p.tagId !== tagId));
        },
        onSettled: () => setActionLoading(null),
    });

    const handleFollow = (tagId: number) => {
        setActionLoading(tagId);
        followTagMutation.mutate(tagId);
    };

    const handleIgnore = (tagId: number) => {
        setActionLoading(tagId);
        ignoreTagMutation.mutate(tagId);
    };

    const handleRemove = (tagId: number) => {
        setActionLoading(tagId);
        removeTagPrefMutation.mutate(tagId);
    };

    const filteredTags = tags.filter(tag =>
        tag.tagName.toLowerCase().includes(search.toLowerCase())
    );

    const displayedTags = filteredTags.filter(tag => {
        const pref = getPreference(tag.tagId);
        if (activeTab === 'followed') return pref?.isFollowed;
        if (activeTab === 'ignored') return pref?.isIgnored;
        return true;
    });

    const followedCount = preferences.filter(p => p.isFollowed).length;
    const ignoredCount = preferences.filter(p => p.isIgnored).length;

    if (authLoading) {
        return (
            <AppLayout>
                <div className="flex items-center justify-center py-16">
                    <div className="w-10 h-10 border-4 border-[var(--primary)]/30 border-t-[var(--primary)] rounded-full animate-spin"></div>
                </div>
            </AppLayout>
        );
    }

    return (
        <AppLayout>
            {/* Header */}
            <div className="mb-6">
                <div className="flex items-center gap-2 mb-2">
                    <Link href="/tags" className="text-[var(--text-muted)] hover:text-[var(--primary)]">
                        <span className="material-symbols-outlined">arrow_back</span>
                    </Link>
                    <h1 className="text-2xl font-bold text-[var(--text-primary)] flex items-center gap-2">
                        <span className="material-symbols-outlined text-[var(--primary)]">settings</span>
                        Tag Preferences
                    </h1>
                </div>
                <p className="text-[var(--text-muted)]">
                    Follow tags to see more questions on topics you're interested in, or ignore tags to hide them.
                </p>
            </div>

            {/* Stats */}
            <div className="grid grid-cols-2 gap-4 mb-6">
                <div className="bg-gradient-to-r from-green-500/10 to-emerald-500/10 border border-green-500/20 rounded-xl p-4">
                    <div className="flex items-center gap-3">
                        <div className="w-10 h-10 bg-green-500/20 rounded-full flex items-center justify-center">
                            <span className="material-symbols-outlined text-green-500">favorite</span>
                        </div>
                        <div>
                            <p className="text-2xl font-bold text-[var(--text-primary)]">{followedCount}</p>
                            <p className="text-sm text-[var(--text-muted)]">Following</p>
                        </div>
                    </div>
                </div>
                <div className="bg-gradient-to-r from-red-500/10 to-rose-500/10 border border-red-500/20 rounded-xl p-4">
                    <div className="flex items-center gap-3">
                        <div className="w-10 h-10 bg-red-500/20 rounded-full flex items-center justify-center">
                            <span className="material-symbols-outlined text-red-500">visibility_off</span>
                        </div>
                        <div>
                            <p className="text-2xl font-bold text-[var(--text-primary)]">{ignoredCount}</p>
                            <p className="text-sm text-[var(--text-muted)]">Ignored</p>
                        </div>
                    </div>
                </div>
            </div>

            {/* Tabs */}
            <div className="flex gap-2 mb-4">
                {(['all', 'followed', 'ignored'] as const).map(tab => (
                    <button
                        key={tab}
                        onClick={() => setActiveTab(tab)}
                        className={`px-4 py-2 rounded-lg font-medium transition ${activeTab === tab
                                ? 'bg-[var(--primary)] text-white'
                                : 'bg-[var(--bg-secondary)] text-[var(--text-muted)] hover:text-[var(--text-primary)]'
                            }`}
                    >
                        {tab === 'all' && 'All Tags'}
                        {tab === 'followed' && `Following (${followedCount})`}
                        {tab === 'ignored' && `Ignored (${ignoredCount})`}
                    </button>
                ))}
            </div>

            {/* Search */}
            <div className="mb-6">
                <div className="relative">
                    <span className="material-symbols-outlined absolute left-4 top-1/2 -translate-y-1/2 text-[var(--text-muted)]">search</span>
                    <input
                        type="text"
                        placeholder="Filter tags..."
                        value={search}
                        onChange={(e) => setSearch(e.target.value)}
                        className="w-full pl-11 pr-4 py-3 bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-xl text-[var(--text-primary)] placeholder-[var(--text-muted)] focus:border-[var(--primary)] focus:ring-2 focus:ring-[var(--primary)]/20 transition"
                    />
                </div>
            </div>

            {/* Tags Grid */}
            {isLoading ? (
                <div className="flex items-center justify-center py-16">
                    <div className="w-10 h-10 border-4 border-[var(--primary)]/30 border-t-[var(--primary)] rounded-full animate-spin"></div>
                </div>
            ) : displayedTags.length === 0 ? (
                <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-2xl p-12 text-center">
                    <span className="material-symbols-outlined text-5xl text-[var(--text-muted)] mb-4">sell</span>
                    <h3 className="text-lg font-semibold text-[var(--text-primary)] mb-2">
                        {activeTab === 'followed' && "You're not following any tags"}
                        {activeTab === 'ignored' && "You haven't ignored any tags"}
                        {activeTab === 'all' && "No tags found"}
                    </h3>
                    <p className="text-[var(--text-muted)]">
                        {activeTab !== 'all' && "Browse all tags to customize your feed"}
                    </p>
                </div>
            ) : (
                <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-4">
                    {displayedTags.map((tag) => {
                        const pref = getPreference(tag.tagId);
                        const isActionLoading = actionLoading === tag.tagId;

                        return (
                            <div
                                key={tag.tagId}
                                className={`bg-[var(--bg-secondary)] border rounded-2xl p-5 transition ${pref?.isFollowed ? 'border-green-500/50' :
                                        pref?.isIgnored ? 'border-red-500/50' :
                                            'border-[var(--border-color)]'
                                    }`}
                            >
                                <div className="flex items-center justify-between mb-3">
                                    <Link
                                        href={`/questions?tag=${tag.tagName}`}
                                        className="px-3 py-1.5 bg-[var(--primary)]/10 text-[var(--primary)] rounded-lg font-medium text-sm hover:bg-[var(--primary)]/20 transition"
                                    >
                                        #{tag.tagName}
                                    </Link>
                                    <span className="text-xs text-[var(--text-muted)]">
                                        {tag.questionCount} questions
                                    </span>
                                </div>

                                <p className="text-sm text-[var(--text-secondary)] mb-4 line-clamp-2">
                                    {tag.description || `Questions about ${tag.tagName}`}
                                </p>

                                {/* Action Buttons */}
                                <div className="flex gap-2">
                                    {pref?.isFollowed ? (
                                        <button
                                            onClick={() => handleRemove(tag.tagId)}
                                            disabled={isActionLoading}
                                            className="flex-1 px-3 py-2 bg-green-500/10 text-green-500 rounded-lg font-medium text-sm hover:bg-green-500/20 transition disabled:opacity-50"
                                        >
                                            {isActionLoading ? (
                                                <span className="material-symbols-outlined animate-spin">hourglass_empty</span>
                                            ) : (
                                                <><span className="material-symbols-outlined mr-1">check_circle</span> Following</>
                                            )}
                                        </button>
                                    ) : (
                                        <button
                                            onClick={() => handleFollow(tag.tagId)}
                                            disabled={isActionLoading}
                                            className="flex-1 px-3 py-2 bg-[var(--bg-tertiary)] text-[var(--text-primary)] rounded-lg font-medium text-sm hover:bg-green-500/10 hover:text-green-500 transition disabled:opacity-50"
                                        >
                                            {isActionLoading ? (
                                                <span className="material-symbols-outlined animate-spin">hourglass_empty</span>
                                            ) : (
                                                <><span className="material-symbols-outlined mr-1">favorite</span> Follow</>
                                            )}
                                        </button>
                                    )}

                                    {pref?.isIgnored ? (
                                        <button
                                            onClick={() => handleRemove(tag.tagId)}
                                            disabled={isActionLoading}
                                            className="px-3 py-2 bg-red-500/10 text-red-500 rounded-lg font-medium text-sm hover:bg-red-500/20 transition disabled:opacity-50"
                                        >
                                            {isActionLoading ? (
                                                <span className="material-symbols-outlined animate-spin">hourglass_empty</span>
                                            ) : (
                                                <span className="material-symbols-outlined">visibility_off</span>
                                            )}
                                        </button>
                                    ) : (
                                        <button
                                            onClick={() => handleIgnore(tag.tagId)}
                                            disabled={isActionLoading}
                                            className="px-3 py-2 bg-[var(--bg-tertiary)] text-[var(--text-muted)] rounded-lg font-medium text-sm hover:bg-red-500/10 hover:text-red-500 transition disabled:opacity-50"
                                            title="Ignore this tag"
                                        >
                                            {isActionLoading ? (
                                                <span className="material-symbols-outlined animate-spin">hourglass_empty</span>
                                            ) : (
                                                <span className="material-symbols-outlined">visibility_off</span>
                                            )}
                                        </button>
                                    )}
                                </div>
                            </div>
                        );
                    })}
                </div>
            )}
        </AppLayout>
    );
}