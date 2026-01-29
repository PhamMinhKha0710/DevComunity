'use client';

import { useEffect, useState } from 'react';
import { useParams } from 'next/navigation';
import Link from 'next/link';
import { useAuth } from '@/lib/contexts/AuthContext';
import apiClient from '@/lib/api/client';
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
            // Fetch question first
            const questionRes = await apiClient.get<Question>(`/questions/${id}`);
            setQuestion(questionRes.data);

            // Fetch answers separately to handle 404 gracefully
            try {
                const answersRes = await apiClient.get<Answer[]>(`/answers/question/${id}`);
                setAnswers(answersRes.data || []);
            } catch {
                // If answers endpoint fails, just set empty array
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

    if (isLoading) {
        return (
            <div className="min-h-screen flex items-center justify-center bg-gray-50 dark:bg-slate-900">
                <div className="inline-block w-10 h-10 border-4 border-orange-500 border-t-transparent rounded-full animate-spin"></div>
            </div>
        );
    }

    if (!question) {
        return (
            <div className="min-h-screen flex items-center justify-center bg-gray-50 dark:bg-slate-900">
                <div className="text-center">
                    <div className="w-20 h-20 bg-gray-200 dark:bg-slate-700 rounded-full mx-auto mb-4 flex items-center justify-center">
                        <i className="bi bi-question-circle text-4xl text-gray-400"></i>
                    </div>
                    <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-2">Question not found</h2>
                    <p className="text-gray-500 mb-4">The question you&apos;re looking for doesn&apos;t exist.</p>
                    <Link href="/" className="inline-flex items-center text-orange-500 hover:text-orange-600 font-medium">
                        <i className="bi bi-arrow-left mr-2"></i>Back to home
                    </Link>
                </div>
            </div>
        );
    }

    return (
        <div className="min-h-screen bg-gray-50 dark:bg-slate-900 py-8">
            <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8">
                {/* Question */}
                <div className="bg-white dark:bg-slate-800 rounded-xl shadow-sm overflow-hidden mb-6">
                    {/* Question Header */}
                    <div className="p-6 border-b border-gray-100 dark:border-slate-700">
                        <h1 className="text-2xl font-bold text-gray-900 dark:text-white mb-3">{question.title}</h1>
                        <div className="flex items-center gap-4 text-sm text-gray-500">
                            <span>
                                <i className="bi bi-clock mr-1"></i>
                                Asked {new Date(question.createdDate).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })}
                            </span>
                            <span>
                                <i className="bi bi-eye mr-1"></i>
                                {question.viewCount || 0} views
                            </span>
                        </div>
                    </div>

                    {/* Question Body */}
                    <div className="p-6 flex gap-6">
                        {/* Voting */}
                        <div className="flex flex-col items-center gap-1 shrink-0">
                            <button
                                onClick={() => handleVote('up', 'question', question.questionId)}
                                className={`w-10 h-10 rounded-lg flex items-center justify-center transition ${question.userVoteType === 'up'
                                    ? 'bg-green-100 text-green-600 dark:bg-green-900/30 dark:text-green-400'
                                    : 'hover:bg-gray-100 dark:hover:bg-slate-700 text-gray-400'
                                    }`}
                            >
                                <i className="bi bi-caret-up-fill text-xl"></i>
                            </button>
                            <span className="text-xl font-bold text-gray-900 dark:text-white py-1">{question.score}</span>
                            <button
                                onClick={() => handleVote('down', 'question', question.questionId)}
                                className={`w-10 h-10 rounded-lg flex items-center justify-center transition ${question.userVoteType === 'down'
                                    ? 'bg-red-100 text-red-600 dark:bg-red-900/30 dark:text-red-400'
                                    : 'hover:bg-gray-100 dark:hover:bg-slate-700 text-gray-400'
                                    }`}
                            >
                                <i className="bi bi-caret-down-fill text-xl"></i>
                            </button>
                            <button className="w-10 h-10 rounded-lg flex items-center justify-center hover:bg-gray-100 dark:hover:bg-slate-700 text-gray-400 mt-2">
                                <i className="bi bi-bookmark"></i>
                            </button>
                        </div>

                        {/* Content */}
                        <div className="flex-1 min-w-0">
                            <div
                                className="prose dark:prose-invert max-w-none text-gray-700 dark:text-gray-300"
                                dangerouslySetInnerHTML={{ __html: question.body }}
                            />

                            <div className="mt-6 flex flex-wrap gap-2">
                                {question.tags?.map((tag) => (
                                    <Link
                                        key={tag.tagId}
                                        href={`/questions?tag=${tag.tagName}`}
                                        className="px-3 py-1 bg-orange-100 dark:bg-orange-900/30 text-orange-600 dark:text-orange-400 text-sm rounded-md hover:bg-orange-200 dark:hover:bg-orange-900/50 transition"
                                    >
                                        {tag.tagName}
                                    </Link>
                                ))}
                            </div>

                            <div className="mt-6 flex items-center justify-end">
                                <div className="bg-blue-50 dark:bg-blue-900/20 rounded-lg p-3 text-sm">
                                    <span className="text-gray-500 dark:text-gray-400">asked by </span>
                                    <Link href={`/users/${question.authorUsername}`} className="text-blue-600 dark:text-blue-400 font-medium hover:underline">
                                        {question.authorUsername}
                                    </Link>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>

                {/* Answers */}
                <div className="mb-6">
                    <h2 className="text-xl font-bold text-gray-900 dark:text-white mb-4 flex items-center">
                        <span>{answers.length} Answer{answers.length !== 1 ? 's' : ''}</span>
                    </h2>

                    {answers.length === 0 ? (
                        <div className="bg-white dark:bg-slate-800 rounded-xl shadow-sm p-8 text-center">
                            <i className="bi bi-chat-square-text text-4xl text-gray-300 dark:text-slate-600 mb-3"></i>
                            <p className="text-gray-500">No answers yet. Be the first to answer!</p>
                        </div>
                    ) : (
                        <div className="space-y-4">
                            {answers.map((answer) => (
                                <div
                                    key={answer.answerId}
                                    className={`bg-white dark:bg-slate-800 rounded-xl shadow-sm overflow-hidden ${answer.isAccepted ? 'ring-2 ring-green-500' : ''
                                        }`}
                                >
                                    <div className="p-6 flex gap-6">
                                        {/* Voting */}
                                        <div className="flex flex-col items-center gap-1 shrink-0">
                                            <button
                                                onClick={() => handleVote('up', 'answer', answer.answerId)}
                                                className={`w-10 h-10 rounded-lg flex items-center justify-center transition ${answer.userVoteType === 'up'
                                                    ? 'bg-green-100 text-green-600'
                                                    : 'hover:bg-gray-100 dark:hover:bg-slate-700 text-gray-400'
                                                    }`}
                                            >
                                                <i className="bi bi-caret-up-fill text-xl"></i>
                                            </button>
                                            <span className="text-xl font-bold text-gray-900 dark:text-white py-1">{answer.score}</span>
                                            <button
                                                onClick={() => handleVote('down', 'answer', answer.answerId)}
                                                className={`w-10 h-10 rounded-lg flex items-center justify-center transition ${answer.userVoteType === 'down'
                                                    ? 'bg-red-100 text-red-600'
                                                    : 'hover:bg-gray-100 dark:hover:bg-slate-700 text-gray-400'
                                                    }`}
                                            >
                                                <i className="bi bi-caret-down-fill text-xl"></i>
                                            </button>
                                            {answer.isAccepted && (
                                                <div className="w-10 h-10 bg-green-500 rounded-lg flex items-center justify-center text-white mt-2">
                                                    <i className="bi bi-check-lg text-xl"></i>
                                                </div>
                                            )}
                                        </div>

                                        {/* Content */}
                                        <div className="flex-1 min-w-0">
                                            <div
                                                className="prose dark:prose-invert max-w-none text-gray-700 dark:text-gray-300"
                                                dangerouslySetInnerHTML={{ __html: answer.body }}
                                            />
                                            <div className="mt-4 flex items-center justify-end">
                                                <div className="bg-gray-50 dark:bg-slate-700 rounded-lg p-3 text-sm">
                                                    <span className="text-gray-500 dark:text-gray-400">answered by </span>
                                                    <Link href={`/users/${answer.authorUsername}`} className="text-blue-600 dark:text-blue-400 font-medium hover:underline">
                                                        {answer.authorUsername}
                                                    </Link>
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            ))}
                        </div>
                    )}
                </div>

                {/* Add Answer Form */}
                {user ? (
                    <div className="bg-white dark:bg-slate-800 rounded-xl shadow-sm p-6">
                        <h3 className="text-lg font-bold text-gray-900 dark:text-white mb-4">Your Answer</h3>
                        <form onSubmit={handleSubmitAnswer}>
                            <textarea
                                value={newAnswer}
                                onChange={(e) => setNewAnswer(e.target.value)}
                                rows={8}
                                className="w-full px-4 py-3 border border-gray-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-orange-500 focus:border-transparent transition resize-none"
                                placeholder="Write your answer here... You can use Markdown!"
                                required
                            />
                            <div className="mt-4 flex justify-end">
                                <button
                                    type="submit"
                                    disabled={isSubmitting || !newAnswer.trim()}
                                    className="px-6 py-3 bg-orange-500 text-white font-semibold rounded-lg hover:bg-orange-600 focus:ring-4 focus:ring-orange-500/50 transition disabled:opacity-50 disabled:cursor-not-allowed"
                                >
                                    {isSubmitting ? (
                                        <span className="inline-flex items-center">
                                            <svg className="animate-spin -ml-1 mr-2 h-4 w-4 text-white" fill="none" viewBox="0 0 24 24">
                                                <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                                                <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                                            </svg>
                                            Posting...
                                        </span>
                                    ) : 'Post Your Answer'}
                                </button>
                            </div>
                        </form>
                    </div>
                ) : (
                    <div className="bg-white dark:bg-slate-800 rounded-xl shadow-sm p-8 text-center">
                        <i className="bi bi-person-circle text-4xl text-gray-300 dark:text-slate-600 mb-3"></i>
                        <p className="text-gray-600 dark:text-gray-400">
                            <Link href="/login" className="text-orange-500 hover:text-orange-600 font-medium">Log in</Link>
                            {' '}or{' '}
                            <Link href="/register" className="text-orange-500 hover:text-orange-600 font-medium">sign up</Link>
                            {' '}to post an answer
                        </p>
                    </div>
                )}
            </div>
        </div>
    );
}
