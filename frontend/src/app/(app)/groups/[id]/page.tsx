'use client';

import { useEffect, useState, useRef } from 'react';
import { useParams, useRouter } from 'next/navigation';
import Link from 'next/link';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import AppLayout from '@/components/AppLayout';
import { useAuth } from '@/lib/contexts/AuthContext';
import apiClient from '@/lib/api/client';
import { groupsApi } from '@/lib/api/groups.api';
import { newsfeedApi } from '@/lib/api/newsfeed.api';
import { votesApi } from '@/lib/api/votes.api';
import { commentsApi } from '@/lib/api/comments.api';
import RelativeTime from '@/components/RelativeTime';
import { authorInitial } from '@/lib/utils';

interface Group {
    groupId: number;
    name: string;
    description: string | null;
    isPrivate: boolean;
    memberCount: number;
    createdAt: string;
    isMember?: boolean;
    currentUserRole?: string | null;
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
    likeCount?: number;
    commentCount?: number;
    userLiked?: boolean;
}

interface PostComment {
    commentId: number;
    body: string;
    createdDate: string;
    userId: number;
    authorUsername: string;
    authorProfilePicture: string | null;
}

export default function GroupDetailPage() {
    const params = useParams();
    const router = useRouter();
    const rawId = params?.id;
    const id = Array.isArray(rawId) ? rawId[0] : rawId;
    const { isAuthenticated, user, isLoading: authLoading } = useAuth();
    const queryClient = useQueryClient();
    const [group, setGroup] = useState<Group | null>(null);
    const [loading, setLoading] = useState(true);
    const [isMember, setIsMember] = useState(false);
    const [newPostContent, setNewPostContent] = useState('');
    const [posting, setPosting] = useState(false);
    const [joinError, setJoinError] = useState<string | null>(null);
    const [editOpen, setEditOpen] = useState(false);
    const [editName, setEditName] = useState('');
    const [editDescription, setEditDescription] = useState('');
    const [editIsPrivate, setEditIsPrivate] = useState(false);
    const [savingGroup, setSavingGroup] = useState(false);
    const [deleteOpen, setDeleteOpen] = useState(false);
    const [deletingGroup, setDeletingGroup] = useState(false);
    const [commentOpenPostId, setCommentOpenPostId] = useState<number | null>(null);
    const [commentTextByPostId, setCommentTextByPostId] = useState<Record<number, string>>({});
    const membershipCheckRef = useRef<AbortController | null>(null);
    const membershipCheckVersion = useRef(0);
    const prevAuthLoading = useRef(true);

    useEffect(() => {
        if (id === undefined || id === '') return;
        void fetchGroup();
    }, [id]);

    const { data: postsData, isLoading: postsLoading } = useQuery({
        queryKey: ['groupPosts', id],
        queryFn: () => newsfeedApi.getGroupFeed(id!),
        enabled: Boolean(id) && isAuthenticated && !authLoading,
    });
    const posts: Post[] = postsData?.items ?? [];

    const { data: postComments = [], isLoading: commentsLoading } = useQuery({
        queryKey: ['comments', 'post', commentOpenPostId],
        queryFn: () => commentsApi.listByPost(commentOpenPostId!),
        enabled: isAuthenticated && commentOpenPostId != null,
    });

    const likePostMutation = useMutation({
        mutationFn: ({ postId, isLiked }: { postId: number; isLiked: boolean }) =>
            isLiked ? votesApi.removePostVote(postId) : votesApi.votePost(postId, { voteType: 'up' }),
        onMutate: async ({ postId, isLiked }) => {
            await queryClient.cancelQueries({ queryKey: ['groupPosts', id] });
            const prev = queryClient.getQueryData(['groupPosts', id]);
            queryClient.setQueryData(['groupPosts', id], (old: { items?: Post[] } | undefined) => {
                if (!old?.items) return old;
                return {
                    ...old,
                    items: old.items.map((p) =>
                        p.postId === postId
                            ? {
                                  ...p,
                                  userLiked: !isLiked,
                                  likeCount: (p.likeCount ?? 0) + (isLiked ? -1 : 1),
                              }
                            : p
                    ),
                };
            });
            return { prev };
        },
        onError: (_err, _vars, context) => {
            if (context?.prev) queryClient.setQueryData(['groupPosts', id], context.prev);
            toast.error('Không thể cập nhật thích');
        },
        onSettled: () => {
            queryClient.invalidateQueries({ queryKey: ['groupPosts', id] });
        },
    });

    const commentMutation = useMutation({
        mutationFn: ({ postId, body }: { postId: number; body: string }) =>
            commentsApi.addToPost(postId, body),
        onSuccess: (data: PostComment, { postId }) => {
            queryClient.setQueryData(['comments', 'post', postId], (old: PostComment[] | undefined) => {
                if (!old?.length) return [data];
                if (old.some((c) => c.commentId === data.commentId)) return old;
                return [...old, data];
            });
            queryClient.invalidateQueries({ queryKey: ['comments', 'post', postId] });
            queryClient.invalidateQueries({ queryKey: ['groupPosts', id] });
        },
        onError: () => {
            toast.error('Không thể gửi bình luận');
        },
    });

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
            await newsfeedApi.createPost({
                content: newPostContent,
                groupId: Number(id),
            });
            setNewPostContent('');
            await queryClient.invalidateQueries({ queryKey: ['newsfeed'] });
            await queryClient.invalidateQueries({ queryKey: ['groupPosts', id] });
        } catch (error) {
            console.error('Error creating post:', error);
            toast.error('Không thể đăng bài');
        } finally {
            setPosting(false);
        }
    };

    const openEditModal = () => {
        if (!group) return;
        setEditName(group.name);
        setEditDescription(group.description ?? '');
        setEditIsPrivate(group.isPrivate);
        setEditOpen(true);
    };

    const handleSaveGroup = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!group || id === undefined || id === '') return;
        const name = editName.trim();
        if (!name) {
            toast.error('Tên nhóm không được để trống');
            return;
        }
        setSavingGroup(true);
        try {
            const updated = await groupsApi.update(id, {
                name,
                description: editDescription.trim() || null,
                isPrivate: editIsPrivate,
            });
            setGroup((prev) =>
                prev
                    ? {
                          ...prev,
                          name: updated.name,
                          description: updated.description ?? null,
                          isPrivate: updated.isPrivate,
                          memberCount: updated.memberCount ?? prev.memberCount,
                          currentUserRole: updated.currentUserRole ?? prev.currentUserRole,
                      }
                    : null
            );
            setEditOpen(false);
            toast.success('Đã cập nhật nhóm');
            await queryClient.invalidateQueries({ queryKey: ['groupPosts', id] });
        } catch (err: unknown) {
            const status = (err as { response?: { status?: number } })?.response?.status;
            toast.error(
                status === 403 ? 'Bạn không có quyền chỉnh sửa nhóm này' : 'Không thể cập nhật nhóm'
            );
        } finally {
            setSavingGroup(false);
        }
    };

    const handleDeleteGroup = async () => {
        if (id === undefined || id === '') return;
        setDeletingGroup(true);
        try {
            await groupsApi.delete(id);
            toast.success('Đã xóa nhóm');
            router.push('/groups');
        } catch (err: unknown) {
            const status = (err as { response?: { status?: number } })?.response?.status;
            toast.error(
                status === 403 ? 'Chỉ chủ nhóm mới có thể xóa nhóm' : 'Không thể xóa nhóm'
            );
        } finally {
            setDeletingGroup(false);
            setDeleteOpen(false);
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

    const isCreator = Boolean(user && group.creator.userId === user.userId);
    const isAdmin = (group.currentUserRole ?? '').toLowerCase() === 'admin';
    const canEditGroup = isAuthenticated && (isAdmin || isCreator);
    const canDeleteGroup = isAuthenticated && isCreator;
    const showLeaveGroup = isAuthenticated && isMember && !isCreator;

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
                            <div className="flex flex-col items-stretch sm:items-end gap-2 w-full sm:w-auto">
                                <div className="flex flex-wrap gap-2 justify-end">
                                    {canEditGroup && (
                                        <button
                                            type="button"
                                            onClick={openEditModal}
                                            className="px-4 py-2.5 rounded-xl font-medium transition flex items-center gap-2 bg-[var(--bg-tertiary)] text-[var(--text-primary)] border border-[var(--border-color)] hover:border-[var(--primary)] hover:text-[var(--primary)]"
                                        >
                                            <span className="material-symbols-outlined text-xl">edit</span>
                                            Chỉnh sửa nhóm
                                        </button>
                                    )}
                                    {canDeleteGroup && (
                                        <button
                                            type="button"
                                            onClick={() => setDeleteOpen(true)}
                                            className="px-4 py-2.5 rounded-xl font-medium transition flex items-center gap-2 bg-red-500/10 text-red-600 dark:text-red-400 border border-red-500/30 hover:bg-red-500/20"
                                        >
                                            <span className="material-symbols-outlined text-xl">delete</span>
                                            Xóa nhóm
                                        </button>
                                    )}
                                    {(!isMember || showLeaveGroup) && (
                                        <button
                                            type="button"
                                            onClick={handleJoinLeave}
                                            className={`px-6 py-2.5 rounded-xl font-medium transition flex items-center gap-2 ${
                                                isMember
                                                    ? 'bg-[var(--bg-tertiary)] text-[var(--text-muted)] hover:bg-red-500/10 hover:text-red-500 border border-[var(--border-color)]'
                                                    : 'bg-[var(--primary)] text-white hover:bg-[var(--primary-dark)]'
                                            }`}
                                        >
                                            {isMember ? (
                                                <>
                                                    <span className="material-symbols-outlined">logout</span> Rời nhóm
                                                </>
                                            ) : (
                                                <>
                                                    <span className="material-symbols-outlined">person_add</span> Tham gia
                                                </>
                                            )}
                                        </button>
                                    )}
                                </div>
                                {isCreator && isMember && (
                                    <p className="text-xs text-[var(--text-muted)] max-w-xs text-right">
                                        Chủ nhóm không thể rời nhóm; hãy xóa nhóm hoặc chuyển quyền (tính năng sắp có).
                                    </p>
                                )}
                                {joinError && <p className="text-red-500 text-sm text-right">{joinError}</p>}
                            </div>
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

                    {isAuthenticated && postsLoading ? (
                        <div className="flex justify-center py-12">
                            <div className="w-10 h-10 border-4 border-[var(--primary)]/30 border-t-[var(--primary)] rounded-full animate-spin" />
                        </div>
                    ) : posts.length > 0 ? (
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
                                    <p className="text-[var(--text-secondary)] whitespace-pre-wrap mb-3">
                                        {post.content}
                                    </p>
                                    {(post.likeCount ?? 0) > 0 && (
                                        <div className="flex items-center gap-1.5 text-sm text-[var(--text-muted)] mb-2">
                                            <span className="w-5 h-5 rounded-full bg-[var(--primary)] flex items-center justify-center">
                                                <span className="material-symbols-outlined text-white text-xs">thumb_up</span>
                                            </span>
                                            {post.likeCount}
                                        </div>
                                    )}
                                    <div className="flex items-center gap-1 pt-3 border-t border-[var(--border-color)]">
                                        <button
                                            type="button"
                                            onClick={() => {
                                                if (likePostMutation.isPending || !user) return;
                                                likePostMutation.mutate({
                                                    postId: post.postId,
                                                    isLiked: post.userLiked ?? false,
                                                });
                                            }}
                                            disabled={likePostMutation.isPending || !user}
                                            className={`flex-1 flex items-center justify-center gap-2 py-2.5 rounded-lg font-medium text-sm transition hover:bg-[var(--bg-tertiary)] disabled:opacity-60 ${
                                                post.userLiked ? 'text-[var(--primary)]' : 'text-[var(--text-muted)]'
                                            }`}
                                        >
                                            <span className="material-symbols-outlined text-xl">
                                                {post.userLiked ? 'thumb_up' : 'thumb_up_off_alt'}
                                            </span>
                                            Thích
                                            {(post.likeCount ?? 0) > 0 && (
                                                <span className="text-xs">({post.likeCount})</span>
                                            )}
                                        </button>
                                        <button
                                            type="button"
                                            onClick={() =>
                                                setCommentOpenPostId(
                                                    commentOpenPostId === post.postId ? null : post.postId
                                                )
                                            }
                                            className={`flex-1 flex items-center justify-center gap-2 py-2.5 rounded-lg font-medium text-sm transition hover:bg-[var(--bg-tertiary)] ${
                                                commentOpenPostId === post.postId
                                                    ? 'text-[var(--primary)]'
                                                    : 'text-[var(--text-muted)]'
                                            }`}
                                        >
                                            <span className="material-symbols-outlined text-xl">chat</span>
                                            Bình luận
                                            {(post.commentCount ?? 0) > 0 && (
                                                <span className="text-xs">({post.commentCount})</span>
                                            )}
                                        </button>
                                    </div>
                                    {commentOpenPostId === post.postId && (
                                        <div className="mt-4 pt-4 border-t border-[var(--border-color)] space-y-3">
                                            {commentsLoading ? (
                                                <p className="text-sm text-[var(--text-muted)]">Đang tải bình luận...</p>
                                            ) : (postComments as PostComment[]).length > 0 ? (
                                                <ul className="space-y-3">
                                                    {(postComments as PostComment[]).map((c) => (
                                                        <li key={c.commentId} className="flex gap-2.5">
                                                            {c.authorProfilePicture ? (
                                                                <img
                                                                    src={c.authorProfilePicture}
                                                                    alt=""
                                                                    className="w-7 h-7 rounded-full object-cover shrink-0 mt-0.5"
                                                                />
                                                            ) : (
                                                                <div className="w-7 h-7 rounded-full bg-gradient-to-br from-blue-500 to-cyan-500 shrink-0 flex items-center justify-center text-white text-xs font-bold mt-0.5">
                                                                    {authorInitial(c.authorUsername)}
                                                                </div>
                                                            )}
                                                            <div className="flex-1 min-w-0">
                                                                <p className="text-sm text-[var(--text-primary)]">
                                                                    <span className="font-medium">{c.authorUsername}</span>{' '}
                                                                    <span className="text-[var(--text-muted)] font-normal">
                                                                        <RelativeTime value={c.createdDate} />
                                                                    </span>
                                                                </p>
                                                                <p className="text-sm text-[var(--text-secondary)] mt-0.5 break-words">
                                                                    {c.body}
                                                                </p>
                                                            </div>
                                                        </li>
                                                    ))}
                                                </ul>
                                            ) : (
                                                <p className="text-sm text-[var(--text-muted)]">Chưa có bình luận.</p>
                                            )}
                                            {user && (
                                                <div className="flex gap-2.5">
                                                    {user.profilePicture ? (
                                                        <img
                                                            src={user.profilePicture}
                                                            alt=""
                                                            className="w-7 h-7 rounded-full object-cover shrink-0 mt-1"
                                                        />
                                                    ) : (
                                                        <div className="w-7 h-7 rounded-full bg-gradient-to-br from-purple-500 to-pink-500 shrink-0 flex items-center justify-center text-white text-xs font-bold mt-1">
                                                            {authorInitial(user.displayName || user.username)}
                                                        </div>
                                                    )}
                                                    <div className="flex-1">
                                                        <textarea
                                                            value={commentTextByPostId[post.postId] ?? ''}
                                                            onChange={(e) =>
                                                                setCommentTextByPostId((prev) => ({
                                                                    ...prev,
                                                                    [post.postId]: e.target.value,
                                                                }))
                                                            }
                                                            rows={2}
                                                            className="w-full px-3 py-2 bg-[var(--bg-tertiary)] border border-[var(--border-color)] rounded-xl text-sm text-[var(--text-primary)] placeholder-[var(--text-muted)] focus:border-[var(--primary)] focus:ring-2 focus:ring-[var(--primary)]/20 transition resize-none"
                                                            placeholder="Viết bình luận..."
                                                        />
                                                        <div className="flex justify-end mt-1.5">
                                                            <button
                                                                type="button"
                                                                onClick={() => {
                                                                    const text =
                                                                        commentTextByPostId[post.postId]?.trim();
                                                                    if (!text || commentMutation.isPending) return;
                                                                    commentMutation.mutate({
                                                                        postId: post.postId,
                                                                        body: text,
                                                                    });
                                                                    setCommentTextByPostId((prev) => ({
                                                                        ...prev,
                                                                        [post.postId]: '',
                                                                    }));
                                                                }}
                                                                disabled={
                                                                    !commentTextByPostId[post.postId]?.trim() ||
                                                                    commentMutation.isPending
                                                                }
                                                                className="px-3 py-1.5 text-xs font-medium bg-[var(--primary)] text-white rounded-lg hover:opacity-90 transition disabled:opacity-50"
                                                            >
                                                                {commentMutation.isPending ? 'Đang gửi...' : 'Gửi'}
                                                            </button>
                                                        </div>
                                                    </div>
                                                </div>
                                            )}
                                        </div>
                                    )}
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
                                <span className="material-symbols-outlined">
                                    {group.isPrivate ? 'lock' : 'public'}
                                </span>
                                <span>{group.isPrivate ? 'Nhóm riêng tư' : 'Nhóm công khai'}</span>
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

            {editOpen && (
                <div
                    role="presentation"
                    className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50"
                    onClick={() => !savingGroup && setEditOpen(false)}
                >
                    <div
                        role="dialog"
                        aria-modal="true"
                        aria-labelledby="edit-group-title"
                        className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-2xl p-6 max-w-md w-full shadow-xl"
                        onClick={(e) => e.stopPropagation()}
                    >
                        <h3
                            id="edit-group-title"
                            className="text-lg font-bold text-[var(--text-primary)] mb-4"
                        >
                            Chỉnh sửa nhóm
                        </h3>
                        <form onSubmit={handleSaveGroup} className="space-y-4">
                            <div>
                                <label
                                    htmlFor="edit-group-name"
                                    className="block text-sm font-medium text-[var(--text-secondary)] mb-1"
                                >
                                    Tên nhóm
                                </label>
                                <input
                                    id="edit-group-name"
                                    value={editName}
                                    onChange={(e) => setEditName(e.target.value)}
                                    className="w-full px-3 py-2.5 rounded-xl border border-[var(--border-color)] bg-[var(--bg-tertiary)] text-[var(--text-primary)]"
                                    required
                                />
                            </div>
                            <div>
                                <label
                                    htmlFor="edit-group-desc"
                                    className="block text-sm font-medium text-[var(--text-secondary)] mb-1"
                                >
                                    Mô tả
                                </label>
                                <textarea
                                    id="edit-group-desc"
                                    value={editDescription}
                                    onChange={(e) => setEditDescription(e.target.value)}
                                    rows={3}
                                    className="w-full px-3 py-2.5 rounded-xl border border-[var(--border-color)] bg-[var(--bg-tertiary)] text-[var(--text-primary)] resize-none"
                                />
                            </div>
                            <label className="flex items-center gap-2 cursor-pointer text-sm text-[var(--text-primary)]">
                                <input
                                    type="checkbox"
                                    checked={editIsPrivate}
                                    onChange={(e) => setEditIsPrivate(e.target.checked)}
                                    className="rounded border-[var(--border-color)]"
                                />
                                Nhóm riêng tư (chỉ thành viên xem được)
                            </label>
                            <div className="flex justify-end gap-2 pt-2">
                                <button
                                    type="button"
                                    onClick={() => setEditOpen(false)}
                                    disabled={savingGroup}
                                    className="px-4 py-2 rounded-xl font-medium text-[var(--text-muted)] hover:bg-[var(--bg-tertiary)]"
                                >
                                    Hủy
                                </button>
                                <button
                                    type="submit"
                                    disabled={savingGroup}
                                    className="px-4 py-2 rounded-xl font-medium bg-[var(--primary)] text-white hover:opacity-90 disabled:opacity-50"
                                >
                                    {savingGroup ? 'Đang lưu...' : 'Lưu'}
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            )}

            {deleteOpen && (
                <div
                    role="presentation"
                    className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50"
                    onClick={() => !deletingGroup && setDeleteOpen(false)}
                >
                    <div
                        role="dialog"
                        aria-modal="true"
                        className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-2xl p-6 max-w-md w-full shadow-xl"
                        onClick={(e) => e.stopPropagation()}
                    >
                        <h3 className="text-lg font-bold text-[var(--text-primary)] mb-2">Xóa nhóm?</h3>
                        <p className="text-sm text-[var(--text-muted)] mb-6">
                            Hành động này không thể hoàn tác. Tất cả bài viết trong nhóm sẽ bị xóa theo chính sách
                            backend.
                        </p>
                        <div className="flex justify-end gap-2">
                            <button
                                type="button"
                                onClick={() => setDeleteOpen(false)}
                                disabled={deletingGroup}
                                className="px-4 py-2 rounded-xl font-medium text-[var(--text-muted)] hover:bg-[var(--bg-tertiary)]"
                            >
                                Hủy
                            </button>
                            <button
                                type="button"
                                onClick={() => void handleDeleteGroup()}
                                disabled={deletingGroup}
                                className="px-4 py-2 rounded-xl font-medium bg-red-600 text-white hover:bg-red-700 disabled:opacity-50"
                            >
                                {deletingGroup ? 'Đang xóa...' : 'Xóa vĩnh viễn'}
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </AppLayout>
    );
}
