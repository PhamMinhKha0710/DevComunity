'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { useParams } from 'next/navigation';
import type { RepositoryCommit } from '@/types';
import apiClient from '@/lib/api/client';

export default function CommitsPage() {
    const params = useParams();
    const id = params.id as string;
    const [commits, setCommits] = useState<RepositoryCommit[]>([]);
    const [isLoading, setIsLoading] = useState(true);

    useEffect(() => {
        fetchCommits();
    }, [id]);

    const fetchCommits = async () => {
        try {
            const response = await apiClient.get<{ items: RepositoryCommit[] }>(`/repositories/${id}/commits`);
            setCommits(response.data.items || []);
        } catch (error) {
            console.error('Failed to fetch commits:', error);
        } finally {
            setIsLoading(false);
        }
    };

    const formatDate = (dateString: string) => {
        const date = new Date(dateString);
        return date.toLocaleDateString('en-US', {
            year: 'numeric',
            month: 'short',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        });
    };

    if (isLoading) {
        return (
            <div className="container py-5 text-center">
                <div className="spinner-border text-primary" role="status">
                    <span className="visually-hidden">Loading...</span>
                </div>
            </div>
        );
    }

    return (
        <div className="container py-4">
            {/* Breadcrumb */}
            <nav aria-label="breadcrumb" className="mb-4">
                <ol className="breadcrumb">
                    <li className="breadcrumb-item"><Link href="/repositories">Repositories</Link></li>
                    <li className="breadcrumb-item"><Link href={`/repositories/${id}`}>Repository</Link></li>
                    <li className="breadcrumb-item active">Commits</li>
                </ol>
            </nav>

            <h2 className="fw-bold mb-4">
                <i className="bi bi-clock-history me-2"></i>Commit History
            </h2>

            {/* Commits List */}
            <div className="card border-0 shadow-sm rounded-4">
                {commits.length > 0 ? (
                    <div className="list-group list-group-flush">
                        {commits.map((commit, index) => (
                            <div key={commit.sha} className="list-group-item p-4 border-0 border-bottom">
                                <div className="row align-items-center">
                                    <div className="col">
                                        <div className="d-flex align-items-start">
                                            <img
                                                src={commit.authorAvatar || '/images/default-avatar.png'}
                                                className="rounded-circle me-3 mt-1"
                                                width="40"
                                                height="40"
                                                alt={commit.authorName}
                                            />
                                            <div>
                                                <h6 className="mb-1 fw-bold">{commit.message}</h6>
                                                <div className="d-flex align-items-center gap-2 text-muted small">
                                                    <span className="fw-medium">{commit.authorName}</span>
                                                    <span>committed {formatDate(commit.committedDate)}</span>
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                    <div className="col-auto">
                                        <div className="d-flex gap-2 align-items-center">
                                            {commit.additions !== undefined && commit.deletions !== undefined && (
                                                <span className="text-muted small">
                                                    <span className="text-success">+{commit.additions}</span>
                                                    {' / '}
                                                    <span className="text-danger">-{commit.deletions}</span>
                                                </span>
                                            )}
                                            <code className="bg-light px-2 py-1 rounded small">{commit.sha.substring(0, 7)}</code>
                                            <button className="btn btn-sm btn-outline-secondary rounded-pill">
                                                <i className="bi bi-clipboard"></i>
                                            </button>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        ))}
                    </div>
                ) : (
                    <div className="card-body text-center py-5">
                        <i className="bi bi-clock-history fs-1 text-muted mb-3"></i>
                        <h5>No commits yet</h5>
                        <p className="text-muted">This repository has no commits.</p>
                    </div>
                )}
            </div>
        </div>
    );
}
