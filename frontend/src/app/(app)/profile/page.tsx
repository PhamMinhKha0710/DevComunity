'use client';

import { useAuth } from '@/lib/contexts/AuthContext';
import { useRouter } from 'next/navigation';
import { useEffect, useState } from 'react';
import Link from 'next/link';
import { useQuery } from '@tanstack/react-query';
import type { Question, Answer } from '@/types';
import { usersApi } from '@/lib/api/users.api';
import AppLayout from '@/components/AppLayout';
import RelativeTime from '@/components/RelativeTime';
import { authorInitial } from '@/lib/utils';

export default function ProfilePage() {
    const { user, isLoading } = useAuth();
    const router = useRouter();
    const [activeTab, setActiveTab] = useState('questions');

    const { data: questionsData } = useQuery({
        queryKey: ['user', user?.userId, 'questions'],
        queryFn: () => usersApi.getQuestions(user!.userId),
        enabled: !!user?.userId,
    });

    const { data: answersData } = useQuery({
        queryKey: ['user', user?.userId, 'answers'],
        queryFn: () => usersApi.getAnswers(user!.userId),
        enabled: !!user?.userId,
    });

    const questions: Question[] = Array.isArray(questionsData) ? questionsData : (questionsData?.items ?? []);
    const answers: Answer[] = Array.isArray(answersData) ? answersData : (answersData?.items ?? []);
    const questionCount = Array.isArray(questionsData) ? questionsData.length : (questionsData?.totalCount ?? questions.length);
    const answerCount = Array.isArray(answersData) ? answersData.length : (answersData?.totalCount ?? answers.length);

    useEffect(() => {
        if (!isLoading && !user) {
            router.push('/auth?mode=login');
        }
    }, [user, isLoading, router]);

    if (isLoading) {
        return (
            <AppLayout showRightSidebar={false}>
                <div className="flex items-center justify-center py-20">
                    <div className="w-10 h-10 border-4 border-[var(--primary)]/30 border-t-[var(--primary)] rounded-full animate-spin"></div>
                </div>
            </AppLayout>
        );
    }

    if (!user) return null;

    const tabs = [
        { key: 'questions', label: 'Questions' },
        { key: 'answers', label: 'Answers' },
        { key: 'saved', label: 'Saved' },
    ];

    return (
        <AppLayout showRightSidebar={false}>
            {/* Cover Banner - overflow only on gradient so avatar is not clipped */}
            <div className="relative">
                <div className="h-48 rounded-t-xl overflow-hidden bg-gradient-to-br from-[var(--primary)] via-blue-500 to-cyan-400"></div>

                {/* Avatar overlapping banner */}
                <div className="absolute -bottom-16 left-8 z-10">
                    {user.profilePicture ? (
                        <img
                            src={user.profilePicture}
                            alt={user.displayName || user.username}
                            className="size-32 rounded-full border-4 border-white dark:border-slate-900 object-cover shadow-lg"
                        />
                    ) : (
                        <div className="size-32 rounded-full border-4 border-white dark:border-slate-900 bg-gradient-to-br from-blue-500 via-indigo-500 to-purple-500 flex items-center justify-center text-white text-5xl font-bold shadow-lg">
                            {authorInitial(user.displayName || user.username)}
                        </div>
                    )}
                </div>
            </div>

            {/* User Info Section */}
            <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 border-t-0 rounded-b-xl px-8 pt-20 pb-6">
                <div className="flex flex-wrap items-start justify-between gap-4">
                    <div>
                        <div className="flex items-center gap-2">
                            <h1 className="text-2xl font-black text-slate-900 dark:text-white">
                                {user.displayName || user.username}
                            </h1>
                            <span className="material-symbols-outlined text-[var(--primary)] text-xl">verified</span>
                        </div>
                        <p className="text-slate-500 dark:text-slate-400 text-sm mt-1">@{user.username}</p>
                        <div className="flex items-center gap-4 mt-3 text-sm text-slate-500 dark:text-slate-400">
                            <span className="flex items-center gap-1">
                                <span className="material-symbols-outlined text-sm">location_on</span>
                                Vietnam
                            </span>
                            <span className="flex items-center gap-1">
                                <span className="material-symbols-outlined text-sm">calendar_today</span>
                                <RelativeTime
                                    value={(user as any).createdDate}
                                    prefix="Joined "
                                    fallback="Member"
                                />
                            </span>
                        </div>
                    </div>
                    <div className="flex items-center gap-3">
                        <Link
                            href="/settings"
                            className="flex items-center gap-2 px-5 py-2.5 border border-slate-200 dark:border-slate-700 rounded-xl text-slate-900 dark:text-white font-bold text-sm hover:bg-slate-50 dark:hover:bg-slate-800 transition"
                        >
                            <span className="material-symbols-outlined text-sm">edit</span>
                            Edit Profile
                        </Link>
                        <button className="flex items-center gap-2 px-5 py-2.5 bg-[var(--primary)] text-white rounded-xl font-bold text-sm hover:bg-[var(--primary)]/90 transition shadow-sm">
                            <span className="material-symbols-outlined text-sm">share</span>
                            Share
                        </button>
                    </div>
                </div>
            </div>

            {/* Stats + Content Layout */}
            <div className="flex gap-8 mt-8">
                {/* Left: Stats + Tabs */}
                <div className="flex-1 min-w-0 space-y-6">
                    {/* Stats Cards */}
                    <div className="grid grid-cols-3 gap-4">
                        <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl p-5">
                            <div className="flex items-center justify-between">
                                <div>
                                    <p className="text-xs font-bold text-slate-500 uppercase tracking-wider mb-1">Reputation</p>
                                    <p className="text-3xl font-black text-slate-900 dark:text-white">
                                        {(user.reputationPoints || 0) >= 1000
                                            ? `${((user.reputationPoints || 0) / 1000).toFixed(1)}k`
                                            : (user.reputationPoints || 0).toLocaleString()}
                                    </p>
                                </div>
                                <span className="material-symbols-outlined text-amber-500 text-2xl">emoji_events</span>
                            </div>
                        </div>
                        <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl p-5">
                            <div className="flex items-center justify-between">
                                <div>
                                    <p className="text-xs font-bold text-slate-500 uppercase tracking-wider mb-1">Questions</p>
                                    <p className="text-3xl font-black text-slate-900 dark:text-white">{questionCount}</p>
                                </div>
                                <span className="material-symbols-outlined text-[var(--primary)] text-2xl">help_center</span>
                            </div>
                        </div>
                        <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl p-5">
                            <div className="flex items-center justify-between">
                                <div>
                                    <p className="text-xs font-bold text-slate-500 uppercase tracking-wider mb-1">Answers</p>
                                    <p className="text-3xl font-black text-slate-900 dark:text-white">{answerCount}</p>
                                </div>
                                <span className="material-symbols-outlined text-indigo-500 text-2xl">forum</span>
                            </div>
                        </div>
                    </div>

                    {/* Tabs */}
                    <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl overflow-hidden">
                        <div className="flex border-b border-slate-200 dark:border-slate-800">
                            {tabs.map((tab) => (
                                <button
                                    key={tab.key}
                                    onClick={() => setActiveTab(tab.key)}
                                    className={`flex-1 px-6 py-4 text-sm font-bold transition-colors border-b-2 ${activeTab === tab.key
                                        ? 'text-[var(--primary)] border-[var(--primary)]'
                                        : 'text-slate-500 border-transparent hover:text-[var(--primary)]'
                                        }`}
                                >
                                    {tab.label}
                                </button>
                            ))}
                        </div>

                        <div className="p-6">
                            {activeTab === 'questions' && (
                                questions.length > 0 ? (
                                    <div className="space-y-3">
                                        {questions.map((q, index) => (
                                            <Link
                                                key={q.questionId}
                                                href={`/questions/${q.questionId}`}
                                                className={`block p-4 rounded-lg hover:bg-slate-50 dark:hover:bg-slate-800 transition border border-slate-200 dark:border-slate-700 ${index > 0 ? 'mt-3' : ''}`}
                                            >
                                                <div className="flex justify-between items-start gap-3">
                                                    <h4 className="text-sm font-bold text-slate-900 dark:text-white line-clamp-2">{q.title}</h4>
                                                    <span className={`shrink-0 px-2.5 py-0.5 rounded-full text-xs font-bold ${q.hasAcceptedAnswer ? 'bg-green-500/10 text-green-600 dark:text-green-400' : 'bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-400'}`}>
                                                        {q.answerCount} answers
                                                    </span>
                                                </div>
                                                <div className="flex gap-4 text-slate-500 dark:text-slate-400 text-xs mt-2">
                                                    <span className="flex items-center gap-1"><span className="material-symbols-outlined text-sm">thumb_up</span>{q.score}</span>
                                                    <span className="flex items-center gap-1"><span className="material-symbols-outlined text-sm">visibility</span>{q.viewCount}</span>
                                                    <RelativeTime value={q.createdDate} />
                                                </div>
                                            </Link>
                                        ))}
                                    </div>
                                ) : (
                                    <div className="text-center py-8">
                                        <span className="material-symbols-outlined text-5xl text-slate-300 mb-4 block">quiz</span>
                                        <h4 className="text-lg font-semibold text-slate-900 dark:text-white mb-2">No questions yet</h4>
                                        <p className="text-slate-500 mb-4">You haven&apos;t asked any questions yet</p>
                                        <Link
                                            href="/questions/ask"
                                            className="inline-flex items-center gap-2 px-5 py-2.5 bg-[var(--primary)] text-white rounded-xl font-semibold text-sm hover:bg-[var(--primary)]/90 transition"
                                        >
                                            <span className="material-symbols-outlined text-sm">add</span>
                                            Ask your first question
                                        </Link>
                                    </div>
                                )
                            )}
                            {activeTab === 'answers' && (
                                answers.length > 0 ? (
                                    <div className="space-y-3">
                                        {answers.map((a, index) => (
                                            <Link
                                                key={a.answerId}
                                                href={`/questions/${a.questionId}#answer-${a.answerId}`}
                                                className={`block p-4 rounded-lg hover:bg-slate-50 dark:hover:bg-slate-800 transition border border-slate-200 dark:border-slate-700 ${index > 0 ? 'mt-3' : ''}`}
                                            >
                                                <div className="flex justify-between items-start gap-3">
                                                    <p className="text-sm text-slate-600 dark:text-slate-300 line-clamp-2">
                                                        {typeof a.body === 'string' ? a.body.replace(/<[^>]*>/g, '').slice(0, 150) : ''}
                                                        {typeof a.body === 'string' && a.body.replace(/<[^>]*>/g, '').length > 150 ? '...' : ''}
                                                    </p>
                                                    {a.isAccepted && (
                                                        <span className="shrink-0 px-2.5 py-0.5 rounded-full text-xs font-bold bg-green-500/10 text-green-600 dark:text-green-400 flex items-center gap-1">
                                                            <span className="material-symbols-outlined text-sm">check_circle</span>
                                                            Accepted
                                                        </span>
                                                    )}
                                                </div>
                                                <div className="flex gap-4 text-slate-500 dark:text-slate-400 text-xs mt-2">
                                                    <span className="flex items-center gap-1"><span className="material-symbols-outlined text-sm">thumb_up</span>{a.score}</span>
                                                    <RelativeTime value={a.createdDate} />
                                                </div>
                                            </Link>
                                        ))}
                                    </div>
                                ) : (
                                    <div className="text-center py-8">
                                        <span className="material-symbols-outlined text-5xl text-slate-300 mb-4 block">forum</span>
                                        <h4 className="text-lg font-semibold text-slate-900 dark:text-white mb-2">No answers yet</h4>
                                        <p className="text-slate-500 mb-4">You haven&apos;t answered any questions yet</p>
                                        <Link
                                            href="/questions"
                                            className="inline-flex items-center gap-2 px-5 py-2.5 border border-slate-200 dark:border-slate-700 text-slate-700 dark:text-slate-300 rounded-xl font-semibold text-sm hover:border-[var(--primary)] hover:text-[var(--primary)] transition"
                                        >
                                            Browse questions
                                        </Link>
                                    </div>
                                )
                            )}
                            {activeTab === 'saved' && (
                                <div className="text-center py-8">
                                    <span className="material-symbols-outlined text-5xl text-slate-300 mb-4 block">bookmark</span>
                                    <h4 className="text-lg font-semibold text-slate-900 dark:text-white mb-2">No saved items</h4>
                                    <p className="text-slate-500">Save questions or answers to revisit later</p>
                                </div>
                            )}
                        </div>

                        {/* View All Link */}
                        <div className="border-t border-slate-200 dark:border-slate-800 px-6 py-3 text-center">
                            <Link href="/questions" className="text-[var(--primary)] text-sm font-bold hover:underline">
                                View All {activeTab === 'questions' ? 'Questions' : activeTab === 'answers' ? 'Answers' : 'Saved'}
                            </Link>
                        </div>
                    </div>
                </div>

                {/* Right Sidebar */}
                <div className="w-72 shrink-0 hidden xl:flex flex-col space-y-6">
                    {/* Badges */}
                    <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl p-5">
                        <div className="flex items-center justify-between mb-4">
                            <h3 className="font-bold text-slate-900 dark:text-white">Badges</h3>
                            <button className="text-[var(--primary)] text-xs font-bold">View All</button>
                        </div>
                        <div className="flex justify-around mb-4">
                            <div className="text-center">
                                <div className="size-12 rounded-full bg-yellow-100 dark:bg-yellow-900/30 flex items-center justify-center mx-auto mb-1">
                                    <span className="material-symbols-outlined text-yellow-500">emoji_events</span>
                                </div>
                                <span className="text-[10px] font-bold text-slate-500 uppercase">Gold</span>
                            </div>
                            <div className="text-center">
                                <div className="size-12 rounded-full bg-slate-100 dark:bg-slate-800 flex items-center justify-center mx-auto mb-1">
                                    <span className="material-symbols-outlined text-slate-400">shield</span>
                                </div>
                                <span className="text-[10px] font-bold text-slate-500 uppercase">Silver</span>
                            </div>
                            <div className="text-center">
                                <div className="size-12 rounded-full bg-orange-100 dark:bg-orange-900/30 flex items-center justify-center mx-auto mb-1">
                                    <span className="material-symbols-outlined text-orange-500">star</span>
                                </div>
                                <span className="text-[10px] font-bold text-slate-500 uppercase">Bronze</span>
                            </div>
                        </div>
                        <div className="border-t border-slate-200 dark:border-slate-800 pt-4">
                            <div className="flex items-center justify-between mb-2">
                                <span className="text-xs text-slate-500">Next Milestone</span>
                                <span className="text-xs font-bold text-slate-900 dark:text-white">{user.reputationPoints || 0} / 1000</span>
                            </div>
                            <div className="w-full bg-slate-100 dark:bg-slate-800 rounded-full h-2">
                                <div
                                    className="bg-[var(--primary)] h-2 rounded-full transition-all"
                                    style={{ width: `${Math.min(((user.reputationPoints || 0) / 1000) * 100, 100)}%` }}
                                ></div>
                            </div>
                        </div>
                    </div>

                    {/* Top Skills */}
                    <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl p-5">
                        <h3 className="font-bold text-slate-900 dark:text-white mb-4">Top Skills</h3>
                        <div className="space-y-4">
                            {[
                                { name: 'JavaScript', level: 'Expert', color: 'bg-[var(--primary)]', width: '95%' },
                                { name: 'TypeScript', level: 'Expert', color: 'bg-[var(--primary)]', width: '90%' },
                                { name: 'React', level: 'Advanced', color: 'bg-[var(--primary)]', width: '80%' },
                                { name: 'Next.js', level: 'Advanced', color: 'bg-[var(--primary)]', width: '75%' },
                            ].map((skill) => (
                                <div key={skill.name}>
                                    <div className="flex items-center justify-between mb-1">
                                        <span className="text-sm font-medium text-slate-700 dark:text-slate-300">{skill.name}</span>
                                        <span className="text-xs font-bold text-[var(--primary)]">{skill.level}</span>
                                    </div>
                                    <div className="w-full bg-slate-100 dark:bg-slate-800 rounded-full h-1.5">
                                        <div className={`${skill.color} h-1.5 rounded-full`} style={{ width: skill.width }}></div>
                                    </div>
                                </div>
                            ))}
                        </div>
                    </div>

                    {/* Communities */}
                    <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl p-5">
                        <h3 className="font-bold text-slate-900 dark:text-white mb-4">Communities</h3>
                        <div className="space-y-4">
                            {[
                                { name: 'React Global', members: '1.2M members', color: 'from-blue-500 to-cyan-500' },
                                { name: 'Next.js Experts', members: '450k members', color: 'from-slate-800 to-slate-600' },
                            ].map((community) => (
                                <div key={community.name} className="flex items-center gap-3">
                                    <div className={`size-10 rounded-xl bg-gradient-to-br ${community.color} flex items-center justify-center text-white font-bold text-sm`}>
                                        {authorInitial(community.name)}
                                    </div>
                                    <div>
                                        <p className="text-sm font-bold text-slate-900 dark:text-white">{community.name}</p>
                                        <p className="text-xs text-slate-500">{community.members}</p>
                                    </div>
                                </div>
                            ))}
                        </div>
                    </div>
                </div>
            </div>
        </AppLayout>
    );
}