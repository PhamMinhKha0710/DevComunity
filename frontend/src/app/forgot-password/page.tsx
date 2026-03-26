'use client';

import { useState, FormEvent } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useAuth } from '@/lib/contexts/AuthContext';

export default function ForgotPasswordPage() {
    const [step, setStep] = useState<'email' | 'otp'>('email');
    const [email, setEmail] = useState('');
    const [code, setCode] = useState('');
    const [newPassword, setNewPassword] = useState('');
    const [confirmPassword, setConfirmPassword] = useState('');
    const [isLoading, setIsLoading] = useState(false);
    const [message, setMessage] = useState('');
    const [error, setError] = useState('');
    const { forgotPasswordOtp } = useAuth();
    const router = useRouter();

    const handleRequestCode = async (e: FormEvent) => {
        e.preventDefault();
        setMessage('');
        setError('');

        if (!email.trim()) {
            setError('Please enter your email address.');
            return;
        }

        setIsLoading(true);
        try {
            const result = await forgotPasswordOtp.requestCode(email);
            if (result.success) {
                setStep('otp');
                setMessage('A verification code has been sent to your email.');
            } else {
                setError(result.message || 'Something went wrong. Please try again.');
            }
        } catch {
            setError('Failed to send verification code. Please try again later.');
        } finally {
            setIsLoading(false);
        }
    };

    const handleResetPassword = async (e: FormEvent) => {
        e.preventDefault();
        setMessage('');
        setError('');

        if (code.trim().length !== 6) {
            setError('Please enter the 6-digit verification code.');
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
            const result = await forgotPasswordOtp.confirmCode({
                email,
                code: code.trim(),
                newPassword,
                confirmPassword,
            });
            if (result.success) {
                router.push('/auth?passwordReset=1');
            } else {
                setError(result.message || 'Invalid or expired verification code.');
            }
        } catch {
            setError('Failed to reset password. Please try again later.');
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <div className="min-h-screen flex items-center justify-center relative bg-slate-50 overflow-hidden">
            <div className="absolute inset-0 overflow-hidden pointer-events-none opacity-40" style={{
                backgroundImage: 'radial-gradient(#cbd5e1 1px, transparent 1px)',
                backgroundSize: '30px 30px'
            }}></div>

            <div className="relative z-10 w-full max-w-md mx-4">
                <div className="bg-white/90 backdrop-blur-xl border border-white/60 rounded-3xl shadow-2xl p-8 lg:p-10">
                    <div className="text-center mb-8">
                        <div className="inline-flex items-center justify-center w-16 h-16 bg-orange-100 rounded-2xl mb-4 text-3xl">
                            {step === 'otp' ? '🔐' : '🔑'}
                        </div>
                        <h1 className="text-3xl font-black text-gray-800 mb-2">
                            {step === 'otp' ? (
                                <>Verify <span className="text-orange-500">Code</span></>
                            ) : (
                                <>Forgot <span className="text-orange-500">Password?</span></>
                            )}
                        </h1>
                        <p className="text-gray-500">
                            {step === 'otp' ? 'Enter the code we sent to your email.' : 'No worries, we\'ll send you a verification code.'}
                        </p>
                    </div>

                    {message && !error && (
                        <div className="bg-green-50 border border-green-200 text-green-700 p-4 rounded-xl text-center mb-6 text-sm font-medium">
                            {message}
                        </div>
                    )}

                    {error && (
                        <div className="mb-6 p-4 bg-red-50 border border-red-200 rounded-xl text-red-600 text-sm">
                            {error}
                        </div>
                    )}

                    {step === 'email' ? (
                        <>
                            <form onSubmit={handleRequestCode} className="space-y-6">
                                <div>
                                    <label className="block text-gray-700 text-sm font-semibold mb-2">Email Address</label>
                                    <input
                                        type="email"
                                        value={email}
                                        onChange={(e) => setEmail(e.target.value)}
                                        placeholder="you@example.com"
                                        className="w-full px-4 py-3.5 bg-gray-50 border-2 border-gray-200 rounded-xl text-gray-800 placeholder-gray-400 focus:border-orange-500 focus:bg-white outline-none transition-all text-lg"
                                        required
                                    />
                                </div>

                                <button
                                    type="submit"
                                    disabled={isLoading}
                                    className="w-full py-4 bg-orange-500 text-white rounded-xl font-bold text-lg hover:bg-orange-600 active:scale-[0.98] transition-all shadow-lg shadow-orange-500/30 disabled:opacity-70"
                                >
                                    {isLoading ? 'Sending...' : 'Send Verification Code'}
                                </button>
                            </form>

                            <div className="mt-8 text-center">
                                <Link href="/auth" className="text-gray-500 hover:text-orange-500 transition-colors inline-flex items-center gap-2 font-medium">
                                    <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><line x1="19" y1="12" x2="5" y2="12"></line><polyline points="12 19 5 12 12 5"></polyline></svg>
                                    Back to Sign In
                                </Link>
                            </div>
                        </>
                    ) : (
                        <>
                            <div className="mb-4 p-3 bg-gray-50 border border-gray-200 rounded-xl text-sm text-gray-600 text-center">
                                Code sent to <span className="font-semibold text-gray-800">{email}</span>
                                <button
                                    onClick={() => { setStep('email'); setMessage(''); setError(''); }}
                                    className="block mx-auto mt-1 text-orange-500 hover:underline font-medium"
                                >
                                    Change email address
                                </button>
                            </div>

                            <form onSubmit={handleResetPassword} className="space-y-5">
                                <div>
                                    <label className="block text-gray-700 text-sm font-semibold mb-2">Verification Code</label>
                                    <input
                                        type="text"
                                        value={code}
                                        onChange={(e) => setCode(e.target.value.replace(/\D/g, '').slice(0, 6))}
                                        placeholder="6-digit code"
                                        maxLength={6}
                                        className="w-full px-4 py-3.5 bg-gray-50 border-2 border-gray-200 rounded-xl text-gray-800 placeholder-gray-400 focus:border-orange-500 focus:bg-white outline-none transition-all text-lg text-center tracking-widest font-mono"
                                        required
                                    />
                                </div>

                                <div>
                                    <label className="block text-gray-700 text-sm font-semibold mb-2">New Password</label>
                                    <input
                                        type="password"
                                        value={newPassword}
                                        onChange={(e) => setNewPassword(e.target.value)}
                                        placeholder="At least 8 characters"
                                        className="w-full px-4 py-3.5 bg-gray-50 border-2 border-gray-200 rounded-xl text-gray-800 placeholder-gray-400 focus:border-orange-500 focus:bg-white outline-none transition-all text-lg"
                                        required
                                        minLength={8}
                                    />
                                </div>

                                <div>
                                    <label className="block text-gray-700 text-sm font-semibold mb-2">Confirm Password</label>
                                    <input
                                        type="password"
                                        value={confirmPassword}
                                        onChange={(e) => setConfirmPassword(e.target.value)}
                                        placeholder="Repeat new password"
                                        className="w-full px-4 py-3.5 bg-gray-50 border-2 border-gray-200 rounded-xl text-gray-800 placeholder-gray-400 focus:border-orange-500 focus:bg-white outline-none transition-all text-lg"
                                        required
                                    />
                                </div>

                                <button
                                    type="submit"
                                    disabled={isLoading}
                                    className="w-full py-4 bg-orange-500 text-white rounded-xl font-bold text-lg hover:bg-orange-600 active:scale-[0.98] transition-all shadow-lg shadow-orange-500/30 disabled:opacity-70"
                                >
                                    {isLoading ? 'Resetting...' : 'Reset Password'}
                                </button>

                                <div className="text-center">
                                    <button
                                        type="button"
                                        onClick={handleRequestCode}
                                        disabled={isLoading}
                                        className="text-gray-400 hover:text-orange-500 transition-colors text-sm font-medium disabled:opacity-50"
                                    >
                                        Did not receive a code? Resend
                                    </button>
                                </div>
                            </form>

                            <div className="mt-6 text-center">
                                <Link href="/auth" className="text-gray-500 hover:text-orange-500 transition-colors inline-flex items-center gap-2 font-medium">
                                    <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><line x1="19" y1="12" x2="5" y2="12"></line><polyline points="12 19 5 12 12 5"></polyline></svg>
                                    Back to Sign In
                                </Link>
                            </div>
                        </>
                    )}
                </div>
            </div>
        </div>
    );
}
