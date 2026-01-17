'use client';

import { useEffect, useState } from 'react';
import { useParams } from 'next/navigation';
import Link from 'next/link';
import type { User, Question, Answer } from '@/types';
import apiClient from '@/lib/api/client';

interface UserProfile extends User {
    bio?: string;
    location?: string;
    website?: string;
    memberSince?: string;
    postCount?: number;
    answerCount?: number;
    badges?: { name: string; type: string }[];
}

export default function UserProfilePage() {
    const params = useParams();
    const userId = params.id as string;
    const [user, setUser] = useState<UserProfile | null>(null);
    const [questions, setQuestions] = useState<Question[]>([]);
    const [answers, setAnswers] = useState<Answer[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [activeTab, setActiveTab] = useState<'questions' | 'answers' | 'about'>('questions');

    useEffect(() => {
        fetchUserProfile();
    }, [userId]);

    const fetchUserProfile = async () => {
        try {
            const [userRes, questionsRes] = await Promise.all([
                apiClient.get<UserProfile>(`/users/${userId}`),
                apiClient.get<{ items: Question[] }>(`/users/${userId}/questions`)
            ]);
            setUser(userRes.data);
            setQuestions(questionsRes.data.items || []);
        } catch (error) {
            console.error('Failed to fetch user profile:', error);
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

    if (!user) {
        return (
            <div className="container py-5 text-center">
                <i className="bi bi-person-x fs-1 text-muted mb-3"></i>
                <h3>User not found</h3>
                <Link href="/users" className="btn btn-primary mt-3">Browse Users</Link>
            </div>
        );
    }

    return (
        <div className="container py-4">
            {/* Profile Header */}
            <div className="card border-0 shadow-sm rounded-4 mb-4 overflow-hidden">
                <div className="bg-primary text-white p-4" style={{
                    background: 'linear-gradient(135deg, #6a11cb 0%, #2575fc 100%)'
                }}>
                    <div className="row align-items-center">
                        <div className="col-auto">
                            <img
                                src={user.profilePicture || '/images/default-avatar.png'}
                                className="rounded-circle border border-3 border-white"
                                width="120"
                                height="120"
                                alt={user.displayName || user.username}
                            />
                        </div>
                        <div className="col">
                            <h1 className="fw-bold mb-1">{user.displayName || user.username}</h1>
                            <p className="mb-2 opacity-75">@{user.username}</p>
                            {user.location && (
                                <p className="mb-0 small">
                                    <i className="bi bi-geo-alt me-1"></i>{user.location}
                                </p>
                            )}
                        </div>
                        <div className="col-auto">
                            <div className="d-flex gap-3 text-center">
                                <div className="bg-white bg-opacity-10 rounded-3 px-3 py-2">
                                    <div className="fw-bold fs-5">{user.reputationPoints || 0}</div>
                                    <small className="text-uppercase opacity-75" style={{ letterSpacing: '1px', fontSize: '0.7rem' }}>Reputation</small>
                                </div>
                                <div className="bg-white bg-opacity-10 rounded-3 px-3 py-2">
                                    <div className="fw-bold fs-5">{questions.length}</div>
                                    <small className="text-uppercase opacity-75" style={{ letterSpacing: '1px', fontSize: '0.7rem' }}>Questions</small>
                                </div>
                                <div className="bg-white bg-opacity-10 rounded-3 px-3 py-2">
                                    <div className="fw-bold fs-5">{user.answerCount || 0}</div>
                                    <small className="text-uppercase opacity-75" style={{ letterSpacing: '1px', fontSize: '0.7rem' }}>Answers</small>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <div className="row">
                {/* Main Content */}
                <div className="col-lg-8">
                    {/* Tabs */}
                    <ul className="nav nav-tabs mb-4">
                        <li className="nav-item">
                            <button
                                className={`nav-link ${activeTab === 'questions' ? 'active' : ''}`}
                                onClick={() => setActiveTab('questions')}
                            >
                                <i className="bi bi-question-circle me-1"></i>Questions ({questions.length})
                            </button>
                        </li>
                        <li className="nav-item">
                            <button
                                className={`nav-link ${activeTab === 'answers' ? 'active' : ''}`}
                                onClick={() => setActiveTab('answers')}
                            >
                                <i className="bi bi-chat-left-text me-1"></i>Answers ({user.answerCount || 0})
                            </button>
                        </li>
                        <li className="nav-item">
                            <button
                                className={`nav-link ${activeTab === 'about' ? 'active' : ''}`}
                                onClick={() => setActiveTab('about')}
                            >
                                <i className="bi bi-person me-1"></i>About
                            </button>
                        </li>
                    </ul>

                    {/* Tab Content */}
                    {activeTab === 'questions' && (
                        <div className="card border-0 shadow-sm rounded-4">
                            {questions.length > 0 ? (
                                <div className="list-group list-group-flush">
                                    {questions.map((q) => (
                                        <Link
                                            key={q.questionId}
                                            href={`/questions/${q.questionId}`}
                                            className="list-group-item list-group-item-action p-3"
                                        >
                                            <div className="d-flex justify-content-between">
                                                <h6 className="mb-1 fw-semibold">{q.title}</h6>
                                                <span className={`badge ${q.hasAcceptedAnswer ? 'bg-success' : 'bg-secondary'} rounded-pill`}>
                                                    {q.answerCount} answers
                                                </span>
                                            </div>
                                            <div className="d-flex gap-3 text-muted small">
                                                <span><i className="bi bi-hand-thumbs-up me-1"></i>{q.score}</span>
                                                <span><i className="bi bi-eye me-1"></i>{q.viewCount}</span>
                                                <span>{new Date(q.createdDate).toLocaleDateString()}</span>
                                            </div>
                                        </Link>
                                    ))}
                                </div>
                            ) : (
                                <div className="card-body text-center py-5">
                                    <i className="bi bi-question-circle fs-1 text-muted mb-3"></i>
                                    <p className="text-muted">No questions yet</p>
                                </div>
                            )}
                        </div>
                    )}

                    {activeTab === 'answers' && (
                        <div className="card border-0 shadow-sm rounded-4">
                            <div className="card-body text-center py-5">
                                <i className="bi bi-chat-left-text fs-1 text-muted mb-3"></i>
                                <p className="text-muted">Answers will be displayed here</p>
                            </div>
                        </div>
                    )}

                    {activeTab === 'about' && (
                        <div className="card border-0 shadow-sm rounded-4">
                            <div className="card-body p-4">
                                <h5 className="fw-bold mb-3">About</h5>
                                <p className="text-muted">{user.bio || 'This user has not added a bio yet.'}</p>

                                <hr />

                                <div className="row g-3">
                                    <div className="col-md-6">
                                        <div className="d-flex align-items-center text-muted">
                                            <i className="bi bi-calendar3 me-2"></i>
                                            <span>Joined {user.memberSince ? new Date(user.memberSince).toLocaleDateString() : 'Recently'}</span>
                                        </div>
                                    </div>
                                    {user.website && (
                                        <div className="col-md-6">
                                            <div className="d-flex align-items-center">
                                                <i className="bi bi-link-45deg me-2 text-muted"></i>
                                                <a href={user.website} target="_blank" rel="noopener noreferrer">{user.website}</a>
                                            </div>
                                        </div>
                                    )}
                                </div>
                            </div>
                        </div>
                    )}
                </div>

                {/* Sidebar */}
                <div className="col-lg-4">
                    {/* Badges */}
                    <div className="card border-0 shadow-sm rounded-4 mb-4">
                        <div className="card-header bg-white border-0 pt-4">
                            <h5 className="fw-bold mb-0"><i className="bi bi-award me-2 text-warning"></i>Badges</h5>
                        </div>
                        <div className="card-body">
                            {user.badges && user.badges.length > 0 ? (
                                <div className="d-flex flex-wrap gap-2">
                                    {user.badges.map((badge, idx) => (
                                        <span key={idx} className={`badge rounded-pill ${badge.type === 'gold' ? 'bg-warning' :
                                                badge.type === 'silver' ? 'bg-secondary' : 'bg-info'
                                            }`}>
                                            {badge.name}
                                        </span>
                                    ))}
                                </div>
                            ) : (
                                <p className="text-muted mb-0">No badges yet</p>
                            )}
                        </div>
                    </div>

                    {/* Stats Card */}
                    <div className="card border-0 shadow-sm rounded-4">
                        <div className="card-header bg-white border-0 pt-4">
                            <h5 className="fw-bold mb-0"><i className="bi bi-graph-up me-2 text-success"></i>Stats</h5>
                        </div>
                        <div className="card-body">
                            <div className="row g-3">
                                <div className="col-6">
                                    <div className="text-center p-3 bg-light rounded-3">
                                        <div className="fs-4 fw-bold text-primary">{user.reputationPoints || 0}</div>
                                        <small className="text-muted">Reputation</small>
                                    </div>
                                </div>
                                <div className="col-6">
                                    <div className="text-center p-3 bg-light rounded-3">
                                        <div className="fs-4 fw-bold text-success">{questions.length}</div>
                                        <small className="text-muted">Questions</small>
                                    </div>
                                </div>
                                <div className="col-6">
                                    <div className="text-center p-3 bg-light rounded-3">
                                        <div className="fs-4 fw-bold text-info">{user.answerCount || 0}</div>
                                        <small className="text-muted">Answers</small>
                                    </div>
                                </div>
                                <div className="col-6">
                                    <div className="text-center p-3 bg-light rounded-3">
                                        <div className="fs-4 fw-bold text-warning">{user.badges?.length || 0}</div>
                                        <small className="text-muted">Badges</small>
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
