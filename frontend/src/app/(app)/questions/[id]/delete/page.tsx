'use client';

import { useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
import Link from 'next/link';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useAuth } from '@/lib/contexts/AuthContext';
import type { Question } from '@/types';
import { questionsApi } from '@/lib/api/questions.api';
import AppLayout from '@/components/AppLayout';

export default function DeleteQuestionPage() {
    const params = useParams();
    const router = useRouter();
    const { user } = useAuth();
    const questionId = params.id as string;

    const queryClient = useQueryClient();
    const [error, setError] = useState('');
    const [confirmText, setConfirmText] = useState('');

    const { data: question, isLoading } = useQuery<Question>({
        queryKey: ['question', questionId],
        queryFn: () => questionsApi.getById(questionId),
    });

    const deleteMutation = useMutation({
        mutationFn: () => questionsApi.delete(questionId),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['questions'] });
            router.push('/questions?deleted=true');
        },
        onError: (err: any) => {
            setError(err.response?.data?.message || 'Failed to delete question');
        },
    });

    const isDeleting = deleteMutation.isPending;

    const handleDelete = () => {
        if (confirmText !== 'DELETE') {
            setError('Please type DELETE to confirm');
            return;
        }
        setError('');
        deleteMutation.mutate();
    };

    if (isLoading) {
        return (
            <AppLayout showRightSidebar={false}>
                <div className="flex items-center justify-center py-20">
                    <div className="w-10 h-10 border-3 border-[rgba(19,127,236,0.2)] border-t-[#137fec] rounded-full animate-spin" />
                </div>
            </AppLayout>
        );
    }

    if (!question) {
        return (
            <AppLayout showRightSidebar={false}>
                <div className="bg-white dark:bg-[var(--bg-secondary)] border border-[#e2e8f0] dark:border-[var(--border-color)] rounded-2xl p-12 text-center">
                    <span className="material-symbols-outlined text-5xl text-amber-500 mb-4 block">warning</span>
                    <h3 className="text-lg font-bold text-[#0f172a] dark:text-white mb-2">Question not found</h3>
                    <Link href="/questions" className="inline-flex items-center gap-2 bg-[#137fec] text-white px-5 py-2.5 rounded-xl font-bold text-sm hover:bg-[#1170d4] transition-all mt-4">
                        Back to Questions
                    </Link>
                </div>
            </AppLayout>
        );
    }

    return (
        <AppLayout showRightSidebar={false}>
            {/* Breadcrumb */}
            <nav className="flex items-center gap-2 text-sm text-[#64748b] mb-4">
                <Link href="/questions" className="hover:text-[#137fec] transition-colors">Questions</Link>
                <span>/</span>
                <Link href={`/questions/${questionId}`} className="hover:text-[#137fec] transition-colors truncate max-w-[150px]">{question.title.substring(0, 30)}...</Link>
                <span>/</span>
                <span className="text-red-500 font-medium">Delete</span>
            </nav>

            <div className="max-w-2xl mx-auto">
                <div className="bg-white dark:bg-[var(--bg-secondary)] border border-red-200 dark:border-red-500/30 rounded-2xl overflow-hidden">
                    <div className="bg-red-600 text-white px-6 py-4">
                        <h1 className="text-lg font-bold flex items-center gap-2">
                            <span className="material-symbols-outlined">warning</span>
                            Delete Question
                        </h1>
                    </div>
                    <div className="p-6">
                        {/* Warning */}
                        <div className="p-4 bg-red-50 dark:bg-red-500/10 border border-red-200 dark:border-red-500/30 rounded-xl flex gap-3 mb-6">
                            <span className="material-symbols-outlined text-red-500 text-2xl shrink-0">warning</span>
                            <div>
                                <h3 className="font-bold text-red-700 dark:text-red-400 mb-1">Warning: This action cannot be undone!</h3>
                                <p className="text-red-600 dark:text-red-300 text-sm">Once you delete this question, all associated answers, comments, and votes will be permanently removed.</p>
                            </div>
                        </div>

                        {/* Question Preview */}
                        <div className="bg-[#f8fafc] dark:bg-[var(--bg-tertiary)] border border-[#e2e8f0] dark:border-[var(--border-color)] rounded-xl p-5 mb-6">
                            <h3 className="font-bold text-[#0f172a] dark:text-white mb-2">{question.title}</h3>
                            <p className="text-[#475569] dark:text-[var(--text-secondary)] text-sm mb-3 line-clamp-3">
                                {question.bodyExcerpt || question.body.substring(0, 200)}...
                            </p>
                            <div className="flex gap-4 text-[#94a3b8] text-xs">
                                <span className="flex items-center gap-1"><span className="material-symbols-outlined text-sm">visibility</span>{question.viewCount} views</span>
                                <span className="flex items-center gap-1"><span className="material-symbols-outlined text-sm">thumb_up</span>{question.score} likes</span>
                                <span className="flex items-center gap-1"><span className="material-symbols-outlined text-sm">chat</span>{question.answerCount} answers</span>
                            </div>
                        </div>

                        {error && (
                            <div className="mb-4 p-4 bg-red-50 dark:bg-red-500/10 border border-red-200 dark:border-red-500/30 rounded-xl text-red-600 dark:text-red-400 text-sm flex items-center gap-2">
                                <span className="material-symbols-outlined text-lg">error</span>
                                {error}
                            </div>
                        )}

                        {/* Confirmation */}
                        <div className="mb-6">
                            <label className="block text-sm font-bold text-[#334155] dark:text-[var(--text-secondary)] mb-2">
                                To confirm, type <code className="bg-red-50 dark:bg-red-500/10 text-red-600 dark:text-red-400 px-1.5 py-0.5 rounded text-xs font-bold">DELETE</code> in the box below:
                            </label>
                            <input
                                type="text"
                                value={confirmText}
                                onChange={(e) => setConfirmText(e.target.value.toUpperCase())}
                                placeholder="Type DELETE to confirm"
                                className="w-full px-4 py-3 bg-[var(--bg-tertiary)] border border-[#e2e8f0] dark:border-[var(--border-color)] rounded-xl text-[var(--text-primary)] placeholder-[#94a3b8] focus:border-red-500 focus:ring-2 focus:ring-red-500/10 outline-none transition"
                            />
                        </div>

                        {/* Actions */}
                        <div className="flex justify-between">
                            <Link href={`/questions/${questionId}`} className="flex items-center gap-1 px-5 py-2.5 rounded-xl font-bold text-sm border border-[#e2e8f0] dark:border-[var(--border-color)] text-[#334155] dark:text-[var(--text-secondary)] hover:bg-[#f1f5f9] dark:hover:bg-[var(--bg-tertiary)] transition-all">
                                <span className="material-symbols-outlined text-base">arrow_back</span>Cancel
                            </Link>
                            <button
                                type="button"
                                onClick={handleDelete}
                                disabled={isDeleting || confirmText !== 'DELETE'}
                                className="flex items-center gap-2 bg-red-600 text-white px-6 py-2.5 rounded-xl font-bold text-sm hover:bg-red-700 transition-all disabled:opacity-50 disabled:cursor-not-allowed"
                            >
                                {isDeleting ? (
                                    <>
                                        <div className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                                        Deleting...
                                    </>
                                ) : (
                                    <>
                                        <span className="material-symbols-outlined text-base">delete</span>Delete Question
                                    </>
                                )}
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        </AppLayout>
    );
}
