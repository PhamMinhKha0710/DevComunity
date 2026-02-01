'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import MainLayout from '@/components/MainLayout';
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
        return date.toLocaleDateString('vi-VN', {
            day: '2-digit',
            month: '2-digit',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        });
    };

    if (!isAuthenticated) {
        return (
            <MainLayout>
                <div className="text-center py-20">
                    <i className="bi bi-lock text-6xl text-gray-400 mb-4"></i>
                    <h2 className="text-2xl font-bold text-gray-600 dark:text-gray-300 mb-4">
                        Login Required
                    </h2>
                    <p className="text-gray-500 mb-6">Please login to view your newsfeed</p>
                    <Link href="/login" className="px-6 py-3 bg-orange-500 text-white rounded-lg hover:bg-orange-600 transition">
                        Login Now
                    </Link>
                </div>
            </MainLayout>
        );
    }

    return (
        <MainLayout>
            <div className="max-w-2xl mx-auto">
                {/* Header */}
                <div className="mb-6">
                    <h1 className="text-3xl font-bold text-gray-900 dark:text-white mb-2">
                        <i className="bi bi-newspaper text-orange-500 mr-2"></i>
                        Newsfeed
                    </h1>
                    <p className="text-gray-600 dark:text-gray-400">
                        Updates from friends and groups you follow
                    </p>
                </div>

                {/* Create Post */}
                <div className="bg-white dark:bg-slate-800 rounded-xl shadow-sm p-4 mb-6 border border-gray-200 dark:border-slate-700">
                    <form onSubmit={handleCreatePost}>
                        <textarea
                            value={newPostContent}
                            onChange={(e) => setNewPostContent(e.target.value)}
                            placeholder="What's on your mind? Share with the community..."
                            className="w-full p-3 border border-gray-300 dark:border-slate-600 rounded-lg bg-gray-50 dark:bg-slate-900 text-gray-900 dark:text-white resize-none focus:ring-2 focus:ring-orange-500 focus:border-transparent"
                            rows={3}
                        />
                        <div className="flex justify-end mt-3">
                            <button
                                type="submit"
                                disabled={posting || !newPostContent.trim()}
                                className="px-4 py-2 bg-orange-500 text-white rounded-lg hover:bg-orange-600 disabled:opacity-50 disabled:cursor-not-allowed transition flex items-center gap-2"
                            >
                                {posting ? (
                                    <><i className="bi bi-arrow-clockwise animate-spin"></i> Posting...</>
                                ) : (
                                    <><i className="bi bi-send"></i> Post</>
                                )}
                            </button>
                        </div>
                    </form>
                </div>

                {/* Posts List */}
                {loading ? (
                    <div className="text-center py-10">
                        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-orange-500 mx-auto"></div>
                        <p className="mt-4 text-gray-500">Loading posts...</p>
                    </div>
                ) : posts.length === 0 ? (
                    <div className="text-center py-16 bg-white dark:bg-slate-800 rounded-xl border border-gray-200 dark:border-slate-700">
                        <i className="bi bi-inbox text-6xl text-gray-400 mb-4"></i>
                        <h3 className="text-xl font-semibold text-gray-600 dark:text-gray-300 mb-2">
                            No posts yet
                        </h3>
                        <p className="text-gray-500">
                            Be the first to share something or follow more users!
                        </p>
                    </div>
                ) : (
                    <div className="space-y-4">
                        {posts.map((post) => (
                            <div key={post.postId} className="bg-white dark:bg-slate-800 rounded-xl shadow-sm p-4 border border-gray-200 dark:border-slate-700">
                                {/* Post Header */}
                                <div className="flex items-center gap-3 mb-3">
                                    <div className="w-10 h-10 bg-gradient-to-br from-blue-500 to-purple-500 rounded-full flex items-center justify-center text-white font-bold">
                                        {post.author.username?.charAt(0).toUpperCase() || 'U'}
                                    </div>
                                    <div>
                                        <Link href={`/users/${post.author.userId}`} className="font-semibold text-gray-900 dark:text-white hover:text-orange-500">
                                            {post.author.displayName || post.author.username}
                                        </Link>
                                        {post.groupName && (
                                            <span className="text-sm text-gray-500">
                                                {' '}posted in <Link href={`/groups/${post.groupId}`} className="text-orange-500 hover:underline">{post.groupName}</Link>
                                            </span>
                                        )}
                                        <p className="text-xs text-gray-500">{formatDate(post.createdAt)}</p>
                                    </div>
                                </div>

                                {/* Post Content */}
                                <p className="text-gray-700 dark:text-gray-300 whitespace-pre-wrap">
                                    {post.content}
                                </p>

                                {/* Post Actions */}
                                <div className="flex gap-4 mt-4 pt-3 border-t border-gray-100 dark:border-slate-700">
                                    <button className="flex items-center gap-2 text-gray-500 hover:text-orange-500 transition">
                                        <i className="bi bi-heart"></i> Like
                                    </button>
                                    <button className="flex items-center gap-2 text-gray-500 hover:text-orange-500 transition">
                                        <i className="bi bi-chat"></i> Comment
                                    </button>
                                    <button className="flex items-center gap-2 text-gray-500 hover:text-orange-500 transition">
                                        <i className="bi bi-share"></i> Share
                                    </button>
                                </div>
                            </div>
                        ))}
                    </div>
                )}
            </div>
        </MainLayout>
    );
}
