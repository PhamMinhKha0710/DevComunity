'use client';

import { useState } from 'react';
import Link from 'next/link';
import apiClient from '@/lib/api/client';

export default function ForgotPasswordPage() {
    const [email, setEmail] = useState('');
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [success, setSuccess] = useState(false);
    const [error, setError] = useState('');

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setIsSubmitting(true);
        setError('');

        try {
            await apiClient.post('/auth/forgot-password', { email });
            setSuccess(true);
        } catch (err: any) {
            setError(err.response?.data?.message || 'Failed to send reset email');
        } finally {
            setIsSubmitting(false);
        }
    };

    return (
        <div className="container py-5">
            <div className="row justify-content-center">
                <div className="col-md-6 col-lg-5">
                    <div className="card border-0 shadow-sm rounded-4">
                        <div className="card-body p-5">
                            <div className="text-center mb-4">
                                <i className="bi bi-key fs-1 text-primary mb-3"></i>
                                <h3 className="fw-bold">Forgot Password?</h3>
                                <p className="text-muted">
                                    No worries! Enter your email and we'll send you reset instructions.
                                </p>
                            </div>

                            {success ? (
                                <div className="text-center">
                                    <div className="alert alert-success" role="alert">
                                        <i className="bi bi-check-circle me-2"></i>
                                        Check your email for password reset instructions.
                                    </div>
                                    <Link href="/login" className="btn btn-primary rounded-pill px-4">
                                        Back to Login
                                    </Link>
                                </div>
                            ) : (
                                <form onSubmit={handleSubmit}>
                                    {error && (
                                        <div className="alert alert-danger" role="alert">
                                            <i className="bi bi-exclamation-circle me-2"></i>{error}
                                        </div>
                                    )}

                                    <div className="mb-4">
                                        <label className="form-label fw-semibold">Email Address</label>
                                        <div className="input-group">
                                            <span className="input-group-text bg-light border-end-0">
                                                <i className="bi bi-envelope"></i>
                                            </span>
                                            <input
                                                type="email"
                                                className="form-control border-start-0"
                                                placeholder="Enter your email"
                                                value={email}
                                                onChange={(e) => setEmail(e.target.value)}
                                                required
                                            />
                                        </div>
                                    </div>

                                    <button
                                        type="submit"
                                        className="btn btn-primary w-100 rounded-pill py-2 mb-4"
                                        disabled={isSubmitting}
                                    >
                                        {isSubmitting ? (
                                            <>
                                                <span className="spinner-border spinner-border-sm me-2"></span>
                                                Sending...
                                            </>
                                        ) : (
                                            <>
                                                <i className="bi bi-send me-2"></i>Send Reset Link
                                            </>
                                        )}
                                    </button>

                                    <div className="text-center">
                                        <Link href="/login" className="text-decoration-none">
                                            <i className="bi bi-arrow-left me-1"></i>Back to Login
                                        </Link>
                                    </div>
                                </form>
                            )}
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
}
