'use client';

import { useAuth } from '@/lib/contexts/AuthContext';
import { useRouter } from 'next/navigation';
import { useEffect } from 'react';
import Link from 'next/link';

export default function ProfilePage() {
    const { user, isLoading } = useAuth();
    const router = useRouter();

    useEffect(() => {
        if (!isLoading && !user) {
            router.push('/login');
        }
    }, [user, isLoading, router]);

    if (isLoading) {
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
            <div className="row">
                {/* Profile Sidebar */}
                <div className="col-lg-4 mb-4">
                    <div className="card border-0 shadow-sm rounded-4">
                        <div className="card-body text-center p-4">
                            <img
                                src={user.profilePicture || '/images/default-avatar.png'}
                                className="rounded-circle mb-3"
                                width="120"
                                height="120"
                                alt="Profile"
                            />
                            <h4 className="fw-bold mb-1">{user.displayName || user.username}</h4>
                            <p className="text-muted mb-3">@{user.username}</p>

                            <div className="row g-3 text-center mb-4">
                                <div className="col-4">
                                    <div className="p-3 rounded-4 bg-primary-subtle">
                                        <h5 className="fw-bold mb-0 text-primary">{user.reputationPoints || 0}</h5>
                                        <small className="text-muted">Reputation</small>
                                    </div>
                                </div>
                                <div className="col-4">
                                    <div className="p-3 rounded-4 bg-light">
                                        <h5 className="fw-bold mb-0">0</h5>
                                        <small className="text-muted">Questions</small>
                                    </div>
                                </div>
                                <div className="col-4">
                                    <div className="p-3 rounded-4 bg-light">
                                        <h5 className="fw-bold mb-0">0</h5>
                                        <small className="text-muted">Answers</small>
                                    </div>
                                </div>
                            </div>

                            <Link href="/settings" className="btn btn-outline-primary rounded-pill d-block">
                                <i className="bi bi-gear me-2"></i>Edit Profile
                            </Link>
                        </div>
                    </div>

                    {/* Badges Card */}
                    <div className="card border-0 shadow-sm rounded-4 mt-4">
                        <div className="card-body">
                            <h6 className="fw-bold mb-3">Badges</h6>
                            <div className="d-flex flex-wrap gap-2">
                                <span className="badge bg-warning rounded-pill px-3 py-2">
                                    <i className="bi bi-award me-1"></i>Gold: 0
                                </span>
                                <span className="badge bg-secondary rounded-pill px-3 py-2">
                                    <i className="bi bi-award me-1"></i>Silver: 0
                                </span>
                                <span className="badge bg-orange rounded-pill px-3 py-2" style={{ backgroundColor: '#cd7f32' }}>
                                    <i className="bi bi-award me-1"></i>Bronze: 0
                                </span>
                            </div>
                        </div>
                    </div>
                </div>

                {/* Main Content */}
                <div className="col-lg-8">
                    {/* Activity Tabs */}
                    <div className="card border-0 shadow-sm rounded-4 mb-4">
                        <div className="card-header bg-transparent border-0 pt-3">
                            <ul className="nav nav-pills" id="profileTab" role="tablist">
                                <li className="nav-item" role="presentation">
                                    <button className="nav-link active rounded-pill" data-bs-toggle="pill" data-bs-target="#questions">
                                        Questions
                                    </button>
                                </li>
                                <li className="nav-item" role="presentation">
                                    <button className="nav-link rounded-pill" data-bs-toggle="pill" data-bs-target="#answers">
                                        Answers
                                    </button>
                                </li>
                                <li className="nav-item" role="presentation">
                                    <button className="nav-link rounded-pill" data-bs-toggle="pill" data-bs-target="#saved">
                                        Saved
                                    </button>
                                </li>
                            </ul>
                        </div>
                        <div className="card-body">
                            <div className="tab-content">
                                <div className="tab-pane fade show active" id="questions">
                                    <div className="text-center py-5 text-muted">
                                        <i className="bi bi-chat-square-text fs-1 mb-3"></i>
                                        <p>You haven't asked any questions yet</p>
                                        <Link href="/questions/ask" className="btn btn-primary rounded-pill">
                                            Ask your first question
                                        </Link>
                                    </div>
                                </div>
                                <div className="tab-pane fade" id="answers">
                                    <div className="text-center py-5 text-muted">
                                        <i className="bi bi-chat-left-text fs-1 mb-3"></i>
                                        <p>You haven't answered any questions yet</p>
                                        <Link href="/questions" className="btn btn-outline-primary rounded-pill">
                                            Browse questions
                                        </Link>
                                    </div>
                                </div>
                                <div className="tab-pane fade" id="saved">
                                    <div className="text-center py-5 text-muted">
                                        <i className="bi bi-bookmark fs-1 mb-3"></i>
                                        <p>No saved items</p>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
}
