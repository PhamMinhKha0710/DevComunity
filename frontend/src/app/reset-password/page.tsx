'use client';

import { useState, FormEvent, Suspense } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import Link from 'next/link';
import { useAuth } from '@/lib/contexts/AuthContext';

function ResetPasswordContent() {
    const searchParams = useSearchParams();
    const router = useRouter();
    const token = searchParams.get('token');
    const email = searchParams.get('email');
    
    const [newPassword, setNewPassword] = useState('');
    const [confirmPassword, setConfirmPassword] = useState('');
    const [isLoading, setIsLoading] = useState(false);
    const [message, setMessage] = useState('');
    const [error, setError] = useState('');
    const { resetPassword } = useAuth();

    const handleSubmit = async (e: FormEvent) => {
        e.preventDefault();
        setMessage('');
        setError('');
        
        if (!token || !email) {
            setError('Invalid or missing reset link parameters.');
            return;
        }

        if (newPassword.length < 8) {
            setError('Password must be at least 8 characters long.');
            return;
        }

        if (newPassword !== confirmPassword) {
            setError('Passwords do not match.');
            return;
        }

        setIsLoading(true);
        try {
            const result = await resetPassword({
                token,
                email,
                newPassword,
                confirmPassword
            });
            if (result.success) {
                setMessage('Your password has been successfully reset. You can now sign in with your new password.');
                setTimeout(() => {
                    router.push('/auth');
                }, 3000);
            } else {
                setError(result.message || 'Failed to reset password. The link may have expired.');
            }
        } catch (err: any) {
            const axiosErr = err as { response?: { data?: { message?: string } } };
            setError(axiosErr?.response?.data?.message || 'Something went wrong. Please try again.');
        } finally {
            setIsLoading(false);
        }
    };

    if (!token || !email) {
        return (
            <div className="bg-white/90 backdrop-blur-xl border border-white/60 rounded-3xl shadow-2xl p-8 lg:p-10 text-center">
                 <div className="inline-flex items-center justify-center w-16 h-16 bg-red-100 rounded-2xl mb-4 text-3xl">
                    ⚠️
                </div>
                <h1 className="text-2xl font-black text-gray-800 mb-4">Invalid Reset Link</h1>
                <p className="text-gray-500 mb-6">This password reset link is invalid or has expired.</p>
                <Link href="/auth" className="inline-block py-3 px-8 bg-orange-500 text-white rounded-xl font-bold hover:bg-orange-600 transition-all">
                    Go to Login
                </Link>
            </div>
        );
    }

    return (
        <div className="bg-white/90 backdrop-blur-xl border border-white/60 rounded-3xl shadow-2xl p-8 lg:p-10">
            <div className="text-center mb-8">
                <div className="inline-flex items-center justify-center w-16 h-16 bg-purple-100 rounded-2xl mb-4 text-3xl">
                    🛡️
                </div>
                <h1 className="text-3xl font-black text-gray-800 mb-2">Reset <span className="text-orange-500">Password</span></h1>
                <p className="text-gray-500">Secure your account with a new password.</p>
            </div>

            {message ? (
                <div className="bg-green-50 border border-green-200 text-green-700 p-4 rounded-xl text-center">
                    <p className="font-medium">{message}</p>
                    <p className="text-sm mt-2">Redirecting to login...</p>
                    <Link href="/auth" className="inline-block mt-4 text-orange-500 font-bold hover:underline">
                        Go to Sign In Now
                    </Link>
                </div>
            ) : (
                <>
                    {error && (
                        <div className="mb-6 p-4 bg-red-50 border border-red-200 rounded-xl text-red-600 text-sm">
                            {error}
                        </div>
                    )}

                    <form onSubmit={handleSubmit} className="space-y-4">
                        <div>
                            <label className="block text-gray-700 text-sm font-semibold mb-2">New Password</label>
                            <input
                                type="password"
                                value={newPassword}
                                onChange={(e) => setNewPassword(e.target.value)}
                                placeholder="••••••••"
                                className="w-full px-4 py-3 bg-gray-50 border-2 border-gray-200 rounded-xl text-gray-800 placeholder-gray-400 focus:border-orange-500 focus:bg-white outline-none transition-all"
                                required
                            />
                        </div>

                        <div>
                            <label className="block text-gray-700 text-sm font-semibold mb-2">Confirm New Password</label>
                            <input
                                type="password"
                                value={confirmPassword}
                                onChange={(e) => setConfirmPassword(e.target.value)}
                                placeholder="••••••••"
                                className="w-full px-4 py-3 bg-gray-50 border-2 border-gray-200 rounded-xl text-gray-800 placeholder-gray-400 focus:border-orange-500 focus:bg-white outline-none transition-all"
                                required
                            />
                        </div>

                        <button
                            type="submit"
                            disabled={isLoading}
                            className="w-full py-4 bg-orange-500 text-white rounded-xl font-bold text-lg hover:bg-orange-600 active:scale-[0.98] transition-all shadow-lg shadow-orange-500/30 disabled:opacity-70"
                        >
                            {isLoading ? 'Resetting Password...' : 'Reset Password'}
                        </button>
                    </form>
                </>
            )}
        </div>
    );
}

export default function ResetPasswordPage() {
    return (
        <div className="min-h-screen flex items-center justify-center relative bg-slate-50 overflow-hidden">
             {/* Background Pattern */}
             <div className="absolute inset-0 overflow-hidden pointer-events-none opacity-40" style={{
                backgroundImage: 'radial-gradient(#cbd5e1 1px, transparent 1px)',
                backgroundSize: '30px 30px'
            }}></div>

            <div className="relative z-10 w-full max-w-md mx-4">
                <Suspense fallback={
                    <div className="bg-white/90 backdrop-blur-xl border border-white/60 rounded-3xl shadow-2xl p-20 flex justify-center items-center">
                         <div className="w-10 h-10 border-4 border-orange-500/30 border-t-orange-500 rounded-full animate-spin"></div>
                    </div>
                }>
                    <ResetPasswordContent />
                </Suspense>
            </div>
        </div>
    );
}
