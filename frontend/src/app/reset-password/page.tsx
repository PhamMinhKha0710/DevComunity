'use client';

import { useState, Suspense } from 'react';
import Link from 'next/link';
import { useSearchParams, useRouter } from 'next/navigation';
import apiClient from '@/lib/api/client';

function ResetPasswordForm() {
    const searchParams = useSearchParams();
    const router = useRouter();
    const token = searchParams.get('token') || '';
    const email = searchParams.get('email') || '';

    const [password, setPassword] = useState('');
    const [confirmPassword, setConfirmPassword] = useState('');
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [error, setError] = useState('');

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();

        if (password !== confirmPassword) {
            setError('Passwords do not match');
            return;
        }

        setIsSubmitting(true);
        setError('');

        try {
            await apiClient.post('/auth/reset-password', {
                email,
                token,
                newPassword: password
            });
            router.push('/login?message=Password reset successful');
        } catch (err: any) {
            setError(err.response?.data?.message || 'Failed to reset password');
        } finally {
            setIsSubmitting(false);
        }
    };

    if (!token) {
        return (
            <div className="text-center">
                <div className="alert alert-danger" role="alert">
                    <i className="bi bi-exclamation-circle me-2"></i>
                    Invalid or expired reset link.
                </div>
                <Link href="/forgot-password" className="btn btn-primary rounded-pill px-4">
                    Request New Reset Link
                </Link>
            </div>
        );
    }

    return (
        <form onSubmit={handleSubmit}>
            {error && (
                <div className="alert alert-danger" role="alert">
                    <i className="bi bi-exclamation-circle me-2"></i>{error}
                </div>
            )}

            <div className="mb-4">
                <label className="form-label fw-semibold">New Password</label>
                <div className="input-group">
                    <span className="input-group-text bg-light border-end-0">
                        <i className="bi bi-lock"></i>
                    </span>
                    <input
                        type="password"
                        className="form-control border-start-0"
                        placeholder="Enter new password"
                        value={password}
                        onChange={(e) => setPassword(e.target.value)}
                        minLength={6}
                        required
                    />
                </div>
            </div>

            <div className="mb-4">
                <label className="form-label fw-semibold">Confirm Password</label>
                <div className="input-group">
                    <span className="input-group-text bg-light border-end-0">
                        <i className="bi bi-lock-fill"></i>
                    </span>
                    <input
                        type="password"
                        className="form-control border-start-0"
                        placeholder="Confirm new password"
                        value={confirmPassword}
                        onChange={(e) => setConfirmPassword(e.target.value)}
                        required
                    />
                </div>
            </div>

            <button
                type="submit"
                className="btn btn-primary w-100 rounded-pill py-2"
                disabled={isSubmitting}
            >
                {isSubmitting ? (
                    <>
                        <span className="spinner-border spinner-border-sm me-2"></span>
                        Resetting...
                    </>
                ) : (
                    <>
                        <i className="bi bi-check-circle me-2"></i>Reset Password
                    </>
                )}
            </button>
        </form>
    );
}

export default function ResetPasswordPage() {
    return (
        <div className="container py-5">
            <div className="row justify-content-center">
                <div className="col-md-6 col-lg-5">
                    <div className="card border-0 shadow-sm rounded-4">
                        <div className="card-body p-5">
                            <div className="text-center mb-4">
                                <i className="bi bi-shield-lock fs-1 text-primary mb-3"></i>
                                <h3 className="fw-bold">Reset Password</h3>
                                <p className="text-muted">
                                    Enter your new password below.
                                </p>
                            </div>

                            <Suspense fallback={
                                <div className="text-center">
                                    <div className="spinner-border text-primary" role="status">
                                        <span className="visually-hidden">Loading...</span>
                                    </div>
                                </div>
                            }>
                                <ResetPasswordForm />
                            </Suspense>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
}
