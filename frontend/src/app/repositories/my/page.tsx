'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useAuth } from '@/lib/contexts/AuthContext';
import type { Repository, PaginatedResponse } from '@/types';
import apiClient from '@/lib/api/client';

export default function MyRepositoriesPage() {
    const { user, isLoading: authLoading } = useAuth();
    const router = useRouter();
    const [repositories, setRepositories] = useState<Repository[]>([]);
    const [isLoading, setIsLoading] = useState(true);

    useEffect(() => {
        if (!authLoading && !user) {
            router.push('/login');
        } else if (user) {
            fetchMyRepositories();
        }
    }, [user, authLoading, router]);

    const fetchMyRepositories = async () => {
        try {
            // Fetch user's repositories
            const response = await apiClient.get<PaginatedResponse<Repository>>(`/repositories?ownerId=${user?.userId}`);
            setRepositories(response.data.items || []);
        } catch (error) {
            console.error('Failed to fetch repositories:', error);
        } finally {
            setIsLoading(false);
        }
    };

    if (authLoading || isLoading) {
        return (
            <div className="container py-5 text-center">
                <div className="spinner-border text-primary" role="status">
                    <span className="visually-hidden">Loading...</span>
                </div>
            </div>
        );
    }

    if (!user) return null;

    return (
        <div className="container py-4">
            {/* Header */}
            <div className="d-flex align-items-center justify-content-between mb-4">
                <div>
                    <nav aria-label="breadcrumb">
                        <ol className="breadcrumb">
                            <li className="breadcrumb-item"><Link href="/repositories">Repositories</Link></li>
                            <li className="breadcrumb-item active">My Repositories</li>
                        </ol>
                    </nav>
                    <h1 className="fw-bold mb-1">
                        <i className="bi bi-person me-2"></i>My Repositories
                    </h1>
                    <p className="text-muted mb-0">Manage your code repositories</p>
                </div>
                <Link href="/repositories/create" className="btn btn-primary rounded-pill">
                    <i className="bi bi-plus-circle me-2"></i>New Repository
                </Link>
            </div>

            {/* Repository List */}
            {repositories.length > 0 ? (
                <div className="card border-0 shadow-sm rounded-4">
                    <div className="list-group list-group-flush">
                        {repositories.map((repo) => (
                            <div key={repo.repositoryId} className="list-group-item p-4 border-0 border-bottom">
                                <div className="row align-items-center">
                                    <div className="col">
                                        <div className="d-flex align-items-center mb-2">
                                            <i className={`bi ${repo.visibility === 'Private' ? 'bi-lock text-warning' : 'bi-unlock text-success'} me-2`}></i>
                                            <h5 className="mb-0 fw-bold">
                                                <Link href={`/repositories/${repo.repositoryId}`} className="text-decoration-none text-dark">
                                                    {repo.repositoryName}
                                                </Link>
                                            </h5>
                                            <span className={`badge ms-2 rounded-pill ${repo.visibility === 'Private' ? 'bg-danger' : 'bg-success'}`}>
                                                {repo.visibility}
                                            </span>
                                        </div>
                                        <p className="text-muted mb-2">
                                            {repo.description || <span className="fst-italic">No description</span>}
                                        </p>
                                        <div className="d-flex gap-3 small text-muted">
                                            {repo.language && (
                                                <span><i className="bi bi-circle-fill me-1" style={{ fontSize: '0.5rem' }}></i>{repo.language}</span>
                                            )}
                                            <span><i className="bi bi-star me-1"></i>{repo.starsCount || 0}</span>
                                            <span><i className="bi bi-diagram-2 me-1"></i>{repo.forksCount || 0}</span>
                                            <span>Updated {new Date(repo.updatedDate || repo.createdDate).toLocaleDateString()}</span>
                                        </div>
                                    </div>
                                    <div className="col-auto">
                                        <div className="dropdown">
                                            <button className="btn btn-sm btn-outline-secondary rounded-pill" data-bs-toggle="dropdown">
                                                <i className="bi bi-three-dots"></i>
                                            </button>
                                            <ul className="dropdown-menu dropdown-menu-end">
                                                <li>
                                                    <Link href={`/repositories/${repo.repositoryId}`} className="dropdown-item">
                                                        <i className="bi bi-eye me-2"></i>View
                                                    </Link>
                                                </li>
                                                <li>
                                                    <Link href={`/repositories/${repo.repositoryId}/settings`} className="dropdown-item">
                                                        <i className="bi bi-gear me-2"></i>Settings
                                                    </Link>
                                                </li>
                                                <li><hr className="dropdown-divider" /></li>
                                                <li>
                                                    <button className="dropdown-item text-danger">
                                                        <i className="bi bi-trash me-2"></i>Delete
                                                    </button>
                                                </li>
                                            </ul>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        ))}
                    </div>
                </div>
            ) : (
                <div className="text-center py-5">
                    <div className="mb-4">
                        <i className="bi bi-archive fs-1 text-muted"></i>
                    </div>
                    <h5>You don't have any repositories yet</h5>
                    <p className="text-muted mb-4">
                        Create your first repository to start sharing code.
                    </p>
                    <Link href="/repositories/create" className="btn btn-primary rounded-pill">
                        <i className="bi bi-plus-circle me-2"></i>Create your first repository
                    </Link>
                </div>
            )}
        </div>
    );
}
