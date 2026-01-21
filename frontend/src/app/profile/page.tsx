'use client';

import { useAuth } from '@/lib/contexts/AuthContext';
import { useRouter } from 'next/navigation';
import { useEffect, useState } from 'react';
import Link from 'next/link';

type Tab = 'questions' | 'answers' | 'saved';

export default function ProfilePage() {
    const { user, isLoading } = useAuth();
    const router = useRouter();
    const [activeTab, setActiveTab] = useState<Tab>('questions');

    useEffect(() => {
        if (!isLoading && !user) {
            router.push('/login');
        }
    }, [user, isLoading, router]);

    if (isLoading) {
        return (
            <div className="min-h-screen flex items-center justify-center bg-gray-50 dark:bg-slate-900">
                <div className="inline-block w-8 h-8 border-4 border-orange-500 border-t-transparent rounded-full animate-spin"></div>
            </div>
        );
    }

    if (!user) return null;

    return (
        <div className="min-h-screen bg-gray-50 dark:bg-slate-900 py-8">
            <div className="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8">
                <div className="grid lg:grid-cols-3 gap-8">
                    {/* Profile Sidebar */}
                    <div className="space-y-6">
                        {/* Profile Card */}
                        <div className="bg-white dark:bg-slate-800 rounded-xl shadow-sm p-6 text-center">
                            <div className="relative inline-block mb-4">
                                <img
                                    src={user.profilePicture || '/images/default-avatar.png'}
                                    className="w-28 h-28 rounded-full border-4 border-gray-100 dark:border-slate-700"
                                    alt="Profile"
                                />
                                <div className="absolute bottom-0 right-0 w-6 h-6 bg-green-500 rounded-full border-2 border-white dark:border-slate-800"></div>
                            </div>
                            <h2 className="text-xl font-bold text-gray-900 dark:text-white mb-1">
                                {user.displayName || user.username}
                            </h2>
                            <p className="text-gray-500 mb-4">@{user.username}</p>

                            <div className="grid grid-cols-3 gap-3 mb-6">
                                <div className="bg-orange-50 dark:bg-orange-900/20 rounded-lg p-3 text-center">
                                    <div className="text-xl font-bold text-orange-500">{user.reputationPoints || 0}</div>
                                    <div className="text-xs text-gray-500">Reputation</div>
                                </div>
                                <div className="bg-gray-50 dark:bg-slate-700 rounded-lg p-3 text-center">
                                    <div className="text-xl font-bold text-gray-900 dark:text-white">0</div>
                                    <div className="text-xs text-gray-500">Questions</div>
                                </div>
                                <div className="bg-gray-50 dark:bg-slate-700 rounded-lg p-3 text-center">
                                    <div className="text-xl font-bold text-gray-900 dark:text-white">0</div>
                                    <div className="text-xs text-gray-500">Answers</div>
                                </div>
                            </div>

                            <Link
                                href="/settings"
                                className="block w-full py-2 border border-orange-500 text-orange-500 rounded-lg hover:bg-orange-50 dark:hover:bg-orange-900/20 transition"
                            >
                                <i className="bi bi-gear mr-2"></i>Edit Profile
                            </Link>
                        </div>

                        {/* Badges Card */}
                        <div className="bg-white dark:bg-slate-800 rounded-xl shadow-sm p-6">
                            <h3 className="font-bold text-gray-900 dark:text-white mb-4">Badges</h3>
                            <div className="flex flex-wrap gap-2">
                                <span className="inline-flex items-center px-3 py-1 bg-yellow-100 text-yellow-700 rounded-full text-sm">
                                    <i className="bi bi-award mr-1"></i>Gold: 0
                                </span>
                                <span className="inline-flex items-center px-3 py-1 bg-gray-200 text-gray-700 rounded-full text-sm">
                                    <i className="bi bi-award mr-1"></i>Silver: 0
                                </span>
                                <span className="inline-flex items-center px-3 py-1 bg-orange-100 text-orange-700 rounded-full text-sm">
                                    <i className="bi bi-award mr-1"></i>Bronze: 0
                                </span>
                            </div>
                        </div>

                        {/* About Card */}
                        <div className="bg-white dark:bg-slate-800 rounded-xl shadow-sm p-6">
                            <h3 className="font-bold text-gray-900 dark:text-white mb-4">About</h3>
                            <div className="space-y-3 text-sm text-gray-600 dark:text-gray-400">
                                <div className="flex items-center">
                                    <i className="bi bi-calendar3 w-5 text-gray-400"></i>
                                    <span>Member since {new Date().toLocaleDateString()}</span>
                                </div>
                                <div className="flex items-center">
                                    <i className="bi bi-envelope w-5 text-gray-400"></i>
                                    <span>{user.email || 'No email provided'}</span>
                                </div>
                            </div>
                        </div>
                    </div>

                    {/* Main Content */}
                    <div className="lg:col-span-2">
                        {/* Activity Tabs */}
                        <div className="bg-white dark:bg-slate-800 rounded-xl shadow-sm overflow-hidden">
                            {/* Tab Headers */}
                            <div className="border-b border-gray-100 dark:border-slate-700 p-4">
                                <div className="flex gap-2">
                                    {[
                                        { key: 'questions' as Tab, icon: 'bi-chat-square-text', label: 'Questions' },
                                        { key: 'answers' as Tab, icon: 'bi-chat-left-text', label: 'Answers' },
                                        { key: 'saved' as Tab, icon: 'bi-bookmark', label: 'Saved' },
                                    ].map(({ key, icon, label }) => (
                                        <button
                                            key={key}
                                            onClick={() => setActiveTab(key)}
                                            className={`inline-flex items-center px-4 py-2 rounded-full text-sm font-medium transition ${activeTab === key
                                                    ? 'bg-orange-500 text-white'
                                                    : 'text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-slate-700'
                                                }`}
                                        >
                                            <i className={`bi ${icon} mr-2`}></i>{label}
                                        </button>
                                    ))}
                                </div>
                            </div>

                            {/* Tab Content */}
                            <div className="p-6">
                                {activeTab === 'questions' && (
                                    <div className="text-center py-12">
                                        <div className="w-16 h-16 bg-gray-100 dark:bg-slate-700 rounded-full mx-auto mb-4 flex items-center justify-center">
                                            <i className="bi bi-chat-square-text text-3xl text-gray-400"></i>
                                        </div>
                                        <h3 className="text-lg font-medium text-gray-900 dark:text-white mb-2">
                                            No questions yet
                                        </h3>
                                        <p className="text-gray-500 mb-4">You haven&apos;t asked any questions</p>
                                        <Link
                                            href="/questions/ask"
                                            className="inline-flex items-center px-4 py-2 bg-orange-500 text-white rounded-lg hover:bg-orange-600 transition"
                                        >
                                            Ask your first question
                                        </Link>
                                    </div>
                                )}

                                {activeTab === 'answers' && (
                                    <div className="text-center py-12">
                                        <div className="w-16 h-16 bg-gray-100 dark:bg-slate-700 rounded-full mx-auto mb-4 flex items-center justify-center">
                                            <i className="bi bi-chat-left-text text-3xl text-gray-400"></i>
                                        </div>
                                        <h3 className="text-lg font-medium text-gray-900 dark:text-white mb-2">
                                            No answers yet
                                        </h3>
                                        <p className="text-gray-500 mb-4">You haven&apos;t answered any questions</p>
                                        <Link
                                            href="/questions"
                                            className="inline-flex items-center px-4 py-2 border border-orange-500 text-orange-500 rounded-lg hover:bg-orange-50 dark:hover:bg-orange-900/20 transition"
                                        >
                                            Browse questions
                                        </Link>
                                    </div>
                                )}

                                {activeTab === 'saved' && (
                                    <div className="text-center py-12">
                                        <div className="w-16 h-16 bg-gray-100 dark:bg-slate-700 rounded-full mx-auto mb-4 flex items-center justify-center">
                                            <i className="bi bi-bookmark text-3xl text-gray-400"></i>
                                        </div>
                                        <h3 className="text-lg font-medium text-gray-900 dark:text-white mb-2">
                                            No saved items
                                        </h3>
                                        <p className="text-gray-500">Questions you save will appear here</p>
                                    </div>
                                )}
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
}
