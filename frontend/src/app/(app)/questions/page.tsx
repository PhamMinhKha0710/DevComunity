'use client';

import { Suspense, useState } from 'react';
import Link from 'next/link';
import { useSearchParams } from 'next/navigation';
import { useQuery } from '@tanstack/react-query';
import type { Question, Tag } from '@/types';
import { questionsApi } from '@/lib/api/questions.api';
import AppLayout from '@/components/AppLayout';

const stripHtml = (html: string): string => {
    if (!html) return '';
    return html.replace(/<[^>]*>/g, '').substring(0, 200);
};

const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffHours < 24) return `${diffHours}h ago`;
    if (diffDays < 7) return `${diffDays}d ago`;
    if (diffDays < 30) return `${diffDays}d ago`;
    return date.toLocaleDateString();
};

const formatViews = (count: number) => {
    if (count >= 1000) return `${(count / 1000).toFixed(1)}k views`;
    return `${count} views`;
};

function QuestionsContent() {
    const searchParams = useSearchParams();
    const [page, setPage] = useState(1);
    const [sortBy, setSortBy] = useState('newest');

    const search = searchParams.get('search') || '';
    const tag = searchParams.get('tag') || '';

    const { data, isLoading } = useQuery({
        queryKey: ['questions', { page, sortBy, search, tag }],
        queryFn: () => questionsApi.list({ page, pageSize: 15, sortBy, search: search || undefined, tag: tag || undefined }),
    });

    const questions: Question[] = data?.items || [];
    const totalPages = data?.totalPages || 1;
    const totalCount = data?.totalCount || 0;

    const sortTabs = [
        { key: 'newest', label: 'Interesting' },
        { key: 'active', label: 'Hot' },
        { key: 'week', label: 'Week' },
        { key: 'votes', label: 'Month' },
    ];

    return (
        <AppLayout>
            {/* Page Header */}
            <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
                <div>
                    <h1 className="text-2xl md:text-3xl font-black text-[var(--text-primary)]">
                        {tag ? `Questions tagged [${tag}]` : search ? `Search: "${search}"` : 'All Questions'}
                    </h1>
                    <p className="text-slate-500 text-sm mt-1">
                        {totalCount > 0 ? `${totalCount.toLocaleString()} questions` : `${questions.length} questions found`}
                    </p>
                </div>
                <Link
                    href="/questions/ask"
                    className="flex items-center gap-2 bg-[var(--primary)] text-white px-6 py-2.5 rounded-lg font-bold text-sm shadow-lg shadow-[var(--primary)]/25 hover:bg-[var(--primary-dark)] transition-all"
                >
                    <span className="material-symbols-outlined text-lg">add</span>
                    Ask Question
                </Link>
            </div>

            {/* Filter Tabs */}
            <div className="flex border-b border-[var(--primary)]/10 gap-6 overflow-x-auto">
                {sortTabs.map((tab) => (
                    <button
                        key={tab.key}
                        onClick={() => setSortBy(tab.key)}
                        className={`pb-3 text-sm font-semibold whitespace-nowrap transition-all border-b-2 ${
                            sortBy === tab.key
                                ? 'border-[var(--primary)] text-[var(--primary)] font-bold'
                                : 'border-transparent text-slate-500 hover:text-[var(--primary)]'
                        }`}
                    >
                        {tab.label}
                    </button>
                ))}
            </div>

            {/* Question List */}
            {isLoading ? (
                <div className="flex items-center justify-center py-16">
                    <div className="w-10 h-10 border-3 border-[var(--primary)]/20 border-t-[var(--primary)] rounded-full animate-spin" />
                </div>
            ) : questions.length === 0 ? (
                <div className="bg-white dark:bg-slate-900 border border-[var(--primary)]/10 rounded-xl p-12 text-center">
                    <span className="material-symbols-outlined text-5xl text-slate-300 mb-4 block">search_off</span>
                    <h3 className="text-lg font-bold text-[var(--text-primary)] mb-2">No questions found</h3>
                    <p className="text-slate-500 mb-4">Try adjusting your search or filters</p>
                    <Link href="/questions/ask" className="inline-flex items-center gap-2 px-5 py-2.5 bg-[var(--primary)] text-white rounded-lg font-bold text-sm shadow-lg shadow-[var(--primary)]/25">
                        <span className="material-symbols-outlined text-lg">add</span> Ask a Question
                    </Link>
                </div>
            ) : (
                <div className="flex flex-col gap-4">
                    {questions.map((question) => (
                        <div
                            key={question.questionId}
                            className="bg-white dark:bg-slate-900 border border-[var(--primary)]/10 p-5 rounded-xl flex gap-6 hover:shadow-md transition-shadow"
                        >
                            {/* Stats */}
                            <div className="hidden sm:flex flex-col items-end gap-3 min-w-[80px]">
                                <div className="text-center">
                                    <div className="flex items-center justify-center gap-1">
                                        <span className="material-symbols-outlined text-sm text-[var(--primary)]">thumb_up</span>
                                        <p className="text-lg font-bold text-[var(--text-primary)]">{question.score}</p>
                                    </div>
                                    <p className="text-[10px] text-slate-500 uppercase font-bold tracking-tight">likes</p>
                                </div>
                                <div className={`text-center px-2 py-1 rounded-md w-full ${
                                    question.hasAcceptedAnswer
                                        ? 'bg-green-500/10 text-green-600 border border-green-200'
                                        : question.answerCount > 0
                                            ? 'bg-[var(--primary)]/10 text-[var(--primary)] border border-[var(--primary)]/20'
                                            : 'border border-slate-200 dark:border-slate-800 text-slate-600 dark:text-slate-400'
                                }`}>
                                    {question.hasAcceptedAnswer && (
                                        <div className="flex items-center justify-center gap-1">
                                            <span className="material-symbols-outlined text-sm">check_circle</span>
                                            <p className="text-sm font-bold">{question.answerCount}</p>
                                        </div>
                                    )}
                                    {!question.hasAcceptedAnswer && (
                                        <p className="text-sm font-bold">{question.answerCount}</p>
                                    )}
                                    <p className="text-[10px] uppercase font-bold tracking-tight">answers</p>
                                </div>
                                <div className="text-center">
                                    <p className="text-sm text-slate-400">{formatViews(question.viewCount)}</p>
                                </div>
                            </div>

                            {/* Content */}
                            <div className="flex-1 flex flex-col gap-3 min-w-0">
                                <Link
                                    href={`/questions/${question.questionId}`}
                                    className="text-lg font-bold text-[var(--text-primary)] hover:text-[var(--primary)] cursor-pointer leading-tight transition-colors"
                                >
                                    {question.title}
                                </Link>
                                <p className="text-slate-600 dark:text-slate-400 text-sm line-clamp-2 leading-relaxed">
                                    {stripHtml(question.bodyExcerpt || question.body || '')}
                                </p>

                                {/* Tags & Author */}
                                <div className="flex flex-wrap items-center justify-between gap-4 mt-1">
                                    <div className="flex gap-2 flex-wrap">
                                        {question.tags?.slice(0, 4).map((t: Tag) => (
                                            <Link
                                                key={t.tagId}
                                                href={`/questions?tag=${t.tagName}`}
                                                className="px-2.5 py-1 bg-[var(--primary)]/5 text-[var(--primary)] text-xs font-semibold rounded hover:bg-[var(--primary)]/10 transition-colors"
                                            >
                                                #{t.tagName}
                                            </Link>
                                        ))}
                                        {question.tags && question.tags.length > 4 && (
                                            <span className="px-2 py-1 text-xs text-slate-400">+{question.tags.length - 4}</span>
                                        )}
                                    </div>
                                    <div className="flex items-center gap-2">
                                        <div className="size-6 rounded-full bg-gradient-to-br from-blue-500 to-indigo-600 flex items-center justify-center text-white text-[10px] font-bold">
                                            {question.authorUsername?.charAt(0).toUpperCase() || '?'}
                                        </div>
                                        <span className="text-xs font-bold text-[var(--primary)] hover:underline cursor-pointer">
                                            {question.authorUsername || 'Anonymous'}
                                        </span>
                                        <span className="text-xs text-slate-500">asked {formatDate(question.createdDate)}</span>
                                    </div>
                                </div>
                            </div>
                        </div>
                    ))}
                </div>
            )}

            {/* Pagination */}
            {totalPages > 1 && (
                <div className="flex justify-center gap-2 mt-2">
                    <button
                        onClick={() => setPage(p => p - 1)}
                        disabled={page === 1}
                        className="px-4 py-2 bg-white dark:bg-slate-900 border border-[var(--primary)]/10 rounded-lg text-slate-600 disabled:opacity-40 disabled:cursor-not-allowed hover:bg-[var(--primary)]/5 transition text-sm font-semibold"
                    >
                        Previous
                    </button>
                    {[...Array(Math.min(5, totalPages))].map((_, i) => (
                        <button
                            key={i}
                            onClick={() => setPage(i + 1)}
                            className={`w-10 h-10 rounded-lg font-bold text-sm transition ${page === i + 1
                                    ? 'bg-[var(--primary)] text-white shadow-lg shadow-[var(--primary)]/25'
                                    : 'bg-white dark:bg-slate-900 border border-[var(--primary)]/10 text-slate-600 hover:bg-[var(--primary)]/5'
                                }`}
                        >
                            {i + 1}
                        </button>
                    ))}
                    <button
                        onClick={() => setPage(p => p + 1)}
                        disabled={page === totalPages}
                        className="px-4 py-2 bg-white dark:bg-slate-900 border border-[var(--primary)]/10 rounded-lg text-slate-600 disabled:opacity-40 disabled:cursor-not-allowed hover:bg-[var(--primary)]/5 transition text-sm font-semibold"
                    >
                        Next
                    </button>
                </div>
            )}
        </AppLayout>
    );
}

export default function QuestionsPage() {
    return (
        <Suspense fallback={
            <AppLayout>
                <div className="flex items-center justify-center py-20">
                    <div className="w-10 h-10 border-3 border-[var(--primary)]/20 border-t-[var(--primary)] rounded-full animate-spin" />
                </div>
            </AppLayout>
        }>
            <QuestionsContent />
        </Suspense>
    );
}
