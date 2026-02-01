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
    const { isAuthenticated } = useAuth();
    const [posts, setPosts] = useState<Post[]>([]);
    const [loading, setLoading] = useState(true);
    const [newPostContent, setNewPostContent] = useState('');
    const [posting, setPosting] = useState(false);

    const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5164';
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
            <AppLayout>
                <div className="flex flex-col items-center justify-center py-20 text-center">
                    <div className="w-20 h-20 bg-[var(--bg-tertiary)] rounded-full flex items-center justify-center mb-6">
                        <i className="bi bi-lock text-4xl text-[var(--text-muted)]"></i>
                    </div>
                    <h2 className="text-2xl font-bold text-[var(--text-primary)] mb-3">Login Required</h2>
                    <p className="text-[var(--text-muted)] mb-6 max-w-md">
                        Please login to view your personalized newsfeed and connect with other developers.
                    </p>
                    <Link href="/login" className="px-6 py-3 bg-[var(--primary)] text-white rounded-xl font-medium hover:bg-[var(--primary-dark)] transition">
                        Login Now
                    </Link>
                </div>
            </AppLayout>
        );
    }

    return (
        <AppLayout>
            {/* Header */}
            <div className="mb-6">
                <h1 className="text-2xl font-bold text-[var(--text-primary)] flex items-center gap-2">
                    <i className="bi bi-newspaper text-[var(--primary)]"></i>
                    Newsfeed
                </h1>
                <p className="text-[var(--text-muted)]">Updates from friends and groups you follow</p>
            </div>

            {/* Create Post */}
            <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-2xl p-5 mb-6">
                <form onSubmit={handleCreatePost}>
                    <div className="flex gap-4">
                        <div className="w-10 h-10 rounded-full bg-gradient-to-br from-blue-500 to-purple-500 flex items-center justify-center text-white font-bold shrink-0">
                            U
                        </div>
                        <div className="flex-1">
                            <textarea
                                value={newPostContent}
                                onChange={(e) => setNewPostContent(e.target.value)}
                                placeholder="What's on your mind? Share with the community..."
                                className="w-full p-4 bg-[var(--bg-tertiary)] border border-[var(--border-color)] rounded-xl text-[var(--text-primary)] placeholder-[var(--text-muted)] resize-none focus:border-[var(--primary)] focus:ring-2 focus:ring-[var(--primary)]/20 transition"
                                rows={3}
                            />
                            <div className="flex items-center justify-between mt-3">
                                <div className="flex gap-2">
                                    <button type="button" className="p-2 rounded-lg hover:bg-[var(--bg-hover)] text-[var(--text-muted)] transition">
                                        <i className="bi bi-image"></i>
                                    </button>
                                    <button type="button" className="p-2 rounded-lg hover:bg-[var(--bg-hover)] text-[var(--text-muted)] transition">
                                        <i className="bi bi-code-slash"></i>
                                    </button>
                                    <button type="button" className="p-2 rounded-lg hover:bg-[var(--bg-hover)] text-[var(--text-muted)] transition">
                                        <i className="bi bi-emoji-smile"></i>
                                    </button>
                                </div>
                                <button
                                    type="submit"
                                    disabled={posting || !newPostContent.trim()}
                                    className="px-5 py-2 bg-[var(--primary)] text-white rounded-xl font-medium hover:bg-[var(--primary-dark)] disabled:opacity-50 disabled:cursor-not-allowed transition flex items-center gap-2"
                                >
                                    {posting ? (
                                        <><i className="bi bi-arrow-clockwise animate-spin"></i> Posting...</>
                                    ) : (
                                        <><i className="bi bi-send"></i> Post</>
                                    )}
                                </button>
                            </div>
                        </div>
                    </div>
                </form>
            </div>

            {/* Posts Feed */}
            {loading ? (
                <div className="flex items-center justify-center py-16">
                    <div className="w-10 h-10 border-4 border-[var(--primary)]/30 border-t-[var(--primary)] rounded-full animate-spin"></div>
                </div>
            ) : posts.length === 0 ? (
                <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-2xl p-12 text-center">
                    <i className="bi bi-inbox text-5xl text-[var(--text-muted)] mb-4"></i>
                    <h3 className="text-lg font-semibold text-[var(--text-primary)] mb-2">No posts yet</h3>
                    <p className="text-[var(--text-muted)]">Be the first to share something or follow more users!</p>
                </div>
            ) : (
                <div className="space-y-4">
                    {posts.map((post) => (
                        <div key={post.postId} className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-2xl p-5 hover:border-[var(--primary)]/30 transition">
                            {/* Post Header */}
                            <div className="flex items-center gap-3 mb-4">
                                <div className="w-10 h-10 rounded-full bg-gradient-to-br from-purple-500 to-pink-500 flex items-center justify-center text-white font-bold">
                                    {post.author.username?.charAt(0).toUpperCase() || 'U'}
                                </div>
                                <div className="flex-1">
                                    <div className="flex items-center gap-2">
                                        <Link href={`/users/${post.author.userId}`} className="font-semibold text-[var(--text-primary)] hover:text-[var(--primary)]">
                                            {post.author.displayName || post.author.username}
                                        </Link>
                                        {post.groupName && (
                                            <span className="text-[var(--text-muted)]">
                                                in <Link href={`/groups/${post.groupId}`} className="text-[var(--primary)] hover:underline">{post.groupName}</Link>
                                            </span>
                                        )}
                                    </div>
                                    <p className="text-xs text-[var(--text-muted)]">{formatDate(post.createdAt)}</p>
                                </div>
                                <button className="p-2 rounded-lg hover:bg-[var(--bg-hover)] text-[var(--text-muted)] transition">
                                    <i className="bi bi-three-dots"></i>
                                </button>
                            </div>

                            {/* Post Content */}
                            <p className="text-[var(--text-secondary)] whitespace-pre-wrap mb-4">
                                {post.content}
                            </p>

                            {/* Post Actions */}
                            <div className="flex items-center gap-6 pt-4 border-t border-[var(--border-color)]">
                                <button className="flex items-center gap-2 text-[var(--text-muted)] hover:text-red-400 transition">
                                    <i className="bi bi-heart"></i>
                                    <span className="text-sm">Like</span>
                                </button>
                                <button className="flex items-center gap-2 text-[var(--text-muted)] hover:text-blue-400 transition">
                                    <i className="bi bi-chat"></i>
                                    <span className="text-sm">Comment</span>
                                </button>
                                <button className="flex items-center gap-2 text-[var(--text-muted)] hover:text-green-400 transition">
                                    <i className="bi bi-share"></i>
                                    <span className="text-sm">Share</span>
                                </button>
                                <button className="flex items-center gap-2 text-[var(--text-muted)] hover:text-yellow-400 transition ml-auto">
                                    <i className="bi bi-bookmark"></i>
                                </button>
                            </div>
                        </div>
                    ))}
                </div>
            )}
        </AppLayout>
    );
}
