'use client';

import { useState } from 'react';
import { useAuth } from '@/lib/contexts/AuthContext';
import { useRouter } from 'next/navigation';
import Link from 'next/link';

export default function LoginPage() {
    const { login } = useAuth();
    const router = useRouter();
    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [rememberMe, setRememberMe] = useState(false);
    const [error, setError] = useState('');
    const [isLoading, setIsLoading] = useState(false);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError('');
        setIsLoading(true);

        try {
            const response = await login({ email, password, rememberMe });
            if (response.success) {
                router.push('/');
            } else {
                setError(response.message || 'Login failed');
            }
        } catch (err: unknown) {
            const error = err as { response?: { data?: { message?: string } } };
            setError(error.response?.data?.message || 'An error occurred');
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <div className="account-page">
            <link href="/css/account-styles.css" rel="stylesheet" />
            <div className="account-container">
                <div className="row g-0">
                    <div className="col-lg-6">
                        <div className="account-card h-100">
                            <div className="account-header">
                                <h2 className="account-title">Welcome Back</h2>
                                <p className="account-subtitle">Sign in to continue to DevCommunity</p>
                            </div>

                            <div className="account-body">
                                {error && (
                                    <div className="alert alert-danger d-flex align-items-center">
                                        <i className="bi bi-exclamation-triangle-fill me-2"></i>
                                        <div>{error}</div>
                                    </div>
                                )}

                                <form onSubmit={handleSubmit}>
                                    <div className="account-form-group">
                                        <i className="bi bi-person account-form-icon"></i>
                                        <input
                                            type="text"
                                            value={email}
                                            onChange={(e) => setEmail(e.target.value)}
                                            className="form-control account-form-control"
                                            placeholder="Username or Email"
                                            autoComplete="username"
                                            required
                                        />
                                    </div>

                                    <div className="account-form-group">
                                        <i className="bi bi-lock account-form-icon"></i>
                                        <input
                                            type="password"
                                            value={password}
                                            onChange={(e) => setPassword(e.target.value)}
                                            className="form-control account-form-control"
                                            placeholder="Password"
                                            autoComplete="current-password"
                                            required
                                        />
                                    </div>

                                    <div className="d-flex justify-content-between mb-4">
                                        <div className="form-check">
                                            <input
                                                type="checkbox"
                                                id="rememberMe"
                                                checked={rememberMe}
                                                onChange={(e) => setRememberMe(e.target.checked)}
                                                className="form-check-input"
                                            />
                                            <label htmlFor="rememberMe" className="form-check-label">Remember me</label>
                                        </div>
                                        <a href="#" className="text-decoration-none">Forgot password?</a>
                                    </div>

                                    <button type="submit" className="btn btn-primary account-submit-btn w-100" disabled={isLoading}>
                                        {isLoading ? 'Signing in...' : 'Sign In'}
                                    </button>

                                    <div className="social-login">
                                        <a href="#" className="social-btn google">
                                            <i className="bi bi-google"></i> Google
                                        </a>
                                        <a href="#" className="social-btn github">
                                            <i className="bi bi-github"></i> GitHub
                                        </a>
                                    </div>
                                </form>
                            </div>

                            <div className="account-footer text-center">
                                <p className="mb-0">
                                    Don&apos;t have an account?
                                    <Link href="/register" className="text-decoration-none fw-medium ms-1">Create an account</Link>
                                </p>
                            </div>
                        </div>
                    </div>

                    <div className="col-lg-6 d-none d-lg-block">
                        <div className="h-100 bg-primary" style={{ background: 'linear-gradient(rgba(42, 26, 135, 0.9), rgba(65, 48, 192, 0.8)), url("/images/code-bg.jpg")', backgroundSize: 'cover', backgroundPosition: 'center' }}>
                            <div className="d-flex flex-column justify-content-center align-items-center h-100 text-white p-5">
                                <div className="mb-4 text-center">
                                    <i className="bi bi-code-square display-1 mb-3"></i>
                                    <h2 className="fw-bold">DevCommunity</h2>
                                    <p className="lead opacity-75">A place for developers to learn, share & build together</p>
                                </div>

                                <div className="register-benefits w-100">
                                    <div className="benefit-item">
                                        <div className="benefit-icon">
                                            <i className="bi bi-question-circle"></i>
                                        </div>
                                        <div>
                                            <h5 className="mb-1">Ask Questions</h5>
                                            <p className="mb-0 opacity-75">Get answers from a community of developers</p>
                                        </div>
                                    </div>

                                    <div className="benefit-item">
                                        <div className="benefit-icon">
                                            <i className="bi bi-share"></i>
                                        </div>
                                        <div>
                                            <h5 className="mb-1">Share Knowledge</h5>
                                            <p className="mb-0 opacity-75">Help others by sharing your expertise</p>
                                        </div>
                                    </div>

                                    <div className="benefit-item">
                                        <div className="benefit-icon">
                                            <i className="bi bi-person-badge"></i>
                                        </div>
                                        <div>
                                            <h5 className="mb-1">Build Reputation</h5>
                                            <p className="mb-0 opacity-75">Earn recognition for your contributions</p>
                                        </div>
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
