'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useAuth } from '@/lib/contexts/AuthContext';
import type { SavedItem } from '@/types';
import apiClient from '@/lib/api/client';
import AppLayout from '@/components/AppLayout';

// Strip HTML helper
const stripHtml = (html: string): string => {
    if (!html) return '';
    return html.replace(/<[^>]*>/g, '').substring(0, 150);
};

export default function SavedItemsPage() {
    const { user, isLoading: authLoading } = useAuth();
    const router = useRouter();
    const [savedItems, setSavedItems] = useState<SavedItem[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [filter, setFilter] = useState<'all' | 'questions' | 'answers'>('all');

    useEffect(() => {
        if (!authLoading && !user) {
            router.push('/login');
        } else if (user) {
            fetchSavedItems();
        }
    }, [user, authLoading, router]);

    const fetchSavedItems = async () => {
        try {
            const response = await apiClient.get<{ items: SavedItem[] }>('/SavedItems');
            setSavedItems(response.data.items || []);
        } catch (error) {
            console.error('Failed to fetch saved items:', error);
        } finally {
            setIsLoading(false);
        }
    };

    const removeSavedItem = async (e: React.MouseEvent, id: number) => {
        e.preventDefault();
        e.stopPropagation();
        try {
            await apiClient.delete(`/SavedItems/${id}`);
            setSavedItems(prev => prev.filter(item => item.savedItemId !== id));
        } catch (error) {
            console.error('Failed to remove saved item:', error);
        }
    };

    const filteredItems = savedItems.filter(item => {
        if (filter === 'all') return true;
        if (filter === 'questions') return item.targetType === 'Question';
        if (filter === 'answers') return item.targetType === 'Answer';
        return true;
    });

    if (authLoading || isLoading) {
        return (
            <AppLayout showRightSidebar={false}>
                <div className="flex items-center justify-center py-20">
                    <div className="w-10 h-10 border-4 border-[var(--primary)]/30 border-t-[var(--primary)] rounded-full animate-spin"></div>
                </div>
            </AppLayout>
        );
    }

    if (!user) return null;

    return (
        <AppLayout showRightSidebar={false}>
            {/* Header */}
            <div className="mb-6">
                <h1 className="text-2xl font-bold text-[var(--text-primary)] flex items-center gap-2">
                    <span className="material-symbols-outlined text-[var(--primary)]">bookmark</span>
                    Saved Items
                </h1>
                <p className="text-[var(--text-muted)]">Your personal collection of questions and answers</p>
            </div>

            {/* Filters */}
            <div className="flex gap-2 mb-6">
                <button
                    onClick={() => setFilter('all')}
                    className={`px-4 py-2 rounded-xl text-sm font-medium transition ${filter === 'all'
                            ? 'bg-[var(--primary)] text-white'
                            : 'bg-[var(--bg-secondary)] text-[var(--text-muted)] border border-[var(--border-color)] hover:border-[var(--primary)]'
                        }`}
                >
                    All ({savedItems.length})
                </button>
                <button
                    onClick={() => setFilter('questions')}
                    className={`px-4 py-2 rounded-xl text-sm font-medium transition flex items-center gap-2 ${filter === 'questions'
                            ? 'bg-[var(--primary)] text-white'
                            : 'bg-[var(--bg-secondary)] text-[var(--text-muted)] border border-[var(--border-color)] hover:border-[var(--primary)]'
                        }`}
                >
                    <span className="material-symbols-outlined">help</span>
                    Questions ({savedItems.filter(i => i.targetType === 'Question').length})
                </button>
                <button
                    onClick={() => setFilter('answers')}
                    className={`px-4 py-2 rounded-xl text-sm font-medium transition flex items-center gap-2 ${filter === 'answers'
                            ? 'bg-[var(--primary)] text-white'
                            : 'bg-[var(--bg-secondary)] text-[var(--text-muted)] border border-[var(--border-color)] hover:border-[var(--primary)]'
                        }`}
                >
                    <span className="material-symbols-outlined">chat</span>
                    Answers ({savedItems.filter(i => i.targetType === 'Answer').length})
                </button>
            </div>

            {/* Saved Items List */}
            {filteredItems.length > 0 ? (
                <div className="space-y-4">
                    {filteredItems.map((item) => (
                        <Link
                            key={item.savedItemId}
                            href={item.targetType === 'Question' && item.question ? `/questions/${item.question.questionId}` : item.targetType === 'Answer' && item.answer ? `/questions/${item.answer.questionId}#answer-${item.answer.answerId}` : '#'}
                            className="block bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-2xl p-5 hover:border-[var(--primary)]/50 transition group"
                        >
                            <div className="flex items-start gap-4">
                                <div className={`w-12 h-12 rounded-xl flex items-center justify-center text-xl shrink-0 ${item.targetType === 'Question'
                                        ? 'bg-blue-500/10 text-blue-500'
                                        : 'bg-green-500/10 text-green-500'
                                    }`}>
                                    <span className="material-symbols-outlined">{item.targetType === 'Question' ? 'help' : 'chat'}</span>
                                </div>
                                <div className="flex-1 min-w-0">
                                    <div className="flex items-start justify-between">
                                        <div>
                                            <span className={`inline-block px-2 py-0.5 rounded text-xs font-bold mb-2 ${item.targetType === 'Question' ? 'bg-blue-500/10 text-blue-500' : 'bg-green-500/10 text-green-500'
                                                }`}>
                                                {item.targetType}
                                            </span>
                                            <h3 className="font-bold text-[var(--text-primary)] mb-1 group-hover:text-[var(--primary)] line-clamp-1">
                                                {item.targetType === 'Question' && item.question ? (
                                                    item.question.title
                                                ) : item.targetType === 'Answer' && item.answer ? (
                                                    'Answer to a question'
                                                ) : (
                                                    'Saved item'
                                                )}
                                            </h3>
                                        </div>
                                        <button
                                            onClick={(e) => removeSavedItem(e, item.savedItemId)}
                                            className="p-2 text-[var(--text-muted)] hover:text-red-500 hover:bg-red-500/10 rounded-lg transition"
                                            title="Remove from saved"
                                        >
                                            <span className="material-symbols-outlined text-lg">bookmark_remove</span>
                                        </button>
                                    </div>
                                    <p className="text-sm text-[var(--text-secondary)] line-clamp-2 mb-3">
                                        {item.targetType === 'Question' && item.question
                                            ? stripHtml(item.question.body)
                                            : item.targetType === 'Answer' && item.answer
                                                ? stripHtml(item.answer.body)
                                                : ''
                                        }
                                    </p>
                                    <div className="text-xs text-[var(--text-muted)] flex items-center gap-1">
                                        <span className="material-symbols-outlined">schedule</span>
                                        Saved on {new Date(item.createdDate).toLocaleDateString()}
                                    </div>
                                </div>
                            </div>
                        </Link>
                    ))}
                </div>
            ) : (
                <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-2xl p-12 text-center">
                    <span className="material-symbols-outlined text-5xl text-[var(--text-muted)] mb-4">bookmark</span>
                    <h3 className="text-lg font-semibold text-[var(--text-primary)] mb-2">No saved items found</h3>
                    <p className="text-[var(--text-muted)] mb-6">
                        {filter === 'all'
                            ? "You haven't saved any items yet."
                            : `You haven't saved any ${filter} yet.`}
                    </p>
                    <Link href="/questions" className="inline-block px-6 py-2 bg-[var(--primary)] text-white rounded-xl font-medium hover:bg-[var(--primary-dark)] transition">
                        Browse Questions
                    </Link>
                </div>
            )}
        </AppLayout>
    );
}