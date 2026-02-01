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
        <div className="min-h-screen flex bg-[var(--bg-primary)]">
            {/* Left Side - Poster */}
            <div className="hidden lg:flex w-1/2 bg-gradient-to-bl from-[#1a1c2e] to-[#0f1016] relative overflow-hidden order-2 lg:order-1">
                <div className="absolute inset-0 bg-[url('/images/grid-pattern.svg')] opacity-20"></div>

                {/* Abstract Shapes */}
                <div className="absolute top-1/3 left-1/3 w-[500px] h-[500px] bg-indigo-500/20 rounded-full blur-[100px] animate-pulse"></div>
                <div className="absolute bottom-1/3 right-1/3 w-[500px] h-[500px] bg-purple-500/20 rounded-full blur-[100px] animate-pulse delay-500"></div>

                <div className="relative z-10 w-full flex flex-col justify-center px-16 text-white text-right">
                    <div className="mb-12">
                        <div className="inline-flex w-16 h-16 bg-white/10 backdrop-blur-xl rounded-2xl items-center justify-center mb-6 border border-white/20 shadow-xl ml-auto">
                            <i className="bi bi-rocket-takeoff-fill text-4xl text-white"></i>
                        </div>
                        <h2 className="text-5xl font-bold mb-6 leading-tight">
                            Launch your<br />
                            <span className="text-transparent bg-clip-text bg-gradient-to-l from-indigo-400 to-purple-400">dev career.</span>
                        </h2>
                        <p className="text-lg text-gray-400 max-w-lg leading-relaxed ml-auto">
                            Create your profile, showcase your projects, and connect with opportunities worldwide.
                        </p>
                    </div>

                    <div className="space-y-6">
                        <div className="flex items-center gap-4 p-4 bg-white/5 backdrop-blur-sm rounded-2xl border border-white/10 flex-row-reverse text-right">
                            <div className="w-12 h-12 bg-indigo-500/20 rounded-xl flex items-center justify-center text-indigo-400 shrink-0">
                                <i className="bi bi-github text-xl"></i>
                            </div>
                            <div>
                                <h3 className="font-bold text-lg">Portfolio Ready</h3>
                                <p className="text-sm text-gray-400">Showcase your repositories automatically</p>
                            </div>
                        </div>
                        <div className="flex items-center gap-4 p-4 bg-white/5 backdrop-blur-sm rounded-2xl border border-white/10 flex-row-reverse text-right">
                            <div className="w-12 h-12 bg-pink-500/20 rounded-xl flex items-center justify-center text-pink-400 shrink-0">
                                <i className="bi bi-award-fill text-xl"></i>
                            </div>
                            <div>
                                <h3 className="font-bold text-lg">Earn Badges</h3>
                                <p className="text-sm text-gray-400">Get recognized for your skills and help</p>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            {/* Right Side - Form */}
            <div className="w-full lg:w-1/2 flex items-center justify-center p-8 lg:p-12 xl:p-16 order-1 lg:order-2">
                <div className="w-full max-w-md space-y-8">
                    {/* Header */}
                    <div className="text-center lg:text-left">
                        <Link href="/" className="inline-block mb-8 lg:mb-12">
                            <span className="text-2xl font-bold bg-gradient-to-r from-[var(--primary)] to-purple-600 bg-clip-text text-transparent">
                                DevCommunity
                            </span>
                        </Link>
                        <h1 className="text-4xl font-bold text-[var(--text-primary)] mb-2">Create Account</h1>
                        <p className="text-[var(--text-secondary)]">Join globally connected developers today</p>
                    </div>

                    {/* Error Alert */}
                    {error && (
                        <div className="bg-red-500/10 border border-red-500/20 text-red-500 px-4 py-3 rounded-xl flex items-center gap-3">
                            <i className="bi bi-exclamation-triangle-fill shrink-0"></i>
                            <span className="text-sm font-medium">{error}</span>
                        </div>
                    )}

                    {/* Form */}
                    <form onSubmit={handleSubmit} className="space-y-5">
                        <div className="grid grid-cols-1 gap-5">
                            <div>
                                <label className="block text-sm font-medium text-[var(--text-secondary)] mb-2">Username</label>
                                <div className="relative">
                                    <input
                                        type="text"
                                        name="username"
                                        value={formData.username}
                                        onChange={handleChange}
                                        className="w-full pl-11 pr-4 py-3.5 bg-[var(--bg-tertiary)] border border-[var(--border-color)] rounded-xl text-[var(--text-primary)] focus:border-[var(--primary)] focus:ring-2 focus:ring-[var(--primary)]/20 transition outline-none"
                                        placeholder="Choose a username"
                                        required
                                    />
                                    <i className="bi bi-person absolute left-4 top-1/2 -translate-y-1/2 text-[var(--text-muted)] text-lg"></i>
                                </div>
                            </div>

                            <div>
                                <label className="block text-sm font-medium text-[var(--text-secondary)] mb-2">Email Address</label>
                                <div className="relative">
                                    <input
                                        type="email"
                                        name="email"
                                        value={formData.email}
                                        onChange={handleChange}
                                        className="w-full pl-11 pr-4 py-3.5 bg-[var(--bg-tertiary)] border border-[var(--border-color)] rounded-xl text-[var(--text-primary)] focus:border-[var(--primary)] focus:ring-2 focus:ring-[var(--primary)]/20 transition outline-none"
                                        placeholder="name@example.com"
                                        required
                                    />
                                    <i className="bi bi-envelope absolute left-4 top-1/2 -translate-y-1/2 text-[var(--text-muted)] text-lg"></i>
                                </div>
                            </div>

                            <div>
                                <label className="block text-sm font-medium text-[var(--text-secondary)] mb-2">Display Name (Optional)</label>
                                <div className="relative">
                                    <input
                                        type="text"
                                        name="displayName"
                                        value={formData.displayName}
                                        onChange={handleChange}
                                        className="w-full pl-11 pr-4 py-3.5 bg-[var(--bg-tertiary)] border border-[var(--border-color)] rounded-xl text-[var(--text-primary)] focus:border-[var(--primary)] focus:ring-2 focus:ring-[var(--primary)]/20 transition outline-none"
                                        placeholder="How should we call you?"
                                    />
                                    <i className="bi bi-tag absolute left-4 top-1/2 -translate-y-1/2 text-[var(--text-muted)] text-lg"></i>
                                </div>
                            </div>

                            <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
                                <div>
                                    <label className="block text-sm font-medium text-[var(--text-secondary)] mb-2">Password</label>
                                    <div className="relative">
                                        <input
                                            type="password"
                                            name="password"
                                            value={formData.password}
                                            onChange={handleChange}
                                            className="w-full pl-11 pr-4 py-3.5 bg-[var(--bg-tertiary)] border border-[var(--border-color)] rounded-xl text-[var(--text-primary)] focus:border-[var(--primary)] focus:ring-2 focus:ring-[var(--primary)]/20 transition outline-none"
                                            placeholder="Create password"
                                            required
                                        />
                                        <i className="bi bi-lock absolute left-4 top-1/2 -translate-y-1/2 text-[var(--text-muted)] text-lg"></i>
                                    </div>
                                </div>
                                <div>
                                    <label className="block text-sm font-medium text-[var(--text-secondary)] mb-2">Confirm Password</label>
                                    <div className="relative">
                                        <input
                                            type="password"
                                            name="confirmPassword"
                                            value={formData.confirmPassword}
                                            onChange={handleChange}
                                            className="w-full pl-11 pr-4 py-3.5 bg-[var(--bg-tertiary)] border border-[var(--border-color)] rounded-xl text-[var(--text-primary)] focus:border-[var(--primary)] focus:ring-2 focus:ring-[var(--primary)]/20 transition outline-none"
                                            placeholder="Repeat password"
                                            required
                                        />
                                        <i className="bi bi-lock-fill absolute left-4 top-1/2 -translate-y-1/2 text-[var(--text-muted)] text-lg"></i>
                                    </div>
                                </div>
                            </div>
                        </div>

                        <div className="flex items-start mb-6">
                            <div className="flex items-center h-5">
                                <input
                                    id="terms"
                                    type="checkbox"
                                    required
                                    className="w-4 h-4 rounded border-[var(--border-color)] text-[var(--primary)] focus:ring-[var(--primary)]/20 cursor-pointer"
                                />
                            </div>
                            <div className="ml-3 text-sm">
                                <label htmlFor="terms" className="font-medium text-[var(--text-secondary)]">
                                    I agree to the <a href="#" className="text-[var(--primary)] hover:underline">Terms of Service</a> and <a href="#" className="text-[var(--primary)] hover:underline">Privacy Policy</a>
                                </label>
                            </div>
                        </div>

                        <button
                            type="submit"
                            disabled={isLoading}
                            className="w-full py-3.5 bg-gradient-to-r from-[var(--primary)] to-purple-600 text-white rounded-xl font-bold text-lg hover:shadow-lg hover:shadow-[var(--primary)]/25 active:scale-[0.98] transition disabled:opacity-70 disabled:cursor-not-allowed"
                        >
                            {isLoading ? (
                                <div className="flex items-center justify-center gap-2">
                                    <div className="w-5 h-5 border-2 border-white/30 border-t-white rounded-full animate-spin"></div>
                                    <span>Creating Account...</span>
                                </div>
                            ) : (
                                'Create Account'
                            )}
                        </button>
                    </form>

                    <div className="relative my-6">
                        <div className="absolute inset-0 flex items-center">
                            <div className="w-full border-t border-[var(--border-color)]"></div>
                        </div>
                        <div className="relative flex justify-center text-sm">
                            <span className="px-4 bg-[var(--bg-primary)] text-[var(--text-muted)]">Or sign up with</span>
                        </div>
                    </div>

                    <div className="grid grid-cols-2 gap-4">
                        <button className="flex items-center justify-center gap-2 px-4 py-3 bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-xl hover:bg-[var(--bg-hover)] transition text-[var(--text-primary)] font-medium">
                            <i className="bi bi-google text-red-500"></i>
                            Google
                        </button>
                        <button className="flex items-center justify-center gap-2 px-4 py-3 bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-xl hover:bg-[var(--bg-hover)] transition text-[var(--text-primary)] font-medium">
                            <i className="bi bi-github"></i>
                            GitHub
                        </button>
                    </div>

                    <p className="text-center text-[var(--text-secondary)]">
                        Already have an account?{' '}
                        <Link href="/login" className="text-[var(--primary)] font-bold hover:text-purple-500 transition">
                            Sign in
                        </Link>
                    </p>
                </div>
            </div>
        </div>
    );
}
