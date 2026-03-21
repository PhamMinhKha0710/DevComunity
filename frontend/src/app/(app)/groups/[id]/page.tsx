'use client';

import { useEffect, useState, useRef } from 'react';
import { useParams } from 'next/navigation';
import Link from 'next/link';
import { useQueryClient } from '@tanstack/react-query';
import AppLayout from '@/components/AppLayout';
import { useAuth } from '@/lib/contexts/AuthContext';
import apiClient from '@/lib/api/client';
import RelativeTime from '@/components/RelativeTime';
import { authorInitial } from '@/lib/utils';

interface Group {
    groupId: number;
    name: string;
    description: string;
    isPrivate: boolean;
    memberCount: number;
    createdAt: string;
    isMember?: boolean;
    creator: {
        userId: number;
        username: string;
        displayName?: string;
        profilePicture?: string | null;
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
    const params = useParams();
    const rawId = params?.id;
    const id = Array.isArray(rawId) ? rawId[0] : rawId;
    const { isAuthenticated, user, isLoading: authLoading } = useAuth();
    const queryClient = useQueryClient();
    const [group, setGroup] = useState<Group | null>(null);
    const [posts, setPosts] = useState<Post[]>([]);
    const [loading, setLoading] = useState(true);
    const [isMember, setIsMember] = useState(false);
    const [newPostContent, setNewPostContent] = useState('');
    const [posting, setPosting] = useState(false);
    const [joinError, setJoinError] = useState<string | null>(null);
    const membershipCheckRef = useRef<AbortController | null>(null);
    const membershipCheckVersion = useRef(0);
    const prevAuthLoading = useRef(true);

    useEffect(() => {
        if (id === undefined || id === '') return;
        void fetchGroup();
    }, [id]);

    useEffect(() => {
        if (id === undefined || id === '') return;
        if (!isAuthenticated || authLoading) return;
        void fetchGroupPosts();
    }, [id, isAuthenticated, authLoading]);

    useEffect(() => {
        if (isAuthenticated && group) {
            checkMembership();
        }
    }, [isAuthenticated, group?.groupId]);

    useEffect(() => {
        if (!group || !user) return;
        if (group.creator.userId === user.userId) {
            setIsMember(true);
        }
    }, [group, user]);

    useEffect(() => {
        if (prevAuthLoading.current && !authLoading && isAuthenticated) {
            prevAuthLoading.current = false;
            void fetchGroup();
        } else if (!authLoading) {
            prevAuthLoading.current = false;
        }
    }, [authLoading, isAuthenticated]);

    const fetchGroup = async () => {
        try {
            const groupRes = await apiClient.get<Group>(`/Groups/${id}`);
            const groupData = groupRes.data;
            setGroup(groupData);
            const isCreator = user && groupData.creator.userId === user.userId;
            const fromApi = groupData.isMember === true;
            setIsMember(fromApi || !!isCreator);
        } catch (error) {
            console.error('Error fetching group:', error);
        } finally {
            setLoading(false);
        }
    };

    const fetchGroupPosts = async () => {
        try {
            const postsRes = await apiClient.get<{ items: Post[] }>(`/Newsfeed/groups/${id}`);
            setPosts(postsRes.data.items || []);
        } catch (error) {
            console.error('Error fetching group posts:', error);
            setPosts([]);
        }
    };

    const checkMembership = async () => {
        if (!group) return;
        if (user && group.creator.userId === user.userId) {
            setIsMember(true);
            return;
        }

        if (membershipCheckRef.current) {
            membershipCheckRef.current.abort();
        }
        const controller = new AbortController();
        membershipCheckRef.current = controller;
        const version = ++membershipCheckVersion.current;

        try {
            const response = await apiClient.get<boolean>(`/Groups/${id}/isMember`, {
                signal: controller.signal,
            });
            if (version !== membershipCheckVersion.current) return;
            setIsMember(response.data);
        } catch (error) {
            if ((error as { name?: string }).name === 'CanceledError') return;
            try {
                const myGroupsRes = await apiClient.get<Group[] | { items: Group[] }>('/Groups/my');
                if (version !== membershipCheckVersion.current) return;
                const data = myGroupsRes.data;
                const myGroups = Array.isArray(data) ? data : (data?.items ?? []);
                setIsMember(myGroups.some(g => g.groupId === Number(id)));
            } catch (e) {
                if (version !== membershipCheckVersion.current) return;
                if (group.isMember === true) setIsMember(true);
            }
        }
    };

    const handleJoinLeave = async () => {
        if (!group) return;
        if (user && group.creator.userId === user.userId) {
            setIsMember(true);
            return;
        }
        setJoinError(null);

        if (membershipCheckRef.current) {
            membershipCheckRef.current.abort();
        }
        ++membershipCheckVersion.current;

        try {
            if (isMember) {
                await apiClient.post(`/Groups/${id}/leave`);
                setIsMember(false);
                setGroup(prev =>
                    prev
                        ? { ...prev, memberCount: Math.max(0, prev.memberCount - 1), isMember: false }
                        : null
                );
                await queryClient.invalidateQueries({ queryKey: ['newsfeed'] });
            } else {
                await apiClient.post(`/Groups/${id}/join`);
                setIsMember(true);
                setGroup(prev =>
                    prev
                        ? { ...prev, memberCount: prev.memberCount + 1, isMember: true }
                        : null
                );
                await queryClient.invalidateQueries({ queryKey: ['newsfeed'] });
            }
        } catch (error: unknown) {
            const err = error as { response?: { data?: { message?: string } } };
            const message = err.response?.data?.message || 'Failed to change membership';
            setJoinError(message);
            try {
                await checkMembership();
            } catch (e) {
            }
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
            await queryClient.invalidateQueries({ queryKey: ['newsfeed'] });
        } catch (error) {
            console.error('Error creating post:', error);
        } finally {
            setPosting(false);
        }
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
                <div className="h-32 sm:h-40 bg-[var(--bg-tertiary)] relative border-b border-[var(--border-color)]">
                    <div className="absolute -bottom-10 sm:-bottom-12 left-4 sm:left-8">
                        <div className="w-20 h-20 sm:w-24 sm:h-24 bg-[var(--bg-secondary)] rounded-2xl p-1.5 sm:p-2 shadow-sm border border-[var(--border-color)]">
                            <div className="w-full h-full bg-[var(--primary)] rounded-xl flex items-center justify-center text-white text-2xl sm:text-3xl font-bold">
                                {authorInitial(group.name)}
                            </div>
                        </div>
                    </div>
                </div>
                <div className="pt-14 sm:pt-16 pb-6 px-4 sm:px-8">
                    <div className="flex flex-col sm:flex-row justify-between items-start gap-4">
                        <div>
                            <h1 className="text-2xl font-bold text-[var(--text-primary)] mb-2 flex items-center gap-2">
                                {group.name}
                                {group.isPrivate && <span className="material-symbols-outlined text-[var(--text-muted)] text-base">lock</span>}
                            </h1>
                            <p className="text-[var(--text-secondary)] max-w-2xl mb-4">
                                {group.description || 'No description available.'}
                            </p>
                            <div className="flex items-center gap-4 text-sm text-[var(--text-muted)]">
                                <span className="flex items-center gap-1">
                                    <span className="material-symbols-outlined">group</span>
                                    {group.memberCount} members
                                </span>
                                <span className="flex items-center gap-1">
                                    <span className="material-symbols-outlined">calendar_today</span>
                                    <RelativeTime value={group.createdAt} prefix="Created " />
                                </span>
                            </div>
                        </div>
                        {isAuthenticated && (
                            <>
                                <button
                                    onClick={handleJoinLeave}
                                    className={`px-6 py-2.5 rounded-xl font-medium transition flex items-center gap-2 ${isMember
                                        ? 'bg-[var(--bg-tertiary)] text-[var(--text-muted)] hover:bg-red-500/10 hover:text-red-500 border border-[var(--border-color)]'
                                        : 'bg-[var(--primary)] text-white hover:bg-[var(--primary-dark)]'
                                        }`}
                                >
                                    {isMember ? (
                                        <><span className="material-symbols-outlined">logout</span> Leave Group</>
                                    ) : (
                                        <><span className="material-symbols-outlined">person_add</span> Join Group</>
                                    )}
                                </button>
                                {joinError && (
                                    <p className="text-red-500 text-sm mt-2">{joinError}</p>
                                )}
                            </>
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
                                    {user?.profilePicture ? (
                                        <img
                                            src={user.profilePicture}
                                            alt={user.displayName || user.username}
                                            className="w-10 h-10 rounded-full object-cover shrink-0"
                                        />
                                    ) : (
                                        <div className="w-10 h-10 rounded-full bg-gradient-to-br from-purple-500 to-pink-500 flex items-center justify-center text-white font-bold shrink-0">
                                            {authorInitial(user?.displayName || user?.username)}
                                        </div>
                                    )}
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
                                        {post.author.profilePicture ? (
                                            <img
                                                src={post.author.profilePicture}
                                                alt={post.author.displayName || post.author.username}
                                                className="w-10 h-10 rounded-full object-cover"
                                            />
                                        ) : (
                                            <div className="w-10 h-10 rounded-full bg-gradient-to-br from-blue-500 to-indigo-500 flex items-center justify-center text-white font-bold">
                                                {authorInitial(post.author.displayName || post.author.username)}
                                            </div>
                                        )}
                                        <div>
                                            <Link href={`/users/${post.author.userId}`} className="font-semibold text-[var(--text-primary)] hover:text-[var(--primary)]">
                                                {post.author.displayName || post.author.username}
                                            </Link>
                                            <RelativeTime
                                                value={post.createdAt}
                                                className="text-xs text-[var(--text-muted)]"
                                            />
                                        </div>
                                    </div>
                                    <p className="text-[var(--text-secondary)] whitespace-pre-wrap mb-4">
                                        {post.content}
                                    </p>
                                    <div className="flex items-center gap-4 text-[var(--text-muted)] pt-4 border-t border-[var(--border-color)]">
                                        <button className="flex items-center gap-2 hover:text-[var(--primary)] transition">
                                            <span className="material-symbols-outlined">favorite</span> Like
                                        </button>
                                        <button className="flex items-center gap-2 hover:text-[var(--primary)] transition">
                                            <span className="material-symbols-outlined">chat</span> Comment
                                        </button>
                                    </div>
                                </div>
                            ))}
                        </div>
                    ) : (
                        <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-2xl p-12 text-center">
                            <span className="material-symbols-outlined text-5xl text-[var(--text-muted)] mb-4">chat</span>
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
                                <span className="material-symbols-outlined">public</span>
                                <span>Public Group</span>
                            </div>
                            <div className="flex items-center gap-2">
                                <span className="material-symbols-outlined">history</span>
                                <span>Active today</span>
                            </div>
                        </div>
                    </div>

                    <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-2xl p-5">
                        <h3 className="font-bold text-[var(--text-primary)] mb-4">Admins</h3>
                        <div className="flex items-center gap-3">
                            {group.creator.profilePicture ? (
                                <img
                                    src={group.creator.profilePicture}
                                    alt={group.creator.displayName || group.creator.username}
                                    className="w-8 h-8 rounded-full object-cover"
                                />
                            ) : (
                                <div className="w-8 h-8 rounded-full bg-gradient-to-br from-blue-500 to-indigo-500 flex items-center justify-center text-white text-xs font-bold">
                                    {authorInitial(group.creator.displayName || group.creator.username)}
                                </div>
                            )}
                            <Link
                                href={`/users/${group.creator.userId}`}
                                className="text-sm font-medium text-[var(--text-primary)] hover:text-[var(--primary)]"
                            >
                                {group.creator.displayName || group.creator.username}
                            </Link>
                        </div>
                    </div>
                </div>
            </div>
        </AppLayout>
    );
}
