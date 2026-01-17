'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useAuth } from '@/lib/contexts/AuthContext';
import type { SavedItem } from '@/types';
import apiClient from '@/lib/api/client';

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
            const response = await apiClient.get<{ items: SavedItem[] }>('/saved');
            setSavedItems(response.data.items || []);
        } catch (error) {
            console.error('Failed to fetch saved items:', error);
        } finally {
            setIsLoading(false);
        }
    };

    const removeSavedItem = async (id: number) => {
        try {
            await apiClient.delete(`/saved/${id}`);
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
            <div className="container py-5 text-center">
                <div className="spinner-border text-primary" role="status">
                    <span className="visually-hidden">Loading...</span>
                </div>
            </div>
        );
    }

    if (!user) return null;

    return (
        <div className="container py-4">
            {/* Header */}
            <div className="d-flex justify-content-between align-items-center mb-4">
                <div>
                    <h1 className="fw-bold mb-1">
                        <i className="bi bi-bookmark me-2"></i>Saved Items
                    </h1>
                    <p className="text-muted mb-0">{savedItems.length} items saved</p>
                </div>
            </div>

            {/* Filters */}
            <div className="card border-0 shadow-sm rounded-4 mb-4">
                <div className="card-body p-3">
                    <div className="d-flex gap-2">
                        <button
                            className={`btn btn-sm rounded-pill ${filter === 'all' ? 'btn-primary' : 'btn-outline-secondary'}`}
                            onClick={() => setFilter('all')}
                        >
                            All ({savedItems.length})
                        </button>
                        <button
                            className={`btn btn-sm rounded-pill ${filter === 'questions' ? 'btn-primary' : 'btn-outline-secondary'}`}
                            onClick={() => setFilter('questions')}
                        >
                            <i className="bi bi-question-circle me-1"></i>
                            Questions ({savedItems.filter(i => i.targetType === 'Question').length})
                        </button>
                        <button
                            className={`btn btn-sm rounded-pill ${filter === 'answers' ? 'btn-primary' : 'btn-outline-secondary'}`}
                            onClick={() => setFilter('answers')}
                        >
                            <i className="bi bi-chat-left-text me-1"></i>
                            Answers ({savedItems.filter(i => i.targetType === 'Answer').length})
                        </button>
                    </div>
                </div>
            </div>

            {/* Saved Items List */}
            <div className="card border-0 shadow-sm rounded-4">
                {filteredItems.length > 0 ? (
                    <div className="list-group list-group-flush">
                        {filteredItems.map((item) => (
                            <div key={item.savedItemId} className="list-group-item p-4 border-0 border-bottom">
                                <div className="row align-items-center">
                                    <div className="col-auto">
                                        <div className={`rounded-3 p-2 ${item.targetType === 'Question' ? 'bg-primary-subtle' : 'bg-success-subtle'}`}>
                                            <i className={`bi ${item.targetType === 'Question' ? 'bi-question-circle text-primary' : 'bi-chat-left-text text-success'} fs-5`}></i>
                                        </div>
                                    </div>
                                    <div className="col">
                                        <span className="badge bg-light text-dark mb-2">{item.targetType}</span>
                                        <h6 className="mb-1 fw-bold">
                                            {item.targetType === 'Question' && item.question ? (
                                                <Link href={`/questions/${item.question.questionId}`} className="text-decoration-none text-dark">
                                                    {item.question.title}
                                                </Link>
                                            ) : item.targetType === 'Answer' && item.answer ? (
                                                <Link href={`/questions/${item.answer.questionId}#answer-${item.answer.answerId}`} className="text-decoration-none text-dark">
                                                    Answer to question
                                                </Link>
                                            ) : (
                                                <span>Saved item</span>
                                            )}
                                        </h6>
                                        <p className="text-muted small mb-0">
                                            {item.targetType === 'Question' && item.question
                                                ? stripHtml(item.question.body) + '...'
                                                : item.targetType === 'Answer' && item.answer
                                                    ? stripHtml(item.answer.body) + '...'
                                                    : ''
                                            }
                                        </p>
                                        <small className="text-muted">
                                            Saved on {new Date(item.createdDate).toLocaleDateString()}
                                        </small>
                                    </div>
                                    <div className="col-auto">
                                        <button
                                            className="btn btn-sm btn-outline-danger rounded-pill"
                                            onClick={() => removeSavedItem(item.savedItemId)}
                                        >
                                            <i className="bi bi-bookmark-x me-1"></i>Remove
                                        </button>
                                    </div>
                                </div>
                            </div>
                        ))}
                    </div>
                ) : (
                    <div className="card-body text-center py-5">
                        <i className="bi bi-bookmark fs-1 text-muted mb-3"></i>
                        <h5>No saved items</h5>
                        <p className="text-muted mb-4">
                            Save questions and answers to read later
                        </p>
                        <Link href="/questions" className="btn btn-primary rounded-pill">
                            Browse Questions
                        </Link>
                    </div>
                )}
            </div>
        </div>
    );
}
