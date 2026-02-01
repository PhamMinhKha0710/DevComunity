'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import apiClient from '@/lib/api/client';
import { useAuth } from '@/lib/contexts/AuthContext';

interface User {
    userId: number;
    username: string;
    displayName?: string;
    profilePicture?: string;
    reputationPoints: number;
    questionCount?: number;
    answerCount?: number;
    createdDate: string;
}

export default function UsersPage() {
    const { user: currentUser } = useAuth();
    const [users, setUsers] = useState<User[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [search, setSearch] = useState('');
    const [sortBy, setSortBy] = useState('reputation');
    const [followingIds, setFollowingIds] = useState<Set<number>>(new Set());
    const [followLoading, setFollowLoading] = useState<number | null>(null);

    useEffect(() => {
        fetchUsers();
    }, [sortBy]);

    useEffect(() => {
        if (currentUser) {
            fetchFollowing();
        }
    }, [currentUser]);

    const fetchFollowing = async () => {
        if (!currentUser) return;
        try {
            const response = await apiClient.get<any[]>(`/Follow/following/${currentUser.userId}`);
            const ids = new Set(response.data.map((f: any) => f.userId));
            setFollowingIds(ids);
        } catch (error) {
            console.error('Failed to fetch following list:', error);
        }
    };

    const handleFollow = async (targetId: number) => {
        if (!currentUser) return;
        setFollowLoading(targetId);
        try {
            await apiClient.post(`/Follow/${targetId}`);
            setFollowingIds(prev => new Set(prev).add(targetId));
        } catch (error) {
            console.error('Failed to follow user:', error);
        } finally {
            setFollowLoading(null);
        }
    };

    const handleUnfollow = async (targetId: number) => {
        if (!currentUser) return;
        setFollowLoading(targetId);
        try {
            await apiClient.delete(`/Follow/${targetId}`);
            setFollowingIds(prev => {
                const next = new Set(prev);
                next.delete(targetId);
                return next;
            });
        } catch (error) {
            console.error('Failed to unfollow user:', error);
        } finally {
            setFollowLoading(null);
        }
    };

    const fetchUsers = async () => {
        try {
            const response = await apiClient.get<any>(`/users?sortBy=${sortBy}`);
            // Handle both array and paginated response { items: [], totalCount: ... }
            const data = response.data;
            if (Array.isArray(data)) {
                setUsers(data);
            } else if (data && Array.isArray(data.items)) {
                setUsers(data.items);
            } else {
                setUsers([]);
            }
        } catch (error) {
            console.error('Failed to fetch users:', error);
            setUsers([]);
        } finally {
            setIsLoading(false);
        }
    };

    const filteredUsers = users.filter(user =>
        user.username.toLowerCase().includes(search.toLowerCase()) ||
        (user.displayName || '').toLowerCase().includes(search.toLowerCase())
    );

    return (
        <div className="container py-4">
            <div className="d-flex justify-content-between align-items-center mb-4">
                <div>
                    <h2 className="fw-bold mb-1">Users</h2>
                    <p className="text-muted mb-0">Connect with developers in our community</p>
                </div>
            </div>

            {/* Search and Filters */}
            <div className="card border-0 shadow-sm rounded-4 mb-4">
                <div className="card-body p-3">
                    <div className="row g-3 align-items-center">
                        <div className="col-md-6">
                            <div className="input-group">
                                <span className="input-group-text bg-transparent border-end-0">
                                    <i className="bi bi-search"></i>
                                </span>
                                <input
                                    type="text"
                                    className="form-control border-start-0"
                                    placeholder="Search users..."
                                    value={search}
                                    onChange={(e) => setSearch(e.target.value)}
                                />
                            </div>
                        </div>
                        <div className="col-md-6">
                            <div className="d-flex gap-2 justify-content-md-end">
                                <button
                                    className={`btn btn-sm ${sortBy === 'reputation' ? 'btn-primary' : 'btn-outline-secondary'} rounded-pill`}
                                    onClick={() => setSortBy('reputation')}
                                >
                                    <i className="bi bi-award me-1"></i>Reputation
                                </button>
                                <button
                                    className={`btn btn-sm ${sortBy === 'newest' ? 'btn-primary' : 'btn-outline-secondary'} rounded-pill`}
                                    onClick={() => setSortBy('newest')}
                                >
                                    <i className="bi bi-person-plus me-1"></i>New Users
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            {/* Users Grid */}
            {isLoading ? (
                <div className="text-center py-5">
                    <div className="spinner-border text-primary" role="status">
                        <span className="visually-hidden">Loading...</span>
                    </div>
                </div>
            ) : (
                <div className="row g-4">
                    {filteredUsers.map((user) => (
                        <div key={user.userId} className="col-md-6 col-lg-4 col-xl-3">
                            <div className="card border-0 shadow-sm rounded-4 h-100 hover-lift">
                                <div className="card-body text-center">
                                    <Link href={`/users/${user.userId}`}>
                                        <img
                                            src={user.profilePicture || '/images/default-avatar.png'}
                                            className="rounded-circle mb-3"
                                            width="80"
                                            height="80"
                                            alt={user.displayName || user.username}
                                        />
                                    </Link>
                                    <h6 className="fw-bold mb-1">
                                        <Link href={`/users/${user.userId}`} className="text-decoration-none text-dark">
                                            {user.displayName || user.username}
                                        </Link>
                                    </h6>
                                    <p className="text-muted small mb-3">@{user.username}</p>
                                    <div className="d-flex justify-content-center gap-3 text-center mb-3">
                                        <div>
                                            <div className="fw-bold text-primary">{user.reputationPoints}</div>
                                            <small className="text-muted">reputation</small>
                                        </div>
                                        <div className="border-start"></div>
                                        <div>
                                            <div className="fw-bold">{user.questionCount || 0}</div>
                                            <small className="text-muted">questions</small>
                                        </div>
                                        <div className="border-start"></div>
                                        <div>
                                            <div className="fw-bold">{user.answerCount || 0}</div>
                                            <small className="text-muted">answers</small>
                                        </div>
                                    </div>

                                    {currentUser && currentUser.userId !== user.userId && (
                                        <button
                                            className={`btn btn-sm ${followingIds.has(user.userId) ? 'btn-outline-primary' : 'btn-primary'} rounded-pill px-4 w-75`}
                                            onClick={() => followingIds.has(user.userId) ? handleUnfollow(user.userId) : handleFollow(user.userId)}
                                            disabled={followLoading === user.userId}
                                        >
                                            {followLoading === user.userId ? (
                                                <span className="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span>
                                            ) : (
                                                followingIds.has(user.userId) ? (
                                                    <><i className="bi bi-person-check me-1"></i>Following</>
                                                ) : (
                                                    <><i className="bi bi-person-plus me-1"></i>Follow</>
                                                )
                                            )}
                                        </button>
                                    )}
                                </div>
                            </div>
                        </div>
                    ))}
                </div>
            )}

            {filteredUsers.length === 0 && !isLoading && (
                <div className="text-center py-5">
                    <i className="bi bi-people fs-1 text-muted mb-3"></i>
                    <h5>No users found</h5>
                    <p className="text-muted">Try a different search term</p>
                </div>
            )}
        </div>
    );
}
