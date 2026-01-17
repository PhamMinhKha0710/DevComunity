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
        <div className="container py-4">
            <div className="row">
                {/* Main Content */}
                <div className="col-lg-9">
                    {/* Header */}
                    <div className="d-flex justify-content-between align-items-center mb-4">
                        <div>
                            <h2 className="fw-bold mb-1">
                                {tag ? `Questions tagged [${tag}]` : search ? `Search results for "${search}"` : 'All Questions'}
                            </h2>
                            <p className="text-muted small mb-0">
                                {questions.length} questions found
                            </p>
                        </div>
                        <Link href="/questions/ask" className="btn btn-primary rounded-pill">
                            <i className="bi bi-plus-circle me-2"></i>Ask Question
                        </Link>
                    </div>

                    {/* Filters */}
                    <div className="card border-0 shadow-sm rounded-4 mb-4">
                        <div className="card-body p-3">
                            <div className="d-flex gap-2 flex-wrap">
                                <button
                                    className={`btn btn-sm ${sortBy === 'newest' ? 'btn-primary' : 'btn-outline-secondary'} rounded-pill`}
                                    onClick={() => setSortBy('newest')}
                                >
                                    <i className="bi bi-clock me-1"></i>Newest
                                </button>
                                <button
                                    className={`btn btn-sm ${sortBy === 'active' ? 'btn-primary' : 'btn-outline-secondary'} rounded-pill`}
                                    onClick={() => setSortBy('active')}
                                >
                                    <i className="bi bi-activity me-1"></i>Active
                                </button>
                                <button
                                    className={`btn btn-sm ${sortBy === 'unanswered' ? 'btn-primary' : 'btn-outline-secondary'} rounded-pill`}
                                    onClick={() => setSortBy('unanswered')}
                                >
                                    <i className="bi bi-question-circle me-1"></i>Unanswered
                                </button>
                                <button
                                    className={`btn btn-sm ${sortBy === 'votes' ? 'btn-primary' : 'btn-outline-secondary'} rounded-pill`}
                                    onClick={() => setSortBy('votes')}
                                >
                                    <i className="bi bi-graph-up me-1"></i>Most Votes
                                </button>
                            </div>
                        </div>
                    </div>

                    {/* Questions List */}
                    <div className="card border-0 shadow-sm rounded-4">
                        {isLoading ? (
                            <div className="card-body p-5 text-center">
                                <div className="spinner-border text-primary" role="status">
                                    <span className="visually-hidden">Loading...</span>
                                </div>
                            </div>
                        ) : questions.length > 0 ? (
                            <div className="list-group list-group-flush">
                                {questions.map((question) => (
                                    <div key={question.questionId} className="list-group-item p-4 border-0 border-bottom">
                                        <div className="row align-items-center">
                                            <div className="col-auto d-none d-sm-block">
                                                <div className="question-stats d-flex flex-column align-items-center text-center" style={{ minWidth: '80px' }}>
                                                    <div className={`votes mb-2 py-2 px-3 rounded-pill ${question.score > 0 ? 'bg-success-subtle' : 'bg-light'}`}>
                                                        <span className={`fw-bold ${question.score > 0 ? 'text-success' : ''}`}>{question.score}</span>
                                                        <small className="d-block">votes</small>
                                                    </div>
                                                    <div className={`answers py-2 px-3 rounded-pill ${question.hasAcceptedAnswer ? 'bg-success' : question.answerCount > 0 ? 'bg-primary-subtle' : 'bg-light'}`}>
                                                        <span className={`fw-bold ${question.hasAcceptedAnswer ? 'text-white' : question.answerCount > 0 ? 'text-primary' : ''}`}>
                                                            {question.answerCount}
                                                        </span>
                                                        <small className={`d-block ${question.hasAcceptedAnswer ? 'text-white' : ''}`}>answers</small>
                                                    </div>
                                                </div>
                                            </div>
                                            <div className="col">
                                                <h5 className="mb-2 fw-bold">
                                                    <Link href={`/questions/${question.questionId}`} className="text-decoration-none text-dark">
                                                        {question.title}
                                                    </Link>
                                                </h5>
                                                <p className="mb-3 text-muted small">
                                                    {stripHtml(question.bodyExcerpt || question.body || '')}...
                                                </p>
                                                <div className="d-flex flex-wrap justify-content-between align-items-center">
                                                    <div className="tags mb-2 mb-sm-0">
                                                        {question.tags?.slice(0, 5).map((t: Tag) => (
                                                            <Link key={t.tagId} href={`/questions?tag=${t.tagName}`} className="badge rounded-pill bg-light text-dark me-1 text-decoration-none">
                                                                {t.tagName}
                                                            </Link>
                                                        ))}
                                                    </div>
                                                    <div className="small text-muted">
                                                        <img src={question.authorProfilePicture || '/images/default-avatar.png'} className="rounded-circle me-1" width="20" height="20" alt="" />
                                                        asked {new Date(question.createdDate).toLocaleDateString()} by <span className="text-primary">{question.authorUsername}</span>
                                                    </div>
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                ))}
                            </div>
                        ) : (
                            <div className="card-body p-5 text-center">
                                <i className="bi bi-search fs-1 text-muted mb-3"></i>
                                <h5>No questions found</h5>
                                <p className="text-muted">Try adjusting your search or filters</p>
                            </div>
                        )}

                        {/* Pagination */}
                        {totalPages > 1 && (
                            <div className="card-footer bg-transparent border-0 p-3">
                                <nav>
                                    <ul className="pagination justify-content-center mb-0">
                                        <li className={`page-item ${page === 1 ? 'disabled' : ''}`}>
                                            <button className="page-link" onClick={() => setPage(p => p - 1)}>Previous</button>
                                        </li>
                                        {[...Array(Math.min(5, totalPages))].map((_, i) => (
                                            <li key={i} className={`page-item ${page === i + 1 ? 'active' : ''}`}>
                                                <button className="page-link" onClick={() => setPage(i + 1)}>{i + 1}</button>
                                            </li>
                                        ))}
                                        <li className={`page-item ${page === totalPages ? 'disabled' : ''}`}>
                                            <button className="page-link" onClick={() => setPage(p => p + 1)}>Next</button>
                                        </li>
                                    </ul>
                                </nav>
                            </div>
                        )}
                    </div>
                </div>

                {/* Sidebar */}
                <div className="col-lg-3">
                    <div className="card border-0 shadow-sm rounded-4 mb-4">
                        <div className="card-body">
                            <h6 className="fw-bold mb-3">Related Tags</h6>
                            <div className="d-flex flex-wrap gap-2">
                                <Link href="/questions?tag=javascript" className="btn btn-sm btn-outline-primary rounded-pill">javascript</Link>
                                <Link href="/questions?tag=react" className="btn btn-sm btn-outline-primary rounded-pill">react</Link>
                                <Link href="/questions?tag=csharp" className="btn btn-sm btn-outline-primary rounded-pill">c#</Link>
                                <Link href="/questions?tag=python" className="btn btn-sm btn-outline-primary rounded-pill">python</Link>
                                <Link href="/questions?tag=sql" className="btn btn-sm btn-outline-primary rounded-pill">sql</Link>
                            </div>
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
            <div className="container py-5 text-center">
                <div className="spinner-border text-primary" role="status">
                    <span className="visually-hidden">Loading...</span>
                </div>
            </div>
        }>
            <QuestionsContent />
        </Suspense>
    );
}
