'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { useAuth } from '@/lib/contexts/AuthContext';
import { useChatContext } from '@/lib/contexts/ChatContext';
import { authorInitial } from '@/lib/utils';

interface NavItem {
    name: string;
    href: string;
    icon: string;
    requireAuth?: boolean;
}

const navItems: NavItem[] = [
    { name: 'Home', href: '/', icon: 'home' },
    { name: 'Questions', href: '/questions', icon: 'quiz' },
    { name: 'Tags', href: '/tags', icon: 'sell' },
    { name: 'Users', href: '/users', icon: 'group' },
    { name: 'Newsfeed', href: '/newsfeed', icon: 'newspaper', requireAuth: true },
    { name: 'Groups', href: '/groups', icon: 'groups', requireAuth: true },
    { name: 'Friends', href: '/friends', icon: 'person_add', requireAuth: true },
    { name: 'Chat', href: '/chat', icon: 'chat_bubble', requireAuth: true },
    { name: 'Saved', href: '/saved', icon: 'bookmark', requireAuth: true },
    { name: 'Repositories', href: '/repositories', icon: 'folder_code', requireAuth: true },
];

export default function ModernSidebar() {
    const pathname = usePathname();
    const { user, isAuthenticated } = useAuth();
    const { totalUnreadChats } = useChatContext();

    const isActive = (href: string) => {
        if (href === '/') return pathname === '/';
        return pathname.startsWith(href);
    };

    return (
        <aside className="w-64 bg-white dark:bg-slate-900 border-r border-slate-200 dark:border-slate-800 flex flex-col sticky top-0 h-screen shrink-0 hidden lg:flex">
            {/* Brand */}
            <div className="p-6 flex items-center gap-3">
                <Link href="/" className="flex items-center gap-3">
                    <div className="size-10 bg-[var(--primary)] rounded-xl flex items-center justify-center text-white">
                        <span className="material-symbols-outlined">hub</span>
                    </div>
                    <div>
                        <h1 className="text-slate-900 dark:text-white text-base font-bold leading-none">SocialTechsy</h1>
                        <p className="text-slate-500 dark:text-slate-400 text-xs mt-1">Community Dashboard</p>
                    </div>
                </Link>
            </div>

            {/* Navigation */}
            <nav className="flex-1 px-4 py-2 space-y-1 overflow-y-auto">
                {navItems.map((item) => {
                    if (item.requireAuth && !isAuthenticated) return null;
                    const active = isActive(item.href);

                    return (
                        <Link
                            key={item.href}
                            href={item.href}
                            className={`flex items-center justify-between px-3 py-2.5 rounded-lg transition-colors ${active
                                ? 'bg-[var(--primary)]/10 text-[var(--primary)] font-semibold'
                                : 'text-slate-600 dark:text-slate-400 hover:bg-slate-50 dark:hover:bg-slate-800'
                                }`}
                        >
                            <div className="flex items-center gap-3">
                                <span className="material-symbols-outlined text-[22px]">{item.icon}</span>
                                <span className="text-sm">{item.name}</span>
                            </div>
                            {item.href === '/chat' && totalUnreadChats > 0 && (
                                <span className="bg-blue-500 text-white text-[10px] font-bold px-2 py-0.5 rounded-full">
                                    {totalUnreadChats > 9 ? '9+' : totalUnreadChats}
                                </span>
                            )}
                        </Link>
                    );
                })}
            </nav>

            {/* User Profile at Bottom */}
            {isAuthenticated && user && (
            <div className="p-4 border-t border-slate-200 dark:border-slate-800">
                    <Link
                        href="/profile"
                        className="flex items-center gap-3 p-2 rounded-lg hover:bg-slate-50 dark:hover:bg-slate-800 transition-colors"
                    >
                        {user.profilePicture ? (
                            <img src={user.profilePicture} alt={user.displayName || user.username} className="size-10 rounded-full object-cover border-2 border-slate-100 dark:border-slate-700" />
                        ) : (
                            <div className="size-10 rounded-full bg-gradient-to-br from-blue-500 to-indigo-600 flex items-center justify-center text-white font-bold border-2 border-slate-100 dark:border-slate-700">
                                {authorInitial(user.displayName || user.username)}
                            </div>
                        )}
                        <div className="flex-1 min-w-0">
                            <p className="text-sm font-semibold text-slate-900 dark:text-white truncate">
                                {user.displayName || user.username}
                            </p>
                            <p className="text-xs text-slate-500 truncate">@{user.username}</p>
                        </div>
                    </Link>
                    <Link href="/settings" className="text-slate-400 hover:text-[var(--primary)] transition-colors mt-2 inline-flex items-center gap-1 text-xs">
                        <span className="material-symbols-outlined text-sm">settings</span>
                        Settings
                    </Link>
                </div>

            )}
        </aside>
    );
}
