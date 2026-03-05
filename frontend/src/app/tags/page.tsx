'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import apiClient from '@/lib/api/client';
import AppLayout from '@/components/AppLayout';

interface Tag {
    tagId: number;
    tagName: string;
    description?: string;
    questionCount: number;
}

interface TagsResponse {
    items?: Tag[];
}

const TAG_ICONS: Record<string, string> = {
    javascript: 'javascript',
    python: 'code_blocks',
    reactjs: 'javascript',
    react: 'javascript',
    html: 'html',
    css: 'css',
    sql: 'database',
    'node.js': 'terminal',
    nodejs: 'terminal',
    swift: 'phone_iphone',
    typescript: 'javascript',
    java: 'code',
    csharp: 'code',
    'c#': 'code',
    '.net': 'code',
    docker: 'deployed_code',
    git: 'commit',
    angular: 'code_blocks',
    vue: 'code_blocks',
    nextjs: 'code_blocks',
    tailwind: 'css',
    mongodb: 'database',
    redis: 'database',
    api: 'api',
    rest: 'api',
};

const getTagIcon = (tagName: string): string => {
    const lower = tagName.toLowerCase();
    return TAG_ICONS[lower] || 'sell';
};

const formatCount = (count: number): string => {
    if (count >= 1_000_000) return `${(count / 1_000_000).toFixed(1)}M`;
    if (count >= 1_000) return `${(count / 1_000).toFixed(0)}k`;
    return count.toLocaleString();
};

export default function TagsPage() {
    const [tags, setTags] = useState<Tag[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [search, setSearch] = useState('');
    const [sortBy, setSortBy] = useState('popular');
    const [page, setPage] = useState(1);
    const perPage = 12;

    useEffect(() => {
        fetchTags();
    }, []);

    const fetchTags = async () => {
        try {
            const response = await apiClient.get<TagsResponse | Tag[]>('/tags');
            const data = response.data;
            if (Array.isArray(data)) {
                setTags(data);
            } else if (data && 'items' in data) {
                setTags(data.items || []);
            } else {
                setTags([]);
            }
        } catch (error) {
            console.error('Failed to fetch tags:', error);
            setTags([]);
        } finally {
            setIsLoading(false);
        }
    };

    const filteredTags = Array.isArray(tags)
        ? tags.filter(tag => tag.tagName.toLowerCase().includes(search.toLowerCase()))
        : [];

    const sortedTags = [...filteredTags].sort((a, b) => {
        if (sortBy === 'popular') return b.questionCount - a.questionCount;
        if (sortBy === 'name') return a.tagName.localeCompare(b.tagName);
        return 0;
    });

    const totalPages = Math.ceil(sortedTags.length / perPage);
    const paginatedTags = sortedTags.slice((page - 1) * perPage, page * perPage);

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

    return (
        <AppLayout showRightSidebar={false}>
            {/* Title & Description */}
            <div className="mb-10">
                <h2 className="text-4xl font-black text-slate-900 dark:text-white tracking-tight mb-2">Tags</h2>
                <p className="text-slate-600 dark:text-slate-400 max-w-2xl leading-relaxed">
                    A tag is a keyword or label that categorizes your question with other, similar questions.
                    Using the right tags makes it easier for others to find and answer your question.
                </p>
            </div>

            {/* Filter & Sort */}
            <div className="flex flex-wrap items-center gap-4 mb-10">
                <div className="relative flex-1 min-w-[300px]">
                    <span className="material-symbols-outlined absolute left-4 top-1/2 -translate-y-1/2 text-slate-400">filter_list</span>
                    <input
                        type="text"
                        value={search}
                        onChange={(e) => { setSearch(e.target.value); setPage(1); }}
                        placeholder="Filter by tag name"
                        className="w-full h-12 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl pl-12 pr-4 text-base focus:ring-2 focus:ring-[var(--primary)] shadow-sm text-slate-900 dark:text-white placeholder:text-slate-400"
                    />
                </div>
                <button
                    onClick={() => setSortBy(sortBy === 'popular' ? 'name' : 'popular')}
                    className="flex items-center gap-2 px-6 h-12 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl font-bold text-sm text-slate-700 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-800 transition-colors shadow-sm"
                >
                    <span className="material-symbols-outlined text-[20px]">sort</span>
                    <span>{sortBy === 'popular' ? 'Popular' : 'A-Z'}</span>
                </button>
            </div>

            {/* Tags Grid */}
            {isLoading ? (
                <div className="flex items-center justify-center py-16">
                    <div className="w-10 h-10 border-4 border-[var(--primary)]/30 border-t-[var(--primary)] rounded-full animate-spin"></div>
                </div>
            ) : paginatedTags.length === 0 ? (
                <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-2xl p-12 text-center">
                    <span className="material-symbols-outlined text-5xl text-slate-300 mb-4 block">sell</span>
                    <h3 className="text-lg font-semibold text-slate-900 dark:text-white mb-2">No tags found</h3>
                    <p className="text-slate-500">Try a different search term</p>
                </div>
            ) : (
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
                    {paginatedTags.map((tag) => (
                        <Link
                            key={tag.tagId}
                            href={`/questions?tag=${tag.tagName}`}
                            className="group bg-white dark:bg-slate-900 p-6 rounded-2xl border border-slate-200 dark:border-slate-800 hover:border-[var(--primary)]/50 transition-all hover:shadow-xl hover:shadow-[var(--primary)]/5"
                        >
                            <div className="w-12 h-12 rounded-xl bg-[var(--primary)]/10 flex items-center justify-center text-[var(--primary)] mb-4 group-hover:scale-110 transition-transform">
                                <span className="material-symbols-outlined text-[28px]">{getTagIcon(tag.tagName)}</span>
                            </div>
                            <h3 className="text-lg font-bold text-slate-900 dark:text-white mb-2">{tag.tagName}</h3>
                            <p className="text-slate-500 dark:text-slate-400 text-sm leading-relaxed mb-4 line-clamp-3">
                                {tag.description || `Questions about ${tag.tagName}`}
                            </p>
                            <div className="flex items-center justify-between pt-4 border-t border-slate-100 dark:border-slate-800">
                                <span className="text-xs font-bold text-slate-400 uppercase tracking-wider">
                                    {formatCount(tag.questionCount)} Qs
                                </span>
                                <span className="text-[var(--primary)] text-sm font-bold flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                                    Explore <span className="material-symbols-outlined text-[16px]">arrow_forward</span>
                                </span>
                            </div>
                        </Link>
                    ))}
                </div>
            )}

            {/* Pagination */}
            {totalPages > 1 && (
                <div className="mt-12 flex justify-center">
                    <div className="flex items-center gap-1 bg-white dark:bg-slate-900 p-1.5 rounded-xl border border-slate-200 dark:border-slate-800 shadow-sm">
                        <button
                            onClick={() => setPage(p => Math.max(1, p - 1))}
                            disabled={page === 1}
                            className="size-10 flex items-center justify-center rounded-lg text-slate-500 hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors disabled:opacity-30"
                        >
                            <span className="material-symbols-outlined">chevron_left</span>
                        </button>
                        {getPageNumbers().map((p, i) =>
                            p === '...' ? (
                                <span key={`dots-${i}`} className="px-2 text-slate-400">...</span>
                            ) : (
                                <button
                                    key={p}
                                    onClick={() => setPage(p as number)}
                                    className={`size-10 flex items-center justify-center rounded-lg font-medium transition-colors ${page === p
                                        ? 'bg-[var(--primary)] text-white font-bold'
                                        : 'text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800'
                                        }`}
                                >
                                    {p}
                                </button>
                            )
                        )}
                        <button
                            onClick={() => setPage(p => Math.min(totalPages, p + 1))}
                            disabled={page === totalPages}
                            className="size-10 flex items-center justify-center rounded-lg text-slate-500 hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors disabled:opacity-30"
                        >
                            <span className="material-symbols-outlined">chevron_right</span>
                        </button>
                    </div>
                </div>
            )}
        </AppLayout>
    );
}
