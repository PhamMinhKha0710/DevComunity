'use client';

import { useEffect, useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
import Link from 'next/link';
import { useAuth } from '@/lib/contexts/AuthContext';
import type { Question } from '@/types';
import apiClient from '@/lib/api/client';

export default function EditQuestionPage() {
    const params = useParams();
    const router = useRouter();
    const { user } = useAuth();
    const questionId = params.id as string;

    const [isLoading, setIsLoading] = useState(true);
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [error, setError] = useState('');
    const [formData, setFormData] = useState({
        title: '',
        body: '',
        tags: '',
    });

    useEffect(() => {
        fetchQuestion();
    }, [questionId]);

    const fetchQuestion = async () => {
        try {
            const response = await apiClient.get<Question>(`/questions/${questionId}`);
            const question = response.data;
            setFormData({
                title: question.title,
                body: question.body,
                tags: question.tags?.map(t => t.tagName).join(', ') || '',
            });
        } catch (err) {
            setError('Failed to load question');
        } finally {
            setIsLoading(false);
        }
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setIsSubmitting(true);
        setError('');

        try {
            await apiClient.put(`/questions/${questionId}`, {
                title: formData.title,
                body: formData.body,
                tags: formData.tags.split(',').map(t => t.trim()).filter(Boolean),
            });
            router.push(`/questions/${questionId}`);
        } catch (err: any) {
            setError(err.response?.data?.message || 'Failed to update question');
        } finally {
            setIsSubmitting(false);
        }
    };

    const insertMarkdown = (syntax: string) => {
        const textarea = document.querySelector('textarea[name="body"]') as HTMLTextAreaElement;
        if (!textarea) return;

        const start = textarea.selectionStart;
        const end = textarea.selectionEnd;
        const text = textarea.value;
        const before = text.substring(0, start);
        const selection = text.substring(start, end);
        const after = text.substring(end);

        let newText = '';
        if (selection) {
            if (syntax.includes('**')) {
                newText = before + `**${selection}**` + after;
            } else if (syntax.includes('*')) {
                newText = before + `*${selection}*` + after;
            } else if (syntax.includes('`')) {
                newText = before + `\`${selection}\`` + after;
            } else {
                newText = before + syntax + selection + after;
            }
        } else {
            newText = before + syntax + after;
        }

        setFormData({ ...formData, body: newText });
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

    return (
        <div className="container py-4">
            {/* Breadcrumb */}
            <nav aria-label="breadcrumb" className="mb-4">
                <ol className="breadcrumb">
                    <li className="breadcrumb-item"><Link href="/">Home</Link></li>
                    <li className="breadcrumb-item"><Link href="/questions">Questions</Link></li>
                    <li className="breadcrumb-item active">Edit Question</li>
                </ol>
            </nav>

            <div className="row">
                {/* Main Form */}
                <div className="col-lg-8">
                    <div className="card border-0 shadow-sm rounded-4 mb-4">
                        <div className="card-header bg-primary text-white py-3 rounded-top-4">
                            <h1 className="card-title fs-4 fw-bold mb-0">
                                <i className="bi bi-pencil-square me-2"></i>Edit Your Question
                            </h1>
                        </div>
                        <div className="card-body p-4">
                            {error && (
                                <div className="alert alert-danger" role="alert">
                                    <i className="bi bi-exclamation-circle me-2"></i>{error}
                                </div>
                            )}

                            <form onSubmit={handleSubmit}>
                                {/* Title */}
                                <div className="mb-4">
                                    <label className="form-label fw-medium">
                                        <i className="bi bi-type-h1 me-1 text-primary"></i>Title
                                    </label>
                                    <input
                                        type="text"
                                        className="form-control form-control-lg rounded-3"
                                        placeholder="What's your question? Be specific."
                                        value={formData.title}
                                        onChange={(e) => setFormData({ ...formData, title: e.target.value })}
                                        required
                                    />
                                    <div className="form-text mt-2">
                                        <i className="bi bi-info-circle text-primary me-1"></i>
                                        Be specific and imagine you're asking a question to another person.
                                    </div>
                                </div>

                                {/* Body with Markdown Toolbar */}
                                <div className="mb-4">
                                    <label className="form-label fw-medium">
                                        <i className="bi bi-textarea-t me-1 text-primary"></i>Body
                                    </label>
                                    <div className="border rounded-3 overflow-hidden shadow-sm">
                                        <div className="bg-light p-2 border-bottom d-flex align-items-center gap-1">
                                            <button type="button" className="btn btn-sm btn-light" onClick={() => insertMarkdown('**bold**')}>
                                                <i className="bi bi-type-bold"></i>
                                            </button>
                                            <button type="button" className="btn btn-sm btn-light" onClick={() => insertMarkdown('*italic*')}>
                                                <i className="bi bi-type-italic"></i>
                                            </button>
                                            <button type="button" className="btn btn-sm btn-light" onClick={() => insertMarkdown('# ')}>
                                                <i className="bi bi-type-h1"></i>
                                            </button>
                                            <button type="button" className="btn btn-sm btn-light" onClick={() => insertMarkdown('## ')}>
                                                <i className="bi bi-type-h2"></i>
                                            </button>
                                            <button type="button" className="btn btn-sm btn-light" onClick={() => insertMarkdown('`code`')}>
                                                <i className="bi bi-code"></i>
                                            </button>
                                            <button type="button" className="btn btn-sm btn-light" onClick={() => insertMarkdown('[Link](url)')}>
                                                <i className="bi bi-link"></i>
                                            </button>
                                            <button type="button" className="btn btn-sm btn-light" onClick={() => insertMarkdown('- ')}>
                                                <i className="bi bi-list-ul"></i>
                                            </button>
                                            <button type="button" className="btn btn-sm btn-light" onClick={() => insertMarkdown('1. ')}>
                                                <i className="bi bi-list-ol"></i>
                                            </button>
                                        </div>
                                        <textarea
                                            name="body"
                                            className="form-control border-0"
                                            rows={12}
                                            placeholder="Include all the information someone would need to answer your question"
                                            value={formData.body}
                                            onChange={(e) => setFormData({ ...formData, body: e.target.value })}
                                            required
                                        />
                                    </div>
                                    <div className="form-text mt-2">
                                        <i className="bi bi-markdown text-primary me-1"></i>
                                        Supports Markdown formatting.
                                    </div>
                                </div>

                                {/* Tags */}
                                <div className="mb-4">
                                    <label className="form-label fw-medium">
                                        <i className="bi bi-tags-fill me-1 text-primary"></i>Tags
                                    </label>
                                    <div className="input-group shadow-sm rounded-3 overflow-hidden">
                                        <span className="input-group-text bg-light border-end-0">
                                            <i className="bi bi-tags"></i>
                                        </span>
                                        <input
                                            type="text"
                                            className="form-control border-start-0"
                                            placeholder="e.g. javascript, react, node.js (comma separated)"
                                            value={formData.tags}
                                            onChange={(e) => setFormData({ ...formData, tags: e.target.value })}
                                        />
                                    </div>
                                    <div className="form-text mt-2">
                                        <i className="bi bi-info-circle text-primary me-1"></i>
                                        Add up to 5 tags to describe what your question is about.
                                    </div>
                                </div>

                                {/* Actions */}
                                <div className="d-flex justify-content-end gap-2 mt-4">
                                    <Link href={`/questions/${questionId}`} className="btn btn-outline-secondary rounded-pill">
                                        <i className="bi bi-x-lg me-1"></i>Cancel
                                    </Link>
                                    <button type="submit" className="btn btn-success btn-lg rounded-pill" disabled={isSubmitting}>
                                        {isSubmitting ? (
                                            <>
                                                <span className="spinner-border spinner-border-sm me-2"></span>
                                                Saving...
                                            </>
                                        ) : (
                                            <>
                                                <i className="bi bi-check-lg me-1"></i>Save Changes
                                            </>
                                        )}
                                    </button>
                                </div>
                            </form>
                        </div>
                    </div>
                </div>

                {/* Sidebar Tips */}
                <div className="col-lg-4">
                    <div className="card border-0 shadow-sm rounded-4 mb-4">
                        <div className="card-header bg-info text-white py-3 rounded-top-4">
                            <h5 className="mb-0 fw-bold"><i className="bi bi-lightbulb-fill me-2"></i>Editing Tips</h5>
                        </div>
                        <div className="card-body p-4">
                            <div className="mb-3 p-3 bg-light rounded-3">
                                <div className="d-flex">
                                    <div className="rounded-circle bg-primary text-white p-2 me-3 d-flex align-items-center justify-content-center" style={{ width: 36, height: 36 }}>
                                        <i className="bi bi-brightness-high"></i>
                                    </div>
                                    <div>
                                        <h6 className="fw-bold mb-1">Improve clarity</h6>
                                        <p className="text-muted small mb-0">Make your question clearer and more focused.</p>
                                    </div>
                                </div>
                            </div>
                            <div className="mb-3 p-3 bg-light rounded-3">
                                <div className="d-flex">
                                    <div className="rounded-circle bg-primary text-white p-2 me-3 d-flex align-items-center justify-content-center" style={{ width: 36, height: 36 }}>
                                        <i className="bi bi-list-check"></i>
                                    </div>
                                    <div>
                                        <h6 className="fw-bold mb-1">Add relevant details</h6>
                                        <p className="text-muted small mb-0">Include any additional context that could help.</p>
                                    </div>
                                </div>
                            </div>
                            <div className="p-3 bg-light rounded-3">
                                <div className="d-flex">
                                    <div className="rounded-circle bg-primary text-white p-2 me-3 d-flex align-items-center justify-content-center" style={{ width: 36, height: 36 }}>
                                        <i className="bi bi-text-paragraph"></i>
                                    </div>
                                    <div>
                                        <h6 className="fw-bold mb-1">Use proper formatting</h6>
                                        <p className="text-muted small mb-0">Format code, use headings for readability.</p>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>

                    <div className="card border-0 shadow-sm rounded-4">
                        <div className="card-header bg-success text-white py-3 rounded-top-4">
                            <h5 className="mb-0 fw-bold"><i className="bi bi-check2-circle me-2"></i>Editing Etiquette</h5>
                        </div>
                        <div className="card-body p-4">
                            <div className="alert alert-light border rounded-3 mb-3">
                                <i className="bi bi-info-circle-fill text-primary me-2"></i>
                                <span className="small">Editing after receiving answers should clarify, not change meaning.</span>
                            </div>
                            <ul className="list-unstyled mb-0">
                                <li className="mb-2 d-flex">
                                    <i className="bi bi-check-circle-fill text-success me-2"></i>
                                    <span className="small">Clarify ambiguous points</span>
                                </li>
                                <li className="mb-2 d-flex">
                                    <i className="bi bi-check-circle-fill text-success me-2"></i>
                                    <span className="small">Fix typos and grammar</span>
                                </li>
                                <li className="mb-2 d-flex">
                                    <i className="bi bi-x-circle-fill text-danger me-2"></i>
                                    <span className="small">Completely change the meaning</span>
                                </li>
                                <li className="d-flex">
                                    <i className="bi bi-x-circle-fill text-danger me-2"></i>
                                    <span className="small">Invalidate existing answers</span>
                                </li>
                            </ul>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
}
