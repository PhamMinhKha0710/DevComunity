'use client';

import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useState, useEffect, useRef } from 'react';
import { useAuth } from '@/lib/contexts/AuthContext';
import { useNotifications } from '@/lib/contexts/NotificationContext';

export default function ModernNavbar() {
    const router = useRouter();
    const { user, isAuthenticated, logout } = useAuth();
    const { unreadCount } = useNotifications();
    const [searchQuery, setSearchQuery] = useState('');
    const [isDark, setIsDark] = useState(false);
    const [showUserMenu, setShowUserMenu] = useState(false);
    const menuRef = useRef<HTMLDivElement>(null);

    useEffect(() => {
        const savedTheme = localStorage.getItem('theme');
        const prefersDark = savedTheme === 'dark';
        setIsDark(prefersDark);
        if (prefersDark) {
            document.documentElement.classList.add('dark');
        } else {
            document.documentElement.classList.remove('dark');
        }
    }, []);

    useEffect(() => {
        const handleClickOutside = (e: MouseEvent) => {
            if (menuRef.current && !menuRef.current.contains(e.target as Node)) {
                setShowUserMenu(false);
            }
        };
        document.addEventListener('mousedown', handleClickOutside);
        return () => document.removeEventListener('mousedown', handleClickOutside);
    }, []);

    const toggleTheme = () => {
        const newIsDark = !isDark;
        setIsDark(newIsDark);
        localStorage.setItem('theme', newIsDark ? 'dark' : 'light');
        if (newIsDark) {
            document.documentElement.classList.add('dark');
        } else {
            document.documentElement.classList.remove('dark');
        }
    };

    const handleSearch = (e: React.FormEvent) => {
        e.preventDefault();
        if (searchQuery.trim()) {
            router.push(`/search?q=${encodeURIComponent(searchQuery.trim())}`);
        }
    };

    return (
        <header className="h-16 bg-white dark:bg-slate-900 border-b border-slate-200 dark:border-slate-800 flex items-center justify-between px-8 z-10 shrink-0">
            {/* Search */}
            <div className="flex-1 max-w-xl">
                <form onSubmit={handleSearch} className="relative">
                    <span className="material-symbols-outlined absolute left-3 top-1/2 -translate-y-1/2 text-slate-400">search</span>
                    <input
                        type="text"
                        value={searchQuery}
                        onChange={(e) => setSearchQuery(e.target.value)}
                        placeholder="Search questions, people or groups..."
                        className="w-full bg-slate-100 dark:bg-slate-800 border-none rounded-lg pl-10 pr-4 py-2 text-sm focus:ring-2 focus:ring-[var(--primary)] transition-all placeholder:text-slate-500"
                    />
                </form>
            </div>

            {/* Right Actions */}
            <div className="flex items-center gap-3">
                {/* Theme Toggle */}
                <button
                    onClick={toggleTheme}
                    className="p-2 text-slate-500 hover:bg-slate-100 dark:hover:bg-slate-800 rounded-lg transition-colors"
                >
                    <span className="material-symbols-outlined">{isDark ? 'light_mode' : 'dark_mode'}</span>
                </button>

                {isAuthenticated ? (
                    <>
                        {/* Notifications */}
                        <Link
                            href="/notifications"
                            className="relative p-2 text-slate-500 hover:bg-slate-100 dark:hover:bg-slate-800 rounded-lg transition-colors"
                        >
                            <span className="material-symbols-outlined">notifications</span>
                            {unreadCount > 0 && (
                                <span className="absolute top-1.5 right-1.5 w-2.5 h-2.5 bg-red-500 rounded-full border-2 border-white dark:border-slate-900"></span>
                            )}
                        </Link>

                        {/* Messages */}
                        <Link
                            href="/chat"
                            className="p-2 text-slate-500 hover:bg-slate-100 dark:hover:bg-slate-800 rounded-lg transition-colors"
                        >
                            <span className="material-symbols-outlined">mail</span>
                        </Link>

                        <div className="h-8 w-px bg-slate-200 dark:bg-slate-700 mx-1"></div>

                        {/* Ask Question Button */}
                        <Link
                            href="/questions/ask"
                            className="bg-[var(--primary)] text-white px-4 py-2 rounded-lg text-sm font-semibold flex items-center gap-2 hover:bg-[var(--primary)]/90 transition-all shadow-sm"
                        >
                            <span className="material-symbols-outlined text-sm">add</span>
                            Ask Question
                        </Link>

                        {/* User Menu */}
                        <div className="relative" ref={menuRef}>
                            <button
                                onClick={() => setShowUserMenu(!showUserMenu)}
                                className="size-10 rounded-full bg-gradient-to-br from-blue-500 to-indigo-600 flex items-center justify-center text-white font-bold border-2 border-[var(--primary)]/20 overflow-hidden"
                            >
                                {user?.profilePicture ? (
                                    <img src={user.profilePicture} alt={user.displayName || user.username} className="size-full object-cover" />
                                ) : (
                                    user?.username?.charAt(0).toUpperCase() || 'U'
                                )}
                            </button>

                            {showUserMenu && (
                                <div className="absolute right-0 mt-2 w-56 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl shadow-xl overflow-hidden z-50">
                                    <div className="p-3 border-b border-slate-200 dark:border-slate-800">
                                        <p className="font-bold text-slate-900 dark:text-white">{user?.displayName || user?.username}</p>
                                        <p className="text-sm text-slate-500">@{user?.username}</p>
                                    </div>
                                    <div className="py-1">
                                        <Link href="/profile" className="flex items-center gap-2 px-3 py-2 text-slate-600 dark:text-slate-400 hover:bg-slate-50 dark:hover:bg-slate-800 transition text-sm">
                                            <span className="material-symbols-outlined text-lg">person</span> Profile
                                        </Link>
                                        <Link href="/settings" className="flex items-center gap-2 px-3 py-2 text-slate-600 dark:text-slate-400 hover:bg-slate-50 dark:hover:bg-slate-800 transition text-sm">
                                            <span className="material-symbols-outlined text-lg">settings</span> Settings
                                        </Link>
                                        <button
                                            onClick={() => { logout(); setShowUserMenu(false); }}
                                            className="w-full flex items-center gap-2 px-3 py-2 text-red-500 hover:bg-red-50 dark:hover:bg-red-500/10 transition text-sm"
                                        >
                                            <span className="material-symbols-outlined text-lg">logout</span> Logout
                                        </button>
                                    </div>
                                </div>
                            )}
                        </div>
                    </>
                ) : (
                    <div className="flex gap-2">
                        <Link href="/login" className="px-4 py-2 text-slate-600 dark:text-slate-400 hover:text-[var(--primary)] transition font-semibold text-sm">
                            Login
                        </Link>
                        <Link href="/register" className="px-5 py-2 bg-[var(--primary)] text-white rounded-lg hover:bg-[var(--primary)]/90 transition font-bold text-sm shadow-sm">
                            Sign Up
                        </Link>
                    </div>
                )}
            </div>
        </header>
    );
}
