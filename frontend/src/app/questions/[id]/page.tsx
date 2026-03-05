'use client';

import { useEffect, useState } from 'react';
import { useParams } from 'next/navigation';
import Link from 'next/link';
import { useAuth } from '@/lib/contexts/AuthContext';
import apiClient from '@/lib/api/client';
import MarkdownContent from '@/components/MarkdownContent';
import type { Question, Answer } from '@/types';
import ModernNavbar from '@/components/ModernNavbar';
import ModernFooter from '@/components/ModernFooter';

export default function QuestionDetailPage() {
    const { id } = useParams();
    const { user } = useAuth();
    const [question, setQuestion] = useState<Question | null>(null);
    const [answers, setAnswers] = useState<Answer[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [newAnswer, setNewAnswer] = useState('');
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [answerSort, setAnswerSort] = useState('score');

    useEffect(() => {
        fetchQuestion();
    }, [id]);

    const fetchQuestion = async () => {
        try {
            const questionRes = await apiClient.get<Question>(`/questions/${id}`);
            setQuestion(questionRes.data);
            try {
                const answersRes = await apiClient.get<Answer[]>(`/questions/${id}/answers`);
                setAnswers(answersRes.data || []);
            } catch {
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
            await apiClient.post(`/votes`, { targetType, targetId, isUpvote: type === 'up' });
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
            await apiClient.post(`/answers`, { questionId: Number(id), body: newAnswer });
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
            year: 'numeric', month: 'long', day: 'numeric', hour: '2-digit', minute: '2-digit'
        });
    };

    const formatRelativeDate = (dateString: string) => {
        const date = new Date(dateString);
        const now = new Date();
        const diffMs = now.getTime() - date.getTime();
        const diffMins = Math.floor(diffMs / 60000);
        const diffHours = Math.floor(diffMs / 3600000);
        const diffDays = Math.floor(diffMs / 86400000);
        if (diffMins < 60) return `${diffMins} minutes ago`;
        if (diffHours < 24) return `${diffHours} hours ago`;
        if (diffDays < 7) return `${diffDays} days ago`;
        return date.toLocaleDateString();
    };

    if (isLoading) {
        return (
            <div className="min-h-screen flex flex-col bg-[#f6f7f8] dark:bg-[#101922]">
                <ModernNavbar />
                <div className="flex items-center justify-center py-20 pt-32">
                    <div className="w-10 h-10 border-3 border-[var(--primary)]/20 border-t-[var(--primary)] rounded-full animate-spin" />
                </div>
            </div>
        );
    }

    if (!question) {
        return (
            <div className="min-h-screen flex flex-col bg-[#f6f7f8] dark:bg-[#101922]">
                <ModernNavbar />
                <div className="text-center py-20 pt-32">
                    <span className="material-symbols-outlined text-5xl text-slate-300 mb-4 block">error</span>
                    <h2 className="text-2xl font-bold text-[var(--text-primary)] mb-4">Question not found</h2>
                    <Link href="/questions" className="text-[var(--primary)] hover:underline font-semibold">
                        ← Back to questions
                    </Link>
                </div>
            </div>
        );
    }

    return (
        <div className="min-h-screen flex flex-col bg-[#f6f7f8] dark:bg-[#101922]">
            <ModernNavbar />

            <main className="flex-1 max-w-[1440px] mx-auto w-full flex gap-8 px-4 md:px-10 py-8 pt-24">
                {/* Left Sidebar */}
                <aside className="hidden lg:flex flex-col w-56 shrink-0 gap-1">
                    <Link href="/" className="flex items-center gap-3 px-3 py-2 rounded-lg text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors">
                        <span className="material-symbols-outlined">home</span>
                        <span className="text-sm font-medium">Home</span>
                    </Link>
                    <Link href="/questions" className="flex items-center gap-3 px-3 py-2 rounded-lg bg-[var(--primary)]/10 text-[var(--primary)]">
                        <span className="material-symbols-outlined">help</span>
                        <span className="text-sm font-bold">Questions</span>
                    </Link>
                    <Link href="/tags" className="flex items-center gap-3 px-3 py-2 rounded-lg text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors">
                        <span className="material-symbols-outlined">sell</span>
                        <span className="text-sm font-medium">Tags</span>
                    </Link>
                    <Link href="/saved" className="flex items-center gap-3 px-3 py-2 rounded-lg text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors">
                        <span className="material-symbols-outlined">bookmark</span>
                        <span className="text-sm font-medium">Saves</span>
                    </Link>
                    <Link href="/users" className="flex items-center gap-3 px-3 py-2 rounded-lg text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors">
                        <span className="material-symbols-outlined">group</span>
                        <span className="text-sm font-medium">Users</span>
                    </Link>

                    <div className="pt-6 mt-4 border-t border-slate-200 dark:border-slate-800">
                        <h3 className="px-3 text-xs font-bold text-slate-400 uppercase tracking-wider mb-3">Popular Tags</h3>
                        <div className="flex flex-wrap gap-2 px-3">
                            {question.tags?.map((tag) => (
                                <Link key={tag.tagId} href={`/questions?tag=${tag.tagName}`}
                                    className="px-2 py-1 bg-slate-100 dark:bg-slate-800 rounded text-xs font-medium text-slate-600 dark:text-slate-400 hover:bg-[var(--primary)]/10 hover:text-[var(--primary)] transition-colors">
                                    {tag.tagName}
                                </Link>
                            ))}
                        </div>
                    </div>
                </aside>

                {/* Main Content */}
                <div className="flex-1 flex flex-col gap-6 min-w-0 max-w-[880px]">
                    {/* Breadcrumbs */}
                    <div className="flex items-center gap-2 text-sm text-slate-500">
                        <Link href="/questions" className="hover:text-[var(--primary)]">Questions</Link>
                        <span className="material-symbols-outlined text-xs">chevron_right</span>
                        {question.tags?.[0] && (
                            <>
                                <Link href={`/questions?tag=${question.tags[0].tagName}`} className="hover:text-[var(--primary)]">
                                    {question.tags[0].tagName}
                                </Link>
                                <span className="material-symbols-outlined text-xs">chevron_right</span>
                            </>
                        )}
                        <span className="text-[var(--text-primary)] font-medium truncate">{question.title.substring(0, 40)}...</span>
                    </div>

                    {/* Title Section */}
                    <div className="flex flex-col gap-4 border-b border-slate-200 dark:border-slate-800 pb-6">
                        <h1 className="text-3xl md:text-4xl font-extrabold text-[var(--text-primary)] leading-tight">
                            {question.title}
                        </h1>
                        <div className="flex flex-wrap items-center gap-4 text-sm text-slate-500">
                            <span className="flex items-center gap-1">
                                <span className="material-symbols-outlined text-sm">schedule</span>
                                Asked {formatRelativeDate(question.createdDate)}
                            </span>
                            <span className="flex items-center gap-1">
                                <span className="material-symbols-outlined text-sm">visibility</span>
                                Viewed {question.viewCount} times
                            </span>
                            <span className="flex items-center gap-1">
                                <span className="material-symbols-outlined text-sm">update</span>
                                Active today
                            </span>
                        </div>
                    </div>

                    {/* Author Info */}
                    <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 p-4 bg-white dark:bg-slate-900 rounded-xl border border-slate-200 dark:border-slate-800">
                        <div className="flex items-center gap-4">
                            <div className="w-12 h-12 rounded-lg bg-gradient-to-br from-blue-500 to-indigo-600 flex items-center justify-center text-white text-lg font-bold">
                                {question.authorUsername?.charAt(0).toUpperCase() || '?'}
                            </div>
                            <div className="flex flex-col">
                                <Link href={`/users/${question.authorId}`} className="font-bold text-[var(--text-primary)] hover:text-[var(--primary)]">
                                    {question.authorUsername}
                                </Link>
                                <div className="flex items-center gap-2 text-xs text-slate-500">
                                    <span className="font-bold text-slate-700 dark:text-slate-300">{question.authorReputation || 0}</span> reputation
                                </div>
                            </div>
                        </div>
                        {user && user.username !== question.authorUsername && (
                            <button className="flex items-center justify-center gap-2 rounded-lg h-10 px-4 bg-slate-100 dark:bg-slate-800 text-[var(--text-primary)] text-sm font-bold hover:bg-slate-200 dark:hover:bg-slate-700 transition-all border border-transparent hover:border-slate-300 dark:hover:border-slate-600">
                                <span className="material-symbols-outlined text-lg">person_add</span>
                                Follow
                            </button>
                        )}
                    </div>

                    {/* Question Body */}
                    <div className="prose prose-slate dark:prose-invert max-w-none">
                        <MarkdownContent content={question.body} className="text-lg leading-relaxed text-slate-700 dark:text-slate-300" />
                    </div>

                    {/* Tags */}
                    <div className="flex flex-wrap gap-2">
                        {question.tags?.map((tag) => (
                            <Link key={tag.tagId} href={`/questions?tag=${tag.tagName}`}
                                className="px-3 py-1.5 bg-[var(--primary)]/10 rounded-lg text-sm font-semibold text-[var(--primary)] hover:bg-[var(--primary)]/20 transition-colors">
                                {tag.tagName}
                            </Link>
                        ))}
                    </div>

                    {/* Vote / Share / Report bar */}
                    <div className="flex items-center justify-between border-t border-slate-200 dark:border-slate-800 pt-6">
                        <div className="flex items-center gap-6">
                            <button
                                onClick={() => handleVote('up', 'question', question.questionId)}
                                className={`flex items-center gap-2 transition-colors ${question.userVoteType === 'up' ? 'text-[var(--primary)]' : 'text-slate-500 hover:text-[var(--primary)]'}`}
                            >
                                <span className="material-symbols-outlined">thumb_up</span>
                                <span className="font-bold">{question.score}</span>
                            </button>
                            <button
                                onClick={() => handleVote('down', 'question', question.questionId)}
                                className={`flex items-center gap-2 transition-colors ${question.userVoteType === 'down' ? 'text-red-500' : 'text-slate-500 hover:text-red-500'}`}
                            >
                                <span className="material-symbols-outlined">thumb_down</span>
                            </button>
                            <button className="flex items-center gap-2 text-slate-500 hover:text-[var(--primary)] transition-colors">
                                <span className="material-symbols-outlined">share</span>
                                <span className="text-sm font-medium">Share</span>
                            </button>
                        </div>
                        <button className="flex items-center gap-2 text-slate-500 hover:text-[var(--primary)] transition-colors">
                            <span className="material-symbols-outlined">flag</span>
                            <span className="text-sm font-medium">Report</span>
                        </button>
                    </div>

                    {/* Answers Section */}
                    <div className="flex items-center justify-between mt-8 mb-4">
                        <h2 className="text-2xl font-bold text-[var(--text-primary)]">
                            {answers.length} {answers.length === 1 ? 'Answer' : 'Answers'}
                        </h2>
                        <div className="flex items-center gap-2">
                            <span className="text-sm text-slate-500">Sort by:</span>
                            <select
                                value={answerSort}
                                onChange={(e) => setAnswerSort(e.target.value)}
                                className="bg-transparent border-none text-sm font-bold text-[var(--text-primary)] focus:ring-0 cursor-pointer"
                            >
                                <option value="score">Highest score</option>
                                <option value="newest">Newest first</option>
                            </select>
                        </div>
                    </div>

                    {/* Answers List */}
                    {answers.length > 0 ? (
                        <div className="flex flex-col gap-4">
                            {answers.map((answer) => (
                                <div
                                    key={answer.answerId}
                                    id={`answer-${answer.answerId}`}
                                    className={`bg-white dark:bg-slate-900 border rounded-xl p-6 transition ${
                                        answer.isAccepted ? 'border-green-500/50 shadow-[0_0_15px_rgba(34,197,94,0.08)]' : 'border-slate-200 dark:border-slate-800'
                                    }`}
                                >
                                    {answer.isAccepted && (
                                        <div className="flex items-center gap-2 text-green-600 mb-4 text-sm font-bold">
                                            <span className="material-symbols-outlined text-lg">check_circle</span>
                                            Accepted Answer
                                        </div>
                                    )}

                                    <div className="prose prose-slate dark:prose-invert max-w-none mb-4" dangerouslySetInnerHTML={{ __html: answer.body }} />

                                    <div className="flex items-center justify-between pt-4 border-t border-slate-200 dark:border-slate-800">
                                        <div className="flex items-center gap-4">
                                            <button
                                                onClick={() => handleVote('up', 'answer', answer.answerId)}
                                                className={`flex items-center gap-1 transition-colors ${answer.userVoteType === 'up' ? 'text-[var(--primary)]' : 'text-slate-500 hover:text-[var(--primary)]'}`}
                                            >
                                                <span className="material-symbols-outlined text-lg">thumb_up</span>
                                                <span className="text-sm font-bold">{answer.score}</span>
                                            </button>
                                            <button
                                                onClick={() => handleVote('down', 'answer', answer.answerId)}
                                                className={`transition-colors ${answer.userVoteType === 'down' ? 'text-red-500' : 'text-slate-500 hover:text-red-500'}`}
                                            >
                                                <span className="material-symbols-outlined text-lg">thumb_down</span>
                                            </button>
                                        </div>
                                        <div className="flex items-center gap-2">
                                            <span className="text-xs text-slate-500">Answered {formatRelativeDate(answer.createdDate)}</span>
                                            <div className="w-6 h-6 rounded-full bg-gradient-to-br from-blue-500 to-indigo-600 flex items-center justify-center text-white text-[10px] font-bold">
                                                {answer.authorUsername?.charAt(0).toUpperCase()}
                                            </div>
                                            <Link href={`/users/${answer.authorId}`} className="text-sm font-bold text-[var(--primary)] hover:underline">
                                                {answer.authorUsername}
                                            </Link>
                                        </div>
                                    </div>
                                </div>
                            ))}
                        </div>
                    ) : null}

                    {/* Add Answer / Join Discussion */}
                    {user ? (
                        <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl p-6 mt-4">
                            <h3 className="text-lg font-bold text-[var(--text-primary)] mb-4">Your Answer</h3>
                            <form onSubmit={handleSubmitAnswer}>
                                <textarea
                                    value={newAnswer}
                                    onChange={(e) => setNewAnswer(e.target.value)}
                                    rows={6}
                                    className="w-full p-4 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-[var(--text-primary)] placeholder:text-slate-400 focus:border-[var(--primary)] focus:ring-2 focus:ring-[var(--primary)]/20 transition resize-y min-h-[150px] text-sm"
                                    placeholder="Write your answer here. Markdown is supported."
                                    required
                                />
                                <div className="flex justify-end mt-4">
                                    <button
                                        type="submit"
                                        disabled={isSubmitting}
                                        className="flex items-center gap-2 px-6 py-3 bg-[var(--primary)] text-white rounded-lg font-bold text-sm hover:bg-[var(--primary-dark)] transition disabled:opacity-50 shadow-lg shadow-[var(--primary)]/25"
                                    >
                                        {isSubmitting ? (
                                            <><span className="material-symbols-outlined text-lg animate-spin">progress_activity</span> Posting...</>
                                        ) : (
                                            <><span className="material-symbols-outlined text-lg">send</span> Post Answer</>
                                        )}
                                    </button>
                                </div>
                            </form>
                        </div>
                    ) : (
                        <div className="flex flex-col items-center justify-center p-12 md:p-20 bg-white dark:bg-slate-900 rounded-2xl border-2 border-dashed border-slate-200 dark:border-slate-800 text-center mt-4">
                            <div className="w-20 h-20 rounded-full bg-[var(--primary)]/10 flex items-center justify-center text-[var(--primary)] mb-6">
                                <span className="material-symbols-outlined text-4xl">lock</span>
                            </div>
                            <h3 className="text-2xl font-bold text-[var(--text-primary)] mb-2">Join the discussion</h3>
                            <p className="text-slate-500 dark:text-slate-400 max-w-sm mb-8">
                                This question doesn&apos;t have any answers yet. Log in or sign up to share your knowledge with the community.
                            </p>
                            <div className="flex flex-col sm:flex-row gap-4 w-full sm:w-auto">
                                <Link href="/login" className="flex min-w-[140px] items-center justify-center rounded-xl h-12 px-8 bg-[var(--primary)] text-white font-bold hover:shadow-lg hover:shadow-[var(--primary)]/20 transition-all">
                                    Log In
                                </Link>
                                <Link href="/register" className="flex min-w-[140px] items-center justify-center rounded-xl h-12 px-8 bg-slate-100 dark:bg-slate-800 text-[var(--text-primary)] font-bold hover:bg-slate-200 dark:hover:bg-slate-700 transition-all border border-slate-200 dark:border-slate-700">
                                    Sign Up
                                </Link>
                            </div>
                        </div>
                    )}
                </div>

                {/* Right Sidebar */}
                <aside className="hidden xl:flex flex-col w-72 shrink-0 gap-6">
                    {/* Related Questions */}
                    <div className="p-5 bg-white dark:bg-slate-900 rounded-xl border border-slate-200 dark:border-slate-800">
                        <h3 className="text-sm font-bold text-[var(--text-primary)] mb-4">Related Questions</h3>
                        <div className="flex flex-col gap-4">
                            <a className="text-sm text-[var(--primary)] hover:underline line-clamp-2 cursor-pointer">How to register multiple implementations of an interface?</a>
                            <a className="text-sm text-[var(--primary)] hover:underline line-clamp-2 cursor-pointer">Scoped vs Transient vs Singleton in DI</a>
                            <a className="text-sm text-[var(--primary)] hover:underline line-clamp-2 cursor-pointer">DI error: Cannot resolve scoped service from singleton</a>
                            <a className="text-sm text-[var(--primary)] hover:underline line-clamp-2 cursor-pointer">Unit Testing Controllers with Dependency Injection</a>
                        </div>
                    </div>

                    {/* Community Tip */}
                    <div className="p-5 bg-gradient-to-br from-[var(--primary)]/5 to-[var(--primary)]/10 rounded-xl border border-[var(--primary)]/20">
                        <div className="flex items-center gap-3 mb-3 text-[var(--primary)]">
                            <span className="material-symbols-outlined">lightbulb</span>
                            <h3 className="text-sm font-bold">Community Tip</h3>
                        </div>
                        <p className="text-xs leading-relaxed text-slate-600 dark:text-slate-400">
                            When debugging DI issues, check if you&apos;re trying to inject a Scoped service into a Singleton. This is a common cause for resolution errors!
                        </p>
                    </div>

                    {/* Ad Space */}
                    <div className="sticky top-24">
                        <div className="bg-slate-200 dark:bg-slate-800 rounded-xl h-[350px] flex flex-col items-center justify-center p-6 text-center overflow-hidden relative">
                            <span className="text-xs font-bold text-slate-400 uppercase tracking-widest mb-2 relative z-10">Advertisement</span>
                            <h4 className="text-lg font-bold text-[var(--text-primary)] mb-4 relative z-10">Deploy your .NET apps in seconds.</h4>
                            <button className="px-6 py-2 bg-[var(--primary)] text-white rounded-lg font-bold text-sm relative z-10">Try CloudHost Free</button>
                        </div>
                    </div>
                </aside>
            </main>

            <ModernFooter />
        </div>
    );
}
