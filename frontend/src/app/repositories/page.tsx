'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { useSearchParams } from 'next/navigation';
import { Suspense } from 'react';
import type { Repository, PaginatedResponse } from '@/types';
import apiClient from '@/lib/api/client';

function RepositoriesContent() {
    const searchParams = useSearchParams();
    const [repositories, setRepositories] = useState<Repository[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [search, setSearch] = useState(searchParams.get('search') || '');

    useEffect(() => {
        fetchRepositories();
    }, [search]);

    const fetchRepositories = async () => {
        setIsLoading(true);
        try {
            let url = '/repositories?page=1&pageSize=20';
            if (search) url += `&search=${encodeURIComponent(search)}`;

            const response = await apiClient.get<PaginatedResponse<Repository>>(url);
            setRepositories(response.data.items || []);
        } catch (error) {
            console.error('Failed to fetch repositories:', error);
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <div className="container py-4">
            {/* Header */}
            <div className="d-flex align-items-center justify-content-between mb-4">
                <div>
                    <h1 className="fw-bold mb-1">
                        <i className="bi bi-archive me-2"></i>Code Repositories
                    </h1>
                    <p className="text-muted mb-0">Explore and share code with the community</p>
                </div>
                <div className="d-flex gap-2">
                    <Link href="/repositories/create" className="btn btn-primary rounded-pill">
                        <i className="bi bi-plus-circle me-2"></i>Create Repository
                    </Link>
                    <Link href="/repositories/my" className="btn btn-outline-primary rounded-pill">
                        <i className="bi bi-person me-2"></i>My Repositories
                    </Link>
                </div>
            </div>

            {/* Search */}
            <div className="card border-0 shadow-sm rounded-4 mb-4">
                <div className="card-body p-3">
                    <form onSubmit={(e) => { e.preventDefault(); fetchRepositories(); }}>
                        <div className="input-group">
                            <span className="input-group-text bg-transparent border-end-0">
                                <i className="bi bi-search"></i>
                            </span>
                            <input
                                type="text"
                                className="form-control border-start-0"
                                placeholder="Search repositories by name, description or language..."
                                value={search}
                                onChange={(e) => setSearch(e.target.value)}
                            />
                            <button type="submit" className="btn btn-primary">Search</button>
                        </div>
                    </form>
                </div>
            </div>

            {/* Repository Grid */}
            {isLoading ? (
                <div className="text-center py-5">
                    <div className="spinner-border text-primary" role="status">
                        <span className="visually-hidden">Loading...</span>
                    </div>
                </div>
            ) : repositories.length > 0 ? (
                <div className="row row-cols-1 row-cols-md-2 g-4">
                    {repositories.map((repo) => (
                        <div key={repo.repositoryId} className="col">
                            <div className="card h-100 border-0 shadow-sm rounded-4 hover-lift">
                                <div className="card-body">
                                    {/* Owner */}
                                    <div className="d-flex align-items-center mb-2">
                                        <img
                                            src={repo.ownerProfilePicture || '/images/default-avatar.png'}
                                            className="rounded-circle me-2"
                                            width="32"
                                            height="32"
                                            alt={repo.ownerUsername}
                                        />
                                        <Link href={`/users/${repo.ownerId}`} className="text-decoration-none text-muted">
                                            {repo.ownerUsername}
                                        </Link>
                                    </div>

                                    {/* Name */}
                                    <h5 className="card-title fw-bold">
                                        <Link href={`/repositories/${repo.repositoryId}`} className="text-decoration-none text-dark">
                                            {repo.repositoryName}
                                        </Link>
                                    </h5>

                                    {/* Description */}
                                    <p className="card-text text-muted">
                                        {repo.description || <span className="fst-italic">No description provided</span>}
                                    </p>

                                    {/* Stats & Badges */}
                                    <div className="d-flex justify-content-between align-items-center">
                                        <div className="d-flex gap-2">
                                            <span className="badge bg-secondary rounded-pill">{repo.defaultBranch || 'main'}</span>
                                            <span className={`badge rounded-pill ${repo.visibility === 'Private' ? 'bg-danger' : 'bg-success'}`}>
                                                {repo.visibility}
                                            </span>
                                            {repo.language && (
                                                <span className="badge bg-primary rounded-pill">{repo.language}</span>
                                            )}
                                        </div>
                                        <div className="d-flex gap-3 text-muted small">
                                            <span><i className="bi bi-star me-1"></i>{repo.starsCount || 0}</span>
                                            <span><i className="bi bi-diagram-2 me-1"></i>{repo.forksCount || 0}</span>
                                        </div>
                                    </div>
                                </div>
                                <div className="card-footer bg-transparent border-0">
                                    <div className="d-flex justify-content-between align-items-center">
                                        <small className="text-muted">
                                            <i className="bi bi-clock me-1"></i>
                                            Updated {new Date(repo.updatedDate || repo.createdDate).toLocaleDateString()}
                                        </small>
                                        <Link href={`/repositories/${repo.repositoryId}`} className="btn btn-sm btn-outline-primary rounded-pill">
                                            <i className="bi bi-code-square me-1"></i>View
                                        </Link>
                                    </div>
                                </div>
                            </div>
                        </div>
                    ))}
                </div>
            ) : (
                <div className="text-center py-5">
                    <div className="mb-4">
                        <i className="bi bi-archive fs-1 text-muted"></i>
                    </div>
                    <h5>No repositories found</h5>
                    <p className="text-muted mb-4">
                        {search ? 'Try a different search term' : 'Be the first to create a repository!'}
                    </p>
                    <Link href="/repositories/create" className="btn btn-primary rounded-pill">
                        <i className="bi bi-plus-circle me-2"></i>Create Repository
                    </Link>
                </div>
            )}
        </div>
    );
}

export default function RepositoriesPage() {
    return (
        <Suspense fallback={
            <div className="container py-5 text-center">
                <div className="spinner-border text-primary" role="status">
                    <span className="visually-hidden">Loading...</span>
                </div>
            </div>
        }>
            <RepositoriesContent />
        </Suspense>
    );
}
