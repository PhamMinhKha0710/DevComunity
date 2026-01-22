'use client';

import { useEffect, useState } from 'react';
import { useParams } from 'next/navigation';
import Link from 'next/link';
import type { User, Question } from '@/types';
import apiClient from '@/lib/api/client';

interface UserProfile extends User {
    bio?: string;
    location?: string;
    website?: string;
    questionCount?: number;
    answerCount?: number;
    createdDate?: string;
}

type Tab = 'questions' | 'answers' | 'about';

export default function UserProfilePage() {
    const params = useParams();
    const userId = params.id as string;
    const [user, setUser] = useState<UserProfile | null>(null);
    const [questions, setQuestions] = useState<Question[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [activeTab, setActiveTab] = useState<Tab>('questions');

    useEffect(() => {
        fetchUserProfile();
    }, [userId]);

    const fetchUserProfile = async () => {
        try {
            const userRes = await apiClient.get<UserProfile>(`/users/${userId}`);
            setUser(userRes.data);

            try {
                const questionsRes = await apiClient.get<{ items: Question[] }>(`/users/${userId}/questions`);
                setQuestions(questionsRes.data.items || []);
            } catch {
                setQuestions([]);
            }
        } catch (error) {
            console.error('Failed to fetch user profile:', error);
        } finally {
            setIsLoading(false);
        }
    };

    if (isLoading) {
        return (
            <div className="min-h-screen flex items-center justify-center bg-gray-50 dark:bg-slate-900">
                <div className="inline-block w-10 h-10 border-4 border-orange-500 border-t-transparent rounded-full animate-spin"></div>
            </div>
        );
    }

    if (!user) {
        return (
            <div className="min-h-screen flex items-center justify-center bg-gray-50 dark:bg-slate-900">
                <div className="text-center">
                    <div className="w-20 h-20 bg-gray-200 dark:bg-slate-700 rounded-full mx-auto mb-4 flex items-center justify-center">
                        <i className="bi bi-person-x text-4xl text-gray-400"></i>
                    </div>
                    <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-2">User not found</h2>
                    <Link href="/users" className="inline-flex items-center px-4 py-2 bg-orange-500 text-white rounded-lg hover:bg-orange-600 transition">
                        Browse Users
                    </Link>
                </div>
            </div>
        );
    }

    return (
        <div className="min-h-screen bg-gray-50 dark:bg-slate-900 py-8">
            <div className="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8">
                {/* Profile Header */}
                <div className="bg-gradient-to-r from-purple-600 to-blue-500 rounded-xl shadow-lg overflow-hidden mb-8">
                    <div className="p-6 sm:p-8">
                        <div className="flex flex-col sm:flex-row items-center gap-6">
                            <img
                                src={user.profilePicture || '/images/default-avatar.png'}
                                className="w-28 h-28 rounded-full border-4 border-white/30"
                                alt={user.displayName || user.username}
                            />
                            <div className="text-center sm:text-left text-white flex-1">
                                <h1 className="text-2xl sm:text-3xl font-bold mb-1">{user.displayName || user.username}</h1>
                                <p className="opacity-75 mb-2">@{user.username}</p>
                                {user.location && (
                                    <p className="text-sm opacity-75">
                                        <i className="bi bi-geo-alt mr-1"></i>{user.location}
                                    </p>
                                )}
                            </div>
                            <div className="flex gap-4 text-white">
                                <div className="bg-white/10 rounded-lg px-4 py-3 text-center">
                                    <div className="text-2xl font-bold">{user.reputationPoints || 0}</div>
                                    <div className="text-xs uppercase tracking-wider opacity-75">Reputation</div>
                                </div>
                                <div className="bg-white/10 rounded-lg px-4 py-3 text-center">
                                    <div className="text-2xl font-bold">{user.questionCount || questions.length}</div>
                                    <div className="text-xs uppercase tracking-wider opacity-75">Questions</div>
                                </div>
                                <div className="bg-white/10 rounded-lg px-4 py-3 text-center">
                                    <div className="text-2xl font-bold">{user.answerCount || 0}</div>
                                    <div className="text-xs uppercase tracking-wider opacity-75">Answers</div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>

                <div className="grid lg:grid-cols-3 gap-8">
                    {/* Main Content */}
                    <div className="lg:col-span-2">
                        {/* Tabs */}
                        <div className="bg-white dark:bg-slate-800 rounded-xl shadow-sm overflow-hidden">
                            <div className="border-b border-gray-100 dark:border-slate-700 p-4">
                                <div className="flex gap-2">
                                    {[
                                        { key: 'questions' as Tab, icon: 'bi-question-circle', label: 'Questions', count: user.questionCount || questions.length },
                                        { key: 'answers' as Tab, icon: 'bi-chat-left-text', label: 'Answers', count: user.answerCount || 0 },
                                        { key: 'about' as Tab, icon: 'bi-person', label: 'About' },
                                    ].map(({ key, icon, label, count }) => (
                                        <button
                                            key={key}
                                            onClick={() => setActiveTab(key)}
                                            className={`inline-flex items-center px-4 py-2 rounded-full text-sm font-medium transition ${activeTab === key
                                                    ? 'bg-orange-500 text-white'
                                                    : 'text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-slate-700'
                                                }`}
                                        >
                                            <i className={`bi ${icon} mr-2`}></i>{label}
                                            {count !== undefined && <span className="ml-1">({count})</span>}
                                        </button>
                                    ))}
                                </div>
                            </div>

                            <div className="p-6">
                                {activeTab === 'questions' && (
                                    questions.length > 0 ? (
                                        <div className="space-y-4">
                                            {questions.map((q) => (
                                                <Link
                                                    key={q.questionId}
                                                    href={`/questions/${q.questionId}`}
                                                    className="block p-4 bg-gray-50 dark:bg-slate-700 rounded-lg hover:bg-gray-100 dark:hover:bg-slate-600 transition"
                                                >
                                                    <div className="flex justify-between items-start mb-2">
                                                        <h3 className="font-semibold text-gray-900 dark:text-white">{q.title}</h3>
                                                        <span className={`px-2 py-1 text-xs rounded-full ${q.hasAcceptedAnswer ? 'bg-green-100 text-green-700' : 'bg-gray-200 text-gray-700'}`}>
                                                            {q.answerCount} answers
                                                        </span>
                                                    </div>
                                                    <div className="flex gap-4 text-sm text-gray-500">
                                                        <span><i className="bi bi-hand-thumbs-up mr-1"></i>{q.score}</span>
                                                        <span><i className="bi bi-eye mr-1"></i>{q.viewCount}</span>
                                                        <span>{new Date(q.createdDate).toLocaleDateString()}</span>
                                                    </div>
                                                </Link>
                                            ))}
                                        </div>
                                    ) : (
                                        <div className="text-center py-12">
                                            <div className="w-16 h-16 bg-gray-100 dark:bg-slate-700 rounded-full mx-auto mb-4 flex items-center justify-center">
                                                <i className="bi bi-question-circle text-3xl text-gray-400"></i>
                                            </div>
                                            <p className="text-gray-500">No questions yet</p>
                                        </div>
                                    )
                                )}

                                {activeTab === 'answers' && (
                                    <div className="text-center py-12">
                                        <div className="w-16 h-16 bg-gray-100 dark:bg-slate-700 rounded-full mx-auto mb-4 flex items-center justify-center">
                                            <i className="bi bi-chat-left-text text-3xl text-gray-400"></i>
                                        </div>
                                        <p className="text-gray-500">Answers will be displayed here</p>
                                    </div>
                                )}

                                {activeTab === 'about' && (
                                    <div>
                                        <h3 className="font-bold text-gray-900 dark:text-white mb-4">About</h3>
                                        <p className="text-gray-600 dark:text-gray-400 mb-6">
                                            {user.bio || 'This user has not added a bio yet.'}
                                        </p>
                                        <div className="border-t border-gray-100 dark:border-slate-700 pt-4 space-y-3">
                                            <div className="flex items-center text-sm text-gray-500">
                                                <i className="bi bi-calendar3 w-6"></i>
                                                <span>Joined {user.createdDate ? new Date(user.createdDate).toLocaleDateString() : 'Recently'}</span>
                                            </div>
                                            {user.website && (
                                                <div className="flex items-center text-sm">
                                                    <i className="bi bi-link-45deg w-6 text-gray-500"></i>
                                                    <a href={user.website} target="_blank" rel="noopener noreferrer" className="text-orange-500 hover:underline">
                                                        {user.website}
                                                    </a>
                                                </div>
                                            )}
                                        </div>
                                    </div>
                                )}
                            </div>
                        </div>
                    </div>

                    {/* Sidebar */}
                    <div className="space-y-6">
                        {/* Badges */}
                        <div className="bg-white dark:bg-slate-800 rounded-xl shadow-sm p-6">
                            <h3 className="font-bold text-gray-900 dark:text-white mb-4">
                                <i className="bi bi-award text-yellow-500 mr-2"></i>Badges
                            </h3>
                            <div className="flex flex-wrap gap-2">
                                <span className="px-3 py-1 bg-yellow-100 text-yellow-700 rounded-full text-sm">Gold: 0</span>
                                <span className="px-3 py-1 bg-gray-200 text-gray-700 rounded-full text-sm">Silver: 0</span>
                                <span className="px-3 py-1 bg-orange-100 text-orange-700 rounded-full text-sm">Bronze: 0</span>
                            </div>
                        </div>

                        {/* Stats */}
                        <div className="bg-white dark:bg-slate-800 rounded-xl shadow-sm p-6">
                            <h3 className="font-bold text-gray-900 dark:text-white mb-4">
                                <i className="bi bi-graph-up text-green-500 mr-2"></i>Stats
                            </h3>
                            <div className="grid grid-cols-2 gap-3">
                                <div className="bg-gray-50 dark:bg-slate-700 rounded-lg p-3 text-center">
                                    <div className="text-xl font-bold text-orange-500">{user.reputationPoints || 0}</div>
                                    <div className="text-xs text-gray-500">Reputation</div>
                                </div>
                                <div className="bg-gray-50 dark:bg-slate-700 rounded-lg p-3 text-center">
                                    <div className="text-xl font-bold text-green-500">{user.questionCount || questions.length}</div>
                                    <div className="text-xs text-gray-500">Questions</div>
                                </div>
                                <div className="bg-gray-50 dark:bg-slate-700 rounded-lg p-3 text-center">
                                    <div className="text-xl font-bold text-blue-500">{user.answerCount || 0}</div>
                                    <div className="text-xs text-gray-500">Answers</div>
                                </div>
                                <div className="bg-gray-50 dark:bg-slate-700 rounded-lg p-3 text-center">
                                    <div className="text-xl font-bold text-yellow-500">0</div>
                                    <div className="text-xs text-gray-500">Badges</div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
}
