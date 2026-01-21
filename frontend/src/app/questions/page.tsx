'use client';

import { Suspense, useEffect, useState } from 'react';
import Link from 'next/link';
import { useSearchParams } from 'next/navigation';
import type { Question, PaginatedResponse, Tag } from '@/types';
import apiClient from '@/lib/api/client';

// Helper to strip HTML tags
const stripHtml = (html: string): string => {
    if (!html) return '';
    return html.replace(/<[^>]*>/g, '').substring(0, 150);
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

    return (
        <div className="min-h-screen bg-gray-50 dark:bg-slate-900 py-8">
            <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
                <div className="grid lg:grid-cols-4 gap-8">
                    {/* Main Content */}
                    <div className="lg:col-span-3">
                        {/* Header */}
                        <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4 mb-6">
                            <div>
                                <h1 className="text-2xl font-bold text-gray-900 dark:text-white">
                                    {tag ? `Questions tagged [${tag}]` : search ? `Search results for "${search}"` : 'All Questions'}
                                </h1>
                                <p className="text-gray-500 text-sm mt-1">
                                    {questions.length} questions found
                                </p>
                            </div>
                            <Link
                                href="/questions/ask"
                                className="inline-flex items-center px-4 py-2 bg-orange-500 text-white font-medium rounded-lg hover:bg-orange-600 transition"
                            >
                                <i className="bi bi-plus-circle mr-2"></i>Ask Question
                            </Link>
                        </div>

                        {/* Filters */}
                        <div className="bg-white dark:bg-slate-800 rounded-xl shadow-sm p-4 mb-6">
                            <div className="flex flex-wrap gap-2">
                                {[
                                    { key: 'newest', icon: 'bi-clock', label: 'Newest' },
                                    { key: 'active', icon: 'bi-activity', label: 'Active' },
                                    { key: 'unanswered', icon: 'bi-question-circle', label: 'Unanswered' },
                                    { key: 'votes', icon: 'bi-graph-up', label: 'Most Votes' },
                                ].map(({ key, icon, label }) => (
                                    <button
                                        key={key}
                                        onClick={() => setSortBy(key)}
                                        className={`inline-flex items-center px-4 py-2 rounded-full text-sm font-medium transition ${sortBy === key
                                                ? 'bg-orange-500 text-white'
                                                : 'bg-gray-100 dark:bg-slate-700 text-gray-700 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-slate-600'
                                            }`}
                                    >
                                        <i className={`bi ${icon} mr-2`}></i>{label}
                                    </button>
                                ))}
                            </div>
                        </div>

                        {/* Questions List */}
                        <div className="bg-white dark:bg-slate-800 rounded-xl shadow-sm overflow-hidden">
                            {isLoading ? (
                                <div className="p-12 text-center">
                                    <div className="inline-block w-8 h-8 border-4 border-orange-500 border-t-transparent rounded-full animate-spin"></div>
                                </div>
                            ) : questions.length > 0 ? (
                                <div className="divide-y divide-gray-100 dark:divide-slate-700">
                                    {questions.map((question) => (
                                        <div key={question.questionId} className="p-4 md:p-6 hover:bg-gray-50 dark:hover:bg-slate-700/50 transition">
                                            <div className="flex gap-4">
                                                {/* Stats */}
                                                <div className="hidden sm:flex flex-col items-center gap-2 text-center min-w-[70px]">
                                                    <div className={`px-3 py-2 rounded-lg text-sm ${question.score > 0
                                                            ? 'bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400'
                                                            : 'bg-gray-100 dark:bg-slate-700 text-gray-600 dark:text-gray-400'
                                                        }`}>
                                                        <span className="font-bold block">{question.score}</span>
                                                        <span className="text-xs">votes</span>
                                                    </div>
                                                    <div className={`px-3 py-2 rounded-lg text-sm ${question.hasAcceptedAnswer
                                                            ? 'bg-green-500 text-white'
                                                            : question.answerCount > 0
                                                                ? 'bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400'
                                                                : 'bg-gray-100 dark:bg-slate-700 text-gray-600 dark:text-gray-400'
                                                        }`}>
                                                        <span className="font-bold block">{question.answerCount}</span>
                                                        <span className="text-xs">answers</span>
                                                    </div>
                                                </div>

                                                {/* Content */}
                                                <div className="flex-1 min-w-0">
                                                    <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-2 hover:text-orange-500 transition">
                                                        <Link href={`/questions/${question.questionId}`}>
                                                            {question.title}
                                                        </Link>
                                                    </h3>
                                                    <p className="text-gray-600 dark:text-gray-400 text-sm mb-3 line-clamp-2">
                                                        {stripHtml(question.bodyExcerpt || question.body || '')}...
                                                    </p>
                                                    <div className="flex flex-wrap items-center justify-between gap-2">
                                                        <div className="flex flex-wrap gap-1">
                                                            {question.tags?.slice(0, 5).map((t: Tag) => (
                                                                <Link
                                                                    key={t.tagId}
                                                                    href={`/questions?tag=${t.tagName}`}
                                                                    className="px-2 py-1 bg-orange-100 dark:bg-orange-900/30 text-orange-600 dark:text-orange-400 text-xs rounded-md hover:bg-orange-200 transition"
                                                                >
                                                                    {t.tagName}
                                                                </Link>
                                                            ))}
                                                        </div>
                                                        <div className="flex items-center text-xs text-gray-500">
                                                            <img
                                                                src={question.authorProfilePicture || '/images/default-avatar.png'}
                                                                className="w-5 h-5 rounded-full mr-1"
                                                                alt=""
                                                            />
                                                            <span>
                                                                asked {new Date(question.createdDate).toLocaleDateString()} by{' '}
                                                                <span className="text-orange-500 font-medium">{question.authorUsername}</span>
                                                            </span>
                                                        </div>
                                                    </div>
                                                </div>
                                            </div>
                                        </div>
                                    ))}
                                </div>
                            ) : (
                                <div className="p-12 text-center">
                                    <i className="bi bi-search text-4xl text-gray-300 dark:text-slate-600 mb-3"></i>
                                    <h3 className="text-lg font-bold text-gray-900 dark:text-white mb-2">No questions found</h3>
                                    <p className="text-gray-500">Try adjusting your search or filters</p>
                                </div>
                            )}

                            {/* Pagination */}
                            {totalPages > 1 && (
                                <div className="p-4 border-t border-gray-100 dark:border-slate-700">
                                    <div className="flex justify-center items-center gap-2">
                                        <button
                                            onClick={() => setPage(p => p - 1)}
                                            disabled={page === 1}
                                            className="px-4 py-2 rounded-lg border border-gray-300 dark:border-slate-600 text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-slate-700 disabled:opacity-50 disabled:cursor-not-allowed transition"
                                        >
                                            Previous
                                        </button>
                                        {[...Array(Math.min(5, totalPages))].map((_, i) => (
                                            <button
                                                key={i}
                                                onClick={() => setPage(i + 1)}
                                                className={`w-10 h-10 rounded-lg font-medium transition ${page === i + 1
                                                        ? 'bg-orange-500 text-white'
                                                        : 'border border-gray-300 dark:border-slate-600 text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-slate-700'
                                                    }`}
                                            >
                                                {i + 1}
                                            </button>
                                        ))}
                                        <button
                                            onClick={() => setPage(p => p + 1)}
                                            disabled={page === totalPages}
                                            className="px-4 py-2 rounded-lg border border-gray-300 dark:border-slate-600 text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-slate-700 disabled:opacity-50 disabled:cursor-not-allowed transition"
                                        >
                                            Next
                                        </button>
                                    </div>
                                </div>
                            )}
                        </div>
                    </div>

                    {/* Sidebar */}
                    <div className="space-y-6">
                        <div className="bg-white dark:bg-slate-800 rounded-xl shadow-sm p-6">
                            <h3 className="font-bold text-gray-900 dark:text-white mb-4">Related Tags</h3>
                            <div className="flex flex-wrap gap-2">
                                {['javascript', 'react', 'csharp', 'python', 'sql', 'docker', 'nextjs'].map((t) => (
                                    <Link
                                        key={t}
                                        href={`/questions?tag=${t}`}
                                        className="px-3 py-1 border border-orange-500 text-orange-500 text-sm rounded-full hover:bg-orange-500 hover:text-white transition"
                                    >
                                        {t}
                                    </Link>
                                ))}
                            </div>
                        </div>

                        <div className="bg-white dark:bg-slate-800 rounded-xl shadow-sm p-6">
                            <h3 className="font-bold text-gray-900 dark:text-white mb-4">Looking for more?</h3>
                            <p className="text-gray-500 text-sm mb-4">
                                Browse our complete list of questions or ask your own
                            </p>
                            <Link
                                href="/questions/ask"
                                className="block w-full py-2 text-center bg-orange-500 text-white rounded-lg hover:bg-orange-600 transition"
                            >
                                Ask a Question
                            </Link>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
}

export default function QuestionsPage() {
    return (
        <Suspense fallback={
            <div className="min-h-screen flex items-center justify-center bg-gray-50 dark:bg-slate-900">
                <div className="inline-block w-8 h-8 border-4 border-orange-500 border-t-transparent rounded-full animate-spin"></div>
            </div>
        }>
            <QuestionsContent />
        </Suspense>
    );
}
