'use client';

import { useState, FormEvent } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { useAuth } from '@/lib/contexts/AuthContext';

export default function LoginPage() {
    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [error, setError] = useState('');
    const [isLoading, setIsLoading] = useState(false);
    const router = useRouter();
    const { login } = useAuth();

    const handleSubmit = async (e: FormEvent) => {
        e.preventDefault();
        setError('');
        setIsLoading(true);

        try {
            await login({ email, password });
            router.push('/');
        } catch (err: unknown) {
            if (err instanceof Error) {
                setError(err.message);
            } else {
                setError('Login failed. Please try again.');
            }
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <div className="min-h-screen flex items-center justify-center relative overflow-hidden">
            {/* Animated Gradient Background */}
            <div className="absolute inset-0 bg-gradient-to-br from-purple-600 via-pink-500 to-orange-400 animate-gradient-shift"></div>

            {/* Floating Shapes */}
            <div className="absolute inset-0 overflow-hidden pointer-events-none">
                <div className="absolute top-20 left-10 w-72 h-72 bg-white/10 rounded-full blur-3xl animate-float-slow"></div>
                <div className="absolute bottom-20 right-10 w-96 h-96 bg-purple-300/20 rounded-full blur-3xl animate-float-medium"></div>
                <div className="absolute top-1/2 left-1/3 w-64 h-64 bg-pink-300/15 rounded-full blur-3xl animate-float-fast"></div>
            </div>

            {/* Login Card */}
            <div className="relative z-10 w-full max-w-md mx-4">
                {/* Glass Card */}
                <div className="backdrop-blur-xl bg-white/10 border border-white/20 rounded-3xl p-8 shadow-2xl transform hover:scale-[1.02] transition-all duration-500">

                    {/* Logo & Branding */}
                    <div className="text-center mb-8">
                        <div className="inline-flex items-center justify-center w-20 h-20 bg-white/20 backdrop-blur-sm rounded-2xl mb-4 shadow-lg transform hover:rotate-6 transition-transform duration-300">
                            <span className="text-4xl">🚀</span>
                        </div>
                        <h1 className="text-3xl font-bold text-white mb-2">DevCommunity</h1>
                        <p className="text-white/70 text-sm">Where developers connect & grow</p>
                    </div>

                    {/* Error Message */}
                    {error && (
                        <div className="mb-6 p-4 bg-red-500/20 border border-red-400/30 rounded-xl text-red-100 text-sm text-center backdrop-blur-sm animate-shake">
                            {error}
                        </div>
                    )}

                    {/* Login Form */}
                    <form onSubmit={handleSubmit} className="space-y-5">
                        {/* Email Input */}
                        <div className="group">
                            <div className="relative">
                                <input
                                    type="email"
                                    value={email}
                                    onChange={(e) => setEmail(e.target.value)}
                                    placeholder="Email address"
                                    required
                                    className="w-full px-5 py-4 bg-white/10 border border-white/20 rounded-xl text-white placeholder-white/50 focus:bg-white/20 focus:border-white/40 focus:ring-2 focus:ring-white/20 outline-none transition-all duration-300 backdrop-blur-sm"
                                />
                                <i className="bi bi-envelope absolute right-4 top-1/2 -translate-y-1/2 text-white/50 group-focus-within:text-white transition-colors"></i>
                            </div>
                        </div>

                        {/* Password Input */}
                        <div className="group">
                            <div className="relative">
                                <input
                                    type="password"
                                    value={password}
                                    onChange={(e) => setPassword(e.target.value)}
                                    placeholder="Password"
                                    required
                                    className="w-full px-5 py-4 bg-white/10 border border-white/20 rounded-xl text-white placeholder-white/50 focus:bg-white/20 focus:border-white/40 focus:ring-2 focus:ring-white/20 outline-none transition-all duration-300 backdrop-blur-sm"
                                />
                                <i className="bi bi-lock absolute right-4 top-1/2 -translate-y-1/2 text-white/50 group-focus-within:text-white transition-colors"></i>
                            </div>
                        </div>

                        {/* Remember & Forgot */}
                        <div className="flex items-center justify-between text-sm">
                            <label className="flex items-center gap-2 text-white/70 cursor-pointer hover:text-white transition-colors">
                                <input type="checkbox" className="w-4 h-4 rounded border-white/30 bg-white/10 text-purple-500 focus:ring-purple-400" />
                                Remember me
                            </label>
                            <Link href="/forgot-password" className="text-white/70 hover:text-white transition-colors">
                                Forgot password?
                            </Link>
                        </div>

                        {/* Submit Button */}
                        <button
                            type="submit"
                            disabled={isLoading}
                            className="w-full py-4 bg-white text-gray-900 rounded-xl font-bold text-lg hover:bg-white/90 active:scale-[0.98] transition-all duration-300 shadow-lg hover:shadow-xl disabled:opacity-70 disabled:cursor-not-allowed group"
                        >
                            {isLoading ? (
                                <span className="flex items-center justify-center gap-2">
                                    <svg className="animate-spin h-5 w-5" viewBox="0 0 24 24">
                                        <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" fill="none"></circle>
                                        <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                                    </svg>
                                    Logging in...
                                </span>
                            ) : (
                                <span className="group-hover:tracking-wider transition-all">Log In</span>
                            )}
                        </button>
                    </form>

                    {/* Divider */}
                    <div className="flex items-center gap-4 my-6">
                        <div className="flex-1 h-px bg-white/20"></div>
                        <span className="text-white/50 text-sm">or continue with</span>
                        <div className="flex-1 h-px bg-white/20"></div>
                    </div>

                    {/* Social Login */}
                    <div className="grid grid-cols-3 gap-3">
                        <button className="flex items-center justify-center py-3 bg-white/10 border border-white/20 rounded-xl hover:bg-white/20 hover:scale-105 active:scale-95 transition-all duration-300 group">
                            <i className="bi bi-google text-xl text-white/70 group-hover:text-white"></i>
                        </button>
                        <button className="flex items-center justify-center py-3 bg-white/10 border border-white/20 rounded-xl hover:bg-white/20 hover:scale-105 active:scale-95 transition-all duration-300 group">
                            <i className="bi bi-github text-xl text-white/70 group-hover:text-white"></i>
                        </button>
                        <button className="flex items-center justify-center py-3 bg-white/10 border border-white/20 rounded-xl hover:bg-white/20 hover:scale-105 active:scale-95 transition-all duration-300 group">
                            <i className="bi bi-twitter-x text-xl text-white/70 group-hover:text-white"></i>
                        </button>
                    </div>

                    {/* Sign Up Link */}
                    <p className="text-center mt-8 text-white/70">
                        Don&apos;t have an account?{' '}
                        <Link href="/register" className="text-white font-semibold hover:underline">
                            Sign up
                        </Link>
                    </p>
                </div>

                {/* Bottom Footer */}
                <div className="mt-8 text-center text-white/50 text-xs space-x-4">
                    <Link href="/about" className="hover:text-white transition-colors">About</Link>
                    <Link href="/help" className="hover:text-white transition-colors">Help</Link>
                    <Link href="/privacy" className="hover:text-white transition-colors">Privacy</Link>
                    <Link href="/terms" className="hover:text-white transition-colors">Terms</Link>
                </div>
            </div>

            {/* Custom Styles */}
            <style jsx>{`
                @keyframes gradient-shift {
                    0%, 100% { background-position: 0% 50%; }
                    50% { background-position: 100% 50%; }
                }
                .animate-gradient-shift {
                    background-size: 200% 200%;
                    animation: gradient-shift 8s ease infinite;
                }
                @keyframes float-slow {
                    0%, 100% { transform: translate(0, 0) rotate(0deg); }
                    50% { transform: translate(30px, -30px) rotate(5deg); }
                }
                @keyframes float-medium {
                    0%, 100% { transform: translate(0, 0) rotate(0deg); }
                    50% { transform: translate(-20px, 20px) rotate(-3deg); }
                }
                @keyframes float-fast {
                    0%, 100% { transform: translate(0, 0); }
                    50% { transform: translate(15px, -15px); }
                }
                .animate-float-slow { animation: float-slow 12s ease-in-out infinite; }
                .animate-float-medium { animation: float-medium 10s ease-in-out infinite; }
                .animate-float-fast { animation: float-fast 8s ease-in-out infinite; }
                @keyframes shake {
                    0%, 100% { transform: translateX(0); }
                    10%, 30%, 50%, 70%, 90% { transform: translateX(-5px); }
                    20%, 40%, 60%, 80% { transform: translateX(5px); }
                }
                .animate-shake { animation: shake 0.5s ease-in-out; }
            `}</style>
        </div>
    );
}
