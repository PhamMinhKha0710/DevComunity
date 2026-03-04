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
            await apiClient.post('/auth/reset-password', { email, token, newPassword: password });
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
                <div className="p-4 bg-red-50 dark:bg-red-500/10 border border-red-200 dark:border-red-500/30 rounded-xl text-red-600 dark:text-red-400 text-sm flex items-center justify-center gap-2 mb-4">
                    <span className="material-symbols-outlined">error</span>
                    Invalid or expired reset link.
                </div>
                <Link href="/forgot-password" className="inline-flex items-center gap-2 bg-[#137fec] text-white px-5 py-2.5 rounded-xl font-bold text-sm hover:bg-[#1170d4] transition-all">
                    Request New Reset Link
                </Link>
            </div>
        );
    }

    return (
        <form onSubmit={handleSubmit} className="space-y-4">
            {error && (
                <div className="p-4 bg-red-50 dark:bg-red-500/10 border border-red-200 dark:border-red-500/30 rounded-xl text-red-600 dark:text-red-400 text-sm flex items-center gap-2">
                    <span className="material-symbols-outlined">error</span>{error}
                </div>
            )}

            <div>
                <label className="block text-sm font-bold text-[#334155] dark:text-[var(--text-secondary)] mb-2">New Password</label>
                <div className="flex items-center border border-[#e2e8f0] dark:border-[var(--border-color)] rounded-xl overflow-hidden focus-within:border-[#137fec] focus-within:ring-2 focus-within:ring-[rgba(19,127,236,0.1)] transition">
                    <span className="px-3 bg-[#f8fafc] dark:bg-[var(--bg-tertiary)] text-[#94a3b8] border-r border-[#e2e8f0] dark:border-[var(--border-color)]">
                        <span className="material-symbols-outlined text-lg">lock</span>
                    </span>
                    <input type="password" placeholder="Enter new password" value={password} onChange={(e) => setPassword(e.target.value)} minLength={6} required className="flex-1 px-4 py-3 bg-transparent text-[var(--text-primary)] placeholder-[#94a3b8] outline-none" />
                </div>
            </div>

            <div>
                <label className="block text-sm font-bold text-[#334155] dark:text-[var(--text-secondary)] mb-2">Confirm Password</label>
                <div className="flex items-center border border-[#e2e8f0] dark:border-[var(--border-color)] rounded-xl overflow-hidden focus-within:border-[#137fec] focus-within:ring-2 focus-within:ring-[rgba(19,127,236,0.1)] transition">
                    <span className="px-3 bg-[#f8fafc] dark:bg-[var(--bg-tertiary)] text-[#94a3b8] border-r border-[#e2e8f0] dark:border-[var(--border-color)]">
                        <span className="material-symbols-outlined text-lg">lock</span>
                    </span>
                    <input type="password" placeholder="Confirm new password" value={confirmPassword} onChange={(e) => setConfirmPassword(e.target.value)} required className="flex-1 px-4 py-3 bg-transparent text-[var(--text-primary)] placeholder-[#94a3b8] outline-none" />
                </div>
            </div>

            <button type="submit" disabled={isSubmitting} className="w-full flex items-center justify-center gap-2 bg-[#137fec] text-white py-3 rounded-xl font-bold text-sm hover:bg-[#1170d4] transition-all disabled:opacity-50 disabled:cursor-not-allowed">
                {isSubmitting ? (
                    <><div className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />Resetting...</>
                ) : (
                    <><span className="material-symbols-outlined text-base">check_circle</span>Reset Password</>
                )}
            </button>
        </form>
    );
}

export default function ResetPasswordPage() {
    return (
        <div className="min-h-screen flex items-center justify-center bg-[var(--bg-primary)] p-4">
            <div className="w-full max-w-md">
                <div className="bg-white dark:bg-[var(--bg-secondary)] border border-[#e2e8f0] dark:border-[var(--border-color)] rounded-2xl p-8">
                    <div className="text-center mb-6">
                        <span className="material-symbols-outlined text-5xl text-[#137fec] mb-3 block">shield</span>
                        <h1 className="text-2xl font-black text-[#0f172a] dark:text-white">Reset Password</h1>
                        <p className="text-[#64748b] text-sm mt-1">Enter your new password below.</p>
                    </div>

                    <Suspense fallback={
                        <div className="flex items-center justify-center py-8">
                            <div className="w-10 h-10 border-3 border-[rgba(19,127,236,0.2)] border-t-[#137fec] rounded-full animate-spin" />
                        </div>
                    }>
                        <ResetPasswordForm />
                    </Suspense>
                </div>
            </div>
        </div>
    );
}
