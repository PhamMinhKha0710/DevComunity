'use client';

import { Suspense, useEffect, useState } from 'react';
import Link from 'next/link';
import { useSearchParams } from 'next/navigation';
import type { Question, PaginatedResponse, Tag } from '@/types';
import apiClient from '@/lib/api/client';
import AppLayout from '@/components/AppLayout';

const stripHtml = (html: string): string => {
    if (!html) return '';
    return html.replace(/<[^>]*>/g, '').substring(0, 150);
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
    return date.toLocaleDateString();
};

function QuestionsContent() {
    const searchParams = useSearchParams();
    const [questions, setQuestions] = useState<Question[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [page, setPage] = useState(1);
    const [totalPages, setTotalPages] = useState(1);
    const [sortBy, setSortBy] = useState('newest');

    const search = searchParams.get('search') || '';
    const tag = searchParams.get('tag') || '';

    useEffect(() => {
        fetchQuestions();
    }, [page, sortBy, search, tag]);

    const fetchQuestions = async () => {
        setIsLoading(true);
        try {
            let url = `/questions?page=${page}&pageSize=15&sortBy=${sortBy}`;
            if (search) url += `&search=${encodeURIComponent(search)}`;
            if (tag) url += `&tag=${encodeURIComponent(tag)}`;

            const response = await apiClient.get<PaginatedResponse<Question>>(url);
            setQuestions(response.data.items || []);
            setTotalPages(response.data.totalPages || 1);
        } catch (error) {
            console.error('Failed to fetch questions:', error);
        } finally {
            setIsLoading(false);
        }
    };

    const sortOptions = [
        { key: 'newest', label: 'Newest', icon: 'bi-clock' },
        { key: 'active', label: 'Active', icon: 'bi-activity' },
        { key: 'unanswered', label: 'Unanswered', icon: 'bi-question-circle' },
        { key: 'votes', label: 'Most Votes', icon: 'bi-graph-up' },
    ];

    return (
        <AppLayout>
            {/* Header */}
            <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4 mb-6">
                <div>
                    <h1 className="text-2xl font-bold text-[var(--text-primary)] flex items-center gap-2">
                        <i className="bi bi-question-circle-fill text-[var(--primary)]"></i>
                        {tag ? `Questions tagged [${tag}]` : search ? `Search: "${search}"` : 'All Questions'}
                    </h1>
                    <p className="text-[var(--text-muted)]">{questions.length} questions found</p>
                </div>
                <Link
                    href="/questions/ask"
                    className="flex items-center gap-2 px-5 py-2.5 bg-[var(--primary)] text-white rounded-xl font-medium hover:bg-[var(--primary-dark)] transition"
                >
                    <i className="bi bi-plus-circle"></i>
                    Ask Question
                </Link>
            </div>

            {/* Sort Filters */}
            <div className="flex flex-wrap gap-2 mb-6">
                {sortOptions.map((opt) => (
                    <button
                        key={opt.key}
                        onClick={() => setSortBy(opt.key)}
                        className={`flex items-center gap-2 px-4 py-2 rounded-xl font-medium text-sm transition ${sortBy === opt.key
                                ? 'bg-[var(--primary)] text-white'
                                : 'bg-[var(--bg-secondary)] text-[var(--text-muted)] border border-[var(--border-color)] hover:border-[var(--primary)]'
                            }`}
                    >
                        <i className={`bi ${opt.icon}`}></i>
                        {opt.label}
                    </button>
                ))}
            </div>

            {/* Questions List */}
            {isLoading ? (
                <div className="flex items-center justify-center py-16">
                    <div className="w-10 h-10 border-4 border-[var(--primary)]/30 border-t-[var(--primary)] rounded-full animate-spin"></div>
                </div>
            ) : questions.length === 0 ? (
                <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-2xl p-12 text-center">
                    <i className="bi bi-search text-5xl text-[var(--text-muted)] mb-4"></i>
                    <h3 className="text-lg font-semibold text-[var(--text-primary)] mb-2">No questions found</h3>
                    <p className="text-[var(--text-muted)] mb-4">Try adjusting your search or filters</p>
                    <Link href="/questions/ask" className="inline-flex items-center gap-2 px-4 py-2 bg-[var(--primary)] text-white rounded-xl">
                        <i className="bi bi-plus-circle"></i> Ask a Question
                    </Link>
                </div>
            ) : (
                <div className="space-y-4">
                    {questions.map((question) => (
                        <div
                            key={question.questionId}
                            className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-2xl p-5 hover:border-[var(--primary)]/50 transition"
                        >
                            <div className="flex gap-4">
                                {/* Stats */}
                                <div className="hidden sm:flex flex-col items-center gap-2 min-w-[70px]">
                                    <div className={`px-3 py-2 rounded-xl text-center w-full ${question.score > 0 ? 'bg-green-500/20 text-green-500' : 'bg-[var(--bg-tertiary)] text-[var(--text-muted)]'}`}>
                                        <span className="block text-lg font-bold">{question.score}</span>
                                        <span className="text-xs">votes</span>
                                    </div>
                                    <div className={`px-3 py-2 rounded-xl text-center w-full ${question.hasAcceptedAnswer ? 'bg-green-500 text-white' :
                                            question.answerCount > 0 ? 'bg-blue-500/20 text-blue-500' :
                                                'bg-[var(--bg-tertiary)] text-[var(--text-muted)]'
                                        }`}>
                                        <span className="block text-lg font-bold">{question.answerCount}</span>
                                        <span className="text-xs">answers</span>
                                    </div>
                                </div>

                                {/* Content */}
                                <div className="flex-1 min-w-0">
                                    <Link
                                        href={`/questions/${question.questionId}`}
                                        className="text-lg font-semibold text-[var(--text-primary)] hover:text-[var(--primary)] transition line-clamp-2"
                                    >
                                        {question.title}
                                    </Link>
                                    <p className="mt-2 text-sm text-[var(--text-muted)] line-clamp-2">
                                        {stripHtml(question.bodyExcerpt || question.body || '')}
                                    </p>

                                    {/* Tags & Meta */}
                                    <div className="flex flex-wrap items-center gap-3 mt-4">
                                        <div className="flex flex-wrap gap-2">
                                            {question.tags?.slice(0, 4).map((t: Tag) => (
                                                <Link
                                                    key={t.tagId}
                                                    href={`/questions?tag=${t.tagName}`}
                                                    className="px-2.5 py-1 text-xs font-medium bg-[var(--primary)]/10 text-[var(--primary)] rounded-lg hover:bg-[var(--primary)]/20 transition"
                                                >
                                                    {t.tagName}
                                                </Link>
                                            ))}
                                        </div>
                                        <div className="flex items-center gap-2 ml-auto text-xs text-[var(--text-muted)]">
                                            <div className="w-5 h-5 rounded-full bg-gradient-to-br from-blue-500 to-purple-500 flex items-center justify-center text-white text-[10px] font-bold">
                                                {question.authorUsername?.charAt(0).toUpperCase() || '?'}
                                            </div>
                                            <span>{question.authorUsername || 'Anonymous'}</span>
                                            <span>•</span>
                                            <span>{formatDate(question.createdDate)}</span>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    ))}
                </div>
            )}

            {/* Pagination */}
            {totalPages > 1 && (
                <div className="flex justify-center gap-2 mt-6">
                    <button
                        onClick={() => setPage(p => p - 1)}
                        disabled={page === 1}
                        className="px-4 py-2 bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-xl text-[var(--text-secondary)] disabled:opacity-50 disabled:cursor-not-allowed hover:border-[var(--primary)] transition"
                    >
                        Previous
                    </button>
                    {[...Array(Math.min(5, totalPages))].map((_, i) => (
                        <button
                            key={i}
                            onClick={() => setPage(i + 1)}
                            className={`w-10 h-10 rounded-xl font-medium transition ${page === i + 1
                                    ? 'bg-[var(--primary)] text-white'
                                    : 'bg-[var(--bg-secondary)] border border-[var(--border-color)] text-[var(--text-secondary)] hover:border-[var(--primary)]'
                                }`}
                        >
                            {i + 1}
                        </button>
                    ))}
                    <button
                        onClick={() => setPage(p => p + 1)}
                        disabled={page === totalPages}
                        className="px-4 py-2 bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-xl text-[var(--text-secondary)] disabled:opacity-50 disabled:cursor-not-allowed hover:border-[var(--primary)] transition"
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
                    <div className="w-10 h-10 border-4 border-[var(--primary)]/30 border-t-[var(--primary)] rounded-full animate-spin"></div>
                </div>
            </AppLayout>
        }>
            <QuestionsContent />
        </Suspense>
    );
}
