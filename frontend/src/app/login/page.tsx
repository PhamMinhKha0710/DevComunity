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
        <div className="min-h-screen flex bg-[var(--bg-primary)]">
            {/* Left Side - Form */}
            <div className="w-full lg:w-1/2 flex items-center justify-center p-8 lg:p-12 xl:p-16">
                <div className="w-full max-w-md space-y-8">
                    {/* Header */}
                    <div className="text-center lg:text-left">
                        <Link href="/" className="inline-block mb-8 lg:mb-12">
                            <span className="text-2xl font-bold bg-gradient-to-r from-[var(--primary)] to-purple-600 bg-clip-text text-transparent">
                                DevCommunity
                            </span>
                        </Link>
                        <h1 className="text-4xl font-bold text-[var(--text-primary)] mb-2">Welcome Back</h1>
                        <p className="text-[var(--text-secondary)]">Sign in to your account to continue</p>
                    </div>

                    {/* Error Alert */}
                    {error && (
                        <div className="bg-red-500/10 border border-red-500/20 text-red-500 px-4 py-3 rounded-xl flex items-center gap-3">
                            <i className="bi bi-exclamation-triangle-fill shrink-0"></i>
                            <span className="text-sm font-medium">{error}</span>
                        </div>
                    )}

                    {/* Form */}
                    <form onSubmit={handleSubmit} className="space-y-6">
                        <div className="space-y-5">
                            <div>
                                <label className="block text-sm font-medium text-[var(--text-secondary)] mb-2">Username or Email</label>
                                <div className="relative">
                                    <input
                                        type="text"
                                        value={email}
                                        onChange={(e) => setEmail(e.target.value)}
                                        className="w-full pl-11 pr-4 py-3.5 bg-[var(--bg-tertiary)] border border-[var(--border-color)] rounded-xl text-[var(--text-primary)] focus:border-[var(--primary)] focus:ring-2 focus:ring-[var(--primary)]/20 transition outline-none"
                                        placeholder="Enter your username or email"
                                        autoComplete="username"
                                        required
                                    />
                                    <i className="bi bi-envelope absolute left-4 top-1/2 -translate-y-1/2 text-[var(--text-muted)] text-lg"></i>
                                </div>
                            </div>

                            <div>
                                <div className="flex justify-between items-center mb-2">
                                    <label className="block text-sm font-medium text-[var(--text-secondary)]">Password</label>
                                    <a href="#" className="text-sm text-[var(--primary)] hover:text-purple-500 font-medium transition">Forgot password?</a>
                                </div>
                                <div className="relative">
                                    <input
                                        type="password"
                                        value={password}
                                        onChange={(e) => setPassword(e.target.value)}
                                        className="w-full pl-11 pr-4 py-3.5 bg-[var(--bg-tertiary)] border border-[var(--border-color)] rounded-xl text-[var(--text-primary)] focus:border-[var(--primary)] focus:ring-2 focus:ring-[var(--primary)]/20 transition outline-none"
                                        placeholder="Enter your password"
                                        autoComplete="current-password"
                                        required
                                    />
                                    <i className="bi bi-lock absolute left-4 top-1/2 -translate-y-1/2 text-[var(--text-muted)] text-lg"></i>
                                </div>
                            </div>
                        </div>

                        <div className="flex items-center">
                            <input
                                type="checkbox"
                                id="rememberMe"
                                checked={rememberMe}
                                onChange={(e) => setRememberMe(e.target.checked)}
                                className="w-4 h-4 rounded border-[var(--border-color)] text-[var(--primary)] focus:ring-[var(--primary)]/20 cursor-pointer"
                            />
                            <label htmlFor="rememberMe" className="ml-2 text-sm text-[var(--text-secondary)] cursor-pointer">Remember me</label>
                        </div>

                        <button
                            type="submit"
                            disabled={isLoading}
                            className="w-full py-3.5 bg-gradient-to-r from-[var(--primary)] to-purple-600 text-white rounded-xl font-bold text-lg hover:shadow-lg hover:shadow-[var(--primary)]/25 active:scale-[0.98] transition disabled:opacity-70 disabled:cursor-not-allowed"
                        >
                            {isLoading ? (
                                <div className="flex items-center justify-center gap-2">
                                    <div className="w-5 h-5 border-2 border-white/30 border-t-white rounded-full animate-spin"></div>
                                    <span>Signing in...</span>
                                </div>
                            ) : (
                                'Sign In'
                            )}
                        </button>
                    </form>

                    <div className="relative">
                        <div className="absolute inset-0 flex items-center">
                            <div className="w-full border-t border-[var(--border-color)]"></div>
                        </div>
                        <div className="relative flex justify-center text-sm">
                            <span className="px-4 bg-[var(--bg-primary)] text-[var(--text-muted)]">Or continue with</span>
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
                        Don't have an account?{' '}
                        <Link href="/register" className="text-[var(--primary)] font-bold hover:text-purple-500 transition">
                            Create an account
                        </Link>
                    </p>
                </div>
            </div>

            {/* Right Side - Poster */}
            <div className="hidden lg:flex w-1/2 bg-gradient-to-br from-[#1a1c2e] to-[#0f1016] relative overflow-hidden">
                <div className="absolute inset-0 bg-[url('/images/grid-pattern.svg')] opacity-20"></div>
                <div className="absolute inset-0 bg-gradient-to-t from-[#0f1016] to-transparent"></div>

                {/* Abstract Shapes */}
                <div className="absolute top-1/4 left-1/4 w-96 h-96 bg-purple-500/30 rounded-full blur-3xl animate-pulse"></div>
                <div className="absolute bottom-1/4 right-1/4 w-96 h-96 bg-blue-500/20 rounded-full blur-3xl animate-pulse delay-1000"></div>

                <div className="relative z-10 w-full flex flex-col justify-center px-16 text-white">
                    <div className="mb-12">
                        <div className="w-16 h-16 bg-white/10 backdrop-blur-xl rounded-2xl flex items-center justify-center mb-6 border border-white/20 shadow-xl">
                            <i className="bi bi-code-square text-4xl text-white"></i>
                        </div>
                        <h2 className="text-5xl font-bold mb-6 leading-tight">
                            Build faster,<br />
                            <span className="text-transparent bg-clip-text bg-gradient-to-r from-blue-400 to-purple-400">together.</span>
                        </h2>
                        <p className="text-lg text-gray-400 max-w-lg leading-relaxed">
                            Join thousands of developers sharing knowledge, building reputation, and growing their careers in a supportive community.
                        </p>
                    </div>

                    <div className="grid gap-6">
                        <div className="flex items-center gap-4 p-4 bg-white/5 backdrop-blur-sm rounded-2xl border border-white/10">
                            <div className="w-12 h-12 bg-blue-500/20 rounded-xl flex items-center justify-center text-blue-400">
                                <i className="bi bi-lightning-charge-fill text-xl"></i>
                            </div>
                            <div>
                                <h3 className="font-bold text-lg">Instant Knowledge</h3>
                                <p className="text-sm text-gray-400">Get answers from experts in real-time</p>
                            </div>
                        </div>
                        <div className="flex items-center gap-4 p-4 bg-white/5 backdrop-blur-sm rounded-2xl border border-white/10">
                            <div className="w-12 h-12 bg-purple-500/20 rounded-xl flex items-center justify-center text-purple-400">
                                <i className="bi bi-people-fill text-xl"></i>
                            </div>
                            <div>
                                <h3 className="font-bold text-lg">Vibrant Community</h3>
                                <p className="text-sm text-gray-400">Connect with like-minded developers</p>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
}
