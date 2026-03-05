'use client';

import { useState } from 'react';
import { useAuth } from '@/lib/contexts/AuthContext';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import apiClient from '@/lib/api/client';
import AppLayout from '@/components/AppLayout';

export default function AskQuestionPage() {
    const { user, isLoading: authLoading } = useAuth();
    const router = useRouter();
    const [title, setTitle] = useState('');
    const [body, setBody] = useState('');
    const [tags, setTags] = useState('');
    const [error, setError] = useState('');
    const [isLoading, setIsLoading] = useState(false);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError('');
        setIsLoading(true);

        try {
            const tagList = tags.split(',').map(t => t.trim()).filter(t => t);
            const response = await apiClient.post('/questions', {
                title,
                body,
                tags: tagList,
            });
            router.push(`/questions/${response.data.questionId}`);
        } catch (err: unknown) {
            const error = err as { response?: { data?: { message?: string } } };
            setError(error.response?.data?.message || 'Failed to create question');
        } finally {
            setIsLoading(false);
        }
    };

    if (authLoading) {
        return (
            <AppLayout showRightSidebar={false}>
                <div className="flex items-center justify-center py-20">
                    <div className="w-10 h-10 border-4 border-[var(--primary)]/30 border-t-[var(--primary)] rounded-full animate-spin"></div>
                </div>
            </AppLayout>
        );
    }

    if (!user) {
        return (
            <AppLayout showRightSidebar={false}>
                <div className="flex flex-col items-center justify-center py-16 text-center">
                    <span className="material-symbols-outlined text-5xl text-[var(--text-muted)] mb-4">lock</span>
                    <h2 className="text-2xl font-bold text-[var(--text-primary)] mb-2">Please log in to ask a question</h2>
                    <p className="text-[var(--text-muted)] mb-6">You need to be signed in to participate in discussions</p>
                    <Link href="/login" className="px-6 py-3 bg-[var(--primary)] text-white rounded-xl font-medium hover:bg-[var(--primary-dark)] transition">
                        Log in to Continue
                    </Link>
                </div>
            </AppLayout>
        );
    }

    return (
        <AppLayout showRightSidebar={false}>
            {/* Header */}
            <div className="mb-6">
                <h1 className="text-2xl font-bold text-[var(--text-primary)] flex items-center gap-2">
                    <span className="material-symbols-outlined text-[var(--primary)]">help</span>
                    Ask a Question
                </h1>
                <p className="text-[var(--text-muted)]">Get help from the community by asking a well-crafted question</p>
            </div>

            {error && (
                <div className="mb-6 p-4 bg-red-500/10 border border-red-500/30 rounded-xl text-red-500 flex items-center gap-2">
                    <span className="material-symbols-outlined">error</span>
                    {error}
                </div>
            )}

            <form onSubmit={handleSubmit} className="space-y-6">
                {/* Title */}
                <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-2xl p-6">
                    <label htmlFor="title" className="block text-lg font-semibold text-[var(--text-primary)] mb-2">
                        Title
                    </label>
                    <p className="text-sm text-[var(--text-muted)] mb-3">
                        Be specific and imagine you&apos;re asking a question to another person.
                    </p>
                    <input
                        id="title"
                        type="text"
                        value={title}
                        onChange={(e) => setTitle(e.target.value)}
                        placeholder="e.g. How to center a div in CSS?"
                        required
                        className="w-full px-4 py-3 bg-[var(--bg-primary)] border border-[var(--border-color)] rounded-xl text-[var(--text-primary)] placeholder-[var(--text-muted)] focus:border-[var(--primary)] focus:ring-2 focus:ring-[var(--primary)]/20 transition"
                    />
                </div>

                {/* Body */}
                <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-2xl p-6">
                    <label htmlFor="body" className="block text-lg font-semibold text-[var(--text-primary)] mb-2">
                        What are the details of your problem?
                    </label>
                    <p className="text-sm text-[var(--text-muted)] mb-3">
                        Introduce the problem and expand on what you put in the title. Include code snippets if relevant.
                    </p>
                    <textarea
                        id="body"
                        value={body}
                        onChange={(e) => setBody(e.target.value)}
                        rows={12}
                        required
                        className="w-full px-4 py-3 bg-[var(--bg-primary)] border border-[var(--border-color)] rounded-xl text-[var(--text-primary)] placeholder-[var(--text-muted)] focus:border-[var(--primary)] focus:ring-2 focus:ring-[var(--primary)]/20 transition resize-none"
                        placeholder="Describe your problem in detail..."
                    />
                </div>

                {/* Tags */}
                <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-2xl p-6">
                    <label htmlFor="tags" className="block text-lg font-semibold text-[var(--text-primary)] mb-2">
                        Tags
                    </label>
                    <p className="text-sm text-[var(--text-muted)] mb-3">
                        Add up to 5 tags to describe what your question is about (comma-separated).
                    </p>
                    <input
                        id="tags"
                        type="text"
                        value={tags}
                        onChange={(e) => setTags(e.target.value)}
                        placeholder="e.g. javascript, react, css"
                        className="w-full px-4 py-3 bg-[var(--bg-primary)] border border-[var(--border-color)] rounded-xl text-[var(--text-primary)] placeholder-[var(--text-muted)] focus:border-[var(--primary)] focus:ring-2 focus:ring-[var(--primary)]/20 transition"
                    />
                    <div className="flex flex-wrap gap-2 mt-3">
                        {['javascript', 'react', 'python', 'csharp', 'css'].map(t => (
                            <button
                                key={t}
                                type="button"
                                onClick={() => setTags(prev => prev ? `${prev}, ${t}` : t)}
                                className="px-3 py-1 text-xs bg-[var(--bg-tertiary)] text-[var(--text-muted)] rounded-lg hover:bg-[var(--primary)]/20 hover:text-[var(--primary)] transition"
                            >
                                + {t}
                            </button>
                        ))}
                    </div>
                </div>

                {/* Submit */}
                <div className="flex items-center gap-4">
                    <button
                        type="submit"
                        disabled={isLoading}
                        className="flex items-center gap-2 px-6 py-3 bg-[var(--primary)] text-white rounded-xl font-medium hover:bg-[var(--primary-dark)] transition disabled:opacity-50"
                    >
                        {isLoading ? (
                            <>
                                <div className="w-5 h-5 border-2 border-white/30 border-t-white rounded-full animate-spin"></div>
                                Posting...
                            </>
                        ) : (
                            <>
                                <span className="material-symbols-outlined">send</span>
                                Post your question
                            </>
                        )}
                    </button>
                    <Link href="/questions" className="px-6 py-3 text-[var(--text-secondary)] hover:text-[var(--text-primary)] transition">
                        Cancel
                    </Link>
                </div>
            </form>
        </AppLayout>
    );
}