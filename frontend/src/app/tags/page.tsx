'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import apiClient from '@/lib/api/client';

interface Tag {
    tagId: number;
    tagName: string;
    description?: string;
    questionCount: number;
}

export default function TagsPage() {
    const [tags, setTags] = useState<Tag[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [search, setSearch] = useState('');

    useEffect(() => {
        fetchTags();
    }, []);

    const fetchTags = async () => {
        try {
            const response = await apiClient.get<Tag[]>('/tags');
            setTags(response.data || []);
        } catch (error) {
            console.error('Failed to fetch tags:', error);
        } finally {
            setIsLoading(false);
        }
    };

    const filteredTags = tags.filter(tag =>
        tag.tagName.toLowerCase().includes(search.toLowerCase())
    );

    return (
        <div className="min-h-screen bg-gray-50 dark:bg-slate-900 py-8">
            <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
                {/* Header */}
                <div className="mb-8">
                    <h1 className="text-3xl font-bold text-gray-900 dark:text-white mb-2">Tags</h1>
                    <p className="text-gray-500">Browse tags to find questions on topics you&apos;re interested in</p>
                </div>

                {/* Search */}
                <div className="bg-white dark:bg-slate-800 rounded-xl shadow-sm p-4 mb-6">
                    <div className="relative">
                        <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                            <i className="bi bi-search text-gray-400"></i>
                        </div>
                        <input
                            type="text"
                            className="w-full pl-10 pr-4 py-3 border border-gray-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-orange-500 focus:border-transparent transition"
                            placeholder="Filter by tag name..."
                            value={search}
                            onChange={(e) => setSearch(e.target.value)}
                        />
                    </div>
                </div>

                {/* Tags Grid */}
                {isLoading ? (
                    <div className="flex items-center justify-center py-12">
                        <div className="inline-block w-8 h-8 border-4 border-orange-500 border-t-transparent rounded-full animate-spin"></div>
                    </div>
                ) : filteredTags.length > 0 ? (
                    <div className="grid md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
                        {filteredTags.map((tag) => (
                            <div key={tag.tagId} className="bg-white dark:bg-slate-800 rounded-xl shadow-sm p-4 hover:shadow-md transition">
                                <div className="flex items-start justify-between mb-2">
                                    <Link
                                        href={`/questions?tag=${tag.tagName}`}
                                        className="px-3 py-1 bg-orange-100 dark:bg-orange-900/30 text-orange-600 dark:text-orange-400 text-sm font-medium rounded-md hover:bg-orange-200 transition"
                                    >
                                        {tag.tagName}
                                    </Link>
                                    <span className="text-xs text-gray-500 bg-gray-100 dark:bg-slate-700 px-2 py-1 rounded">
                                        {tag.questionCount} questions
                                    </span>
                                </div>
                                <p className="text-sm text-gray-600 dark:text-gray-400 line-clamp-2">
                                    {tag.description || `Questions about ${tag.tagName}`}
                                </p>
                            </div>
                        ))}
                    </div>
                ) : (
                    <div className="text-center py-12">
                        <div className="w-16 h-16 bg-gray-100 dark:bg-slate-700 rounded-full mx-auto mb-4 flex items-center justify-center">
                            <i className="bi bi-tags text-3xl text-gray-400"></i>
                        </div>
                        <h3 className="text-lg font-medium text-gray-900 dark:text-white mb-2">No tags found</h3>
                        <p className="text-gray-500">Try a different search term</p>
                    </div>
                )}
            </div>
        </div>
    );
}
