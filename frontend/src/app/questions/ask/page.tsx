'use client';

import { useState } from 'react';
import { useAuth } from '@/lib/contexts/AuthContext';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import apiClient from '@/lib/api/client';

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
            <div className="min-h-screen flex items-center justify-center bg-gray-50 dark:bg-slate-900">
                <div className="inline-block w-10 h-10 border-4 border-orange-500 border-t-transparent rounded-full animate-spin"></div>
            </div>
        );
    }

    if (!user) {
        return (
            <div className="min-h-screen flex items-center justify-center bg-gray-50 dark:bg-slate-900">
                <div className="text-center">
                    <div className="w-20 h-20 bg-gray-200 dark:bg-slate-700 rounded-full mx-auto mb-4 flex items-center justify-center">
                        <i className="bi bi-lock text-4xl text-gray-400"></i>
                    </div>
                    <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4">Please log in to ask a question</h2>
                    <Link href="/login" className="inline-flex items-center px-6 py-3 bg-orange-500 text-white rounded-lg hover:bg-orange-600 transition">
                        <i className="bi bi-box-arrow-in-right mr-2"></i>Log in
                    </Link>
                </div>
            </div>
        );
    }

    return (
        <div className="min-h-screen bg-gray-50 dark:bg-slate-900 py-8">
            <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8">
                {/* Header */}
                <div className="mb-8">
                    <h1 className="text-3xl font-bold text-gray-900 dark:text-white mb-2">Ask a public question</h1>
                    <p className="text-gray-500">Get answers from the DevCommunity community</p>
                </div>

                {error && (
                    <div className="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-red-700 dark:text-red-400 px-4 py-3 rounded-lg mb-6 flex items-center">
                        <i className="bi bi-exclamation-circle mr-2"></i>{error}
                    </div>
                )}

                <form onSubmit={handleSubmit} className="space-y-6">
                    {/* Title */}
                    <div className="bg-white dark:bg-slate-800 rounded-xl shadow-sm p-6">
                        <label htmlFor="title" className="block text-lg font-semibold text-gray-900 dark:text-white mb-2">
                            Title
                        </label>
                        <p className="text-sm text-gray-500 mb-3">
                            Be specific and imagine you&apos;re asking a question to another person.
                        </p>
                        <input
                            id="title"
                            type="text"
                            value={title}
                            onChange={(e) => setTitle(e.target.value)}
                            placeholder="e.g. How to center a div in CSS?"
                            required
                            className="w-full px-4 py-3 border border-gray-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-orange-500 focus:border-transparent transition"
                        />
                    </div>

                    {/* Body */}
                    <div className="bg-white dark:bg-slate-800 rounded-xl shadow-sm p-6">
                        <label htmlFor="body" className="block text-lg font-semibold text-gray-900 dark:text-white mb-2">
                            What are the details of your problem?
                        </label>
                        <p className="text-sm text-gray-500 mb-3">
                            Introduce the problem and expand on what you put in the title. Minimum 30 characters.
                        </p>
                        <textarea
                            id="body"
                            value={body}
                            onChange={(e) => setBody(e.target.value)}
                            rows={12}
                            required
                            className="w-full px-4 py-3 border border-gray-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-orange-500 focus:border-transparent transition resize-none"
                            placeholder="Describe your problem in detail..."
                        />
                        <div className="mt-2 text-sm text-gray-400">
                            {body.length} / 30 characters minimum
                        </div>
                    </div>

                    {/* Tags */}
                    <div className="bg-white dark:bg-slate-800 rounded-xl shadow-sm p-6">
                        <label htmlFor="tags" className="block text-lg font-semibold text-gray-900 dark:text-white mb-2">
                            Tags
                        </label>
                        <p className="text-sm text-gray-500 mb-3">
                            Add up to 5 tags to describe what your question is about (comma-separated).
                        </p>
                        <input
                            id="tags"
                            type="text"
                            value={tags}
                            onChange={(e) => setTags(e.target.value)}
                            placeholder="e.g. javascript, react, css"
                            className="w-full px-4 py-3 border border-gray-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-orange-500 focus:border-transparent transition"
                        />
                        <div className="mt-3 flex flex-wrap gap-2">
                            {tags.split(',').map((tag, idx) => tag.trim() && (
                                <span key={idx} className="px-3 py-1 bg-orange-100 dark:bg-orange-900/30 text-orange-600 dark:text-orange-400 text-sm rounded-md">
                                    {tag.trim()}
                                </span>
                            ))}
                        </div>
                    </div>

                    {/* Submit Button */}
                    <div className="flex items-center gap-4">
                        <button
                            type="submit"
                            disabled={isLoading || body.length < 30}
                            className="px-6 py-3 bg-orange-500 text-white font-medium rounded-lg hover:bg-orange-600 focus:ring-2 focus:ring-offset-2 focus:ring-orange-500 disabled:opacity-50 disabled:cursor-not-allowed transition"
                        >
                            {isLoading ? (
                                <>
                                    <i className="bi bi-arrow-repeat animate-spin mr-2"></i>Posting...
                                </>
                            ) : (
                                <>
                                    <i className="bi bi-send mr-2"></i>Post your question
                                </>
                            )}
                        </button>
                        <Link href="/questions" className="text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200">
                            Cancel
                        </Link>
                    </div>
                </form>

                {/* Tips Card */}
                <div className="mt-8 bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-xl p-6">
                    <h3 className="font-bold text-blue-900 dark:text-blue-300 mb-3">
                        <i className="bi bi-lightbulb mr-2"></i>Tips for asking a good question
                    </h3>
                    <ul className="text-sm text-blue-800 dark:text-blue-400 space-y-2">
                        <li><i className="bi bi-check-circle mr-2"></i>Search to see if your question has been asked before</li>
                        <li><i className="bi bi-check-circle mr-2"></i>Be specific and include code examples if possible</li>
                        <li><i className="bi bi-check-circle mr-2"></i>Describe what you expected to happen and what actually happened</li>
                        <li><i className="bi bi-check-circle mr-2"></i>Include relevant tags to help others find your question</li>
                    </ul>
                </div>
            </div>
        </div>
    );
}
