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
        setError('');

        if (!email.trim()) {
            setError('Please enter your email address');
            return;
        }
        if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
            setError('Please enter a valid email address');
            return;
        }

        setIsSubmitting(true);

        try {
            await apiClient.post('/auth/forgot-password', { email });
            setSuccess(true);
        } catch (err: any) {
            setError(err.response?.data?.message || 'Failed to send reset email. Please try again.');
        } finally {
            setIsSubmitting(false);
        }
    };

    return (
        <div className="min-h-screen flex items-center justify-center bg-[var(--bg-primary)] p-4">
            <div className="w-full max-w-md">
                <div className="bg-white dark:bg-[var(--bg-secondary)] border border-[#e2e8f0] dark:border-[var(--border-color)] rounded-2xl p-8">
                    <div className="text-center mb-6">
                        <span className="material-symbols-outlined text-5xl text-[#137fec] mb-3 block">key</span>
                        <h1 className="text-2xl font-black text-[#0f172a] dark:text-white">Forgot Password?</h1>
                        <p className="text-[#64748b] text-sm mt-1">No worries! Enter your email and we&apos;ll send you reset instructions.</p>
                    </div>

                    {success ? (
                        <div className="text-center">
                            <div className="p-4 bg-green-50 dark:bg-green-500/10 border border-green-200 dark:border-green-500/30 rounded-xl text-green-600 dark:text-green-400 text-sm flex items-center gap-2 mb-4">
                                <span className="material-symbols-outlined">check_circle</span>
                                Check your email for password reset instructions.
                            </div>
                            <Link href="/login" className="inline-flex items-center gap-2 bg-[#137fec] text-white px-5 py-2.5 rounded-xl font-bold text-sm hover:bg-[#1170d4] transition-all">
                                Back to Login
                            </Link>
                        </div>
                    ) : (
                        <form onSubmit={handleSubmit} className="space-y-4">
                            {error && (
                                <div className="p-4 bg-red-50 dark:bg-red-500/10 border border-red-200 dark:border-red-500/30 rounded-xl text-red-600 dark:text-red-400 text-sm flex items-center gap-2">
                                    <span className="material-symbols-outlined">error</span>{error}
                                </div>
                            )}

                            <div>
                                <label className="block text-sm font-bold text-[#334155] dark:text-[var(--text-secondary)] mb-2">Email Address</label>
                                <div className="flex items-center border border-[#e2e8f0] dark:border-[var(--border-color)] rounded-xl overflow-hidden focus-within:border-[#137fec] focus-within:ring-2 focus-within:ring-[rgba(19,127,236,0.1)] transition">
                                    <span className="px-3 bg-[#f8fafc] dark:bg-[var(--bg-tertiary)] text-[#94a3b8] border-r border-[#e2e8f0] dark:border-[var(--border-color)]">
                                        <span className="material-symbols-outlined text-lg">mail</span>
                                    </span>
                                    <input
                                        type="email"
                                        placeholder="Enter your email"
                                        value={email}
                                        onChange={(e) => setEmail(e.target.value)}
                                        required
                                        className="flex-1 px-4 py-3 bg-transparent text-[var(--text-primary)] placeholder-[#94a3b8] outline-none"
                                    />
                                </div>
                            </div>

                            <button
                                type="submit"
                                disabled={isSubmitting}
                                className="w-full flex items-center justify-center gap-2 bg-[#137fec] text-white py-3 rounded-xl font-bold text-sm hover:bg-[#1170d4] transition-all disabled:opacity-50 disabled:cursor-not-allowed"
                            >
                                {isSubmitting ? (
                                    <><div className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />Sending...</>
                                ) : (
                                    <><span className="material-symbols-outlined text-base">send</span>Send Reset Link</>
                                )}
                            </button>

                            <div className="text-center">
                                <Link href="/auth?mode=login" className="text-[#137fec] hover:underline text-sm flex items-center justify-center gap-1">
                                    <span className="material-symbols-outlined text-sm">arrow_back</span>Back to Login
                                </Link>
                            </div>
                        </form>
                    )}
                </div>
            </div>
        </div>
    );
}
