'use client';

import { useState, FormEvent } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { useAuth } from '@/lib/contexts/AuthContext';

export default function RegisterPage() {
    const [formData, setFormData] = useState({
        username: '',
        email: '',
        displayName: '',
        password: '',
        confirmPassword: ''
    });
    const [error, setError] = useState('');
    const [isLoading, setIsLoading] = useState(false);
    const router = useRouter();
    const { register } = useAuth();

    const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        setFormData({ ...formData, [e.target.name]: e.target.value });
    };

    const handleSubmit = async (e: FormEvent) => {
        e.preventDefault();
        setError('');

        if (formData.password !== formData.confirmPassword) {
            setError('Passwords do not match');
            return;
        }

        if (formData.password.length < 6) {
            setError('Password must be at least 6 characters');
            return;
        }

        setIsLoading(true);

        try {
            await register({
                username: formData.username,
                email: formData.email,
                password: formData.password,
                confirmPassword: formData.confirmPassword,
                displayName: formData.displayName
            });
            router.push('/');
        } catch (err: unknown) {
            if (err instanceof Error) {
                setError(err.message);
            } else {
                setError('Registration failed. Please try again.');
            }
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <div className="min-h-screen flex items-center justify-center relative overflow-hidden py-12">
            {/* Animated Gradient Background - Different from Login */}
            <div className="absolute inset-0 bg-gradient-to-tr from-indigo-600 via-purple-500 to-pink-400 animate-gradient-shift"></div>

            {/* Floating Shapes */}
            <div className="absolute inset-0 overflow-hidden pointer-events-none">
                <div className="absolute top-10 right-20 w-80 h-80 bg-white/10 rounded-full blur-3xl animate-float-slow"></div>
                <div className="absolute bottom-10 left-20 w-72 h-72 bg-indigo-300/20 rounded-full blur-3xl animate-float-medium"></div>
                <div className="absolute top-1/3 right-1/4 w-56 h-56 bg-pink-300/15 rounded-full blur-3xl animate-float-fast"></div>
            </div>

            {/* Register Card */}
            <div className="relative z-10 w-full max-w-md mx-4">
                {/* Glass Card */}
                <div className="backdrop-blur-xl bg-white/10 border border-white/20 rounded-3xl p-8 shadow-2xl transform hover:scale-[1.01] transition-all duration-500">

                    {/* Logo & Branding */}
                    <div className="text-center mb-6">
                        <div className="inline-flex items-center justify-center w-16 h-16 bg-white/20 backdrop-blur-sm rounded-2xl mb-3 shadow-lg transform hover:rotate-6 transition-transform duration-300">
                            <span className="text-3xl">🚀</span>
                        </div>
                        <h1 className="text-2xl font-bold text-white mb-1">Join DevCommunity</h1>
                        <p className="text-white/60 text-sm">Start your developer journey today</p>
                    </div>

                    {/* Error Message */}
                    {error && (
                        <div className="mb-4 p-3 bg-red-500/20 border border-red-400/30 rounded-xl text-red-100 text-sm text-center backdrop-blur-sm animate-shake">
                            {error}
                        </div>
                    )}

                    {/* Register Form */}
                    <form onSubmit={handleSubmit} className="space-y-4">
                        {/* Username */}
                        <div className="group">
                            <div className="relative">
                                <input
                                    type="text"
                                    name="username"
                                    value={formData.username}
                                    onChange={handleChange}
                                    placeholder="Username"
                                    required
                                    className="w-full px-4 py-3.5 bg-white/10 border border-white/20 rounded-xl text-white placeholder-white/50 focus:bg-white/20 focus:border-white/40 focus:ring-2 focus:ring-white/20 outline-none transition-all duration-300 backdrop-blur-sm"
                                />
                                <i className="bi bi-person absolute right-4 top-1/2 -translate-y-1/2 text-white/50 group-focus-within:text-white transition-colors"></i>
                            </div>
                        </div>

                        {/* Email */}
                        <div className="group">
                            <div className="relative">
                                <input
                                    type="email"
                                    name="email"
                                    value={formData.email}
                                    onChange={handleChange}
                                    placeholder="Email address"
                                    required
                                    className="w-full px-4 py-3.5 bg-white/10 border border-white/20 rounded-xl text-white placeholder-white/50 focus:bg-white/20 focus:border-white/40 focus:ring-2 focus:ring-white/20 outline-none transition-all duration-300 backdrop-blur-sm"
                                />
                                <i className="bi bi-envelope absolute right-4 top-1/2 -translate-y-1/2 text-white/50 group-focus-within:text-white transition-colors"></i>
                            </div>
                        </div>

                        {/* Display Name */}
                        <div className="group">
                            <div className="relative">
                                <input
                                    type="text"
                                    name="displayName"
                                    value={formData.displayName}
                                    onChange={handleChange}
                                    placeholder="Display name (optional)"
                                    className="w-full px-4 py-3.5 bg-white/10 border border-white/20 rounded-xl text-white placeholder-white/50 focus:bg-white/20 focus:border-white/40 focus:ring-2 focus:ring-white/20 outline-none transition-all duration-300 backdrop-blur-sm"
                                />
                                <i className="bi bi-card-text absolute right-4 top-1/2 -translate-y-1/2 text-white/50 group-focus-within:text-white transition-colors"></i>
                            </div>
                        </div>

                        {/* Password */}
                        <div className="group">
                            <div className="relative">
                                <input
                                    type="password"
                                    name="password"
                                    value={formData.password}
                                    onChange={handleChange}
                                    placeholder="Password"
                                    required
                                    className="w-full px-4 py-3.5 bg-white/10 border border-white/20 rounded-xl text-white placeholder-white/50 focus:bg-white/20 focus:border-white/40 focus:ring-2 focus:ring-white/20 outline-none transition-all duration-300 backdrop-blur-sm"
                                />
                                <i className="bi bi-lock absolute right-4 top-1/2 -translate-y-1/2 text-white/50 group-focus-within:text-white transition-colors"></i>
                            </div>
                        </div>

                        {/* Confirm Password */}
                        <div className="group">
                            <div className="relative">
                                <input
                                    type="password"
                                    name="confirmPassword"
                                    value={formData.confirmPassword}
                                    onChange={handleChange}
                                    placeholder="Confirm password"
                                    required
                                    className="w-full px-4 py-3.5 bg-white/10 border border-white/20 rounded-xl text-white placeholder-white/50 focus:bg-white/20 focus:border-white/40 focus:ring-2 focus:ring-white/20 outline-none transition-all duration-300 backdrop-blur-sm"
                                />
                                <i className="bi bi-shield-lock absolute right-4 top-1/2 -translate-y-1/2 text-white/50 group-focus-within:text-white transition-colors"></i>
                            </div>
                        </div>

                        {/* Terms Agreement */}
                        <label className="flex items-start gap-3 text-sm text-white/70 cursor-pointer">
                            <input type="checkbox" required className="w-4 h-4 mt-0.5 rounded border-white/30 bg-white/10 text-purple-500 focus:ring-purple-400" />
                            <span>I agree to the <Link href="/terms" className="text-white hover:underline">Terms of Service</Link> and <Link href="/privacy" className="text-white hover:underline">Privacy Policy</Link></span>
                        </label>

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
                                    Creating account...
                                </span>
                            ) : (
                                <span className="group-hover:tracking-wider transition-all">Create Account</span>
                            )}
                        </button>
                    </form>

                    {/* Divider */}
                    <div className="flex items-center gap-4 my-5">
                        <div className="flex-1 h-px bg-white/20"></div>
                        <span className="text-white/50 text-sm">or sign up with</span>
                        <div className="flex-1 h-px bg-white/20"></div>
                    </div>

                    {/* Social Login */}
                    <div className="grid grid-cols-3 gap-3">
                        <button className="flex items-center justify-center py-3 bg-white/10 border border-white/20 rounded-xl hover:bg-white/20 hover:scale-105 active:scale-95 transition-all duration-300 group">
                            <i className="bi bi-google text-lg text-white/70 group-hover:text-white"></i>
                        </button>
                        <button className="flex items-center justify-center py-3 bg-white/10 border border-white/20 rounded-xl hover:bg-white/20 hover:scale-105 active:scale-95 transition-all duration-300 group">
                            <i className="bi bi-github text-lg text-white/70 group-hover:text-white"></i>
                        </button>
                        <button className="flex items-center justify-center py-3 bg-white/10 border border-white/20 rounded-xl hover:bg-white/20 hover:scale-105 active:scale-95 transition-all duration-300 group">
                            <i className="bi bi-twitter-x text-lg text-white/70 group-hover:text-white"></i>
                        </button>
                    </div>

                    {/* Login Link */}
                    <p className="text-center mt-6 text-white/70">
                        Already have an account?{' '}
                        <Link href="/login" className="text-white font-semibold hover:underline">
                            Log in
                        </Link>
                    </p>
                </div>

                {/* Bottom Footer */}
                <div className="mt-6 text-center text-white/50 text-xs space-x-4">
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
                    animation: gradient-shift 10s ease infinite;
                }
                @keyframes float-slow {
                    0%, 100% { transform: translate(0, 0) rotate(0deg); }
                    50% { transform: translate(-25px, 25px) rotate(-5deg); }
                }
                @keyframes float-medium {
                    0%, 100% { transform: translate(0, 0) rotate(0deg); }
                    50% { transform: translate(20px, -20px) rotate(3deg); }
                }
                @keyframes float-fast {
                    0%, 100% { transform: translate(0, 0); }
                    50% { transform: translate(-15px, 15px); }
                }
                .animate-float-slow { animation: float-slow 14s ease-in-out infinite; }
                .animate-float-medium { animation: float-medium 11s ease-in-out infinite; }
                .animate-float-fast { animation: float-fast 9s ease-in-out infinite; }
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
