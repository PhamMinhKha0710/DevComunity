'use client';

import Link from 'next/link';
import { useEffect, useState } from 'react';
import { useAuth } from '@/lib/contexts/AuthContext';

interface TrendingTopic {
    name: string;
    count: number;
}

interface TopContributor {
    userId: number;
    username: string;
    displayName: string;
    reputation: number;
}

export default function RightSidebar() {
    const { isAuthenticated } = useAuth();
    const [trendingTopics] = useState<TrendingTopic[]>([
        { name: 'react', count: 1523 },
        { name: 'javascript', count: 1289 },
        { name: 'typescript', count: 987 },
        { name: 'python', count: 856 },
        { name: 'nextjs', count: 654 },
    ]);

    const [topContributors] = useState<TopContributor[]>([
        { userId: 1, username: 'johndoe', displayName: 'John Doe', reputation: 12500 },
        { userId: 2, username: 'janesmith', displayName: 'Jane Smith', reputation: 9800 },
        { userId: 3, username: 'devmaster', displayName: 'Dev Master', reputation: 7650 },
    ]);

    return (
        <aside className="hidden xl:block w-80 h-[calc(100vh-64px)] sticky top-16 py-6 pr-4 overflow-y-auto">
            {/* Trending Topics */}
            <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-2xl p-4 mb-4">
                <h3 className="flex items-center gap-2 font-semibold text-[var(--text-primary)] mb-4">
                    <i className="bi bi-fire text-orange-500"></i>
                    Trending Topics
                </h3>
                <div className="space-y-2">
                    {trendingTopics.map((topic) => (
                        <Link
                            key={topic.name}
                            href={`/questions?tag=${topic.name}`}
                            className="flex items-center justify-between p-2 rounded-lg hover:bg-[var(--bg-hover)] transition group"
                        >
                            <span className="text-[var(--primary)] font-medium group-hover:underline">
                                #{topic.name}
                            </span>
                            <span className="text-xs text-[var(--text-muted)] bg-[var(--bg-tertiary)] px-2 py-1 rounded-full">
                                {topic.count.toLocaleString('en-US')} posts
                            </span>
                        </Link>
                    ))}
                </div>
            </div>

            {/* Top Contributors */}
            <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-2xl p-4 mb-4">
                <h3 className="flex items-center gap-2 font-semibold text-[var(--text-primary)] mb-4">
                    <i className="bi bi-trophy text-yellow-500"></i>
                    Top Contributors
                </h3>
                <div className="space-y-3">
                    {topContributors.map((user, index) => (
                        <Link
                            key={user.userId}
                            href={`/users/${user.userId}`}
                            className="flex items-center gap-3 p-2 rounded-lg hover:bg-[var(--bg-hover)] transition"
                        >
                            <span className={`w-6 h-6 flex items-center justify-center text-sm font-bold rounded-full ${index === 0 ? 'bg-yellow-500 text-black' :
                                index === 1 ? 'bg-gray-400 text-black' :
                                    'bg-orange-600 text-white'
                                }`}>
                                {index + 1}
                            </span>
                            <div className="w-8 h-8 rounded-full bg-gradient-to-br from-blue-500 to-purple-500 flex items-center justify-center text-white text-sm font-bold">
                                {user.username.charAt(0).toUpperCase()}
                            </div>
                            <div className="flex-1 min-w-0">
                                <p className="text-sm font-medium text-[var(--text-primary)] truncate">{user.displayName}</p>
                                <p className="text-xs text-[var(--text-muted)]">{user.reputation.toLocaleString('en-US')} rep</p>
                            </div>
                        </Link>
                    ))}
                </div>
            </div>

            {/* Quick Actions */}
            {isAuthenticated && (
                <div className="bg-gradient-to-br from-[var(--bg-secondary)] to-[var(--bg-tertiary)] border border-[var(--border-color)] rounded-2xl p-4">
                    <h3 className="flex items-center gap-2 font-semibold text-[var(--text-primary)] mb-4">
                        <i className="bi bi-lightning-fill text-yellow-400"></i>
                        Quick Actions
                    </h3>
                    <div className="space-y-2">
                        <Link
                            href="/questions/ask"
                            className="flex items-center gap-2 p-3 w-full bg-[var(--primary)] text-white rounded-xl hover:bg-[var(--primary-dark)] transition font-medium"
                        >
                            <i className="bi bi-plus-circle"></i>
                            Ask a Question
                        </Link>
                        <Link
                            href="/groups"
                            className="flex items-center gap-2 p-3 w-full bg-[var(--bg-hover)] text-[var(--text-primary)] rounded-xl border border-[var(--border-color)] hover:border-[var(--primary)] transition"
                        >
                            <i className="bi bi-collection"></i>
                            Create a Group
                        </Link>
                        <Link
                            href="/users"
                            className="flex items-center gap-2 p-3 w-full bg-[var(--bg-hover)] text-[var(--text-primary)] rounded-xl border border-[var(--border-color)] hover:border-[var(--primary)] transition"
                        >
                            <i className="bi bi-person-plus"></i>
                            Find Friends
                        </Link>
                    </div>
                </div>
            )}
        </aside>
    );
}
