'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useAuth } from '@/lib/contexts/AuthContext';
import type { Tag } from '@/types';
import apiClient from '@/lib/api/client';
import AppLayout from '@/components/AppLayout';

export default function TagPreferencesPage() {
    const { user, isLoading: authLoading } = useAuth();
    const router = useRouter();
    const [allTags, setAllTags] = useState<Tag[]>([]);
    const [watchedTags, setWatchedTags] = useState<string[]>([]);
    const [ignoredTags, setIgnoredTags] = useState<string[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [searchTerm, setSearchTerm] = useState('');
    const [activeTab, setActiveTab] = useState<'watched' | 'ignored'>('watched');

    useEffect(() => {
        if (!authLoading && !user) {
            router.push('/login');
        } else if (user) {
            fetchData();
        }
    }, [user, authLoading, router]);

    const fetchData = async () => {
        try {
            const [tagsRes, prefsRes] = await Promise.all([
                apiClient.get<{ items: Tag[] }>('/tags?pageSize=100'),
                apiClient.get<{ watched: string[]; ignored: string[] }>('/users/tag-preferences')
            ]);
            setAllTags(tagsRes.data.items || []);
            setWatchedTags(prefsRes.data.watched || []);
            setIgnoredTags(prefsRes.data.ignored || []);
        } catch (error) {
            console.error('Failed to fetch tag preferences:', error);
            setAllTags([
                { tagId: 1, tagName: 'javascript', usageCount: 1500 },
                { tagId: 2, tagName: 'react', usageCount: 1200 },
                { tagId: 3, tagName: 'typescript', usageCount: 800 },
                { tagId: 4, tagName: 'node.js', usageCount: 700 },
                { tagId: 5, tagName: 'python', usageCount: 600 },
                { tagId: 6, tagName: 'csharp', usageCount: 500 },
                { tagId: 7, tagName: 'html', usageCount: 450 },
                { tagId: 8, tagName: 'css', usageCount: 400 },
            ]);
        } finally {
            setIsLoading(false);
        }
    };

    const toggleTag = (tagName: string, type: 'watched' | 'ignored') => {
        if (type === 'watched') {
            if (watchedTags.includes(tagName)) {
                setWatchedTags(watchedTags.filter(t => t !== tagName));
            } else {
                setWatchedTags([...watchedTags, tagName]);
                setIgnoredTags(ignoredTags.filter(t => t !== tagName));
            }
        } else {
            if (ignoredTags.includes(tagName)) {
                setIgnoredTags(ignoredTags.filter(t => t !== tagName));
            } else {
                setIgnoredTags([...ignoredTags, tagName]);
                setWatchedTags(watchedTags.filter(t => t !== tagName));
            }
        }
    };

    const savePreferences = async () => {
        try {
            await apiClient.put('/users/tag-preferences', { watched: watchedTags, ignored: ignoredTags });
        } catch (error) {
            console.error('Failed to save preferences:', error);
        }
    };

    const filteredTags = allTags.filter(tag =>
        tag.tagName.toLowerCase().includes(searchTerm.toLowerCase())
    );

    if (authLoading || isLoading) {
        return (
            <AppLayout>
                <div className="flex items-center justify-center py-20">
                    <div className="w-10 h-10 border-3 border-[rgba(19,127,236,0.2)] border-t-[#137fec] rounded-full animate-spin" />
                </div>
            </AppLayout>
        );
    }

    if (!user) return null;

    const tabs = [
        { id: 'watched' as const, label: `Watched Tags (${watchedTags.length})`, icon: 'visibility', color: 'text-green-500' },
        { id: 'ignored' as const, label: `Ignored Tags (${ignoredTags.length})`, icon: 'visibility_off', color: 'text-red-500' },
    ];

    return (
        <AppLayout>
            {/* Header */}
            <div className="mb-6">
                <h1 className="text-xl font-black tracking-tight text-[#0f172a] dark:text-white flex items-center gap-2">
                    <span className="material-symbols-outlined text-[#137fec]">sell</span>Tag Preferences
                </h1>
                <p className="text-[#64748b] text-sm mt-1">Customize your feed by watching or ignoring tags.</p>
            </div>

            <div className="grid lg:grid-cols-3 gap-6">
                <div className="lg:col-span-2 space-y-4">
                    {/* Tabs */}
                    <div className="flex gap-1 bg-[#f1f5f9] dark:bg-[var(--bg-tertiary)] rounded-xl p-1 w-fit">
                        {tabs.map(tab => (
                            <button
                                key={tab.id}
                                onClick={() => setActiveTab(tab.id)}
                                className={`flex items-center gap-1.5 px-4 py-2 rounded-lg text-sm font-bold transition-colors ${activeTab === tab.id ? 'bg-white dark:bg-[var(--bg-secondary)] text-[#0f172a] dark:text-white shadow-sm' : 'text-[#64748b] hover:text-[#0f172a] dark:hover:text-white'}`}
                            >
                                <span className={`material-symbols-outlined text-base ${tab.color}`}>{tab.icon}</span>
                                {tab.label}
                            </button>
                        ))}
                    </div>

                    {/* Tab Content */}
                    <div className="bg-white dark:bg-[var(--bg-secondary)] border border-[#e2e8f0] dark:border-[var(--border-color)] rounded-2xl p-5">
                        {activeTab === 'watched' ? (
                            <>
                                <div className="p-3 bg-green-50 dark:bg-green-500/10 border border-green-200 dark:border-green-500/30 rounded-xl text-sm mb-4 flex items-start gap-2">
                                    <span className="material-symbols-outlined text-green-500 text-base mt-0.5">info</span>
                                    <div>
                                        <p className="font-bold text-green-700 dark:text-green-400">Watched Tags</p>
                                        <p className="text-green-600 dark:text-green-300 text-xs">Questions with these tags will be highlighted in your feed.</p>
                                    </div>
                                </div>
                                {watchedTags.length > 0 ? (
                                    <div className="flex flex-wrap gap-2 mb-4">
                                        {watchedTags.map(tag => (
                                            <span key={tag} className="flex items-center gap-1.5 px-3 py-1.5 rounded-full bg-green-50 dark:bg-green-500/10 text-green-600 dark:text-green-400 text-xs font-bold">
                                                <span className="material-symbols-outlined text-sm">visibility</span>
                                                {tag}
                                                <button onClick={() => toggleTag(tag, 'watched')} className="ml-1 hover:text-green-800 dark:hover:text-green-200">
                                                    <span className="material-symbols-outlined text-sm">close</span>
                                                </button>
                                            </span>
                                        ))}
                                    </div>
                                ) : (
                                    <p className="text-[#94a3b8] text-center py-3 text-sm">No watched tags yet. Add tags below to watch them.</p>
                                )}
                            </>
                        ) : (
                            <>
                                <div className="p-3 bg-red-50 dark:bg-red-500/10 border border-red-200 dark:border-red-500/30 rounded-xl text-sm mb-4 flex items-start gap-2">
                                    <span className="material-symbols-outlined text-red-500 text-base mt-0.5">info</span>
                                    <div>
                                        <p className="font-bold text-red-700 dark:text-red-400">Ignored Tags</p>
                                        <p className="text-red-600 dark:text-red-300 text-xs">Questions with these tags will be hidden from your feed.</p>
                                    </div>
                                </div>
                                {ignoredTags.length > 0 ? (
                                    <div className="flex flex-wrap gap-2 mb-4">
                                        {ignoredTags.map(tag => (
                                            <span key={tag} className="flex items-center gap-1.5 px-3 py-1.5 rounded-full bg-red-50 dark:bg-red-500/10 text-red-600 dark:text-red-400 text-xs font-bold">
                                                <span className="material-symbols-outlined text-sm">visibility_off</span>
                                                {tag}
                                                <button onClick={() => toggleTag(tag, 'ignored')} className="ml-1 hover:text-red-800 dark:hover:text-red-200">
                                                    <span className="material-symbols-outlined text-sm">close</span>
                                                </button>
                                            </span>
                                        ))}
                                    </div>
                                ) : (
                                    <p className="text-[#94a3b8] text-center py-3 text-sm">No ignored tags. Add tags below to ignore them.</p>
                                )}
                            </>
                        )}
                    </div>

                    {/* Search & Add Tags */}
                    <div className="bg-white dark:bg-[var(--bg-secondary)] border border-[#e2e8f0] dark:border-[var(--border-color)] rounded-2xl p-5">
                        <h3 className="font-bold text-[#0f172a] dark:text-white mb-4">Add Tags</h3>
                        <div className="relative mb-4">
                            <span className="material-symbols-outlined absolute left-3 top-1/2 -translate-y-1/2 text-[#94a3b8]">search</span>
                            <input
                                type="text"
                                placeholder="Search tags..."
                                value={searchTerm}
                                onChange={(e) => setSearchTerm(e.target.value)}
                                className="w-full pl-10 pr-4 py-2.5 bg-[var(--bg-tertiary)] border border-[#e2e8f0] dark:border-[var(--border-color)] rounded-xl text-[var(--text-primary)] placeholder-[#94a3b8] outline-none focus:border-[#137fec] transition text-sm"
                            />
                        </div>

                        <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-2">
                            {filteredTags.map(tag => (
                                <div key={tag.tagId} className="flex items-center justify-between p-2.5 border border-[#e2e8f0] dark:border-[var(--border-color)] rounded-xl">
                                    <div>
                                        <span className="px-2 py-0.5 rounded-md bg-[rgba(19,127,236,0.1)] text-[#137fec] text-xs font-bold">{tag.tagName}</span>
                                        <span className="text-[#94a3b8] text-xs ml-1">×{tag.usageCount}</span>
                                    </div>
                                    <div className="flex gap-1">
                                        <button
                                            onClick={() => toggleTag(tag.tagName, 'watched')}
                                            title="Watch"
                                            className={`w-7 h-7 rounded-lg flex items-center justify-center transition ${watchedTags.includes(tag.tagName) ? 'bg-green-500 text-white' : 'border border-green-300 dark:border-green-500/30 text-green-500 hover:bg-green-50 dark:hover:bg-green-500/10'}`}
                                        >
                                            <span className="material-symbols-outlined text-sm">visibility</span>
                                        </button>
                                        <button
                                            onClick={() => toggleTag(tag.tagName, 'ignored')}
                                            title="Ignore"
                                            className={`w-7 h-7 rounded-lg flex items-center justify-center transition ${ignoredTags.includes(tag.tagName) ? 'bg-red-500 text-white' : 'border border-red-300 dark:border-red-500/30 text-red-500 hover:bg-red-50 dark:hover:bg-red-500/10'}`}
                                        >
                                            <span className="material-symbols-outlined text-sm">visibility_off</span>
                                        </button>
                                    </div>
                                </div>
                            ))}
                        </div>
                    </div>

                    {/* Save */}
                    <div className="flex justify-end">
                        <button onClick={savePreferences} className="flex items-center gap-2 bg-[#137fec] text-white px-6 py-2.5 rounded-xl font-bold text-sm shadow-[0_4px_12px_rgba(19,127,236,0.2)] hover:bg-[#1170d4] hover:-translate-y-px transition-all">
                            <span className="material-symbols-outlined text-base">check</span>Save Preferences
                        </button>
                    </div>
                </div>

                {/* Sidebar */}
                <div>
                    <div className="bg-white dark:bg-[var(--bg-secondary)] border border-[#e2e8f0] dark:border-[var(--border-color)] rounded-2xl p-5">
                        <h3 className="font-bold text-[#0f172a] dark:text-white mb-3 flex items-center gap-2">
                            <span className="material-symbols-outlined text-amber-500">lightbulb</span>Tips
                        </h3>
                        <div className="space-y-3">
                            {[
                                { icon: 'check_circle', color: 'text-green-500', text: 'Watch tags you want to follow closely' },
                                { icon: 'check_circle', color: 'text-green-500', text: 'Ignore tags for topics you\'re not interested in' },
                                { icon: 'check_circle', color: 'text-green-500', text: 'Your preferences affect your homepage feed' },
                                { icon: 'info', color: 'text-[#137fec]', text: 'You can always change these later' },
                            ].map((tip, i) => (
                                <div key={i} className="flex items-start gap-2">
                                    <span className={`material-symbols-outlined text-base mt-0.5 ${tip.color}`}>{tip.icon}</span>
                                    <span className="text-[#475569] dark:text-[var(--text-secondary)] text-sm">{tip.text}</span>
                                </div>
                            ))}
                        </div>
                    </div>
                </div>
            </div>
        </AppLayout>
    );
}