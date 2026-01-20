'use client';

import Link from 'next/link';
import { useAuth } from '@/lib/contexts/AuthContext';
import { useState, useEffect } from 'react';

export default function Navbar() {
    const { user, isAuthenticated, logout } = useAuth();
    const [theme, setTheme] = useState<'light' | 'dark'>('light');
    const [isMenuOpen, setIsMenuOpen] = useState(false);

    useEffect(() => {
        const savedTheme = localStorage.getItem('theme') as 'light' | 'dark' || 'light';
        setTheme(savedTheme);
        document.documentElement.setAttribute('data-theme', savedTheme);
    }, []);

    const toggleTheme = () => {
        const newTheme = theme === 'light' ? 'dark' : 'light';
        setTheme(newTheme);
        localStorage.setItem('theme', newTheme);
        document.documentElement.setAttribute('data-theme', newTheme);
    };

    return (
        <nav className="sticky top-0 z-50 bg-white dark:bg-slate-900 border-b border-gray-200 dark:border-slate-700 shadow-sm">
            <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
                <div className="flex justify-between items-center h-16">
                    {/* Logo */}
                    <Link href="/" className="flex items-center gap-2">
                        <div className="w-10 h-10 bg-gradient-to-br from-orange-500 to-orange-600 rounded-lg flex items-center justify-center">
                            <i className="bi bi-code-slash text-white text-xl"></i>
                        </div>
                        <span className="font-bold text-xl text-gray-900 dark:text-white">
                            Dev<span className="text-orange-500">Community</span>
                        </span>
                    </Link>

                    {/* Search - Desktop */}
                    <div className="hidden md:flex flex-1 max-w-xl mx-8">
                        <div className="relative w-full">
                            <input
                                type="text"
                                placeholder="Search questions, tags, users..."
                                className="w-full pl-10 pr-4 py-2 border border-gray-300 dark:border-slate-600 rounded-full bg-gray-50 dark:bg-slate-800 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-orange-500 focus:border-transparent"
                            />
                            <i className="bi bi-search absolute left-3 top-1/2 -translate-y-1/2 text-gray-400"></i>
                        </div>
                    </div>

                    {/* Nav Links - Desktop */}
                    <div className="hidden md:flex items-center gap-4">
                        <Link href="/questions" className="text-gray-600 dark:text-gray-300 hover:text-orange-500 transition">
                            Questions
                        </Link>
                        <Link href="/tags" className="text-gray-600 dark:text-gray-300 hover:text-orange-500 transition">
                            Tags
                        </Link>
                        <Link href="/users" className="text-gray-600 dark:text-gray-300 hover:text-orange-500 transition">
                            Users
                        </Link>

                        {/* Theme Toggle */}
                        <button
                            onClick={toggleTheme}
                            className="p-2 rounded-full hover:bg-gray-100 dark:hover:bg-slate-800 transition"
                            aria-label="Toggle theme"
                        >
                            {theme === 'light' ? (
                                <i className="bi bi-moon-fill text-gray-600"></i>
                            ) : (
                                <i className="bi bi-sun-fill text-yellow-400"></i>
                            )}
                        </button>

                        {isAuthenticated && user ? (
                            <>
                                {/* Notifications */}
                                <button className="p-2 rounded-full hover:bg-gray-100 dark:hover:bg-slate-800 transition relative">
                                    <i className="bi bi-bell text-gray-600 dark:text-gray-300"></i>
                                    <span className="absolute top-0 right-0 w-2 h-2 bg-red-500 rounded-full"></span>
                                </button>

                                {/* Ask Question */}
                                <Link
                                    href="/questions/ask"
                                    className="px-4 py-2 bg-orange-500 text-white rounded-lg hover:bg-orange-600 transition font-medium"
                                >
                                    Ask Question
                                </Link>

                                {/* User Menu */}
                                <div className="relative">
                                    <button
                                        onClick={() => setIsMenuOpen(!isMenuOpen)}
                                        className="flex items-center gap-2 p-1 rounded-full hover:bg-gray-100 dark:hover:bg-slate-800 transition"
                                    >
                                        <div className="w-8 h-8 bg-gradient-to-br from-blue-500 to-purple-500 rounded-full flex items-center justify-center text-white font-medium">
                                            {user.username?.charAt(0).toUpperCase() || 'U'}
                                        </div>
                                    </button>

                                    {isMenuOpen && (
                                        <div className="absolute right-0 mt-2 w-48 bg-white dark:bg-slate-800 rounded-lg shadow-lg border border-gray-200 dark:border-slate-700 py-1">
                                            <Link href="/profile" className="block px-4 py-2 text-gray-700 dark:text-gray-200 hover:bg-gray-100 dark:hover:bg-slate-700">
                                                <i className="bi bi-person me-2"></i> Profile
                                            </Link>
                                            <Link href="/settings" className="block px-4 py-2 text-gray-700 dark:text-gray-200 hover:bg-gray-100 dark:hover:bg-slate-700">
                                                <i className="bi bi-gear me-2"></i> Settings
                                            </Link>
                                            <hr className="my-1 border-gray-200 dark:border-slate-700" />
                                            <button
                                                onClick={logout}
                                                className="w-full text-left px-4 py-2 text-red-600 hover:bg-gray-100 dark:hover:bg-slate-700"
                                            >
                                                <i className="bi bi-box-arrow-right me-2"></i> Logout
                                            </button>
                                        </div>
                                    )}
                                </div>
                            </>
                        ) : (
                            <>
                                <Link
                                    href="/login"
                                    className="px-4 py-2 text-gray-600 dark:text-gray-300 hover:text-orange-500 transition font-medium"
                                >
                                    Log in
                                </Link>
                                <Link
                                    href="/register"
                                    className="px-4 py-2 bg-orange-500 text-white rounded-lg hover:bg-orange-600 transition font-medium"
                                >
                                    Sign up
                                </Link>
                            </>
                        )}
                    </div>

                    {/* Mobile Menu Button */}
                    <button
                        onClick={() => setIsMenuOpen(!isMenuOpen)}
                        className="md:hidden p-2 rounded-lg hover:bg-gray-100 dark:hover:bg-slate-800"
                    >
                        <i className={`bi ${isMenuOpen ? 'bi-x-lg' : 'bi-list'} text-xl text-gray-600 dark:text-gray-300`}></i>
                    </button>
                </div>

                {/* Mobile Menu */}
                {isMenuOpen && (
                    <div className="md:hidden py-4 border-t border-gray-200 dark:border-slate-700">
                        <div className="mb-4">
                            <input
                                type="text"
                                placeholder="Search..."
                                className="w-full px-4 py-2 border border-gray-300 dark:border-slate-600 rounded-lg bg-gray-50 dark:bg-slate-800"
                            />
                        </div>
                        <div className="space-y-2">
                            <Link href="/questions" className="block py-2 text-gray-600 dark:text-gray-300 hover:text-orange-500">
                                Questions
                            </Link>
                            <Link href="/tags" className="block py-2 text-gray-600 dark:text-gray-300 hover:text-orange-500">
                                Tags
                            </Link>
                            <Link href="/users" className="block py-2 text-gray-600 dark:text-gray-300 hover:text-orange-500">
                                Users
                            </Link>
                            {!isAuthenticated && (
                                <div className="flex gap-2 pt-4">
                                    <Link href="/login" className="flex-1 text-center py-2 border border-orange-500 text-orange-500 rounded-lg">
                                        Log in
                                    </Link>
                                    <Link href="/register" className="flex-1 text-center py-2 bg-orange-500 text-white rounded-lg">
                                        Sign up
                                    </Link>
                                </div>
                            )}
                        </div>
                    </div>
                )}
            </div>
        </nav>
    );
}
