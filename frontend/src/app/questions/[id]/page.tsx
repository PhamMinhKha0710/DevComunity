'use client';

import { useEffect, useState } from 'react';
import { useParams } from 'next/navigation';
import Link from 'next/link';
import { useAuth } from '@/lib/contexts/AuthContext';
import apiClient from '@/lib/api/client';
import AppLayout from '@/components/AppLayout';
import MarkdownContent from '@/components/MarkdownContent';
import type { Question, Answer } from '@/types';

export default function QuestionDetailPage() {
    const { id } = useParams();
    const { user } = useAuth();
    const [question, setQuestion] = useState<Question | null>(null);
    const [answers, setAnswers] = useState<Answer[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [newAnswer, setNewAnswer] = useState('');
    const [isSubmitting, setIsSubmitting] = useState(false);

    useEffect(() => {
        fetchQuestion();
    }, [id]);

    const fetchQuestion = async () => {
        try {
            // Fetch question first - this is required
            const questionRes = await apiClient.get<Question>(`/questions/${id}`);
            setQuestion(questionRes.data);

            // Try to fetch answers separately - don't fail if answers endpoint doesn't exist
            try {
                const answersRes = await apiClient.get<Answer[]>(`/questions/${id}/answers`);
                setAnswers(answersRes.data || []);
            } catch (answersError) {
                console.warn('Failed to fetch answers (endpoint may not exist):', answersError);
                setAnswers([]);
            }
        } catch (error) {
            console.error('Failed to fetch question:', error);
        } finally {
            setIsLoading(false);
        }
    };

    const handleVote = async (type: 'up' | 'down', targetType: 'question' | 'answer', targetId: number) => {
        if (!user) return;
        try {
            await apiClient.post(`/votes`, {
                targetType,
                targetId,
                isUpvote: type === 'up',
            });
            fetchQuestion();
        } catch (error) {
            console.error('Vote failed:', error);
        }
    };

    const handleSubmitAnswer = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!newAnswer.trim() || !user) return;

        setIsSubmitting(true);
        try {
            await apiClient.post(`/answers`, {
                questionId: Number(id),
                body: newAnswer,
            });
            setNewAnswer('');
            fetchQuestion();
        } catch (error) {
            console.error('Failed to submit answer:', error);
        } finally {
            setIsSubmitting(false);
        }
    };

    const formatDate = (dateString: string) => {
        return new Date(dateString).toLocaleDateString(undefined, {
            year: 'numeric',
            month: 'long',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        });
    };

    if (isLoading) {
        return (
            <AppLayout>
                <div className="flex items-center justify-center py-20">
                    <div className="w-10 h-10 border-4 border-[var(--primary)]/30 border-t-[var(--primary)] rounded-full animate-spin"></div>
                </div>
            </AppLayout>
        );
    }

    if (!question) {
        return (
            <AppLayout>
                <div className="text-center py-20">
                    <h2 className="text-2xl font-bold text-[var(--text-primary)] mb-4">Question not found</h2>
                    <Link href="/" className="text-[var(--primary)] hover:underline">
                        ← Back to home
                    </Link>
                </div>
            </AppLayout>
        );
    }

    return (
        <AppLayout>
            <div className="max-w-4xl mx-auto">
                {/* Question */}
                <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-2xl p-6 mb-6 shadow-sm">
                    {/* Header */}
                    <div className="mb-6 border-b border-[var(--border-color)] pb-4">
                        <div className="flex items-start justify-between gap-4 mb-4">
                            <h1 className="text-2xl sm:text-3xl font-bold text-[var(--text-primary)] leading-tight">
                                {question.title}
                            </h1>
                            {question.status === 'Solved' && (
                                <span className="px-3 py-1 bg-green-500/10 text-green-500 border border-green-500/20 rounded-full text-sm font-medium whitespace-nowrap">
                                    <i className="bi bi-check-circle-fill mr-1"></i> Solved
                                </span>
                            )}
                        </div>
                        <div className="flex flex-wrap items-center gap-4 text-sm text-[var(--text-muted)]">
                            <span className="flex items-center gap-1">
                                <i className="bi bi-clock"></i>
                                {formatDate(question.createdDate)}
                            </span>
                            <span className="flex items-center gap-1">
                                <i className="bi bi-eye"></i>
                                {question.viewCount} views
                            </span>
                        </div>
                    </div>

                    <div className="flex gap-6">
                        {/* Voting */}
                        <div className="flex flex-col items-center gap-2">
                            <button
                                onClick={() => handleVote('up', 'question', question.questionId)}
                                className={`w-10 h-10 rounded-full flex items-center justify-center transition ${question.userVoteType === 'up'
                                    ? 'bg-[var(--primary)] text-white'
                                    : 'bg-[var(--bg-tertiary)] text-[var(--text-muted)] hover:bg-[var(--bg-hover)]'
                                    }`}
                                title="Upvote"
                            >
                                <i className="bi bi-caret-up-fill text-xl"></i>
                            </button>
                            <span className="text-xl font-bold text-[var(--text-primary)]">{question.score}</span>
                            <button
                                onClick={() => handleVote('down', 'question', question.questionId)}
                                className={`w-10 h-10 rounded-full flex items-center justify-center transition ${question.userVoteType === 'down'
                                    ? 'bg-red-500 text-white'
                                    : 'bg-[var(--bg-tertiary)] text-[var(--text-muted)] hover:bg-[var(--bg-hover)]'
                                    }`}
                                title="Downvote"
                            >
                                <i className="bi bi-caret-down-fill text-xl"></i>
                            </button>
                            <button className="mt-2 w-8 h-8 rounded-full bg-[var(--bg-tertiary)] text-[var(--text-muted)] hover:text-[var(--primary)] flex items-center justify-center transition" title="Save">
                                <i className={`bi ${question.isSaved ? 'bi-bookmark-fill text-[var(--primary)]' : 'bi-bookmark'}`}></i>
                            </button>
                        </div>

                        {/* Content */}
                        <div className="flex-1 min-w-0">
                            <MarkdownContent
                                content={question.body}
                                className="mb-6 text-[var(--text-secondary)]"
                            />

                            <div className="flex flex-wrap gap-2 mb-6">
                                {question.tags?.map((tag) => (
                                    <Link
                                        key={tag.tagId}
                                        href={`/tags?search=${tag.tagName}`}
                                        className="px-3 py-1 bg-[var(--primary)]/10 text-[var(--primary)] rounded-lg text-sm font-medium hover:bg-[var(--primary)] hover:text-white transition"
                                    >
                                        #{tag.tagName}
                                    </Link>
                                ))}
                            </div>

                            <div className="flex items-center justify-between pt-4 border-t border-[var(--border-color)]">
                                <div className="flex gap-4">
                                    <button className="text-[var(--text-muted)] hover:text-[var(--text-primary)] text-sm font-medium transition">
                                        All questions
                                    </button>
                                </div>
                                <div className="flex items-center gap-3 bg-[var(--bg-tertiary)] p-3 rounded-xl border border-[var(--border-color)]">
                                    <div className="w-10 h-10 rounded-lg bg-gradient-to-br from-blue-500 to-cyan-500 flex items-center justify-center text-white font-bold">
                                        {question.authorUsername?.charAt(0).toUpperCase()}
                                    </div>
                                    <div className="text-sm">
                                        <div className="text-[var(--text-muted)]">Asked by</div>
                                        <Link href={`/users/${question.authorId}`} className="font-semibold text-[var(--primary)] hover:underline">
                                            {question.authorUsername}
                                        </Link>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>

                {/* Answers Section */}
                <div className="mb-8">
                    <h2 className="text-xl font-bold text-[var(--text-primary)] mb-4 flex items-center gap-2">
                        <i className="bi bi-chat-left-text-fill text-[var(--primary)]"></i>
                        {answers.length} Answers
                    </h2>

                    <div className="space-y-4">
                        {answers.map((answer) => (
                            <div
                                key={answer.answerId}
                                id={`answer-${answer.answerId}`}
                                className={`bg-[var(--bg-secondary)] border rounded-2xl p-6 transition ${answer.isAccepted
                                    ? 'border-green-500/50 shadow-[0_0_15px_rgba(34,197,94,0.1)]'
                                    : 'border-[var(--border-color)]'
                                    }`}
                            >
                                <div className="flex gap-6">
                                    <div className="flex flex-col items-center gap-2">
                                        <button
                                            onClick={() => handleVote('up', 'answer', answer.answerId)}
                                            className={`w-10 h-10 rounded-full flex items-center justify-center transition ${answer.userVoteType === 'up'
                                                ? 'bg-[var(--primary)] text-white'
                                                : 'bg-[var(--bg-tertiary)] text-[var(--text-muted)] hover:bg-[var(--bg-hover)]'
                                                }`}
                                        >
                                            <i className="bi bi-caret-up-fill text-xl"></i>
                                        </button>
                                        <span className="text-xl font-bold text-[var(--text-primary)]">{answer.score}</span>
                                        <button
                                            onClick={() => handleVote('down', 'answer', answer.answerId)}
                                            className={`w-10 h-10 rounded-full flex items-center justify-center transition ${answer.userVoteType === 'down'
                                                ? 'bg-red-500 text-white'
                                                : 'bg-[var(--bg-tertiary)] text-[var(--text-muted)] hover:bg-[var(--bg-hover)]'
                                                }`}
                                        >
                                            <i className="bi bi-caret-down-fill text-xl"></i>
                                        </button>
                                        {answer.isAccepted && (
                                            <div className="mt-2 w-10 h-10 rounded-full bg-green-500 text-white flex items-center justify-center" title="Accepted Answer">
                                                <i className="bi bi-check-lg text-2xl"></i>
                                            </div>
                                        )}
                                    </div>

                                    <div className="flex-1 min-w-0">
                                        <div
                                            className="prose dark:prose-invert max-w-none mb-4 text-[var(--text-secondary)]"
                                            dangerouslySetInnerHTML={{ __html: answer.body }}
                                        />

                                        <div className="flex items-center justify-between pt-4 border-t border-[var(--border-color)]">
                                            <div className="text-xs text-[var(--text-muted)]">
                                                Answered {formatDate(answer.createdDate)}
                                            </div>
                                            <div className="flex items-center gap-2">
                                                <div className="w-6 h-6 rounded bg-gradient-to-br from-purple-500 to-pink-500 flex items-center justify-center text-white text-xs font-bold">
                                                    {answer.authorUsername?.charAt(0).toUpperCase()}
                                                </div>
                                                <Link href={`/users/${answer.authorId}`} className="text-sm font-medium text-[var(--primary)] hover:underline">
                                                    {answer.authorUsername}
                                                </Link>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        ))}
                    </div>
                </div>

                {/* Add Answer Form */}
                {user ? (
                    <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-2xl p-6 shadow-sm">
                        <h3 className="text-lg font-semibold text-[var(--text-primary)] mb-4">Your Answer</h3>
                        <form onSubmit={handleSubmitAnswer}>
                            <div className="mb-4">
                                <textarea
                                    value={newAnswer}
                                    onChange={(e) => setNewAnswer(e.target.value)}
                                    rows={6}
                                    className="w-full p-4 bg-[var(--bg-tertiary)] border border-[var(--border-color)] rounded-xl text-[var(--text-primary)] placeholder-[var(--text-muted)] focus:border-[var(--primary)] focus:ring-2 focus:ring-[var(--primary)]/20 transition resize-y min-h-[150px]"
                                    placeholder="Write your answer here. Markdown is supported."
                                    required
                                />
                            </div>
                            <div className="flex justify-end">
                                <button
                                    type="submit"
                                    disabled={isSubmitting}
                                    className="px-6 py-3 bg-[var(--primary)] text-white rounded-xl font-medium hover:bg-[var(--primary-dark)] transition disabled:opacity-50 flex items-center gap-2"
                                >
                                    {isSubmitting ? (
                                        <><i className="bi bi-arrow-clockwise animate-spin"></i> Posting...</>
                                    ) : (
                                        <><i className="bi bi-send-fill"></i> Post Answer</>
                                    )}
                                </button>
                            </div>
                        </form>
                    </div>
                ) : (
                    <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-2xl p-8 text-center">
                        <i className="bi bi-lock-fill text-4xl text-[var(--text-muted)] mb-3"></i>
                        <h3 className="text-lg font-semibold text-[var(--text-primary)] mb-2">Join the discussion</h3>
                        <p className="text-[var(--text-muted)] mb-6">Log in or sign up to leave an answer</p>
                        <div className="flex justify-center gap-4">
                            <Link href="/login" className="px-6 py-2 bg-[var(--primary)] text-white rounded-xl font-medium hover:bg-[var(--primary-dark)] transition">
                                Log In
                            </Link>
                            <Link href="/register" className="px-6 py-2 bg-[var(--bg-tertiary)] text-[var(--text-primary)] rounded-xl font-medium border border-[var(--border-color)] hover:border-[var(--primary)] transition">
                                Sign Up
                            </Link>
                        </div>
                    </div>
                )}
            </div>
        </AppLayout>
    );
}
