'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import apiClient from '@/lib/api/client';
import AppLayout from '@/components/AppLayout';

interface Tag {
    tagId: number;
    tagName: string;
    description?: string;
    questionCount: number;
}

interface TagsResponse {
    items?: Tag[];
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
            const response = await apiClient.get<TagsResponse | Tag[]>('/tags');
            // Handle both array and object response
            const data = response.data;
            if (Array.isArray(data)) {
                setTags(data);
            } else if (data && 'items' in data) {
                setTags(data.items || []);
            } else {
                setTags([]);
            }
        } catch (error) {
            console.error('Failed to fetch tags:', error);
            setTags([]);
        } finally {
            setIsLoading(false);
        }
    };

    const filteredTags = Array.isArray(tags)
        ? tags.filter(tag => tag.tagName.toLowerCase().includes(search.toLowerCase()))
        : [];

    return (
        <AppLayout>
            {/* Header */}
            <div className="mb-6">
                <h1 className="text-2xl font-bold text-[var(--text-primary)] flex items-center gap-2">
                    <i className="bi bi-tags-fill text-[var(--primary)]"></i>
                    Tags
                </h1>
                <p className="text-[var(--text-muted)]">Browse tags to find questions on topics you're interested in</p>
            </div>

            {/* Search */}
            <div className="mb-6">
                <div className="relative">
                    <i className="bi bi-search absolute left-4 top-1/2 -translate-y-1/2 text-[var(--text-muted)]"></i>
                    <input
                        type="text"
                        placeholder="Filter by tag name..."
                        value={search}
                        onChange={(e) => setSearch(e.target.value)}
                        className="w-full pl-11 pr-4 py-3 bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-xl text-[var(--text-primary)] placeholder-[var(--text-muted)] focus:border-[var(--primary)] focus:ring-2 focus:ring-[var(--primary)]/20 transition"
                    />
                </div>
            </div>

            {/* Tags Grid */}
            {isLoading ? (
                <div className="flex items-center justify-center py-16">
                    <div className="w-10 h-10 border-4 border-[var(--primary)]/30 border-t-[var(--primary)] rounded-full animate-spin"></div>
                </div>
            ) : filteredTags.length === 0 ? (
                <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-2xl p-12 text-center">
                    <i className="bi bi-tags text-5xl text-[var(--text-muted)] mb-4"></i>
                    <h3 className="text-lg font-semibold text-[var(--text-primary)] mb-2">No tags found</h3>
                    <p className="text-[var(--text-muted)]">Try a different search term</p>
                </div>
            ) : (
                <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-4">
                    {filteredTags.map((tag) => (
                        <Link
                            key={tag.tagId}
                            href={`/questions?tag=${tag.tagName}`}
                            className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-2xl p-5 hover:border-[var(--primary)]/50 transition group"
                        >
                            <div className="flex items-center justify-between mb-3">
                                <span className="px-3 py-1.5 bg-[var(--primary)]/10 text-[var(--primary)] rounded-lg font-medium text-sm">
                                    #{tag.tagName}
                                </span>
                                <span className="text-xs text-[var(--text-muted)] bg-[var(--bg-tertiary)] px-2 py-1 rounded-full">
                                    {tag.questionCount} questions
                                </span>
                            </div>
                            <p className="text-sm text-[var(--text-secondary)] line-clamp-2">
                                {tag.description || `Questions about ${tag.tagName}`}
                            </p>
                        </Link>
                    ))}
                </div>
            )}
        </AppLayout>
    );
}
