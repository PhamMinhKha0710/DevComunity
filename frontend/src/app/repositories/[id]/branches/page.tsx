'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { useParams } from 'next/navigation';
import apiClient from '@/lib/api/client';

interface Branch {
    name: string;
    isDefault: boolean;
    lastCommit?: {
        sha: string;
        message: string;
        date: string;
    };
}

export default function BranchesPage() {
    const params = useParams();
    const id = params.id as string;
    const [branches, setBranches] = useState<Branch[]>([]);
    const [isLoading, setIsLoading] = useState(true);

    useEffect(() => {
        fetchBranches();
    }, [id]);

    const fetchBranches = async () => {
        try {
            // Mock data since API doesn't have branches endpoint yet
            setBranches([
                { name: 'main', isDefault: true, lastCommit: { sha: 'abc1234', message: 'Initial commit', date: new Date().toISOString() } },
                { name: 'develop', isDefault: false },
            ]);
        } catch (error) {
            console.error('Failed to fetch branches:', error);
        } finally {
            setIsLoading(false);
        }
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
                    <li className="breadcrumb-item active">Branches</li>
                </ol>
            </nav>

            <div className="d-flex justify-content-between align-items-center mb-4">
                <h2 className="fw-bold mb-0">
                    <i className="bi bi-diagram-3 me-2"></i>Branches
                </h2>
                <button className="btn btn-primary rounded-pill">
                    <i className="bi bi-plus-circle me-2"></i>New Branch
                </button>
            </div>

            {/* Branches List */}
            <div className="card border-0 shadow-sm rounded-4">
                {branches.length > 0 ? (
                    <div className="list-group list-group-flush">
                        {branches.map((branch) => (
                            <div key={branch.name} className="list-group-item p-4 border-0 border-bottom">
                                <div className="row align-items-center">
                                    <div className="col">
                                        <div className="d-flex align-items-center">
                                            <i className="bi bi-git me-2 text-primary"></i>
                                            <h6 className="mb-0 fw-bold">{branch.name}</h6>
                                            {branch.isDefault && (
                                                <span className="badge bg-success ms-2 rounded-pill">Default</span>
                                            )}
                                        </div>
                                        {branch.lastCommit && (
                                            <small className="text-muted">
                                                Last commit: {branch.lastCommit.message} ({branch.lastCommit.sha.substring(0, 7)})
                                            </small>
                                        )}
                                    </div>
                                    <div className="col-auto">
                                        <div className="dropdown">
                                            <button className="btn btn-sm btn-outline-secondary rounded-pill" data-bs-toggle="dropdown">
                                                <i className="bi bi-three-dots"></i>
                                            </button>
                                            <ul className="dropdown-menu dropdown-menu-end">
                                                <li>
                                                    <Link href={`/repositories/${id}?branch=${branch.name}`} className="dropdown-item">
                                                        <i className="bi bi-eye me-2"></i>View files
                                                    </Link>
                                                </li>
                                                <li>
                                                    <Link href={`/repositories/${id}/commits?branch=${branch.name}`} className="dropdown-item">
                                                        <i className="bi bi-clock-history me-2"></i>View commits
                                                    </Link>
                                                </li>
                                                {!branch.isDefault && (
                                                    <>
                                                        <li><hr className="dropdown-divider" /></li>
                                                        <li>
                                                            <button className="dropdown-item text-danger">
                                                                <i className="bi bi-trash me-2"></i>Delete branch
                                                            </button>
                                                        </li>
                                                    </>
                                                )}
                                            </ul>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        ))}
                    </div>
                ) : (
                    <div className="card-body text-center py-5">
                        <i className="bi bi-diagram-3 fs-1 text-muted mb-3"></i>
                        <h5>No branches</h5>
                        <p className="text-muted">This repository has no branches yet.</p>
                    </div>
                )}
            </div>
        </div>
    );
}
