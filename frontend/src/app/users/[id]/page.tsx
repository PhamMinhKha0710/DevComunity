'use client';

import { useEffect, useState } from 'react';
import { useParams } from 'next/navigation';
import Link from 'next/link';
import type { User, Question, Answer } from '@/types';
import apiClient from '@/lib/api/client';
import AppLayout from '@/components/AppLayout';

interface UserProfile extends User {
    bio?: string;
    location?: string;
    website?: string;
    memberSince?: string;
    postCount?: number;
    answerCount?: number;
    badges?: { name: string; type: string }[];
}

export default function UserProfilePage() {
    const params = useParams();
    const userId = params.id as string;
    const [user, setUser] = useState<UserProfile | null>(null);
    const [questions, setQuestions] = useState<Question[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [activeTab, setActiveTab] = useState<'questions' | 'answers' | 'about'>('questions');

    useEffect(() => {
        fetchUserProfile();
    }, [userId]);

    const fetchUserProfile = async () => {
        try {
            const [userRes, questionsRes] = await Promise.all([
                apiClient.get<UserProfile>(`/users/${userId}`),
                apiClient.get<{ items: Question[] }>(`/users/${userId}/questions`)
            ]);
            setUser(userRes.data);
            setQuestions(questionsRes.data.items || []);
        } catch (error) {
            console.error('Failed to fetch user profile:', error);
        } finally {
            setIsLoading(false);
        }
    };

    if (isLoading) {
        return (
            <AppLayout>
                <div className="flex items-center justify-center py-20">
                    <div className="w-10 h-10 border-3 border-[rgba(19,127,236,0.2)] border-t-[#137fec] rounded-full animate-spin" />
                </div>
            </AppLayout>
        );
    }

    if (!user) {
        return (
            <AppLayout>
                <div className="bg-white dark:bg-[var(--bg-secondary)] border border-[#e2e8f0] dark:border-[var(--border-color)] rounded-2xl p-12 text-center">
                    <span className="material-symbols-outlined text-5xl text-[#94a3b8] mb-4 block">person_remove</span>
                    <h3 className="text-lg font-bold text-[#0f172a] dark:text-white mb-2">User not found</h3>
                    <Link href="/users" className="inline-flex items-center gap-2 bg-[#137fec] text-white px-5 py-2.5 rounded-xl font-bold text-sm hover:bg-[#1170d4] transition-all mt-4">
                        Browse Users
                    </Link>
                </div>
            </AppLayout>
        );
    }

    const tabs = [
        { id: 'questions' as const, label: `Questions (${questions.length})`, icon: 'help' },
        { id: 'answers' as const, label: `Answers (${user.answerCount || 0})`, icon: 'chat' },
        { id: 'about' as const, label: 'About', icon: 'person' },
    ];

    return (
        <AppLayout>
            {/* Profile Header */}
            <div className="bg-white dark:bg-[var(--bg-secondary)] border border-[#e2e8f0] dark:border-[var(--border-color)] rounded-2xl overflow-hidden mb-6">
                <div className="h-32 bg-gradient-to-r from-[#6a11cb] to-[#2575fc]" />
                <div className="px-6 pb-6 -mt-12">
                    <div className="flex flex-col sm:flex-row items-start sm:items-end gap-4">
                        <img
                            src={user.profilePicture || '/images/default-avatar.png'}
                            className="w-24 h-24 rounded-2xl border-4 border-white dark:border-[var(--bg-secondary)] object-cover"
                            alt={user.displayName || user.username}
                        />
                        <div className="flex-1">
                            <h1 className="text-2xl font-black text-[#0f172a] dark:text-white">{user.displayName || user.username}</h1>
                            <p className="text-[#64748b] text-sm">@{user.username}</p>
                            {user.location && (
                                <p className="text-[#64748b] text-sm flex items-center gap-1 mt-1">
                                    <span className="material-symbols-outlined text-sm">location_on</span>{user.location}
                                </p>
                            )}
                        </div>
                        <div className="flex gap-3">
                            {[
                                { value: user.reputationPoints || 0, label: 'Reputation' },
                                { value: questions.length, label: 'Questions' },
                                { value: user.answerCount || 0, label: 'Answers' },
                            ].map(stat => (
                                <div key={stat.label} className="text-center px-4 py-2 bg-[#f1f5f9] dark:bg-[var(--bg-tertiary)] rounded-xl">
                                    <div className="text-lg font-bold text-[#0f172a] dark:text-white">{stat.value}</div>
                                    <div className="text-[10px] uppercase tracking-wider text-[#64748b] font-bold">{stat.label}</div>
                                </div>
                            ))}
                        </div>
                    </div>
                </div>
            </div>

            <div className="grid lg:grid-cols-3 gap-6">
                {/* Main Content */}
                <div className="lg:col-span-2 space-y-4">
                    {/* Tabs */}
                    <div className="flex gap-1 bg-[#f1f5f9] dark:bg-[var(--bg-tertiary)] rounded-xl p-1 w-fit">
                        {tabs.map(tab => (
                            <button
                                key={tab.id}
                                onClick={() => setActiveTab(tab.id)}
                                className={`flex items-center gap-1.5 px-4 py-2 rounded-lg text-sm font-bold transition-colors ${activeTab === tab.id ? 'bg-white dark:bg-[var(--bg-secondary)] text-[#0f172a] dark:text-white shadow-sm' : 'text-[#64748b] hover:text-[#0f172a] dark:hover:text-white'}`}
                            >
                                <span className="material-symbols-outlined text-base">{tab.icon}</span>
                                {tab.label}
                            </button>
                        ))}
                    </div>

                    {/* Questions Tab */}
                    {activeTab === 'questions' && (
                        <div className="bg-white dark:bg-[var(--bg-secondary)] border border-[#e2e8f0] dark:border-[var(--border-color)] rounded-2xl overflow-hidden">
                            {questions.length > 0 ? (
                                questions.map((q, index) => (
                                    <Link
                                        key={q.questionId}
                                        href={`/questions/${q.questionId}`}
                                        className={`block p-4 hover:bg-[#f8fafc] dark:hover:bg-[var(--bg-tertiary)] transition ${index > 0 ? 'border-t border-[#e2e8f0] dark:border-[var(--border-color)]' : ''}`}
                                    >
                                        <div className="flex justify-between items-start gap-3">
                                            <h3 className="text-sm font-bold text-[#0f172a] dark:text-white">{q.title}</h3>
                                            <span className={`px-2.5 py-0.5 rounded-full text-xs font-bold shrink-0 ${q.hasAcceptedAnswer ? 'bg-green-50 dark:bg-green-500/10 text-green-600 dark:text-green-400' : 'bg-[#f1f5f9] dark:bg-[var(--bg-tertiary)] text-[#64748b]'}`}>
                                                {q.answerCount} answers
                                            </span>
                                        </div>
                                        <div className="flex gap-4 text-[#94a3b8] text-xs mt-2">
                                            <span className="flex items-center gap-1"><span className="material-symbols-outlined text-sm">thumb_up</span>{q.score}</span>
                                            <span className="flex items-center gap-1"><span className="material-symbols-outlined text-sm">visibility</span>{q.viewCount}</span>
                                            <span>{new Date(q.createdDate).toLocaleDateString()}</span>
                                        </div>
                                    </Link>
                                ))
                            ) : (
                                <div className="p-12 text-center">
                                    <span className="material-symbols-outlined text-5xl text-[#94a3b8] mb-4 block">help</span>
                                    <p className="text-[#64748b]">No questions yet</p>
                                </div>
                            )}
                        </div>
                    )}

                    {/* Answers Tab */}
                    {activeTab === 'answers' && (
                        <div className="bg-white dark:bg-[var(--bg-secondary)] border border-[#e2e8f0] dark:border-[var(--border-color)] rounded-2xl p-12 text-center">
                            <span className="material-symbols-outlined text-5xl text-[#94a3b8] mb-4 block">chat</span>
                            <p className="text-[#64748b]">Answers will be displayed here</p>
                        </div>
                    )}

                    {/* About Tab */}
                    {activeTab === 'about' && (
                        <div className="bg-white dark:bg-[var(--bg-secondary)] border border-[#e2e8f0] dark:border-[var(--border-color)] rounded-2xl p-6">
                            <h3 className="font-bold text-[#0f172a] dark:text-white mb-3">About</h3>
                            <p className="text-[#475569] dark:text-[var(--text-secondary)]">{user.bio || 'This user has not added a bio yet.'}</p>
                            <div className="border-t border-[#e2e8f0] dark:border-[var(--border-color)] mt-4 pt-4 grid sm:grid-cols-2 gap-3">
                                <div className="flex items-center gap-2 text-[#64748b] text-sm">
                                    <span className="material-symbols-outlined text-base">calendar_today</span>
                                    Joined {user.memberSince ? new Date(user.memberSince).toLocaleDateString() : 'Recently'}
                                </div>
                                {user.website && (
                                    <div className="flex items-center gap-2 text-sm">
                                        <span className="material-symbols-outlined text-base text-[#94a3b8]">link</span>
                                        <a href={user.website} target="_blank" rel="noopener noreferrer" className="text-[#137fec] hover:underline truncate">{user.website}</a>
                                    </div>
                                )}
                            </div>
                        </div>
                    )}
                </div>

                {/* Sidebar */}
                <div className="space-y-4">
                    {/* Badges */}
                    <div className="bg-white dark:bg-[var(--bg-secondary)] border border-[#e2e8f0] dark:border-[var(--border-color)] rounded-2xl p-5">
                        <h3 className="font-bold text-[#0f172a] dark:text-white mb-3 flex items-center gap-2">
                            <span className="material-symbols-outlined text-amber-500">military_tech</span>Badges
                        </h3>
                        {user.badges && user.badges.length > 0 ? (
                            <div className="flex flex-wrap gap-2">
                                {user.badges.map((badge, idx) => (
                                    <span key={idx} className={`px-3 py-1 rounded-full text-xs font-bold ${badge.type === 'gold' ? 'bg-amber-50 dark:bg-amber-500/10 text-amber-600' : badge.type === 'silver' ? 'bg-gray-100 dark:bg-gray-500/10 text-gray-600' : 'bg-blue-50 dark:bg-blue-500/10 text-blue-600'}`}>
                                        {badge.name}
                                    </span>
                                ))}
                            </div>
                        ) : (
                            <p className="text-[#94a3b8] text-sm">No badges yet</p>
                        )}
                    </div>

                    {/* Stats */}
                    <div className="bg-white dark:bg-[var(--bg-secondary)] border border-[#e2e8f0] dark:border-[var(--border-color)] rounded-2xl p-5">
                        <h3 className="font-bold text-[#0f172a] dark:text-white mb-3 flex items-center gap-2">
                            <span className="material-symbols-outlined text-green-500">trending_up</span>Stats
                        </h3>
                        <div className="grid grid-cols-2 gap-3">
                            {[
                                { value: user.reputationPoints || 0, label: 'Reputation', color: 'text-[#137fec]' },
                                { value: questions.length, label: 'Questions', color: 'text-green-500' },
                                { value: user.answerCount || 0, label: 'Answers', color: 'text-blue-400' },
                                { value: user.badges?.length || 0, label: 'Badges', color: 'text-amber-500' },
                            ].map(stat => (
                                <div key={stat.label} className="text-center p-3 bg-[#f8fafc] dark:bg-[var(--bg-tertiary)] rounded-xl">
                                    <div className={`text-lg font-bold ${stat.color}`}>{stat.value}</div>
                                    <div className="text-[#94a3b8] text-xs">{stat.label}</div>
                                </div>
                            ))}
                        </div>
                    </div>
                </div>
            </div>
        </AppLayout>
    );
}
