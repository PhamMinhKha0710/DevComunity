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
            const [questionRes, answersRes] = await Promise.all([
                apiClient.get<Question>(`/questions/${id}`),
                apiClient.get<Answer[]>(`/questions/${id}/answers`),
            ]);
            setQuestion(questionRes.data);
            setAnswers(answersRes.data || []);
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
            <div className="min-h-screen flex items-center justify-center">
                <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-orange-500"></div>
            </div>
        );
    }

    if (!question) {
        return (
            <div className="min-h-screen flex items-center justify-center">
                <div className="text-center">
                    <h2 className="text-2xl font-bold text-gray-900">Question not found</h2>
                    <Link href="/" className="mt-4 inline-block text-orange-500 hover:text-orange-600">
                        ← Back to home
                    </Link>
                </div>
            </div>
        );
    }

    return (
        <div className="min-h-screen bg-gray-50">
            <header className="bg-white shadow-sm border-b">
                <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4">
                    <Link href="/" className="text-2xl font-bold text-orange-500">
                        DevComunity
                    </Link>
                </div>
            </header>

            <main className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
                {/* Question */}
                <div className="bg-white rounded-lg shadow p-6 mb-6">
                    <h1 className="text-2xl font-bold text-gray-900 mb-4">{question.title}</h1>

                    <div className="flex gap-6">
                        {/* Voting */}
                        <div className="flex flex-col items-center gap-2">
                            <button
                                onClick={() => handleVote('up', 'question', question.questionId)}
                                className={`p-2 rounded hover:bg-gray-100 ${question.userVoteType === 'up' ? 'text-orange-500' : 'text-gray-400'}`}
                            >
                                ▲
                            </button>
                            <span className="text-xl font-semibold">{question.score}</span>
                            <button
                                onClick={() => handleVote('down', 'question', question.questionId)}
                                className={`p-2 rounded hover:bg-gray-100 ${question.userVoteType === 'down' ? 'text-orange-500' : 'text-gray-400'}`}
                            >
                                ▼
                            </button>
                        </div>

                        {/* Content */}
                        <div className="flex-1">
                            <div className="prose max-w-none" dangerouslySetInnerHTML={{ __html: question.body }} />

                            <div className="mt-6 flex gap-2 flex-wrap">
                                {question.tags?.map((tag) => (
                                    <span key={tag.tagId} className="bg-blue-50 text-blue-600 px-2 py-1 rounded text-sm">
                                        {tag.tagName}
                                    </span>
                                ))}
                            </div>

                            <div className="mt-4 text-sm text-gray-500">
                                asked by <span className="text-blue-600">{question.authorUsername}</span>
                            </div>
                        </div>
                    </div>
                </div>

                {/* Answers */}
                <div className="mb-6">
                    <h2 className="text-xl font-bold text-gray-900 mb-4">{answers.length} Answers</h2>

                    {answers.map((answer) => (
                        <div key={answer.answerId} className={`bg-white rounded-lg shadow p-6 mb-4 ${answer.isAccepted ? 'border-l-4 border-green-500' : ''}`}>
                            <div className="flex gap-6">
                                <div className="flex flex-col items-center gap-2">
                                    <button
                                        onClick={() => handleVote('up', 'answer', answer.answerId)}
                                        className={`p-2 rounded hover:bg-gray-100 ${answer.userVoteType === 'up' ? 'text-orange-500' : 'text-gray-400'}`}
                                    >
                                        ▲
                                    </button>
                                    <span className="text-xl font-semibold">{answer.score}</span>
                                    <button
                                        onClick={() => handleVote('down', 'answer', answer.answerId)}
                                        className={`p-2 rounded hover:bg-gray-100 ${answer.userVoteType === 'down' ? 'text-orange-500' : 'text-gray-400'}`}
                                    >
                                        ▼
                                    </button>
                                    {answer.isAccepted && (
                                        <span className="text-green-500 text-2xl">✓</span>
                                    )}
                                </div>

                                <div className="flex-1">
                                    <div className="prose max-w-none" dangerouslySetInnerHTML={{ __html: answer.body }} />
                                    <div className="mt-4 text-sm text-gray-500">
                                        answered by <span className="text-blue-600">{answer.authorUsername}</span>
                                    </div>
                                </div>
                            </div>
                        </div>
                    ))}
                </div>

                {/* Add Answer Form */}
                {user ? (
                    <div className="bg-white rounded-lg shadow p-6">
                        <h3 className="text-lg font-semibold text-gray-900 mb-4">Your Answer</h3>
                        <form onSubmit={handleSubmitAnswer}>
                            <textarea
                                value={newAnswer}
                                onChange={(e) => setNewAnswer(e.target.value)}
                                rows={8}
                                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-orange-500 focus:border-orange-500"
                                placeholder="Write your answer here..."
                                required
                            />
                            <button
                                type="submit"
                                disabled={isSubmitting}
                                className="mt-4 bg-blue-600 text-white px-6 py-2 rounded-md hover:bg-blue-700 disabled:opacity-50 transition"
                            >
                                {isSubmitting ? 'Posting...' : 'Post Your Answer'}
                            </button>
                        </form>
                    </div>
                ) : (
                    <div className="bg-white rounded-lg shadow p-6 text-center">
                        <p className="text-gray-600">
                            <Link href="/login" className="text-orange-500 hover:text-orange-600">Log in</Link> to post an answer
                        </p>
                    </div>
                )}
            </main>
        </div>
    );
}
