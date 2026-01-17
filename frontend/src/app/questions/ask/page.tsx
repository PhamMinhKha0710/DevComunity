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
            <div className="min-h-screen flex items-center justify-center">
                <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-orange-500"></div>
            </div>
        );
    }

    if (!user) {
        return (
            <div className="min-h-screen flex items-center justify-center bg-gray-50">
                <div className="text-center">
                    <h2 className="text-2xl font-bold text-gray-900 mb-4">Please log in to ask a question</h2>
                    <Link href="/login" className="bg-orange-500 text-white px-6 py-3 rounded-md hover:bg-orange-600 transition">
                        Log in
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

            <main className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
                <h1 className="text-3xl font-bold text-gray-900 mb-8">Ask a public question</h1>

                {error && (
                    <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded mb-6">
                        {error}
                    </div>
                )}

                <form onSubmit={handleSubmit} className="space-y-6">
                    <div className="bg-white p-6 rounded-lg shadow">
                        <label htmlFor="title" className="block text-lg font-semibold text-gray-900 mb-2">
                            Title
                        </label>
                        <p className="text-sm text-gray-500 mb-2">
                            Be specific and imagine you&apos;re asking a question to another person.
                        </p>
                        <input
                            id="title"
                            type="text"
                            value={title}
                            onChange={(e) => setTitle(e.target.value)}
                            placeholder="e.g. How to center a div in CSS?"
                            required
                            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-orange-500 focus:border-orange-500"
                        />
                    </div>

                    <div className="bg-white p-6 rounded-lg shadow">
                        <label htmlFor="body" className="block text-lg font-semibold text-gray-900 mb-2">
                            What are the details of your problem?
                        </label>
                        <p className="text-sm text-gray-500 mb-2">
                            Introduce the problem and expand on what you put in the title.
                        </p>
                        <textarea
                            id="body"
                            value={body}
                            onChange={(e) => setBody(e.target.value)}
                            rows={10}
                            required
                            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-orange-500 focus:border-orange-500"
                            placeholder="Describe your problem in detail..."
                        />
                    </div>

                    <div className="bg-white p-6 rounded-lg shadow">
                        <label htmlFor="tags" className="block text-lg font-semibold text-gray-900 mb-2">
                            Tags
                        </label>
                        <p className="text-sm text-gray-500 mb-2">
                            Add up to 5 tags to describe what your question is about (comma-separated).
                        </p>
                        <input
                            id="tags"
                            type="text"
                            value={tags}
                            onChange={(e) => setTags(e.target.value)}
                            placeholder="e.g. javascript, react, css"
                            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-orange-500 focus:border-orange-500"
                        />
                    </div>

                    <button
                        type="submit"
                        disabled={isLoading}
                        className="bg-blue-600 text-white px-6 py-3 rounded-md hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 disabled:opacity-50 transition"
                    >
                        {isLoading ? 'Posting...' : 'Post your question'}
                    </button>
                </form>
            </main>
        </div>
    );
}
