'use client';

import { useEffect, useState, Suspense } from 'react';
import { useSearchParams } from 'next/navigation';
import Link from 'next/link';
import AppLayout from '@/components/AppLayout';
import apiClient from '@/lib/api/client';

interface QuestionResult {
    questionId: number;
    title: string;
    score: number;
    answerCount: number;
    viewCount: number;
    authorName: string;
    createdDate: string;
    tags: string[];
}

interface UserResult {
    userId: number;
    username: string;
    displayName: string | null;
    profilePicture: string | null;
    reputationPoints: number;
}

interface TagResult {
    tagId: number;
    tagName: string;
    description: string | null;
    questionCount: number;
}

interface SearchResponse {
    questions: QuestionResult[];
    users: UserResult[];
    tags: TagResult[];
}

function SearchContent() {
    const searchParams = useSearchParams();
    const query = searchParams.get('q') || '';
    const [results, setResults] = useState<SearchResponse>({ questions: [], users: [], tags: [] });
    const [loading, setLoading] = useState(false);
    const [activeTab, setActiveTab] = useState<'all' | 'questions' | 'users' | 'tags'>('all');

    useEffect(() => {
        if (query) {
            fetchResults();
        }
    }, [query]);

    const fetchResults = async () => {
        setLoading(true);
        try {
            const response = await apiClient.get<SearchResponse>(`/Search?q=${encodeURIComponent(query)}`);
            setResults(response.data);
        } catch (error) {
            console.error('Search failed:', error);
        } finally {
            setLoading(false);
        }
    };

    const tabs = [
        { id: 'all', label: 'All Results', count: results.questions.length + results.users.length + results.tags.length },
        { id: 'questions', label: 'Questions', count: results.questions.length },
        { id: 'users', label: 'Users', count: results.users.length },
        { id: 'tags', label: 'Tags', count: results.tags.length },
    ];

    if (!query) {
        return (
            <div className="text-center py-20">
                <div className="inline-block p-4 rounded-full bg-[var(--bg-secondary)] border border-[var(--border-color)] mb-4 shadow-sm">
                    <i className="bi bi-search text-4xl text-[var(--primary)]"></i>
                </div>
                <h2 className="text-2xl font-bold text-[var(--text-primary)] mb-2">Search DevCommunity</h2>
                <p className="text-[var(--text-secondary)]">Enter a keyword to find questions, users, or topics.</p>
            </div>
        );
    }

    if (loading) {
        return (
            <div className="flex items-center justify-center py-20">
                <div className="w-10 h-10 border-4 border-[var(--primary)]/30 border-t-[var(--primary)] rounded-full animate-spin"></div>
            </div>
        );
    }

    const hasResults = results.questions.length > 0 || results.users.length > 0 || results.tags.length > 0;

    return (
        <div>
            <div className="mb-6">
                <h1 className="text-2xl font-bold text-[var(--text-primary)] mb-1">
                    Search results for &quot;<span className="text-[var(--primary)]">{query}</span>&quot;
                </h1>
                <p className="text-[var(--text-muted)]">
                    Found {results.questions.length + results.users.length + results.tags.length} items
                </p>
            </div>

            {/* Tabs */}
            <div className="flex gap-2 overflow-x-auto pb-4 mb-6 scrollbar-hide">
                {tabs.map(tab => (
                    <button
                        key={tab.id}
                        onClick={() => setActiveTab(tab.id as any)}
                        className={`flex items-center gap-2 px-4 py-2 rounded-xl font-medium whitespace-nowrap transition ${activeTab === tab.id
                                ? 'bg-[var(--primary)] text-white shadow-lg shadow-[var(--primary)]/25'
                                : 'bg-[var(--bg-secondary)] text-[var(--text-secondary)] hover:bg-[var(--bg-hover)]'
                            }`}
                    >
                        {tab.label}
                        <span className={`px-1.5 py-0.5 rounded text-xs ${activeTab === tab.id ? 'bg-white/20' : 'bg-[var(--bg-tertiary)]'}`}>
                            {tab.count}
                        </span>
                    </button>
                ))}
            </div>

            {hasResults ? (
                <div className="space-y-8">
                    {/* Questions Section */}
                    {(activeTab === 'all' || activeTab === 'questions') && results.questions.length > 0 && (
                        <div className="space-y-4">
                            {activeTab === 'all' && (
                                <h3 className="text-lg font-bold text-[var(--text-primary)] flex items-center gap-2">
                                    <i className="bi bi-question-circle-fill text-blue-500"></i> Questions
                                </h3>
                            )}
                            <div className="grid gap-4">
                                {results.questions.map(q => (
                                    <div key={q.questionId} className="card p-5 hover:border-[var(--primary)]/50 transition group">
                                        <div className="flex items-start gap-4">
                                            <div className="flex flex-col items-center gap-2 text-sm min-w-[60px] text-[var(--text-secondary)]">
                                                <div className="flex flex-col items-center">
                                                    <span className="font-bold text-lg">{q.score}</span>
                                                    <span className="text-xs">votes</span>
                                                </div>
                                                <div className={`px-2 py-1 rounded-lg ${q.answerCount > 0 ? 'bg-green-500/10 text-green-500 border border-green-500/20' : 'bg-[var(--bg-tertiary)]'}`}>
                                                    <span className="font-bold">{q.answerCount}</span>
                                                    <span className="text-xs ml-1">ans</span>
                                                </div>
                                            </div>
                                            <div className="flex-1 min-w-0">
                                                <Link href={`/questions/${q.questionId}`} className="block mb-1">
                                                    <h3 className="text-lg font-bold text-[var(--text-primary)] group-hover:text-[var(--primary)] line-clamp-2 transition">
                                                        {q.title}
                                                    </h3>
                                                </Link>
                                                <div className="flex flex-wrap gap-2 mb-3">
                                                    {q.tags.map(tag => (
                                                        <span key={tag} className="tag">{tag}</span>
                                                    ))}
                                                </div>
                                                <div className="flex items-center gap-2 text-xs text-[var(--text-muted)]">
                                                    <span>Asked by <span className="text-[var(--text-primary)] font-medium">{q.authorName}</span></span>
                                                    <span>•</span>
                                                    <span>{new Date(q.createdDate).toLocaleDateString()}</span>
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                ))}
                            </div>
                        </div>
                    )}

                    {/* Users Section */}
                    {(activeTab === 'all' || activeTab === 'users') && results.users.length > 0 && (
                        <div className="space-y-4">
                            {activeTab === 'all' && (
                                <h3 className="text-lg font-bold text-[var(--text-primary)] flex items-center gap-2">
                                    <i className="bi bi-people-fill text-purple-500"></i> Users
                                </h3>
                            )}
                            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                                {results.users.map(user => (
                                    <div key={user.userId} className="card p-4 hover:border-[var(--primary)]/50 transition flex items-center gap-3">
                                        <div className="w-12 h-12 rounded-full bg-gradient-to-br from-purple-500 to-pink-500 flex items-center justify-center text-white font-bold text-lg">
                                            {user.profilePicture ? (
                                                <img src={user.profilePicture} alt={user.username} className="w-full h-full rounded-full object-cover" />
                                            ) : (
                                                user.username.charAt(0).toUpperCase()
                                            )}
                                        </div>
                                        <div>
                                            <Link href={`/users/${user.userId}`} className="font-bold text-[var(--text-primary)] hover:text-[var(--primary)] block">
                                                {user.displayName || user.username}
                                            </Link>
                                            <div className="flex items-center gap-2 text-sm text-[var(--text-muted)]">
                                                <i className="bi bi-trophy-fill text-yellow-500 text-xs"></i>
                                                <span>{user.reputationPoints} reputation</span>
                                            </div>
                                        </div>
                                    </div>
                                ))}
                            </div>
                        </div>
                    )}

                    {/* Tags Section */}
                    {(activeTab === 'all' || activeTab === 'tags') && results.tags.length > 0 && (
                        <div className="space-y-4">
                            {activeTab === 'all' && (
                                <h3 className="text-lg font-bold text-[var(--text-primary)] flex items-center gap-2">
                                    <i className="bi bi-tags-fill text-orange-500"></i> Tags
                                </h3>
                            )}
                            <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4">
                                {results.tags.map(tag => (
                                    <div key={tag.tagId} className="card p-4 hover:border-[var(--primary)]/50 transition">
                                        <div className="flex items-center justify-between mb-2">
                                            <span className="tag text-sm">{tag.tagName}</span>
                                            <span className="text-xs text-[var(--text-muted)]">{tag.questionCount} posts</span>
                                        </div>
                                        <p className="text-xs text-[var(--text-secondary)] line-clamp-2">
                                            {tag.description || 'No description available for this tag.'}
                                        </p>
                                    </div>
                                ))}
                            </div>
                        </div>
                    )}
                </div>
            ) : (
                <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-2xl p-16 text-center">
                    <div className="w-20 h-20 bg-[var(--bg-tertiary)] rounded-full flex items-center justify-center mx-auto mb-6">
                        <i className="bi bi-search text-3xl text-[var(--text-muted)]"></i>
                    </div>
                    <h3 className="text-xl font-bold text-[var(--text-primary)] mb-2">No results found</h3>
                    <p className="text-[var(--text-secondary)] max-w-md mx-auto">
                        We couldn&apos;t find any matches for &quot;{query}&quot;. Try adjusting your search keywords or checking for typos.
                    </p>
                </div>
            )}
        </div>
    );
}

export default function SearchPage() {
    return (
        <AppLayout>
            <Suspense fallback={
                <div className="flex items-center justify-center py-20">
                    <div className="w-10 h-10 border-4 border-[var(--primary)]/30 border-t-[var(--primary)] rounded-full animate-spin"></div>
                </div>
            }>
                <SearchContent />
            </Suspense>
        </AppLayout>
    );
}
