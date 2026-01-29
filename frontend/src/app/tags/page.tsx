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
        <div className="container py-4">
            <div className="d-flex justify-content-between align-items-center mb-4">
                <div>
                    <h2 className="fw-bold mb-1">Tags</h2>
                    <p className="text-muted mb-0">Browse tags to find questions on topics you're interested in</p>
                </div>
            </div>

            {/* Search */}
            <div className="card border-0 shadow-sm rounded-4 mb-4">
                <div className="card-body p-3">
                    <div className="input-group">
                        <span className="input-group-text bg-transparent border-end-0">
                            <i className="bi bi-search"></i>
                        </span>
                        <input
                            type="text"
                            className="form-control border-start-0"
                            placeholder="Filter by tag name..."
                            value={search}
                            onChange={(e) => setSearch(e.target.value)}
                        />
                    </div>
                </div>
            </div>

            {/* Tags Grid */}
            {isLoading ? (
                <div className="text-center py-5">
                    <div className="spinner-border text-primary" role="status">
                        <span className="visually-hidden">Loading...</span>
                    </div>
                </div>
            ) : (
                <div className="row g-4">
                    {filteredTags.map((tag) => (
                        <div key={tag.tagId} className="col-md-6 col-lg-4">
                            <div className="card border-0 shadow-sm rounded-4 h-100 hover-lift">
                                <div className="card-body">
                                    <div className="d-flex justify-content-between align-items-start mb-2">
                                        <Link href={`/questions?tag=${tag.tagName}`} className="btn btn-sm btn-outline-primary rounded-pill">
                                            {tag.tagName}
                                        </Link>
                                        <span className="badge bg-light text-dark">{tag.questionCount} questions</span>
                                    </div>
                                    <p className="text-muted small mb-0">
                                        {tag.description || `Questions about ${tag.tagName}`}
                                    </p>
                                </div>
                            </div>
                        </div>
                    ))}
                </div>
            )}

            {filteredTags.length === 0 && !isLoading && (
                <div className="text-center py-5">
                    <i className="bi bi-tags fs-1 text-muted mb-3"></i>
                    <h5>No tags found</h5>
                    <p className="text-muted">Try a different search term</p>
                </div>
            )}
        </div>
    );
}
