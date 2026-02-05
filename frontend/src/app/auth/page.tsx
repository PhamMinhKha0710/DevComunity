'use client';

import { useState, FormEvent, useEffect, Suspense } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import Link from 'next/link';
import { useAuth } from '@/lib/contexts/AuthContext';

function AuthContent() {
    const searchParams = useSearchParams();
    const initialMode = searchParams.get('mode') === 'register' ? 'register' : 'login';
    const [isLoginMode, setIsLoginMode] = useState(initialMode === 'login');

    // Login form state
    const [loginEmail, setLoginEmail] = useState('');
    const [loginPassword, setLoginPassword] = useState('');
    const [showLoginPassword, setShowLoginPassword] = useState(false);

    // Register form state
    const [fullName, setFullName] = useState('');
    const [registerEmail, setRegisterEmail] = useState('');
    const [registerPassword, setRegisterPassword] = useState('');
    const [confirmPassword, setConfirmPassword] = useState('');
    const [showRegisterPassword, setShowRegisterPassword] = useState(false);
    const [showConfirmPassword, setShowConfirmPassword] = useState(false);
    const [agreeTerms, setAgreeTerms] = useState(false);

    // Shared state
    const [error, setError] = useState('');
    const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
    const [isLoading, setIsLoading] = useState(false);

    const router = useRouter();
    const { login, register } = useAuth();

    useEffect(() => {
        const mode = isLoginMode ? 'login' : 'register';
        window.history.replaceState(null, '', `/auth?mode=${mode}`);
    }, [isLoginMode]);

    const validateLoginForm = () => {
        const errors: Record<string, string> = {};
        if (!loginEmail.trim()) errors.email = 'Email is required';
        if (!loginPassword) errors.password = 'Password is required';
        else if (loginPassword.length < 6) errors.password = 'Password must be at least 6 characters';
        setFieldErrors(errors);
        return Object.keys(errors).length === 0;
    };

    const validateRegisterForm = () => {
        const errors: Record<string, string> = {};
        if (!fullName.trim()) errors.fullName = 'Full name is required';
        if (!registerEmail.trim()) errors.email = 'Email is required';
        else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(registerEmail)) errors.email = 'Please enter a valid email';
        if (!registerPassword) errors.password = 'Password is required';
        else if (registerPassword.length < 6) errors.password = 'Password must be at least 6 characters';
        if (!confirmPassword) errors.confirmPassword = 'Please confirm your password';
        else if (registerPassword !== confirmPassword) errors.confirmPassword = 'Passwords do not match';
        if (!agreeTerms) errors.terms = 'You must agree to the terms';
        setFieldErrors(errors);
        return Object.keys(errors).length === 0;
    };

    const handleLoginSubmit = async (e: FormEvent) => {
        e.preventDefault();
        setError('');
        if (!validateLoginForm()) return;
        setIsLoading(true);
        try {
            await login({ email: loginEmail, password: loginPassword });
            router.push('/');
        } catch (err: unknown) {
            setError(err instanceof Error ? err.message : 'Login failed. Please try again.');
        } finally {
            setIsLoading(false);
        }
    };

    const handleRegisterSubmit = async (e: FormEvent) => {
        e.preventDefault();
        setError('');
        if (!validateRegisterForm()) return;
        setIsLoading(true);
        try {
            await register({
                username: fullName.replace(/\s+/g, '').toLowerCase(),
                email: registerEmail,
                password: registerPassword,
                confirmPassword: confirmPassword,
                displayName: fullName,
            });
            router.push('/');
        } catch (err: unknown) {
            setError(err instanceof Error ? err.message : 'Registration failed. Please try again.');
        } finally {
            setIsLoading(false);
        }
    };

    const toggleMode = () => {
        setError('');
        setFieldErrors({});
        setIsLoginMode(!isLoginMode);
    };

    return (
        <div className="min-h-screen flex items-center justify-center relative overflow-hidden bg-slate-50">
            {/* Background Pattern */}
            <div className="absolute inset-0 overflow-hidden pointer-events-none opacity-40" style={{
                backgroundImage: 'radial-gradient(#cbd5e1 1px, transparent 1px)',
                backgroundSize: '30px 30px'
            }}></div>

            {/* Main Container */}
            <div className="relative z-10 w-full max-w-5xl mx-4">
                <div className="bg-white/90 backdrop-blur-xl border border-white/60 rounded-3xl shadow-2xl overflow-hidden">

                    <div className="relative flex flex-col lg:flex-row min-h-[600px] lg:min-h-[650px]">

                        {/* Left Side - Fixed: Register Form (visible when register mode) OR Welcome Panel (visible when login mode) */}
                        <div className="lg:w-1/2 relative overflow-hidden">
                            {/* Gradient Overlay for Login Mode */}
                            <div
                                className={`absolute inset-0 bg-blue-600 flex items-center justify-center transition-all duration-700 ease-in-out ${isLoginMode ? 'translate-x-0 opacity-100' : '-translate-x-full opacity-0'
                                    }`}
                            >
                                <div className="text-center p-8 lg:p-12 text-white max-w-md">
                                    <div className="inline-flex items-center justify-center w-20 h-20 bg-white/20 backdrop-blur-sm rounded-2xl mb-6 shadow-lg">
                                        <span className="text-4xl">🚀</span>
                                    </div>
                                    <h1 className="text-4xl lg:text-5xl font-black mb-4 tracking-tight drop-shadow-lg">
                                        Welcome<br />Back!
                                    </h1>
                                    <p className="text-white/90 text-lg mb-8 leading-relaxed">
                                        Sign in to continue your journey with the developer community.
                                    </p>
                                    <p className="text-white/80 text-sm mb-4">Don&apos;t have an account?</p>
                                    <button
                                        onClick={toggleMode}
                                        className="px-8 py-3 bg-white/10 border border-white/20 text-white rounded-xl font-bold text-lg hover:bg-white/20 active:scale-95 transition-all shadow-lg"
                                    >
                                        Sign Up
                                    </button>
                                </div>
                            </div>

                            {/* Register Form - visible in register mode */}
                            <div
                                className={`p-8 lg:p-12 flex flex-col justify-center min-h-[600px] transition-all duration-700 ease-in-out ${!isLoginMode ? 'translate-x-0 opacity-100' : 'translate-x-full opacity-0 absolute inset-0'
                                    }`}
                            >
                                <div className="max-w-sm mx-auto w-full">
                                    <h2 className="text-3xl lg:text-4xl font-black text-gray-800 mb-2 tracking-tight">
                                        Create <span className="text-orange-500">Account</span>
                                    </h2>
                                    <p className="text-gray-500 mb-6">Join our community of developers</p>

                                    {error && !isLoginMode && (
                                        <div className="mb-4 p-3 bg-red-50 border border-red-200 rounded-xl text-red-600 text-sm">
                                            {error}
                                        </div>
                                    )}

                                    <form onSubmit={handleRegisterSubmit} className="space-y-4">
                                        <div>
                                            <label className="block text-gray-700 text-sm font-semibold mb-2">Full Name</label>
                                            <input
                                                type="text"
                                                value={fullName}
                                                onChange={(e) => setFullName(e.target.value)}
                                                placeholder="John Doe"
                                                className={`w-full px-4 py-3 bg-gray-50 border-2 ${fieldErrors.fullName ? 'border-red-400' : 'border-gray-200'} rounded-xl text-gray-800 placeholder-gray-400 focus:border-orange-500 focus:bg-white outline-none transition-all`}
                                            />
                                            {fieldErrors.fullName && <p className="text-red-500 text-xs mt-1">{fieldErrors.fullName}</p>}
                                        </div>

                                        <div>
                                            <label className="block text-gray-700 text-sm font-semibold mb-2">Email</label>
                                            <input
                                                type="email"
                                                value={registerEmail}
                                                onChange={(e) => setRegisterEmail(e.target.value)}
                                                placeholder="you@example.com"
                                                className={`w-full px-4 py-3 bg-gray-50 border-2 ${fieldErrors.email ? 'border-red-400' : 'border-gray-200'} rounded-xl text-gray-800 placeholder-gray-400 focus:border-orange-500 focus:bg-white outline-none transition-all`}
                                            />
                                            {fieldErrors.email && <p className="text-red-500 text-xs mt-1">{fieldErrors.email}</p>}
                                        </div>

                                        <div>
                                            <label className="block text-gray-700 text-sm font-semibold mb-2">Password</label>
                                            <div className="relative">
                                                <input
                                                    type={showRegisterPassword ? 'text' : 'password'}
                                                    value={registerPassword}
                                                    onChange={(e) => setRegisterPassword(e.target.value)}
                                                    placeholder="••••••••"
                                                    className={`w-full px-4 py-3 bg-gray-50 border-2 ${fieldErrors.password ? 'border-red-400' : 'border-gray-200'} rounded-xl text-gray-800 placeholder-gray-400 focus:border-orange-500 focus:bg-white outline-none transition-all pr-12`}
                                                />
                                                <button
                                                    type="button"
                                                    onClick={() => setShowRegisterPassword(!showRegisterPassword)}
                                                    className="absolute right-4 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600 transition-colors"
                                                >
                                                    <i className={`bi ${showRegisterPassword ? 'bi-eye-slash' : 'bi-eye'} text-lg`}></i>
                                                </button>
                                            </div>
                                            {fieldErrors.password && <p className="text-red-500 text-xs mt-1">{fieldErrors.password}</p>}
                                        </div>

                                        <div>
                                            <label className="block text-gray-700 text-sm font-semibold mb-2">Confirm Password</label>
                                            <div className="relative">
                                                <input
                                                    type={showConfirmPassword ? 'text' : 'password'}
                                                    value={confirmPassword}
                                                    onChange={(e) => setConfirmPassword(e.target.value)}
                                                    placeholder="••••••••"
                                                    className={`w-full px-4 py-3 bg-gray-50 border-2 ${fieldErrors.confirmPassword ? 'border-red-400' : 'border-gray-200'} rounded-xl text-gray-800 placeholder-gray-400 focus:border-orange-500 focus:bg-white outline-none transition-all pr-12`}
                                                />
                                                <button
                                                    type="button"
                                                    onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                                                    className="absolute right-4 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600 transition-colors"
                                                >
                                                    <i className={`bi ${showConfirmPassword ? 'bi-eye-slash' : 'bi-eye'} text-lg`}></i>
                                                </button>
                                            </div>
                                            {fieldErrors.confirmPassword && <p className="text-red-500 text-xs mt-1">{fieldErrors.confirmPassword}</p>}
                                        </div>

                                        <div className="flex items-start gap-3">
                                            <input
                                                type="checkbox"
                                                id="terms"
                                                checked={agreeTerms}
                                                onChange={(e) => setAgreeTerms(e.target.checked)}
                                                className="w-4 h-4 mt-1 rounded border-gray-300 text-orange-500 focus:ring-orange-500"
                                            />
                                            <label htmlFor="terms" className="text-gray-600 text-sm">
                                                I agree to the <Link href="/terms" className="text-orange-500 hover:underline">Terms</Link> and <Link href="/privacy" className="text-orange-500 hover:underline">Privacy</Link>
                                            </label>
                                        </div>
                                        {fieldErrors.terms && <p className="text-red-500 text-xs">{fieldErrors.terms}</p>}

                                        <button
                                            type="submit"
                                            disabled={isLoading}
                                            className="w-full py-3.5 bg-orange-500 text-white rounded-xl font-bold text-lg hover:bg-orange-600 active:scale-[0.98] transition-all shadow-lg shadow-orange-500/30 disabled:opacity-70"
                                        >
                                            {isLoading ? 'Creating Account...' : 'Create Account'}
                                        </button>
                                    </form>

                                    <p className="text-center mt-6 text-gray-500 lg:hidden">
                                        Already have an account?{' '}
                                        <button onClick={toggleMode} className="text-orange-500 font-semibold hover:underline">
                                            Sign In
                                        </button>
                                    </p>
                                </div>
                            </div>
                        </div>

                        {/* Right Side - Fixed: Login Form (visible when login mode) OR Welcome Panel (visible when register mode) */}
                        <div className="lg:w-1/2 relative overflow-hidden border-l border-gray-100">
                            {/* Login Form - visible in login mode */}
                            <div
                                className={`p-8 lg:p-12 flex flex-col justify-center min-h-[600px] transition-all duration-700 ease-in-out ${isLoginMode ? 'translate-x-0 opacity-100' : '-translate-x-full opacity-0 absolute inset-0'
                                    }`}
                            >
                                <div className="max-w-sm mx-auto w-full">
                                    <h2 className="text-3xl lg:text-4xl font-black text-gray-800 mb-2 tracking-tight">
                                        Sign <span className="text-orange-500">In</span>
                                    </h2>
                                    <p className="text-gray-500 mb-6">Enter your credentials to continue</p>

                                    {error && isLoginMode && (
                                        <div className="mb-4 p-3 bg-red-50 border border-red-200 rounded-xl text-red-600 text-sm">
                                            {error}
                                        </div>
                                    )}

                                    <form onSubmit={handleLoginSubmit} className="space-y-5">
                                        <div>
                                            <label className="block text-gray-700 text-sm font-semibold mb-2">Email or Username</label>
                                            <input
                                                type="text"
                                                value={loginEmail}
                                                onChange={(e) => setLoginEmail(e.target.value)}
                                                placeholder="you@example.com"
                                                className={`w-full px-4 py-3.5 bg-gray-50 border-2 ${fieldErrors.email ? 'border-red-400' : 'border-gray-200'} rounded-xl text-gray-800 placeholder-gray-400 focus:border-orange-500 focus:bg-white outline-none transition-all text-lg`}
                                            />
                                            {fieldErrors.email && <p className="text-red-500 text-xs mt-1">{fieldErrors.email}</p>}
                                        </div>

                                        <div>
                                            <div className="flex justify-between items-center mb-2">
                                                <label className="text-gray-700 text-sm font-semibold">Password</label>
                                                <Link href="/forgot-password" className="text-orange-500 text-sm hover:underline font-medium">
                                                    Forgot password?
                                                </Link>
                                            </div>
                                            <div className="relative">
                                                <input
                                                    type={showLoginPassword ? 'text' : 'password'}
                                                    value={loginPassword}
                                                    onChange={(e) => setLoginPassword(e.target.value)}
                                                    placeholder="••••••••"
                                                    className={`w-full px-4 py-3.5 bg-gray-50 border-2 ${fieldErrors.password ? 'border-red-400' : 'border-gray-200'} rounded-xl text-gray-800 placeholder-gray-400 focus:border-orange-500 focus:bg-white outline-none transition-all pr-12 text-lg`}
                                                />
                                                <button
                                                    type="button"
                                                    onClick={() => setShowLoginPassword(!showLoginPassword)}
                                                    className="absolute right-4 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600 transition-colors"
                                                >
                                                    <i className={`bi ${showLoginPassword ? 'bi-eye-slash' : 'bi-eye'} text-xl`}></i>
                                                </button>
                                            </div>
                                            {fieldErrors.password && <p className="text-red-500 text-xs mt-1">{fieldErrors.password}</p>}
                                        </div>

                                        <div className="flex items-center gap-2">
                                            <input type="checkbox" id="remember" className="w-4 h-4 rounded border-gray-300 text-orange-500 focus:ring-orange-500" />
                                            <label htmlFor="remember" className="text-gray-600 text-sm">Remember me</label>
                                        </div>

                                        <button
                                            type="submit"
                                            disabled={isLoading}
                                            className="w-full py-4 bg-orange-500 text-white rounded-xl font-bold text-lg hover:bg-orange-600 active:scale-[0.98] transition-all shadow-lg shadow-orange-500/30 disabled:opacity-70"
                                        >
                                            {isLoading ? 'Signing In...' : 'Sign In'}
                                        </button>
                                    </form>

                                    <div className="flex items-center gap-4 my-6">
                                        <div className="flex-1 h-px bg-gray-200"></div>
                                        <span className="text-gray-400 text-sm">or</span>
                                        <div className="flex-1 h-px bg-gray-200"></div>
                                    </div>

                                    <div className="grid grid-cols-3 gap-3">
                                        <button className="flex items-center justify-center py-3 bg-gray-50 border-2 border-gray-200 rounded-xl hover:bg-gray-100 hover:border-gray-300 transition-all">
                                            <i className="bi bi-google text-xl text-gray-600"></i>
                                        </button>
                                        <button className="flex items-center justify-center py-3 bg-gray-50 border-2 border-gray-200 rounded-xl hover:bg-gray-100 hover:border-gray-300 transition-all">
                                            <i className="bi bi-github text-xl text-gray-600"></i>
                                        </button>
                                        <button className="flex items-center justify-center py-3 bg-gray-50 border-2 border-gray-200 rounded-xl hover:bg-gray-100 hover:border-gray-300 transition-all">
                                            <i className="bi bi-twitter-x text-xl text-gray-600"></i>
                                        </button>
                                    </div>

                                    <p className="text-center mt-6 text-gray-500 lg:hidden">
                                        Don&apos;t have an account?{' '}
                                        <button onClick={toggleMode} className="text-orange-500 font-semibold hover:underline">
                                            Sign Up
                                        </button>
                                    </p>
                                </div>
                            </div>

                            {/* Gradient Overlay for Register Mode */}
                            <div
                                className={`absolute inset-0 bg-orange-500 flex items-center justify-center transition-all duration-700 ease-in-out ${!isLoginMode ? 'translate-x-0 opacity-100' : 'translate-x-full opacity-0'
                                    }`}
                            >
                                <div className="text-center p-8 lg:p-12 text-white max-w-md">
                                    <div className="inline-flex items-center justify-center w-20 h-20 bg-white/20 backdrop-blur-sm rounded-2xl mb-6 shadow-lg">
                                        <span className="text-4xl">🎉</span>
                                    </div>
                                    <h1 className="text-4xl lg:text-5xl font-black mb-4 tracking-tight drop-shadow-lg">
                                        Join Our<br />Community!
                                    </h1>
                                    <p className="text-white/90 text-lg mb-8 leading-relaxed">
                                        Connect with developers, share knowledge, and grow together.
                                    </p>
                                    <ul className="text-left space-y-2 mb-8 text-white/90">
                                        <li className="flex items-center gap-3">
                                            <span className="w-6 h-6 rounded-full bg-white/20 flex items-center justify-center text-sm">✓</span>
                                            Ask questions & get answers
                                        </li>
                                        <li className="flex items-center gap-3">
                                            <span className="w-6 h-6 rounded-full bg-white/20 flex items-center justify-center text-sm">✓</span>
                                            Share your expertise
                                        </li>
                                        <li className="flex items-center gap-3">
                                            <span className="w-6 h-6 rounded-full bg-white/20 flex items-center justify-center text-sm">✓</span>
                                            Build your reputation
                                        </li>
                                    </ul>
                                    <p className="text-white/80 text-sm mb-4">Already have an account?</p>
                                    <button
                                        onClick={toggleMode}
                                        className="px-8 py-3 bg-white text-purple-600 rounded-xl font-bold text-lg hover:bg-white/90 active:scale-95 transition-all shadow-lg"
                                    >
                                        Sign In
                                    </button>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>

                {/* Footer Links */}
                <div className="mt-8 text-center text-gray-500 text-sm space-x-6">
                    <Link href="/" className="hover:text-gray-700 transition-colors">Home</Link>
                    <Link href="/about" className="hover:text-gray-700 transition-colors">About</Link>
                    <Link href="/help" className="hover:text-gray-700 transition-colors">Help</Link>
                    <Link href="/privacy" className="hover:text-gray-700 transition-colors">Privacy</Link>
                </div>
            </div>
        </div>
    );
}

export default function AuthPage() {
    return (
        <Suspense fallback={
            <div className="min-h-screen flex items-center justify-center bg-slate-50">
                <div className="w-10 h-10 border-4 border-orange-500/30 border-t-orange-500 rounded-full animate-spin"></div>
            </div>
        }>
            <AuthContent />
        </Suspense>
    );
}

