'use client';

import { useState } from 'react';
import { useAuth } from '@/lib/contexts/AuthContext';
import { useRouter } from 'next/navigation';
import Link from 'next/link';

export default function RegisterPage() {
    const { register } = useAuth();
    const router = useRouter();
    const [formData, setFormData] = useState({
        username: '',
        email: '',
        password: '',
        confirmPassword: '',
        displayName: '',
    });
    const [error, setError] = useState('');
    const [isLoading, setIsLoading] = useState(false);

    const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        setFormData({ ...formData, [e.target.name]: e.target.value });
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError('');

        if (formData.password !== formData.confirmPassword) {
            setError('Passwords do not match');
            return;
        }

        setIsLoading(true);

        try {
            const response = await register(formData);
            if (response.success) {
                router.push('/');
            } else {
                setError(response.message || 'Registration failed');
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
                    <div className="col-lg-6 d-none d-lg-block">
                        <div className="h-100 bg-primary" style={{ background: 'linear-gradient(rgba(42, 26, 135, 0.9), rgba(65, 48, 192, 0.8)), url("/images/code-bg.jpg")', backgroundSize: 'cover', backgroundPosition: 'center' }}>
                            <div className="d-flex flex-column justify-content-center align-items-center h-100 text-white p-5">
                                <div className="mb-4 text-center">
                                    <i className="bi bi-code-square display-1 mb-3"></i>
                                    <h2 className="fw-bold">Join DevCommunity</h2>
                                    <p className="lead opacity-75">Start your journey with thousands of developers</p>
                                </div>

                                <div className="register-benefits w-100">
                                    <div className="benefit-item">
                                        <div className="benefit-icon">
                                            <i className="bi bi-trophy"></i>
                                        </div>
                                        <div>
                                            <h5 className="mb-1">Earn Badges</h5>
                                            <p className="mb-0 opacity-75">Get recognized for your contributions</p>
                                        </div>
                                    </div>

                                    <div className="benefit-item">
                                        <div className="benefit-icon">
                                            <i className="bi bi-chat-dots"></i>
                                        </div>
                                        <div>
                                            <h5 className="mb-1">Real-time Chat</h5>
                                            <p className="mb-0 opacity-75">Connect with other developers instantly</p>
                                        </div>
                                    </div>

                                    <div className="benefit-item">
                                        <div className="benefit-icon">
                                            <i className="bi bi-git"></i>
                                        </div>
                                        <div>
                                            <h5 className="mb-1">Share Code</h5>
                                            <p className="mb-0 opacity-75">Host and showcase your repositories</p>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>

                    <div className="col-lg-6">
                        <div className="account-card h-100">
                            <div className="account-header">
                                <h2 className="account-title">Create Account</h2>
                                <p className="account-subtitle">Join DevCommunity today</p>
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
                                            name="username"
                                            value={formData.username}
                                            onChange={handleChange}
                                            className="form-control account-form-control"
                                            placeholder="Username"
                                            required
                                        />
                                    </div>

                                    <div className="account-form-group">
                                        <i className="bi bi-person-badge account-form-icon"></i>
                                        <input
                                            type="text"
                                            name="displayName"
                                            value={formData.displayName}
                                            onChange={handleChange}
                                            className="form-control account-form-control"
                                            placeholder="Display Name (optional)"
                                        />
                                    </div>

                                    <div className="account-form-group">
                                        <i className="bi bi-envelope account-form-icon"></i>
                                        <input
                                            type="email"
                                            name="email"
                                            value={formData.email}
                                            onChange={handleChange}
                                            className="form-control account-form-control"
                                            placeholder="Email address"
                                            required
                                        />
                                    </div>

                                    <div className="account-form-group">
                                        <i className="bi bi-lock account-form-icon"></i>
                                        <input
                                            type="password"
                                            name="password"
                                            value={formData.password}
                                            onChange={handleChange}
                                            className="form-control account-form-control"
                                            placeholder="Password"
                                            required
                                        />
                                    </div>

                                    <div className="account-form-group">
                                        <i className="bi bi-lock-fill account-form-icon"></i>
                                        <input
                                            type="password"
                                            name="confirmPassword"
                                            value={formData.confirmPassword}
                                            onChange={handleChange}
                                            className="form-control account-form-control"
                                            placeholder="Confirm Password"
                                            required
                                        />
                                    </div>

                                    <div className="form-check mb-4">
                                        <input type="checkbox" id="agree" className="form-check-input" required />
                                        <label htmlFor="agree" className="form-check-label small">
                                            I agree to the <a href="#" className="text-decoration-none">Terms of Service</a> and <a href="#" className="text-decoration-none">Privacy Policy</a>
                                        </label>
                                    </div>

                                    <button type="submit" className="btn btn-primary account-submit-btn w-100" disabled={isLoading}>
                                        {isLoading ? 'Creating account...' : 'Create Account'}
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
                                    Already have an account?
                                    <Link href="/login" className="text-decoration-none fw-medium ms-1">Sign in</Link>
                                </p>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
}
