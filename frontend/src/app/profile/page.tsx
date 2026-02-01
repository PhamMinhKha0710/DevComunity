'use client';

import { useAuth } from '@/lib/contexts/AuthContext';
import { useRouter } from 'next/navigation';
import { useEffect, useState } from 'react';
import Link from 'next/link';
import AppLayout from '@/components/AppLayout';

export default function ProfilePage() {
    const { user, isLoading } = useAuth();
    const router = useRouter();
    const [activeTab, setActiveTab] = useState('questions');

    useEffect(() => {
        if (!isLoading && !user) {
            router.push('/login');
        }
    }, [user, isLoading, router]);

    if (isLoading) {
        return (
            <AppLayout>
                <div className="flex items-center justify-center py-20">
                    <div className="w-10 h-10 border-4 border-[var(--primary)]/30 border-t-[var(--primary)] rounded-full animate-spin"></div>
                </div>
            </AppLayout>
        );
    }

    if (!user) return null;

    const tabs = [
        { key: 'questions', label: 'Questions', icon: 'bi-question-circle' },
        { key: 'answers', label: 'Answers', icon: 'bi-chat-left-text' },
        { key: 'saved', label: 'Saved', icon: 'bi-bookmark' },
    ];

    return (
        <AppLayout>
            <div className="grid lg:grid-cols-3 gap-6">
                {/* Profile Card */}
                <div className="lg:col-span-1 space-y-4">
                    <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-2xl p-6 text-center">
                        {/* Avatar */}
                        <div className="relative inline-block">
                            <div className="w-28 h-28 rounded-full bg-gradient-to-br from-purple-500 via-pink-500 to-orange-500 p-1 mx-auto">
                                <div className="w-full h-full rounded-full bg-[var(--bg-secondary)] flex items-center justify-center text-4xl font-bold text-[var(--text-primary)]">
                                    {user.displayName?.charAt(0).toUpperCase() || user.username?.charAt(0).toUpperCase() || '?'}
                                </div>
                            </div>
                        </div>

                        <h3 className="text-xl font-bold text-[var(--text-primary)] mt-4">{user.displayName || user.username}</h3>
                        <p className="text-[var(--text-muted)]">@{user.username}</p>

                        {/* Stats */}
                        <div className="grid grid-cols-3 gap-3 mt-6">
                            <div className="bg-[var(--primary)]/10 rounded-xl p-3">
                                <span className="block text-xl font-bold text-[var(--primary)]">{user.reputationPoints || 0}</span>
                                <span className="text-xs text-[var(--text-muted)]">Reputation</span>
                            </div>
                            <div className="bg-[var(--bg-tertiary)] rounded-xl p-3">
                                <span className="block text-xl font-bold text-[var(--text-primary)]">0</span>
                                <span className="text-xs text-[var(--text-muted)]">Questions</span>
                            </div>
                            <div className="bg-[var(--bg-tertiary)] rounded-xl p-3">
                                <span className="block text-xl font-bold text-[var(--text-primary)]">0</span>
                                <span className="text-xs text-[var(--text-muted)]">Answers</span>
                            </div>
                        </div>

                        <Link
                            href="/settings"
                            className="flex items-center justify-center gap-2 mt-6 py-3 w-full border border-[var(--border-color)] rounded-xl text-[var(--text-secondary)] hover:border-[var(--primary)] hover:text-[var(--primary)] transition"
                        >
                            <i className="bi bi-gear"></i>
                            Edit Profile
                        </Link>
                    </div>

                    {/* Badges */}
                    <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-2xl p-5">
                        <h4 className="font-semibold text-[var(--text-primary)] mb-4">Badges</h4>
                        <div className="flex flex-wrap gap-2">
                            <span className="flex items-center gap-1 px-3 py-1.5 bg-yellow-500/20 text-yellow-500 rounded-lg text-sm">
                                <i className="bi bi-award"></i> Gold: 0
                            </span>
                            <span className="flex items-center gap-1 px-3 py-1.5 bg-gray-400/20 text-gray-400 rounded-lg text-sm">
                                <i className="bi bi-award"></i> Silver: 0
                            </span>
                            <span className="flex items-center gap-1 px-3 py-1.5 bg-orange-600/20 text-orange-600 rounded-lg text-sm">
                                <i className="bi bi-award"></i> Bronze: 0
                            </span>
                        </div>
                    </div>
                </div>

                {/* Activity */}
                <div className="lg:col-span-2">
                    <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-2xl">
                        {/* Tabs */}
                        <div className="flex gap-1 p-2 border-b border-[var(--border-color)]">
                            {tabs.map((tab) => (
                                <button
                                    key={tab.key}
                                    onClick={() => setActiveTab(tab.key)}
                                    className={`flex items-center gap-2 px-4 py-2 rounded-xl font-medium transition ${activeTab === tab.key
                                            ? 'bg-[var(--primary)] text-white'
                                            : 'text-[var(--text-muted)] hover:bg-[var(--bg-tertiary)]'
                                        }`}
                                >
                                    <i className={`bi ${tab.icon}`}></i>
                                    {tab.label}
                                </button>
                            ))}
                        </div>

                        {/* Content */}
                        <div className="p-8">
                            {activeTab === 'questions' && (
                                <div className="text-center py-8">
                                    <i className="bi bi-chat-square-text text-5xl text-[var(--text-muted)] mb-4"></i>
                                    <h4 className="text-lg font-semibold text-[var(--text-primary)] mb-2">No questions yet</h4>
                                    <p className="text-[var(--text-muted)] mb-4">You haven&apos;t asked any questions yet</p>
                                    <Link
                                        href="/questions/ask"
                                        className="inline-flex items-center gap-2 px-5 py-2.5 bg-[var(--primary)] text-white rounded-xl font-medium hover:bg-[var(--primary-dark)] transition"
                                    >
                                        <i className="bi bi-plus-circle"></i>
                                        Ask your first question
                                    </Link>
                                </div>
                            )}
                            {activeTab === 'answers' && (
                                <div className="text-center py-8">
                                    <i className="bi bi-chat-left-text text-5xl text-[var(--text-muted)] mb-4"></i>
                                    <h4 className="text-lg font-semibold text-[var(--text-primary)] mb-2">No answers yet</h4>
                                    <p className="text-[var(--text-muted)] mb-4">You haven&apos;t answered any questions yet</p>
                                    <Link
                                        href="/questions"
                                        className="inline-flex items-center gap-2 px-5 py-2.5 border border-[var(--border-color)] text-[var(--text-secondary)] rounded-xl hover:border-[var(--primary)] hover:text-[var(--primary)] transition"
                                    >
                                        Browse questions
                                    </Link>
                                </div>
                            )}
                            {activeTab === 'saved' && (
                                <div className="text-center py-8">
                                    <i className="bi bi-bookmark text-5xl text-[var(--text-muted)] mb-4"></i>
                                    <h4 className="text-lg font-semibold text-[var(--text-primary)] mb-2">No saved items</h4>
                                    <p className="text-[var(--text-muted)]">Save questions or answers to revisit later</p>
                                </div>
                            )}
                        </div>
                    </div>
                </div>
            </div>
        </AppLayout>
    );
}
