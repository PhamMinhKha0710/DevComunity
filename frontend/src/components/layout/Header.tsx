'use client';

import Link from 'next/link';
import { useAuth } from '@/lib/contexts/AuthContext';
import { useNotifications } from '@/lib/contexts/NotificationContext';
import { useState } from 'react';

export default function Header() {
    const { user, isLoading, logout } = useAuth();
    const { unreadCount } = useNotifications();
    const [searchQuery, setSearchQuery] = useState('');

    return (
        <header className="flex-shrink-0">
            <div className="hero-nav-bg">
                <div className="hero-nav-overlay"></div>
                <nav className="navbar navbar-expand-lg">
                    <div className="container-fluid">
                        {/* Brand */}
                        <Link href="/" className="navbar-brand d-flex align-items-center text-white" data-aos="fade-right" data-aos-duration="600">
                            <div className="brand-logo-container me-2">
                                <i className="bi bi-code-square fs-4 brand-icon"></i>
                            </div>
                            <span className="fw-semibold">DevCommunity</span>
                        </Link>

                        {/* Mobile Toggle */}
                        <button className="navbar-toggler" type="button" data-bs-toggle="collapse" data-bs-target="#navbarContent">
                            <span className="navbar-toggler-icon"></span>
                        </button>

                        <div className="collapse navbar-collapse" id="navbarContent">
                            {/* Search */}
                            <div className="search-container mx-lg-4 flex-grow-1" data-aos="fade-down" data-aos-delay="100">
                                <form action="/questions" method="get">
                                    <div className="input-group nav-search-group">
                                        <span className="input-group-text bg-transparent border-0">
                                            <i className="bi bi-search text-white"></i>
                                        </span>
                                        <input
                                            type="text"
                                            name="search"
                                            className="form-control py-2 nav-search-input"
                                            placeholder="Search questions, tags, or users..."
                                            value={searchQuery}
                                            onChange={(e) => setSearchQuery(e.target.value)}
                                        />
                                    </div>
                                </form>
                            </div>

                            {/* Nav Items */}
                            <ul className="navbar-nav ms-auto d-flex align-items-center">
                                {/* Mobile Links */}
                                <li className="nav-item d-lg-none border-bottom pb-2 mb-2 w-100">
                                    <div className="nav-mobile-links">
                                        <Link href="/" className="nav-link-mobile mb-2 d-block text-white">
                                            <i className="bi bi-house-door me-2"></i> Home
                                        </Link>
                                        <Link href="/questions" className="nav-link-mobile mb-2 d-block text-white">
                                            <i className="bi bi-question-circle me-2"></i> Questions
                                        </Link>
                                        <Link href="/tags" className="nav-link-mobile mb-2 d-block text-white">
                                            <i className="bi bi-tags me-2"></i> Tags
                                        </Link>
                                        <Link href="/users" className="nav-link-mobile mb-2 d-block text-white">
                                            <i className="bi bi-people me-2"></i> Users
                                        </Link>
                                    </div>
                                </li>

                                {/* Theme Switch */}
                                <li className="nav-item d-none d-lg-block me-2">
                                    <div className="theme-switch">
                                        <label className="btn btn-icon theme-switch-label">
                                            <i className="bi bi-sun-fill"></i>
                                        </label>
                                    </div>
                                </li>

                                {isLoading ? (
                                    <li className="nav-item">
                                        <span className="text-white-50">Loading...</span>
                                    </li>
                                ) : user ? (
                                    <>
                                        {/* Saved Items */}
                                        <li className="nav-item mx-1" data-aos="fade-left" data-aos-delay="200">
                                            <Link href="/saved" className="nav-link d-flex align-items-center p-2 nav-icon-btn" title="Saved items">
                                                <i className="bi bi-bookmark fs-5"></i>
                                            </Link>
                                        </li>

                                        {/* Notifications with badge */}
                                        <li className="nav-item mx-1 position-relative" data-aos="fade-left" data-aos-delay="250">
                                            <Link href="/notifications" className="nav-link d-flex align-items-center p-2 nav-icon-btn" title="Notifications">
                                                <i className="bi bi-bell fs-5"></i>
                                                {unreadCount > 0 && (
                                                    <span className="position-absolute top-0 start-100 translate-middle badge rounded-pill bg-danger" style={{ fontSize: '0.65rem' }}>
                                                        {unreadCount > 99 ? '99+' : unreadCount}
                                                    </span>
                                                )}
                                            </Link>
                                        </li>

                                        {/* Chat */}
                                        <li className="nav-item mx-1" data-aos="fade-left" data-aos-delay="300">
                                            <Link href="/chat" className="nav-link d-flex align-items-center p-2 nav-icon-btn" title="Messages">
                                                <i className="bi bi-chat fs-5"></i>
                                            </Link>
                                        </li>

                                        {/* User Dropdown */}
                                        <li className="nav-item dropdown mx-1" data-aos="fade-left" data-aos-delay="350">
                                            <a className="nav-link dropdown-toggle d-flex align-items-center p-2" href="#" role="button" data-bs-toggle="dropdown">
                                                <img
                                                    src={user.profilePicture || '/images/default-avatar.png'}
                                                    className="rounded-circle"
                                                    width="32"
                                                    height="32"
                                                    alt="Profile"
                                                />
                                            </a>
                                            <ul className="dropdown-menu dropdown-menu-end">
                                                <li className="px-3 py-2 border-bottom">
                                                    <div className="fw-bold">{user.displayName || user.username}</div>
                                                    <small className="text-muted">@{user.username}</small>
                                                </li>
                                                <li><Link href="/profile" className="dropdown-item"><i className="bi bi-person me-2"></i>Profile</Link></li>
                                                <li><Link href="/settings" className="dropdown-item"><i className="bi bi-gear me-2"></i>Settings</Link></li>
                                                <li><hr className="dropdown-divider" /></li>
                                                <li>
                                                    <button onClick={logout} className="dropdown-item text-danger">
                                                        <i className="bi bi-box-arrow-right me-2"></i>Sign out
                                                    </button>
                                                </li>
                                            </ul>
                                        </li>
                                    </>
                                ) : (
                                    <>
                                        <li className="nav-item mx-1" data-aos="fade-left" data-aos-delay="200">
                                            <Link href="/login" className="btn btn-sign-in rounded-pill px-3">
                                                Log in
                                            </Link>
                                        </li>
                                        <li className="nav-item mx-1" data-aos="fade-left" data-aos-delay="250">
                                            <Link href="/register" className="btn btn-sign-up rounded-pill px-3">
                                                Sign up
                                            </Link>
                                        </li>
                                    </>
                                )}
                            </ul>
                        </div>
                    </div>
                </nav>

                {/* Page Title Area */}
                <div className="page-title-container">
                    <div className="container">
                        {/* Title injected via page */}
                    </div>
                </div>
            </div>
        </header>
    );
}
