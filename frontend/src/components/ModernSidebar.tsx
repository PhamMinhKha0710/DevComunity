'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { useState } from 'react';
import { useAuth } from '@/lib/contexts/AuthContext';
import { useNotifications } from '@/lib/contexts/NotificationContext';

interface NavItem {
    name: string;
    href: string;
    icon: string;
    badge?: number;
    requireAuth?: boolean;
}

const mainNav: NavItem[] = [
    { name: 'Home', href: '/', icon: 'bi-house-fill' },
    { name: 'Questions', href: '/questions', icon: 'bi-question-circle-fill' },
    { name: 'Tags', href: '/tags', icon: 'bi-tags-fill' },
    { name: 'Users', href: '/users', icon: 'bi-people-fill' },
];

const socialNav: NavItem[] = [
    { name: 'Newsfeed', href: '/newsfeed', icon: 'bi-newspaper', requireAuth: true },
    { name: 'Groups', href: '/groups', icon: 'bi-collection-fill', requireAuth: true },
    { name: 'Friends', href: '/friends', icon: 'bi-person-hearts', requireAuth: true },
];

const personalNav: NavItem[] = [
    { name: 'Chat', href: '/chat', icon: 'bi-chat-dots-fill', requireAuth: true },
    { name: 'Saved', href: '/saved', icon: 'bi-bookmark-fill', requireAuth: true },
    { name: 'My Tags', href: '/tags/preferences', icon: 'bi-heart-fill', requireAuth: true },
];

export default function ModernSidebar() {
    const pathname = usePathname();
    const { isAuthenticated } = useAuth();
    const { unreadCount } = useNotifications();
    const [collapsed, setCollapsed] = useState(false);

    const isActive = (href: string) => {
        if (href === '/') return pathname === '/';
        return pathname.startsWith(href);
    };

    const renderNavItem = (item: NavItem) => {
        if (item.requireAuth && !isAuthenticated) return null;

        const active = isActive(item.href);
        const badgeCount = item.href === '/notifications' ? unreadCount : item.badge;

        return (
            <Link
                key={item.href}
                href={item.href}
                className={`group flex items-center gap-3 px-3 py-2.5 rounded-xl transition-all duration-200 relative
                    ${active
                        ? 'bg-[var(--primary)] text-white shadow-lg shadow-[var(--primary)]/30'
                        : 'text-[var(--text-secondary)] hover:bg-[var(--bg-hover)] hover:text-[var(--text-primary)]'
                    }
                    ${collapsed ? 'justify-center' : ''}`}
            >
                {active && !collapsed && (
                    <span className="absolute left-0 top-1/2 -translate-y-1/2 w-1 h-6 bg-white rounded-r-full"></span>
                )}
                <i className={`bi ${item.icon} text-lg ${active ? 'text-white' : 'text-[var(--text-muted)] group-hover:text-[var(--primary)]'}`}></i>
                {!collapsed && <span className="font-medium">{item.name}</span>}
                {!collapsed && badgeCount && badgeCount > 0 && (
                    <span className={`ml-auto px-2 py-0.5 text-xs font-bold rounded-full ${active ? 'bg-white text-[var(--primary)]' : 'bg-red-500 text-white'}`}>
                        {badgeCount > 99 ? '99+' : badgeCount}
                    </span>
                )}
            </Link>
        );
    };

    const SectionTitle = ({ children }: { children: string }) => (
        !collapsed ? (
            <h3 className="px-3 mb-2 text-xs font-semibold uppercase tracking-wider text-[var(--text-muted)]">
                {children}
            </h3>
        ) : <div className="border-b border-[var(--border-color)] my-2"></div>
    );

    return (
        <aside className={`hidden lg:flex flex-col ${collapsed ? 'w-20' : 'w-64'} h-[calc(100vh-64px)] sticky top-16 bg-[var(--bg-secondary)] border-r border-[var(--border-color)] transition-all duration-300`}>
            <div className="flex flex-col p-4 gap-6 overflow-y-auto flex-1">
                {/* Collapse Toggle */}
                <button
                    onClick={() => setCollapsed(!collapsed)}
                    className="hidden lg:flex items-center justify-center w-8 h-8 rounded-lg bg-[var(--bg-tertiary)] hover:bg-[var(--bg-hover)] text-[var(--text-muted)] transition absolute -right-4 top-6 border border-[var(--border-color)]"
                >
                    <i className={`bi bi-chevron-${collapsed ? 'right' : 'left'} text-sm`}></i>
                </button>

                {/* Main Navigation */}
                <div>
                    <SectionTitle>Main</SectionTitle>
                    <nav className="space-y-1">
                        {mainNav.map(renderNavItem)}
                    </nav>
                </div>

                {/* Social */}
                {isAuthenticated && (
                    <div>
                        <SectionTitle>Social</SectionTitle>
                        <nav className="space-y-1">
                            {socialNav.map(renderNavItem)}
                        </nav>
                    </div>
                )}

                {/* Personal */}
                {isAuthenticated && (
                    <div>
                        <SectionTitle>Personal</SectionTitle>
                        <nav className="space-y-1">
                            {personalNav.map(renderNavItem)}
                        </nav>
                    </div>
                )}
            </div>


        </aside>
    );
}
