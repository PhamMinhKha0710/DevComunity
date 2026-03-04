'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import AppLayout from '@/components/AppLayout';
import { useAuth } from '@/lib/contexts/AuthContext';

interface Post {
    postId: number;
    author: {
        userId: number;
        username: string;
        displayName: string;
        profilePicture: string | null;
    };
    groupId: number | null;
    groupName: string | null;
    content: string;
    createdAt: string;
}

export default function NewsfeedPage() {
    const { isAuthenticated, user: currentUser } = useAuth();
    const [posts, setPosts] = useState<Post[]>([]);
    const [loading, setLoading] = useState(true);
    const [newPostContent, setNewPostContent] = useState('');
    const [posting, setPosting] = useState(false);
    const [activeTab, setActiveTab] = useState('all');

    const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5122';
    const getToken = () => localStorage.getItem('accessToken');

    useEffect(() => {
        if (isAuthenticated) {
            fetchNewsfeed();
        } else {
            setLoading(false);
        }
    }, [isAuthenticated]);

    const fetchNewsfeed = async () => {
        try {
            const response = await fetch(`${API_BASE_URL}/api/Newsfeed`, {
                headers: { Authorization: `Bearer ${getToken()}` }
            });
            if (response.ok) {
                const data = await response.json();
                setPosts(data.items || []);
            }
        } catch (error) {
            console.error('Error fetching newsfeed:', error);
        } finally {
            setLoading(false);
        }
    };

    const handleCreatePost = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!newPostContent.trim()) return;

        setPosting(true);
        try {
            const response = await fetch(`${API_BASE_URL}/api/Newsfeed/posts`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    Authorization: `Bearer ${getToken()}`
                },
                body: JSON.stringify({ content: newPostContent, groupId: null })
            });
            if (response.ok) {
                const newPost = await response.json();
                setPosts([newPost, ...posts]);
                setNewPostContent('');
            }
        } catch (error) {
            console.error('Error creating post:', error);
        } finally {
            setPosting(false);
        }
    };

    const formatDate = (dateString: string) => {
        const date = new Date(dateString);
        const now = new Date();
        const diffMs = now.getTime() - date.getTime();
        const diffMins = Math.floor(diffMs / 60000);
        const diffHours = Math.floor(diffMs / 3600000);
        const diffDays = Math.floor(diffMs / 86400000);

        if (diffMins < 60) return `${diffMins}m ago`;
        if (diffHours < 24) return `${diffHours}h ago`;
        if (diffDays < 7) return `${diffDays}d ago`;
        return date.toLocaleDateString();
    };

    if (!isAuthenticated) {
        return (
            <AppLayout showRightSidebar={false}>
                <div className="flex flex-col items-center justify-center py-20 text-center">
                    <div className="w-20 h-20 bg-slate-100 dark:bg-slate-800 rounded-full flex items-center justify-center mb-6">
                        <span className="material-symbols-outlined text-4xl text-slate-400">lock</span>
                    </div>
                    <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-3">Login Required</h2>
                    <p className="text-slate-500 mb-6 max-w-md">
                        Please login to view your personalized newsfeed and connect with other developers.
                    </p>
                    <Link href="/login" className="px-6 py-3 bg-[var(--primary)] text-white rounded-xl font-bold hover:bg-[var(--primary)]/90 transition">
                        Login Now
                    </Link>
                </div>
            </AppLayout>
        );
    }

    const tabs = [
        { key: 'all', label: 'All Updates' },
        { key: 'following', label: 'Following' },
        { key: 'community', label: 'Community News' },
    ];

    return (
        <AppLayout showRightSidebar={false}>
            <div className="grid grid-cols-1 xl:grid-cols-12 gap-8">
                {/* Main Feed Column */}
                <div className="xl:col-span-8 flex flex-col gap-6">
                    {/* Create Post */}
                    <div className="bg-white dark:bg-slate-900 rounded-xl p-5 shadow-sm border border-slate-200 dark:border-slate-800">
                        <form onSubmit={handleCreatePost}>
                            <div className="flex gap-4">
                                <div className="w-10 h-10 rounded-full shrink-0 overflow-hidden">
                                    {currentUser?.profilePicture ? (
                                        <img src={currentUser.profilePicture} alt="" className="w-full h-full object-cover" />
                                    ) : (
                                        <div className="w-full h-full bg-gradient-to-br from-blue-500 to-purple-500 flex items-center justify-center text-white font-bold">
                                            {currentUser?.username?.charAt(0).toUpperCase() || 'U'}
                                        </div>
                                    )}
                                </div>
                                <div className="w-full">
                                    <textarea
                                        value={newPostContent}
                                        onChange={(e) => setNewPostContent(e.target.value)}
                                        placeholder="What's happening in your tech world?"
                                        className="w-full h-24 p-4 rounded-xl bg-slate-50 dark:bg-slate-800 border-none focus:ring-2 focus:ring-[var(--primary)]/30 resize-none text-slate-900 dark:text-white placeholder:text-slate-400 text-sm"
                                        rows={3}
                                    />
                                    <div className="flex items-center justify-between mt-4">
                                        <div className="flex gap-2">
                                            <button type="button" className="p-2 rounded-lg text-slate-500 hover:bg-slate-100 dark:hover:bg-slate-800 hover:text-[var(--primary)] transition-colors">
                                                <span className="material-symbols-outlined">image</span>
                                            </button>
                                            <button type="button" className="p-2 rounded-lg text-slate-500 hover:bg-slate-100 dark:hover:bg-slate-800 hover:text-[var(--primary)] transition-colors">
                                                <span className="material-symbols-outlined">videocam</span>
                                            </button>
                                            <button type="button" className="p-2 rounded-lg text-slate-500 hover:bg-slate-100 dark:hover:bg-slate-800 hover:text-[var(--primary)] transition-colors">
                                                <span className="material-symbols-outlined">event</span>
                                            </button>
                                            <button type="button" className="p-2 rounded-lg text-slate-500 hover:bg-slate-100 dark:hover:bg-slate-800 hover:text-[var(--primary)] transition-colors">
                                                <span className="material-symbols-outlined">equalizer</span>
                                            </button>
                                        </div>
                                        <button
                                            type="submit"
                                            disabled={posting || !newPostContent.trim()}
                                            className="px-6 py-2 bg-[var(--primary)] text-white text-sm font-bold rounded-xl shadow-lg shadow-[var(--primary)]/20 hover:opacity-90 active:scale-95 transition-all disabled:opacity-50 disabled:cursor-not-allowed"
                                        >
                                            {posting ? 'Posting...' : 'Post'}
                                        </button>
                                    </div>
                                </div>
                            </div>
                        </form>
                    </div>

                    {/* Feed Tabs */}
                    <div className="flex border-b border-slate-200 dark:border-slate-800 gap-8">
                        {tabs.map((tab) => (
                            <button
                                key={tab.key}
                                onClick={() => setActiveTab(tab.key)}
                                className={`pb-3 border-b-2 text-sm font-bold transition-colors ${activeTab === tab.key
                                    ? 'border-[var(--primary)] text-[var(--primary)]'
                                    : 'border-transparent text-slate-500 hover:text-slate-700 dark:hover:text-slate-300'
                                    }`}
                            >
                                {tab.label}
                            </button>
                        ))}
                    </div>

                    {/* Posts Feed */}
                    {loading ? (
                        <div className="flex items-center justify-center py-16">
                            <div className="w-10 h-10 border-4 border-[var(--primary)]/30 border-t-[var(--primary)] rounded-full animate-spin"></div>
                        </div>
                    ) : posts.length === 0 ? (
                        <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-2xl p-12 text-center">
                            <span className="material-symbols-outlined text-5xl text-slate-300 mb-4 block">inbox</span>
                            <h3 className="text-lg font-semibold text-slate-900 dark:text-white mb-2">No posts yet</h3>
                            <p className="text-slate-500">Be the first to share something or follow more users!</p>
                        </div>
                    ) : (
                        <div className="flex flex-col gap-6">
                            {posts.map((post) => (
                                <div key={post.postId} className="bg-white dark:bg-slate-900 rounded-xl overflow-hidden shadow-sm border border-slate-200 dark:border-slate-800 transition-all hover:shadow-md">
                                    {/* Post Header */}
                                    <div className="p-5 flex items-center justify-between">
                                        <div className="flex items-center gap-3">
                                            <div className="h-10 w-10 rounded-full overflow-hidden">
                                                {post.author.profilePicture ? (
                                                    <img src={post.author.profilePicture} alt="" className="w-full h-full object-cover" />
                                                ) : (
                                                    <div className="w-full h-full bg-gradient-to-br from-purple-500 to-pink-500 flex items-center justify-center text-white font-bold">
                                                        {post.author.username?.charAt(0).toUpperCase() || 'U'}
                                                    </div>
                                                )}
                                            </div>
                                            <div>
                                                <h4 className="text-sm font-bold text-slate-900 dark:text-white">
                                                    <Link href={`/users/${post.author.userId}`} className="hover:text-[var(--primary)] transition-colors">
                                                        {post.author.displayName || post.author.username}
                                                    </Link>
                                                </h4>
                                                <p className="text-xs text-slate-500">
                                                    {formatDate(post.createdAt)}
                                                    {post.groupName && (
                                                        <> • in <Link href={`/groups/${post.groupId}`} className="text-[var(--primary)] font-medium">{post.groupName}</Link></>
                                                    )}
                                                </p>
                                            </div>
                                        </div>
                                        <button className="text-slate-400 hover:text-slate-600 dark:hover:text-slate-300">
                                            <span className="material-symbols-outlined">more_horiz</span>
                                        </button>
                                    </div>

                                    {/* Post Content */}
                                    <div className="px-5 pb-4">
                                        <p className="text-slate-700 dark:text-slate-300 text-sm leading-relaxed whitespace-pre-wrap">
                                            {post.content}
                                        </p>
                                    </div>

                                    {/* Post Actions */}
                                    <div className="px-5 py-4 border-t border-slate-100 dark:border-slate-800 flex items-center justify-between">
                                        <div className="flex items-center gap-6">
                                            <button className="flex items-center gap-2 text-slate-500 hover:text-[var(--primary)] transition-colors">
                                                <span className="material-symbols-outlined text-xl">favorite</span>
                                                <span className="text-xs font-semibold">0</span>
                                            </button>
                                            <button className="flex items-center gap-2 text-slate-500 hover:text-[var(--primary)] transition-colors">
                                                <span className="material-symbols-outlined text-xl">chat_bubble</span>
                                                <span className="text-xs font-semibold">0</span>
                                            </button>
                                            <button className="flex items-center gap-2 text-slate-500 hover:text-[var(--primary)] transition-colors">
                                                <span className="material-symbols-outlined text-xl">share</span>
                                                <span className="text-xs font-semibold">0</span>
                                            </button>
                                        </div>
                                        <button className="text-slate-400 hover:text-[var(--primary)]">
                                            <span className="material-symbols-outlined text-xl">bookmark</span>
                                        </button>
                                    </div>
                                </div>
                            ))}
                        </div>
                    )}
                </div>

                {/* Right Sidebar */}
                <div className="hidden xl:flex xl:col-span-4 flex-col gap-6">
                    {/* Trending Topics */}
                    <div className="bg-white dark:bg-slate-900 rounded-xl p-5 shadow-sm border border-slate-200 dark:border-slate-800">
                        <h4 className="text-sm font-bold text-slate-900 dark:text-white mb-4">Trending Topics</h4>
                        <div className="flex flex-col gap-4">
                            {[
                                { tag: '#Web3', posts: '1.2k posts this week' },
                                { tag: '#AIRevolution', posts: '856 posts this week' },
                                { tag: '#RemoteWork', posts: '432 posts this week' },
                            ].map((topic) => (
                                <div key={topic.tag}>
                                    <p className="text-xs font-bold text-[var(--primary)]">{topic.tag}</p>
                                    <p className="text-xs text-slate-500">{topic.posts}</p>
                                </div>
                            ))}
                            <button className="text-xs font-bold text-slate-400 hover:text-[var(--primary)] transition-colors mt-2 text-left">View more</button>
                        </div>
                    </div>

                    {/* Who to Follow */}
                    <div className="bg-white dark:bg-slate-900 rounded-xl p-5 shadow-sm border border-slate-200 dark:border-slate-800">
                        <div className="flex items-center justify-between mb-4">
                            <h4 className="text-sm font-bold text-slate-900 dark:text-white">Who to follow</h4>
                            <button className="text-xs font-bold text-[var(--primary)]">Refresh</button>
                        </div>
                        <div className="flex flex-col gap-5">
                            {[
                                { name: 'David G.', role: 'Product Manager' },
                                { name: 'Elena R.', role: 'Cloud Architect' },
                                { name: 'Sam T.', role: 'DevOps Engineer' },
                            ].map((person) => (
                                <div key={person.name} className="flex items-center justify-between">
                                    <div className="flex items-center gap-3">
                                        <div className="h-9 w-9 rounded-full bg-gradient-to-br from-blue-500 to-indigo-500 flex items-center justify-center text-white text-xs font-bold">
                                            {person.name.charAt(0)}
                                        </div>
                                        <div>
                                            <p className="text-xs font-bold text-slate-900 dark:text-white">{person.name}</p>
                                            <p className="text-[10px] text-slate-500">{person.role}</p>
                                        </div>
                                    </div>
                                    <button className="p-1.5 rounded-lg text-[var(--primary)] bg-[var(--primary)]/10 hover:bg-[var(--primary)] hover:text-white transition-all">
                                        <span className="material-symbols-outlined text-sm">person_add</span>
                                    </button>
                                </div>
                            ))}
                        </div>
                    </div>

                    {/* Upgrade to Pro */}
                    <div className="bg-gradient-to-br from-[var(--primary)] to-blue-700 rounded-xl p-5 shadow-lg text-white">
                        <h4 className="font-bold text-lg mb-2">Upgrade to Pro</h4>
                        <p className="text-xs opacity-90 mb-4 leading-relaxed">Get early access to exclusive community events and premium developer tools.</p>
                        <button className="w-full py-2.5 bg-white text-[var(--primary)] rounded-xl text-xs font-bold hover:bg-slate-100 transition-colors">
                            Learn More
                        </button>
                    </div>

                    {/* Footer Links */}
                    <div className="flex flex-wrap gap-x-4 gap-y-2 px-2">
                        <a className="text-[10px] text-slate-400 hover:text-[var(--primary)]" href="#">About</a>
                        <a className="text-[10px] text-slate-400 hover:text-[var(--primary)]" href="#">Accessibility</a>
                        <a className="text-[10px] text-slate-400 hover:text-[var(--primary)]" href="#">Help Center</a>
                        <a className="text-[10px] text-slate-400 hover:text-[var(--primary)]" href="#">Privacy & Terms</a>
                        <p className="text-[10px] text-slate-400 mt-2">© 2026 SocialTechsy Corp.</p>
                    </div>
                </div>
            </div>
        </AppLayout>
    );
}
