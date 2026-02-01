'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { useState, useEffect } from 'react';
import { useAuth } from '@/lib/contexts/AuthContext';
import { useNotifications } from '@/lib/contexts/NotificationContext';

export default function ModernNavbar() {
    const pathname = usePathname();
    const { user, isAuthenticated, logout } = useAuth();
    const { unreadCount } = useNotifications();
    const [searchQuery, setSearchQuery] = useState('');
    const [isDark, setIsDark] = useState(true);
    const [showUserMenu, setShowUserMenu] = useState(false);

    useEffect(() => {
        // Default to dark mode
        document.documentElement.classList.add('dark');
    }, []);

    const toggleTheme = () => {
        setIsDark(!isDark);
        document.documentElement.classList.toggle('dark');
    };

    return (
        <nav className="fixed top-0 left-0 right-0 z-50 h-16 bg-[var(--bg-secondary)] border-b border-[var(--border-color)] backdrop-blur-lg">
            <div className="flex items-center justify-between h-full px-4 max-w-[1920px] mx-auto">
                {/* Logo */}
                <Link href="/" className="flex items-center gap-2 text-xl font-bold">
                    <span className="text-2xl">🚀</span>
                    <span className="text-[var(--primary)] hidden sm:inline">Dev</span>
                    <span className="text-[var(--text-primary)] hidden sm:inline">Community</span>
                </Link>

                {/* Search Bar */}
                <div className="flex-1 max-w-xl mx-4 hidden md:block">
                    <div className="relative">
                        <i className="bi bi-search absolute left-3 top-1/2 -translate-y-1/2 text-[var(--text-muted)]"></i>
                        <input
                            type="text"
                            value={searchQuery}
                            onChange={(e) => setSearchQuery(e.target.value)}
                            placeholder="Search questions, users, or tags..."
                            className="w-full pl-10 pr-4 py-2.5 bg-[var(--bg-tertiary)] border border-[var(--border-color)] rounded-xl text-[var(--text-primary)] placeholder-[var(--text-muted)] focus:border-[var(--primary)] focus:ring-2 focus:ring-[var(--primary)]/20 transition-all"
                        />
                    </div>
                </div>

                {/* Right Actions */}
                <div className="flex items-center gap-3">
                    {/* Theme Toggle */}
                    <button
                        onClick={toggleTheme}
                        className="p-2 rounded-lg hover:bg-[var(--bg-tertiary)] text-[var(--text-muted)] hover:text-[var(--text-primary)] transition"
                    >
                        <i className={`bi ${isDark ? 'bi-sun' : 'bi-moon'} text-lg`}></i>
                    </button>

                    {isAuthenticated ? (
                        <>
                            {/* Create Button */}
                            <Link
                                href="/questions/ask"
                                className="flex items-center gap-2 px-4 py-2 bg-[var(--primary)] text-white rounded-lg hover:bg-[var(--primary-dark)] transition font-medium"
                            >
                                <i className="bi bi-plus-circle"></i>
                                <span className="hidden sm:inline">Create</span>
                            </Link>

                            {/* Notifications */}
                            <Link href="/notifications" className="relative p-2 rounded-lg hover:bg-[var(--bg-tertiary)] transition">
                                <i className="bi bi-bell text-xl text-[var(--text-muted)]"></i>
                                {unreadCount > 0 && (
                                    <span className="absolute -top-0.5 -right-0.5 min-w-[18px] h-[18px] flex items-center justify-center px-1 text-xs font-bold bg-red-500 text-white rounded-full">
                                        {unreadCount > 99 ? '99+' : unreadCount}
                                    </span>
                                )}
                            </Link>

                            {/* User Menu */}
                            <div className="relative">
                                <button
                                    onClick={() => setShowUserMenu(!showUserMenu)}
                                    className="flex items-center gap-2 p-1 rounded-lg hover:bg-[var(--bg-tertiary)] transition"
                                >
                                    <div className="w-9 h-9 rounded-full bg-gradient-to-br from-purple-500 to-pink-500 flex items-center justify-center text-white font-bold">
                                        {user?.username?.charAt(0).toUpperCase() || 'U'}
                                    </div>
                                    <i className="bi bi-chevron-down text-[var(--text-muted)] text-sm"></i>
                                </button>

                                {showUserMenu && (
                                    <div className="absolute right-0 mt-2 w-56 bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-xl shadow-xl overflow-hidden">
                                        <div className="p-3 border-b border-[var(--border-color)]">
                                            <p className="font-medium text-[var(--text-primary)]">{user?.displayName || user?.username}</p>
                                            <p className="text-sm text-[var(--text-muted)]">@{user?.username}</p>
                                        </div>
                                        <div className="py-1">
                                            <Link href="/profile" className="flex items-center gap-2 px-3 py-2 text-[var(--text-secondary)] hover:bg-[var(--bg-tertiary)] transition">
                                                <i className="bi bi-person"></i> Profile
                                            </Link>
                                            <Link href="/settings" className="flex items-center gap-2 px-3 py-2 text-[var(--text-secondary)] hover:bg-[var(--bg-tertiary)] transition">
                                                <i className="bi bi-gear"></i> Settings
                                            </Link>
                                            <button
                                                onClick={() => { logout(); setShowUserMenu(false); }}
                                                className="w-full flex items-center gap-2 px-3 py-2 text-red-400 hover:bg-[var(--bg-tertiary)] transition"
                                            >
                                                <i className="bi bi-box-arrow-right"></i> Logout
                                            </button>
                                        </div>
                                    </div>
                                )}
                            </div>
                        </>
                    ) : (
                        <div className="flex gap-2">
                            <Link href="/login" className="px-4 py-2 text-[var(--text-secondary)] hover:text-[var(--text-primary)] transition">
                                Login
                            </Link>
                            <Link href="/register" className="px-4 py-2 bg-[var(--primary)] text-white rounded-lg hover:bg-[var(--primary-dark)] transition">
                                Sign Up
                            </Link>
                        </div>
                    )}
                </div>
            </div>
        </nav>
    );
}
