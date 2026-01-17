'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { useAuth } from '@/lib/contexts/AuthContext';
import type { CreateRepositoryRequest } from '@/types';
import apiClient from '@/lib/api/client';

export default function CreateRepositoryPage() {
    const { user, isLoading: authLoading } = useAuth();
    const router = useRouter();
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [error, setError] = useState('');
    const [formData, setFormData] = useState<CreateRepositoryRequest>({
        name: '',
        description: '',
        isPrivate: false,
    });

    if (authLoading) {
        return (
            <div className="container py-5 text-center">
                <div className="spinner-border text-primary" role="status">
                    <span className="visually-hidden">Loading...</span>
                </div>
            </div>
        );
    }

    if (!user) {
        router.push('/login');
        return null;
    }

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setIsSubmitting(true);
        setError('');

        try {
            const response = await apiClient.post('/repositories', formData);
            router.push(`/repositories/${response.data.repositoryId}`);
        } catch (err: any) {
            setError(err.response?.data?.message || 'Failed to create repository');
        } finally {
            setIsSubmitting(false);
        }
    };

    return (
        <div className="container py-4">
            <div className="row justify-content-center">
                <div className="col-lg-8">
                    {/* Header */}
                    <div className="mb-4">
                        <nav aria-label="breadcrumb">
                            <ol className="breadcrumb">
                                <li className="breadcrumb-item"><Link href="/repositories">Repositories</Link></li>
                                <li className="breadcrumb-item active">Create New</li>
                            </ol>
                        </nav>
                        <h1 className="fw-bold">
                            <i className="bi bi-plus-circle me-2"></i>Create a new repository
                        </h1>
                        <p className="text-muted">
                            A repository contains all project files, including the revision history.
                        </p>
                    </div>

                    {/* Form */}
                    <div className="card border-0 shadow-sm rounded-4">
                        <div className="card-body p-4">
                            {error && (
                                <div className="alert alert-danger" role="alert">
                                    <i className="bi bi-exclamation-circle me-2"></i>{error}
                                </div>
                            )}

                            <form onSubmit={handleSubmit}>
                                {/* Owner + Name */}
                                <div className="mb-4">
                                    <label className="form-label fw-semibold">Owner / Repository name *</label>
                                    <div className="d-flex align-items-center gap-2">
                                        <div className="d-flex align-items-center bg-light rounded-pill px-3 py-2">
                                            <img
                                                src={user.profilePicture || '/images/default-avatar.png'}
                                                className="rounded-circle me-2"
                                                width="24"
                                                height="24"
                                                alt=""
                                            />
                                            <span>{user.username}</span>
                                        </div>
                                        <span className="text-muted">/</span>
                                        <input
                                            type="text"
                                            className="form-control"
                                            placeholder="repository-name"
                                            value={formData.name}
                                            onChange={(e) => setFormData({ ...formData, name: e.target.value.toLowerCase().replace(/\s+/g, '-') })}
                                            required
                                            pattern="[a-z0-9-]+"
                                        />
                                    </div>
                                    <small className="text-muted">
                                        Great repository names are short and memorable.
                                    </small>
                                </div>

                                {/* Description */}
                                <div className="mb-4">
                                    <label className="form-label fw-semibold">Description (optional)</label>
                                    <textarea
                                        className="form-control"
                                        rows={3}
                                        placeholder="A short description of what this repository is about..."
                                        value={formData.description}
                                        onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                                    />
                                </div>

                                {/* Visibility */}
                                <div className="mb-4">
                                    <label className="form-label fw-semibold">Visibility</label>
                                    <div className="d-flex flex-column gap-2">
                                        <div className="form-check p-3 border rounded-3">
                                            <input
                                                className="form-check-input"
                                                type="radio"
                                                name="visibility"
                                                id="public"
                                                checked={!formData.isPrivate}
                                                onChange={() => setFormData({ ...formData, isPrivate: false })}
                                            />
                                            <label className="form-check-label w-100" htmlFor="public">
                                                <div className="d-flex align-items-center">
                                                    <i className="bi bi-unlock fs-4 me-3 text-success"></i>
                                                    <div>
                                                        <strong>Public</strong>
                                                        <p className="text-muted mb-0 small">
                                                            Anyone on the internet can see this repository.
                                                        </p>
                                                    </div>
                                                </div>
                                            </label>
                                        </div>
                                        <div className="form-check p-3 border rounded-3">
                                            <input
                                                className="form-check-input"
                                                type="radio"
                                                name="visibility"
                                                id="private"
                                                checked={formData.isPrivate}
                                                onChange={() => setFormData({ ...formData, isPrivate: true })}
                                            />
                                            <label className="form-check-label w-100" htmlFor="private">
                                                <div className="d-flex align-items-center">
                                                    <i className="bi bi-lock fs-4 me-3 text-warning"></i>
                                                    <div>
                                                        <strong>Private</strong>
                                                        <p className="text-muted mb-0 small">
                                                            You choose who can see and commit to this repository.
                                                        </p>
                                                    </div>
                                                </div>
                                            </label>
                                        </div>
                                    </div>
                                </div>

                                <hr className="my-4" />

                                <div className="d-flex justify-content-between">
                                    <Link href="/repositories" className="btn btn-outline-secondary rounded-pill">
                                        Cancel
                                    </Link>
                                    <button
                                        type="submit"
                                        className="btn btn-primary rounded-pill px-4"
                                        disabled={isSubmitting || !formData.name}
                                    >
                                        {isSubmitting ? (
                                            <>
                                                <span className="spinner-border spinner-border-sm me-2" role="status"></span>
                                                Creating...
                                            </>
                                        ) : (
                                            <>
                                                <i className="bi bi-plus-circle me-2"></i>Create repository
                                            </>
                                        )}
                                    </button>
                                </div>
                            </form>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
}
