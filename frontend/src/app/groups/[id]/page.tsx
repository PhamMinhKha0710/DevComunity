'use client';

import { useEffect, useState } from 'react';
import { useParams } from 'next/navigation';
import Link from 'next/link';
import AppLayout from '@/components/AppLayout';
import { useAuth } from '@/lib/contexts/AuthContext';
import apiClient from '@/lib/api/client';

interface Group {
    groupId: number;
    name: string;
    description: string;
    isPrivate: boolean;
    memberCount: number;
    createdAt: string;
    creator: {
        userId: number;
        username: string;
    };
}

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

export default function GroupDetailPage() {
    const { id } = useParams();
    const { isAuthenticated, user } = useAuth();
    const [group, setGroup] = useState<Group | null>(null);
    const [posts, setPosts] = useState<Post[]>([]);
    const [loading, setLoading] = useState(true);
    const [isMember, setIsMember] = useState(false);
    const [newPostContent, setNewPostContent] = useState('');
    const [posting, setPosting] = useState(false);

    useEffect(() => {
        fetchGroupDetails();
    }, [id]);

    useEffect(() => {
        if (isAuthenticated && group) {
            checkMembership();
        }
    }, [isAuthenticated, group]);

    const fetchGroupDetails = async () => {
        try {
            const [groupRes, postsRes] = await Promise.all([
                apiClient.get<Group>(`/Groups/${id}`),
                apiClient.get<{ items: Post[] }>(`/Newsfeed/groups/${id}`) // Correct endpoint for group posts
            ]);
            setGroup(groupRes.data);
            setPosts(postsRes.data.items || []);
        } catch (error) {
            console.error('Error fetching group details:', error);
        } finally {
            setLoading(false);
        }
    };

    const checkMembership = async () => {
        try {
            const response = await apiClient.get<boolean>(`/Groups/${id}/isMember`);
            setIsMember(response.data);
        } catch (error) {
            // Fallback: check my groups list if endpoint doesn't exist
            try {
                const myGroupsRes = await apiClient.get<{ items: Group[] }>('/Groups/my');
                const myGroups = myGroupsRes.data.items || [];
                setIsMember(myGroups.some(g => g.groupId === Number(id)));
            } catch (e) {
                console.error('Error checking membership:', e);
            }
        }
    };

    const handleJoinLeave = async () => {
        try {
            if (isMember) {
                await apiClient.post(`/Groups/${id}/leave`);
                setIsMember(false);
                setGroup(prev => prev ? { ...prev, memberCount: prev.memberCount - 1 } : null);
            } else {
                await apiClient.post(`/Groups/${id}/join`);
                setIsMember(true);
                setGroup(prev => prev ? { ...prev, memberCount: prev.memberCount + 1 } : null);
            }
        } catch (error) {
            console.error('Error changing membership:', error);
        }
    };

    const handleCreatePost = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!newPostContent.trim()) return;

        setPosting(true);
        try {
            const response = await apiClient.post<Post>('/Newsfeed/posts', {
                content: newPostContent,
                groupId: Number(id)
            });
            setPosts([response.data, ...posts]);
            setNewPostContent('');
        } catch (error) {
            console.error('Error creating post:', error);
        } finally {
            setPosting(false);
        }
    };

    const formatDate = (dateString: string) => {
        return new Date(dateString).toLocaleDateString(undefined, {
            year: 'numeric',
            month: 'long',
            day: 'numeric'
        });
    };

    if (loading) {
        return (
            <AppLayout>
                <div className="flex items-center justify-center py-20">
                    <div className="w-10 h-10 border-4 border-[var(--primary)]/30 border-t-[var(--primary)] rounded-full animate-spin"></div>
                </div>
            </AppLayout>
        );
    }

    if (!group) {
        return (
            <AppLayout>
                <div className="text-center py-20">
                    <h2 className="text-2xl font-bold text-[var(--text-primary)] mb-4">Group not found</h2>
                    <Link href="/groups" className="text-[var(--primary)] hover:underline">
                        ← Back to groups
                    </Link>
                </div>
            </AppLayout>
        );
    }

    return (
        <AppLayout>
            {/* Header / Hero */}
            <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-2xl overflow-hidden mb-6">
                <div className="h-32 bg-gradient-to-r from-blue-600 to-cyan-500 relative">
                    <div className="absolute -bottom-12 left-8">
                        <div className="w-24 h-24 bg-[var(--bg-secondary)] rounded-2xl p-2">
                            <div className="w-full h-full interval bg-gradient-to-br from-blue-500 to-purple-500 rounded-xl flex items-center justify-center text-white text-3xl font-bold">
                                {group.name.charAt(0).toUpperCase()}
                            </div>
                        </div>
                    </div>
                </div>
                <div className="pt-16 pb-6 px-8">
                    <div className="flex justify-between items-start">
                        <div>
                            <h1 className="text-2xl font-bold text-[var(--text-primary)] mb-2 flex items-center gap-2">
                                {group.name}
                                {group.isPrivate && <i className="bi bi-lock-fill text-[var(--text-muted)] text-base"></i>}
                            </h1>
                            <p className="text-[var(--text-secondary)] max-w-2xl mb-4">
                                {group.description || 'No description available.'}
                            </p>
                            <div className="flex items-center gap-4 text-sm text-[var(--text-muted)]">
                                <span className="flex items-center gap-1">
                                    <i className="bi bi-people-fill"></i>
                                    {group.memberCount} members
                                </span>
                                <span className="flex items-center gap-1">
                                    <i className="bi bi-calendar3"></i>
                                    Created {formatDate(group.createdAt)}
                                </span>
                            </div>
                        </div>
                        {isAuthenticated && (
                            <button
                                onClick={handleJoinLeave}
                                className={`px-6 py-2.5 rounded-xl font-medium transition flex items-center gap-2 ${isMember
                                    ? 'bg-[var(--bg-tertiary)] text-[var(--text-muted)] hover:bg-red-500/10 hover:text-red-500 border border-[var(--border-color)]'
                                    : 'bg-[var(--primary)] text-white hover:bg-[var(--primary-dark)]'
                                    }`}
                            >
                                {isMember ? (
                                    <><i className="bi bi-box-arrow-right"></i> Leave Group</>
                                ) : (
                                    <><i className="bi bi-person-plus-fill"></i> Join Group</>
                                )}
                            </button>
                        )}
                    </div>
                </div>
            </div>

            <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
                {/* Main Content (Feed) */}
                <div className="lg:col-span-2 space-y-6">
                    {isAuthenticated && isMember && (
                        <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-2xl p-5">
                            <form onSubmit={handleCreatePost}>
                                <div className="flex gap-4">
                                    <div className="w-10 h-10 rounded-full bg-gradient-to-br from-purple-500 to-pink-500 flex items-center justify-center text-white font-bold shrink-0">
                                        {user?.username?.charAt(0).toUpperCase()}
                                    </div>
                                    <div className="flex-1">
                                        <textarea
                                            value={newPostContent}
                                            onChange={(e) => setNewPostContent(e.target.value)}
                                            placeholder={`Share something with ${group.name}...`}
                                            className="w-full p-4 bg-[var(--bg-tertiary)] border border-[var(--border-color)] rounded-xl text-[var(--text-primary)] placeholder-[var(--text-muted)] resize-none focus:border-[var(--primary)] focus:ring-2 focus:ring-[var(--primary)]/20 transition"
                                            rows={2}
                                        />
                                        <div className="flex justify-end mt-3">
                                            <button
                                                type="submit"
                                                disabled={posting || !newPostContent.trim()}
                                                className="px-5 py-2 bg-[var(--primary)] text-white rounded-xl font-medium hover:bg-[var(--primary-dark)] disabled:opacity-50 disabled:cursor-not-allowed transition flex items-center gap-2"
                                            >
                                                {posting ? 'Posting...' : 'Post'}
                                            </button>
                                        </div>
                                    </div>
                                </div>
                            </form>
                        </div>
                    )}

                    {posts.length > 0 ? (
                        <div className="space-y-4">
                            {posts.map((post) => (
                                <div key={post.postId} className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-2xl p-5">
                                    <div className="flex items-center gap-3 mb-4">
                                        <div className="w-10 h-10 rounded-full bg-gradient-to-br from-blue-500 to-indigo-500 flex items-center justify-center text-white font-bold">
                                            {post.author.username?.charAt(0).toUpperCase()}
                                        </div>
                                        <div>
                                            <Link href={`/users/${post.author.userId}`} className="font-semibold text-[var(--text-primary)] hover:text-[var(--primary)]">
                                                {post.author.displayName || post.author.username}
                                            </Link>
                                            <p className="text-xs text-[var(--text-muted)]">{new Date(post.createdAt).toLocaleDateString()}</p>
                                        </div>
                                    </div>
                                    <p className="text-[var(--text-secondary)] whitespace-pre-wrap mb-4">
                                        {post.content}
                                    </p>
                                    <div className="flex items-center gap-4 text-[var(--text-muted)] pt-4 border-t border-[var(--border-color)]">
                                        <button className="flex items-center gap-2 hover:text-[var(--primary)] transition">
                                            <i className="bi bi-heart"></i> Like
                                        </button>
                                        <button className="flex items-center gap-2 hover:text-[var(--primary)] transition">
                                            <i className="bi bi-chat"></i> Comment
                                        </button>
                                    </div>
                                </div>
                            ))}
                        </div>
                    ) : (
                        <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-2xl p-12 text-center">
                            <i className="bi bi-chat-square-text text-5xl text-[var(--text-muted)] mb-4"></i>
                            <h3 className="text-lg font-semibold text-[var(--text-primary)] mb-2">No posts yet</h3>
                            <p className="text-[var(--text-muted)]">Be the first to share something in this group!</p>
                        </div>
                    )}
                </div>

                {/* Sidebar (Right) */}
                <div className="space-y-6">
                    <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-2xl p-5">
                        <h3 className="font-bold text-[var(--text-primary)] mb-4">About</h3>
                        <div className="space-y-3 text-sm text-[var(--text-muted)]">
                            <div className="flex items-center gap-2">
                                <i className="bi bi-globe"></i>
                                <span>Public Group</span>
                            </div>
                            <div className="flex items-center gap-2">
                                <i className="bi bi-clock-history"></i>
                                <span>Active today</span>
                            </div>
                        </div>
                    </div>

                    <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-2xl p-5">
                        <h3 className="font-bold text-[var(--text-primary)] mb-4">Admins</h3>
                        <div className="flex items-center gap-3">
                            <div className="w-8 h-8 rounded-full bg-gradient-to-br from-yellow-500 to-orange-500 flex items-center justify-center text-white text-xs font-bold">
                                A
                            </div>
                            <span className="text-sm font-medium text-[var(--text-primary)]">Admin User</span>
                        </div>
                    </div>
                </div>
            </div>
        </AppLayout>
    );
}
