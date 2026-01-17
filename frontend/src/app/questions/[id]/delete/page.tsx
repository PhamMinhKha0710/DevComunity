'use client';

import { useEffect, useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
import Link from 'next/link';
import { useAuth } from '@/lib/contexts/AuthContext';
import type { Question } from '@/types';
import apiClient from '@/lib/api/client';

export default function DeleteQuestionPage() {
    const params = useParams();
    const router = useRouter();
    const { user } = useAuth();
    const questionId = params.id as string;

    const [question, setQuestion] = useState<Question | null>(null);
    const [isLoading, setIsLoading] = useState(true);
    const [isDeleting, setIsDeleting] = useState(false);
    const [error, setError] = useState('');
    const [confirmText, setConfirmText] = useState('');

    useEffect(() => {
        fetchQuestion();
    }, [questionId]);

    const fetchQuestion = async () => {
        try {
            const response = await apiClient.get<Question>(`/questions/${questionId}`);
            setQuestion(response.data);
        } catch (err) {
            setError('Failed to load question');
        } finally {
            setIsLoading(false);
        }
    };

    const handleDelete = async () => {
        if (confirmText !== 'DELETE') {
            setError('Please type DELETE to confirm');
            return;
        }

        setIsDeleting(true);
        setError('');

        try {
            await apiClient.delete(`/questions/${questionId}`);
            router.push('/questions?deleted=true');
        } catch (err: any) {
            setError(err.response?.data?.message || 'Failed to delete question');
        } finally {
            setIsDeleting(false);
        }
    };

    if (isLoading) {
        return (
            <div className="container py-5 text-center">
                <div className="spinner-border text-primary" role="status">
                    <span className="visually-hidden">Loading...</span>
                </div>
            </div>
        );
    }

    if (!question) {
        return (
            <div className="container py-5 text-center">
                <i className="bi bi-exclamation-triangle fs-1 text-warning mb-3"></i>
                <h3>Question not found</h3>
                <Link href="/questions" className="btn btn-primary mt-3">Back to Questions</Link>
            </div>
        );
    }

    return (
        <div className="container py-4">
            <div className="row justify-content-center">
                <div className="col-lg-8">
                    {/* Breadcrumb */}
                    <nav aria-label="breadcrumb" className="mb-4">
                        <ol className="breadcrumb">
                            <li className="breadcrumb-item"><Link href="/">Home</Link></li>
                            <li className="breadcrumb-item"><Link href="/questions">Questions</Link></li>
                            <li className="breadcrumb-item"><Link href={`/questions/${questionId}`}>{question.title.substring(0, 30)}...</Link></li>
                            <li className="breadcrumb-item active">Delete</li>
                        </ol>
                    </nav>

                    <div className="card border-danger rounded-4 shadow-sm">
                        <div className="card-header bg-danger text-white py-3 rounded-top-4">
                            <h1 className="card-title fs-4 fw-bold mb-0">
                                <i className="bi bi-exclamation-triangle-fill me-2"></i>Delete Question
                            </h1>
                        </div>
                        <div className="card-body p-4">
                            <div className="alert alert-danger d-flex align-items-start mb-4">
                                <i className="bi bi-exclamation-triangle-fill fs-4 me-3"></i>
                                <div>
                                    <h5 className="alert-heading">Warning: This action cannot be undone!</h5>
                                    <p className="mb-0">Once you delete this question, all associated answers, comments, and votes will be permanently removed.</p>
                                </div>
                            </div>

                            {/* Question Preview */}
                            <div className="card bg-light border-0 rounded-3 mb-4">
                                <div className="card-body">
                                    <h5 className="fw-bold mb-3">{question.title}</h5>
                                    <p className="text-muted mb-3" style={{ maxHeight: '100px', overflow: 'hidden' }}>
                                        {question.bodyExcerpt || question.body.substring(0, 200)}...
                                    </p>
                                    <div className="d-flex gap-3 text-muted small">
                                        <span><i className="bi bi-eye me-1"></i>{question.viewCount} views</span>
                                        <span><i className="bi bi-hand-thumbs-up me-1"></i>{question.score} votes</span>
                                        <span><i className="bi bi-chat-left-text me-1"></i>{question.answerCount} answers</span>
                                    </div>
                                </div>
                            </div>

                            {error && (
                                <div className="alert alert-danger" role="alert">
                                    <i className="bi bi-exclamation-circle me-2"></i>{error}
                                </div>
                            )}

                            {/* Confirmation Input */}
                            <div className="mb-4">
                                <label className="form-label fw-semibold">
                                    To confirm, type <code>DELETE</code> in the box below:
                                </label>
                                <input
                                    type="text"
                                    className="form-control"
                                    value={confirmText}
                                    onChange={(e) => setConfirmText(e.target.value.toUpperCase())}
                                    placeholder="Type DELETE to confirm"
                                />
                            </div>

                            {/* Actions */}
                            <div className="d-flex justify-content-between">
                                <Link href={`/questions/${questionId}`} className="btn btn-outline-secondary rounded-pill">
                                    <i className="bi bi-arrow-left me-1"></i>Cancel
                                </Link>
                                <button
                                    type="button"
                                    className="btn btn-danger rounded-pill px-4"
                                    onClick={handleDelete}
                                    disabled={isDeleting || confirmText !== 'DELETE'}
                                >
                                    {isDeleting ? (
                                        <>
                                            <span className="spinner-border spinner-border-sm me-2"></span>
                                            Deleting...
                                        </>
                                    ) : (
                                        <>
                                            <i className="bi bi-trash me-1"></i>Delete Question
                                        </>
                                    )}
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
}
