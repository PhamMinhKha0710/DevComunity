'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { useAuth } from '@/lib/contexts/AuthContext';
import { useNotifications } from '@/lib/contexts/NotificationContext';

interface NavItem {
    name: string;
    href: string;
    icon: string;
    badge?: number;
    requireAuth?: boolean;
}

const mainNavItems: NavItem[] = [
    { name: 'Home', href: '/', icon: 'bi-house' },
    { name: 'Questions', href: '/questions', icon: 'bi-question-circle' },
    { name: 'Tags', href: '/tags', icon: 'bi-tags' },
    { name: 'Users', href: '/users', icon: 'bi-people' },
];

const socialNavItems: NavItem[] = [
    { name: 'Newsfeed', href: '/newsfeed', icon: 'bi-newspaper', requireAuth: true },
    { name: 'Groups', href: '/groups', icon: 'bi-people-fill', requireAuth: true },
    { name: 'Friends', href: '/friends', icon: 'bi-person-hearts', requireAuth: true },
];

const personalNavItems: NavItem[] = [
    { name: 'Notifications', href: '/notifications', icon: 'bi-bell', requireAuth: true },
    { name: 'Saved Items', href: '/saved', icon: 'bi-bookmark', requireAuth: true },
    { name: 'Settings', href: '/settings', icon: 'bi-gear', requireAuth: true },
];

export default function Sidebar() {
    const pathname = usePathname();
    const { isAuthenticated } = useAuth();
    const { unreadCount } = useNotifications();

    const isActive = (href: string) => {
        if (href === '/') return pathname === '/';
        return pathname.startsWith(href);
    };

    const renderNavItem = (item: NavItem) => {
        if (item.requireAuth && !isAuthenticated) return null;

        const active = isActive(item.href);
        const badge = item.href === '/notifications' ? unreadCount : item.badge;

        return (
            <Link
                key={item.href}
                href={item.href}
                className={`flex items-center gap-3 px-4 py-2.5 rounded-lg transition-all duration-200 group
                    ${active
                        ? 'bg-orange-500 text-white shadow-md'
                        : 'text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-slate-800 hover:text-orange-500'
                    }`}
            >
                <i className={`bi ${item.icon} text-lg ${active ? 'text-white' : 'group-hover:text-orange-500'}`}></i>
                <span className="font-medium">{item.name}</span>
                {badge && badge > 0 && (
                    <span className={`ml-auto px-2 py-0.5 text-xs font-bold rounded-full
                        ${active ? 'bg-white text-orange-500' : 'bg-red-500 text-white'}`}>
                        {badge > 99 ? '99+' : badge}
                    </span>
                )}
            </Link>
        );
    };

    return (
        <aside className="hidden lg:flex flex-col w-64 h-[calc(100vh-64px)] sticky top-16 bg-white dark:bg-slate-900 border-r border-gray-200 dark:border-slate-700 overflow-y-auto">
            <div className="flex flex-col p-4 gap-6">
                {/* Main Navigation */}
                <div>
                    <h3 className="px-4 mb-2 text-xs font-semibold text-gray-400 uppercase tracking-wider">
                        Main
                    </h3>
                    <nav className="space-y-1">
                        {mainNavItems.map(renderNavItem)}
                    </nav>
                </div>

                {/* Social - Only for authenticated users */}
                {isAuthenticated && (
                    <div>
                        <h3 className="px-4 mb-2 text-xs font-semibold text-gray-400 uppercase tracking-wider">
                            Social
                        </h3>
                        <nav className="space-y-1">
                            {socialNavItems.map(renderNavItem)}
                        </nav>
                    </div>
                )}

                {/* Personal - Only for authenticated users */}
                {isAuthenticated && (
                    <div>
                        <h3 className="px-4 mb-2 text-xs font-semibold text-gray-400 uppercase tracking-wider">
                            Personal
                        </h3>
                        <nav className="space-y-1">
                            {personalNavItems.map(renderNavItem)}
                        </nav>
                    </div>
                )}

                {/* Quick Stats for authenticated users */}
                {isAuthenticated && (
                    <div className="mt-auto p-4 bg-gradient-to-br from-orange-50 to-amber-50 dark:from-slate-800 dark:to-slate-700 rounded-xl">
                        <h4 className="text-sm font-semibold text-gray-700 dark:text-gray-200 mb-3">
                            Quick Actions
                        </h4>
                        <Link
                            href="/questions/ask"
                            className="flex items-center justify-center gap-2 w-full py-2.5 bg-orange-500 text-white rounded-lg hover:bg-orange-600 transition font-medium shadow-sm"
                        >
                            <i className="bi bi-plus-circle"></i>
                            Ask Question
                        </Link>
                    </div>
                )}
            </div>
        </aside>
    );
}
