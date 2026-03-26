'use client';

import { useState, useEffect, useCallback, useMemo, useRef } from 'react';
import Link from 'next/link';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { HubConnectionState } from '@microsoft/signalr';
import toast from 'react-hot-toast';
import AppLayout from '@/components/AppLayout';
import { useAuth } from '@/lib/contexts/AuthContext';
import { newsfeedApi } from '@/lib/api/newsfeed.api';
import { votesApi } from '@/lib/api/votes.api';
import { savedItemsApi } from '@/lib/api/savedItems.api';
import { commentsApi } from '@/lib/api/comments.api';
import { tagsApi } from '@/lib/api/tags.api';
import { usersApi } from '@/lib/api/users.api';
import { followApi } from '@/lib/api/social.api';
import { mediaApi } from '@/lib/api/media.api';
import { useHub } from '@/lib/signalr/useHub';
import RelativeTime from '@/components/RelativeTime';
import { authorInitial } from '@/lib/utils';

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
    userSaved?: boolean;
    mediaUrls?: string | null;
    visibility?: number;
}

interface PostComment {
    commentId: number;
    body: string;
    createdDate: string;
    userId: number;
    authorUsername: string;
    authorProfilePicture: string | null;
}

export default function NewsfeedPage() {
    const { isAuthenticated, user: currentUser } = useAuth();
    const queryClient = useQueryClient();
    const [newPostContent, setNewPostContent] = useState('');
    const [postVisibility, setPostVisibility] = useState<number>(0); // 0 = Public
    const [activeTab, setActiveTab] = useState<'all' | 'following' | 'friends' | 'community'>('all');
    const [commentOpenPostId, setCommentOpenPostId] = useState<number | null>(null);
    const [commentTextByPostId, setCommentTextByPostId] = useState<Record<number, string>>({});
    const [selectedImages, setSelectedImages] = useState<{ file: File; preview: string; uploadedUrl?: string }[]>([]);
    const [uploading, setUploading] = useState(false);
    const fileInputRef = useRef<HTMLInputElement>(null);
    const [postMenuOpenId, setPostMenuOpenId] = useState<number | null>(null);
    const postMenuRef = useRef<HTMLDivElement>(null);
    const [editingPostId, setEditingPostId] = useState<number | null>(null);
    const [editContent, setEditContent] = useState('');
    const [editExistingUrls, setEditExistingUrls] = useState<string[]>([]);
    const [editNewImages, setEditNewImages] = useState<{ file: File; preview: string }[]>([]);
    const [uploadingEdit, setUploadingEdit] = useState(false);
    const editFileInputRef = useRef<HTMLInputElement>(null);
    const activityHub = useHub('activity');

    const resolveMediaSrc = (url: string) =>
        url.startsWith('http') ? url : `${process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5122'}${url}`;

    const { data: postsData, isLoading: loading } = useQuery({
        queryKey: ['newsfeed', activeTab],
        queryFn: () => newsfeedApi.list(activeTab),
        enabled: isAuthenticated,
    });

    // Posts are now fetched based on activeTab from the backend
    const posts: Post[] = postsData?.items || [];

    const { data: postComments = [], isLoading: commentsLoading } = useQuery({
        queryKey: ['comments', 'post', commentOpenPostId],
        queryFn: () => commentsApi.listByPost(commentOpenPostId!),
        enabled: isAuthenticated && commentOpenPostId != null,
    });

    const { data: trendingTagsData } = useQuery({
        queryKey: ['tags', 'trending'],
        queryFn: () => tagsApi.list({ page: 1, pageSize: 5, sortBy: 'popular' }),
        enabled: isAuthenticated,
    });

    const { data: whoToFollowData, refetch: refetchWhoToFollow } = useQuery({
        queryKey: ['users', 'whoToFollow'],
        queryFn: () => usersApi.list({ page: 1, pageSize: 6, sortBy: 'reputation' }),
        enabled: isAuthenticated,
    });

    const followUserMutation = useMutation({
        mutationFn: (userId: number) => followApi.follow(userId),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['users', 'whoToFollow'] });
            toast.success('Đã theo dõi');
        },
        onError: () => {
            toast.error('Không thể theo dõi');
        },
    });

    const trendingTopics = trendingTagsData?.items ?? [];
    const whoToFollowUsers = useMemo(() => {
        const items = whoToFollowData?.items ?? [];
        if (!currentUser?.userId) return items;
        return items.filter((u: { userId: number }) => u.userId !== currentUser.userId).slice(0, 3);
    }, [whoToFollowData?.items, currentUser?.userId]);

    const handleNewPost = useCallback(() => {
        queryClient.invalidateQueries({ queryKey: ['newsfeed'] });
    }, [queryClient]);

    const handleNewPostComment = useCallback((payload: { postId: number }) => {
        const { postId } = payload;
        queryClient.invalidateQueries({ queryKey: ['comments', 'post', postId] });
        queryClient.invalidateQueries({ queryKey: ['newsfeed'] });
    }, [queryClient]);

    useEffect(() => {
        if (activityHub.connectionState !== HubConnectionState.Connected) return;
        activityHub.on('NewPost', handleNewPost);
        activityHub.on('NewGroupPost', handleNewPost);
        activityHub.on('NewQuestion', handleNewPost);
        activityHub.on('NewPostComment', handleNewPostComment);
        return () => {
            activityHub.off('NewPost', handleNewPost);
            activityHub.off('NewGroupPost', handleNewPost);
            activityHub.off('NewQuestion', handleNewPost);
            activityHub.off('NewPostComment', handleNewPostComment);
        };
    }, [activityHub, handleNewPost, handleNewPostComment]);

    useEffect(() => {
        if (postMenuOpenId == null) return;
        const handleMouseDown = (e: MouseEvent) => {
            if (postMenuRef.current && !postMenuRef.current.contains(e.target as Node)) {
                setPostMenuOpenId(null);
            }
        };
        document.addEventListener('mousedown', handleMouseDown);
        return () => document.removeEventListener('mousedown', handleMouseDown);
    }, [postMenuOpenId]);

    const createPostMutation = useMutation({
        mutationFn: (data: { content: string; mediaUrls?: string | null; visibility?: number }) =>
            newsfeedApi.createPost({ content: data.content, groupId: null, mediaUrls: data.mediaUrls, visibility: data.visibility }),
        onSettled: () => {
            queryClient.invalidateQueries({ queryKey: ['newsfeed'] });
            setNewPostContent('');
            setSelectedImages([]);
            setPostVisibility(0);
        },
    });

    const likePostMutation = useMutation({
        mutationFn: ({ postId, isLiked }: { postId: number; isLiked: boolean }) =>
            isLiked ? votesApi.removePostVote(postId) : votesApi.votePost(postId, { voteType: 'up' }),
        onMutate: async ({ postId, isLiked }) => {
            await queryClient.cancelQueries({ queryKey: ['newsfeed'] });
            const prev = queryClient.getQueryData(['newsfeed']);
            queryClient.setQueryData(['newsfeed'], (old: any) => {
                if (!old?.items) return old;
                return {
                    ...old,
                    items: old.items.map((p: Post) =>
                        p.postId === postId
                            ? {
                                ...p,
                                userLiked: !isLiked,
                                likeCount: (p.likeCount ?? 0) + (isLiked ? -1 : 1)
                            }
                            : p
                    )
                };
            });
            return { prev };
        },
        onError: (_err, _vars, context) => {
            if (context?.prev) queryClient.setQueryData(['newsfeed'], context.prev);
        },
        onSettled: () => {
            queryClient.invalidateQueries({ queryKey: ['newsfeed'] });
        },
    });

    const savePostMutation = useMutation({
        mutationFn: ({ postId, isSaved }: { postId: number; isSaved: boolean }) =>
            isSaved ? savedItemsApi.unsavePost(postId) : savedItemsApi.savePost(postId),
        onMutate: async ({ postId, isSaved }) => {
            await queryClient.cancelQueries({ queryKey: ['newsfeed'] });
            const prev = queryClient.getQueryData(['newsfeed']);
            queryClient.setQueryData(['newsfeed'], (old: any) => {
                if (!old?.items) return old;
                return {
                    ...old,
                    items: old.items.map((p: Post) =>
                        p.postId === postId
                            ? { ...p, userSaved: !isSaved }
                            : p
                    )
                };
            });
            return { prev };
        },
        onError: (_err, _vars, context) => {
            if (context?.prev) queryClient.setQueryData(['newsfeed'], context.prev);
        },
        onSettled: () => {
            queryClient.invalidateQueries({ queryKey: ['newsfeed'] });
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
            queryClient.invalidateQueries({ queryKey: ['newsfeed'] });
        },
    });

    const updatePostMutation = useMutation({
        mutationFn: ({
            postId,
            content,
            mediaUrls,
        }: {
            postId: number;
            content: string;
            mediaUrls: string | null;
        }) => newsfeedApi.updatePost(postId, { content, mediaUrls }),
        onSuccess: () => {
            setEditNewImages((prev) => {
                prev.forEach((img) => URL.revokeObjectURL(img.preview));
                return [];
            });
            setEditingPostId(null);
            setEditContent('');
            setEditExistingUrls([]);
            queryClient.invalidateQueries({ queryKey: ['newsfeed'] });
            toast.success('Đã cập nhật bài viết');
        },
        onError: () => {
            toast.error('Không thể cập nhật bài viết');
        },
    });

    const deletePostMutation = useMutation({
        mutationFn: (postId: number) => newsfeedApi.deletePost(postId),
        onSuccess: () => {
            setPostMenuOpenId(null);
            queryClient.invalidateQueries({ queryKey: ['newsfeed'] });
            toast.success('Đã xóa bài viết');
        },
        onError: () => {
            toast.error('Không thể xóa bài viết');
        },
    });

    const posting = createPostMutation.isPending || uploading;

    const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
        const files = Array.from(e.target.files || []);
        if (files.length === 0) return;

        const newImages = files.map(file => ({
            file,
            preview: URL.createObjectURL(file)
        }));

        setSelectedImages(prev => [...prev, ...newImages].slice(0, 5)); // Limit to 5
        e.target.value = ''; // Reset
    };

    const removeImage = (index: number) => {
        setSelectedImages(prev => {
            const updated = [...prev];
            URL.revokeObjectURL(updated[index].preview);
            updated.splice(index, 1);
            return updated;
        });
    };

    const handleOpenPostEdit = (post: Post) => {
        setPostMenuOpenId(null);
        setEditingPostId(post.postId);
        setEditContent(post.content);
        setEditExistingUrls(post.mediaUrls ? post.mediaUrls.split(';').filter(Boolean) : []);
        setEditNewImages([]);
    };

    const handleCancelPostEdit = () => {
        setEditNewImages((prev) => {
            prev.forEach((img) => URL.revokeObjectURL(img.preview));
            return [];
        });
        setEditingPostId(null);
        setEditContent('');
        setEditExistingUrls([]);
    };

    const removeEditExistingUrl = (index: number) => {
        setEditExistingUrls((prev) => prev.filter((_, i) => i !== index));
    };

    const handleEditFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
        const files = Array.from(e.target.files || []);
        if (files.length === 0) return;
        const newImages = files.map((file) => ({ file, preview: URL.createObjectURL(file) }));
        setEditNewImages((prev) => [...prev, ...newImages].slice(0, 5));
        e.target.value = '';
    };

    const removeEditNewImage = (index: number) => {
        setEditNewImages((prev) => {
            const updated = [...prev];
            URL.revokeObjectURL(updated[index].preview);
            updated.splice(index, 1);
            return updated;
        });
    };

    const handleSavePostEdit = async () => {
        if (editingPostId == null) return;
        if (!editContent.trim() && editExistingUrls.length === 0 && editNewImages.length === 0) {
            toast.error('Nội dung hoặc ảnh không được để trống');
            return;
        }
        let mediaUrls: string | null = null;
        const urlParts = [...editExistingUrls];
        if (editNewImages.length > 0) {
            setUploadingEdit(true);
            try {
                const uploadPromises = editNewImages.map(async (img) => {
                    const formData = new FormData();
                    formData.append('file', img.file);
                    const result = await mediaApi.upload(formData);
                    return result.url;
                });
                const newUrls = await Promise.all(uploadPromises);
                urlParts.push(...newUrls);
            } catch {
                toast.error('Lỗi khi tải ảnh lên');
                setUploadingEdit(false);
                return;
            }
            setUploadingEdit(false);
        }
        if (urlParts.length > 0) {
            mediaUrls = urlParts.join(';');
        }
        updatePostMutation.mutate({ postId: editingPostId, content: editContent, mediaUrls });
    };

    const handleCreatePost = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!newPostContent.trim() && selectedImages.length === 0) return;

        let mediaUrls: string | null = null;

        if (selectedImages.length > 0) {
            setUploading(true);
            try {
                const uploadPromises = selectedImages.map(async img => {
                    const formData = new FormData();
                    formData.append('file', img.file);
                    const result = await mediaApi.upload(formData);
                    return result.url;
                });

                const urls = await Promise.all(uploadPromises);
                mediaUrls = urls.join(';');
            } catch (error) {
                toast.error('Lỗi khi tải ảnh lên');
                setUploading(false);
                return;
            }
            setUploading(false);
        }

        createPostMutation.mutate({ content: newPostContent, mediaUrls, visibility: postVisibility });
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
                    <Link href="/auth?mode=login" className="px-6 py-3 bg-[var(--primary)] text-white rounded-xl font-bold hover:bg-[var(--primary)]/90 transition">
                        Login Now
                    </Link>
                </div>
            </AppLayout>
        );
    }

    const tabs = [
        { key: 'all', label: 'Tất cả', icon: 'explore' },
        { key: 'following', label: 'Đang theo dõi', icon: 'person_add' },
        { key: 'friends', label: 'Bạn bè', icon: 'group' },
        { key: 'community', label: 'Cộng đồng', icon: 'forum' },
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
                                            {authorInitial(currentUser?.displayName || currentUser?.username)}
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

                                    {/* Image Previews */}
                                    {selectedImages.length > 0 && (
                                        <div className="flex flex-wrap gap-2 mt-3">
                                            {selectedImages.map((img, idx) => (
                                                <div key={idx} className="relative w-20 h-20 rounded-lg overflow-hidden border border-slate-200 dark:border-slate-700 shadow-sm group">
                                                    <img src={img.preview} alt="" className="w-full h-full object-cover" />
                                                    <button
                                                        type="button"
                                                        onClick={() => removeImage(idx)}
                                                        className="absolute top-1 right-1 w-5 h-5 bg-black/50 text-white rounded-full flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity"
                                                    >
                                                        <span className="material-symbols-outlined text-xs">close</span>
                                                    </button>
                                                </div>
                                            ))}
                                            {selectedImages.length < 5 && (
                                                <button
                                                    type="button"
                                                    onClick={() => fileInputRef.current?.click()}
                                                    className="w-20 h-20 rounded-lg border-2 border-dashed border-slate-200 dark:border-slate-700 flex flex-col items-center justify-center text-slate-400 hover:text-[var(--primary)] hover:border-[var(--primary)] transition-all"
                                                >
                                                    <span className="material-symbols-outlined">add</span>
                                                    <span className="text-[10px]">Thêm</span>
                                                </button>
                                            )}
                                        </div>
                                    )}

                                    <div className="flex items-center justify-between mt-4">
                                        <div className="flex items-center gap-2">
                                            <input
                                                type="file"
                                                ref={fileInputRef}
                                                onChange={handleFileSelect}
                                                accept="image/*"
                                                multiple
                                                className="hidden"
                                            />
                                            <button
                                                type="button"
                                                onClick={() => fileInputRef.current?.click()}
                                                disabled={uploading}
                                                className="p-2 rounded-lg text-slate-500 hover:bg-slate-100 dark:hover:bg-slate-800 hover:text-[var(--primary)] transition-colors disabled:opacity-50"
                                                title="Thêm ảnh"
                                            >
                                                <span className="material-symbols-outlined">image</span>
                                            </button>
                                            
                                            <div className="h-6 w-[1px] bg-slate-200 dark:border-slate-700 mx-1"></div>

                                            <select 
                                                value={postVisibility}
                                                onChange={(e) => setPostVisibility(parseInt(e.target.value))}
                                                className="bg-transparent text-xs font-bold text-slate-500 hover:text-slate-700 dark:hover:text-slate-300 border-none focus:ring-0 cursor-pointer outline-none"
                                            >
                                                <option value={0}>🌎 Công khai</option>
                                                <option value={1}>👥 Bạn bè</option>
                                                <option value={2}>🔔 Đang theo dõi</option>
                                                <option value={3}>🔒 Riêng tư</option>
                                            </select>
                                        </div>
                                        <button
                                            type="submit"
                                            disabled={posting || (!newPostContent.trim() && selectedImages.length === 0)}
                                            className="px-6 py-2 bg-[var(--primary)] text-white text-sm font-bold rounded-xl shadow-lg shadow-[var(--primary)]/20 hover:opacity-90 active:scale-95 transition-all disabled:opacity-50 disabled:cursor-not-allowed"
                                        >
                                            {uploading ? 'Uploading...' : posting ? 'Posting...' : 'Post'}
                                        </button>
                                    </div>
                                </div>
                            </div>
                        </form>
                    </div>

                    {/* Feed Tabs - Glassmorphism style */}
                    <div className="sticky top-0 z-10 -mx-1 px-1 py-2 bg-white/70 dark:bg-slate-900/70 backdrop-blur-md border-b border-slate-200 dark:border-slate-800">
                        <div className="flex gap-2 overflow-x-auto no-scrollbar">
                            {tabs.map((tab) => (
                                <button
                                    key={tab.key}
                                    onClick={() => setActiveTab(tab.key as 'all' | 'following' | 'friends' | 'community')}
                                    className={`flex items-center gap-2 px-4 py-2 rounded-full text-sm font-bold transition-all whitespace-nowrap ${activeTab === tab.key
                                        ? 'bg-[var(--primary)] text-white shadow-lg shadow-[var(--primary)]/30 scale-105'
                                        : 'text-slate-500 hover:bg-slate-100 dark:hover:bg-slate-800'
                                        }`}
                                >
                                    <span className="material-symbols-outlined text-lg">{tab.icon}</span>
                                    {tab.label}
                                </button>
                            ))}
                        </div>
                    </div>

                    {/* Posts Feed */}
                    {loading ? (
                        <div className="flex flex-col gap-6">
                            {[1, 2, 3].map(i => (
                                <div key={i} className="bg-white dark:bg-slate-900 rounded-2xl h-64 animate-pulse border border-slate-200 dark:border-slate-800" />
                            ))}
                        </div>
                    ) : (posts.length === 0) ? (
                        <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-2xl p-12 text-center">
                            <span className="material-symbols-outlined text-5xl text-slate-300 mb-4 block">inbox</span>
                            <h3 className="text-lg font-semibold text-slate-900 dark:text-white mb-2">
                                {activeTab === 'community' ? 'No community posts' : activeTab === 'following' ? 'No posts from following' : 'No posts yet'}
                            </h3>
                            <p className="text-slate-500">
                                {activeTab === 'community' ? 'Join groups to see community posts!' : activeTab === 'following' ? 'Follow more users to see their posts!' : 'Be the first to share something or follow more users!'}
                            </p>
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
                                                        {authorInitial(post.author.displayName || post.author.username)}
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
                                                    <RelativeTime value={post.createdAt} />
                                                    {post.groupName && (
                                                        <> • in <Link href={`/groups/${post.groupId}`} className="text-[var(--primary)] font-medium">{post.groupName}</Link></>
                                                    )}
                                                </p>
                                            </div>
                                        </div>
                                        {currentUser?.userId === post.author.userId && editingPostId !== post.postId ? (
                                            <div
                                                className="relative shrink-0"
                                                ref={postMenuOpenId === post.postId ? postMenuRef : undefined}
                                            >
                                                <button
                                                    type="button"
                                                    onClick={() =>
                                                        setPostMenuOpenId((id) => (id === post.postId ? null : post.postId))
                                                    }
                                                    className="text-slate-400 hover:text-slate-600 dark:hover:text-slate-300 p-1 rounded-lg hover:bg-slate-100 dark:hover:bg-slate-800"
                                                    aria-label="Tùy chọn bài viết"
                                                >
                                                    <span className="material-symbols-outlined">more_horiz</span>
                                                </button>
                                                {postMenuOpenId === post.postId && (
                                                    <div className="absolute right-0 top-full mt-1 z-20 min-w-[180px] rounded-xl border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 shadow-lg py-1">
                                                        <button
                                                            type="button"
                                                            className="w-full text-left px-4 py-2.5 text-sm text-slate-700 dark:text-slate-200 hover:bg-slate-50 dark:hover:bg-slate-700 flex items-center gap-2"
                                                            onClick={() => handleOpenPostEdit(post)}
                                                        >
                                                            <span className="material-symbols-outlined text-lg">edit</span>
                                                            Chỉnh sửa
                                                        </button>
                                                        <button
                                                            type="button"
                                                            className="w-full text-left px-4 py-2.5 text-sm text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-950/30 flex items-center gap-2"
                                                            onClick={() => {
                                                                setPostMenuOpenId(null);
                                                                if (window.confirm('Xóa bài viết này?')) {
                                                                    deletePostMutation.mutate(post.postId);
                                                                }
                                                            }}
                                                            disabled={deletePostMutation.isPending}
                                                        >
                                                            <span className="material-symbols-outlined text-lg">delete</span>
                                                            Xóa bài viết
                                                        </button>
                                                    </div>
                                                )}
                                            </div>
                                        ) : null}
                                    </div>

                                    {/* Post Content — view or edit */}
                                    {editingPostId === post.postId ? (
                                        <div className="px-5 pb-4 space-y-3">
                                            <textarea
                                                value={editContent}
                                                onChange={(e) => setEditContent(e.target.value)}
                                                rows={4}
                                                className="w-full p-3 rounded-xl bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-sm text-slate-900 dark:text-white resize-none focus:ring-2 focus:ring-[var(--primary)]/30"
                                                placeholder="Nội dung bài viết..."
                                            />
                                            {(editExistingUrls.length > 0 || editNewImages.length > 0) && (
                                                <div className="flex flex-wrap gap-2">
                                                    {editExistingUrls.map((url, idx) => (
                                                        <div
                                                            key={`ex-${idx}`}
                                                            className="relative w-20 h-20 rounded-lg overflow-hidden border border-slate-200 dark:border-slate-700 shadow-sm group"
                                                        >
                                                            <img
                                                                src={resolveMediaSrc(url)}
                                                                alt=""
                                                                className="w-full h-full object-cover"
                                                            />
                                                            <button
                                                                type="button"
                                                                onClick={() => removeEditExistingUrl(idx)}
                                                                className="absolute top-1 right-1 w-5 h-5 bg-black/50 text-white rounded-full flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity"
                                                            >
                                                                <span className="material-symbols-outlined text-xs">close</span>
                                                            </button>
                                                        </div>
                                                    ))}
                                                    {editNewImages.map((img, idx) => (
                                                        <div
                                                            key={`new-${idx}`}
                                                            className="relative w-20 h-20 rounded-lg overflow-hidden border border-slate-200 dark:border-slate-700 shadow-sm group"
                                                        >
                                                            <img
                                                                src={img.preview}
                                                                alt=""
                                                                className="w-full h-full object-cover"
                                                            />
                                                            <button
                                                                type="button"
                                                                onClick={() => removeEditNewImage(idx)}
                                                                className="absolute top-1 right-1 w-5 h-5 bg-black/50 text-white rounded-full flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity"
                                                            >
                                                                <span className="material-symbols-outlined text-xs">close</span>
                                                            </button>
                                                        </div>
                                                    ))}
                                                </div>
                                            )}
                                            <input
                                                ref={editFileInputRef}
                                                type="file"
                                                accept="image/*"
                                                multiple
                                                className="hidden"
                                                onChange={handleEditFileSelect}
                                            />
                                            {editExistingUrls.length + editNewImages.length < 5 && (
                                                <button
                                                    type="button"
                                                    onClick={() => editFileInputRef.current?.click()}
                                                    className="text-sm font-medium text-[var(--primary)] hover:underline"
                                                >
                                                    Thêm ảnh
                                                </button>
                                            )}
                                            <div className="flex justify-end gap-2 pt-1">
                                                <button
                                                    type="button"
                                                    onClick={handleCancelPostEdit}
                                                    disabled={updatePostMutation.isPending || uploadingEdit}
                                                    className="px-4 py-2 text-sm font-medium rounded-xl border border-slate-200 dark:border-slate-600 text-slate-700 dark:text-slate-200 hover:bg-slate-50 dark:hover:bg-slate-800 disabled:opacity-50"
                                                >
                                                    Hủy
                                                </button>
                                                <button
                                                    type="button"
                                                    onClick={handleSavePostEdit}
                                                    disabled={updatePostMutation.isPending || uploadingEdit}
                                                    className="px-4 py-2 text-sm font-bold rounded-xl bg-[var(--primary)] text-white hover:opacity-90 disabled:opacity-50"
                                                >
                                                    {uploadingEdit || updatePostMutation.isPending ? 'Đang lưu...' : 'Lưu'}
                                                </button>
                                            </div>
                                        </div>
                                    ) : (
                                        <>
                                            <div className="px-5 pb-3">
                                                <p className="text-slate-700 dark:text-slate-300 text-sm leading-relaxed whitespace-pre-wrap">
                                                    {post.content}
                                                </p>
                                            </div>

                                            {post.mediaUrls && (
                                                <div
                                                    className={`px-5 pb-4 grid gap-1 ${
                                                        post.mediaUrls.split(';').length === 1 ? 'grid-cols-1' : 'grid-cols-2'
                                                    }`}
                                                >
                                                    {post.mediaUrls.split(';').map((url, i) => (
                                                        <div
                                                            key={i}
                                                            className={`rounded-xl overflow-hidden border border-slate-100 dark:border-slate-800 shadow-sm ${
                                                                post.mediaUrls!.split(';').length === 3 && i === 0
                                                                    ? 'row-span-2'
                                                                    : ''
                                                            }`}
                                                        >
                                                            <img
                                                                src={resolveMediaSrc(url)}
                                                                alt=""
                                                                className="w-full h-full object-cover max-h-[400px]"
                                                            />
                                                        </div>
                                                    ))}
                                                </div>
                                            )}
                                        </>
                                    )}

                                    {/* Like count - same as question */}
                                    {(post.likeCount ?? 0) > 0 && (
                                        <div className="flex items-center gap-1.5 px-5 pb-2 text-sm text-slate-500">
                                            <span className="w-5 h-5 rounded-full bg-[var(--primary)] flex items-center justify-center">
                                                <span className="material-symbols-outlined text-white text-xs">thumb_up</span>
                                            </span>
                                            {post.likeCount}
                                        </div>
                                    )}

                                    {/* Post Actions - same layout as question (Thích, Bình luận, Lưu) */}
                                    <div className="px-5 py-3 border-t border-slate-100 dark:border-slate-800 flex items-center gap-1">
                                        <button
                                            type="button"
                                            onClick={() => {
                                                if (likePostMutation.isPending || !currentUser) return;
                                                likePostMutation.mutate({ postId: post.postId, isLiked: post.userLiked ?? false });
                                            }}
                                            disabled={likePostMutation.isPending}
                                            className={`flex-1 flex items-center justify-center gap-2 py-2.5 rounded-lg font-medium text-sm transition hover:bg-slate-100 dark:hover:bg-slate-800 disabled:opacity-60 ${
                                                post.userLiked ? 'text-[var(--primary)]' : 'text-slate-500'
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
                                            onClick={() => setCommentOpenPostId(commentOpenPostId === post.postId ? null : post.postId)}
                                            className={`flex-1 flex items-center justify-center gap-2 py-2.5 rounded-lg font-medium text-sm transition hover:bg-slate-100 dark:hover:bg-slate-800 ${
                                                commentOpenPostId === post.postId ? 'text-[var(--primary)]' : 'text-slate-500'
                                            }`}
                                        >
                                            <span className="material-symbols-outlined text-xl">comment</span>
                                            Bình luận
                                            {(post.commentCount ?? 0) > 0 && (
                                                <span className="text-xs">({post.commentCount})</span>
                                            )}
                                        </button>
                                        <button
                                            type="button"
                                            onClick={() => {
                                                if (savePostMutation.isPending || !currentUser) return;
                                                savePostMutation.mutate({ postId: post.postId, isSaved: post.userSaved ?? false });
                                            }}
                                            disabled={savePostMutation.isPending}
                                            className={`flex-1 flex items-center justify-center gap-2 py-2.5 rounded-lg font-medium text-sm transition hover:bg-slate-100 dark:hover:bg-slate-800 disabled:opacity-60 ${
                                                post.userSaved ? 'text-[var(--primary)]' : 'text-slate-500'
                                            }`}
                                        >
                                            <span className="material-symbols-outlined text-xl">
                                                {post.userSaved ? 'bookmark' : 'bookmark_border'}
                                            </span>
                                            {post.userSaved ? 'Đã lưu' : 'Lưu'}
                                        </button>
                                    </div>

                                    {/* Comment section - same style as question answers/comments */}
                                    {commentOpenPostId === post.postId && (
                                        <div className="px-5 pb-4 pt-2 border-t border-slate-100 dark:border-slate-800">
                                            <div className="ml-0 pl-0 space-y-3">
                                                {commentsLoading ? (
                                                    <p className="text-sm text-slate-500">Đang tải bình luận...</p>
                                                ) : (postComments as PostComment[]).length > 0 ? (
                                                    <ul className="space-y-3">
                                                        {(postComments as PostComment[]).map((c) => (
                                                            <li key={c.commentId} className="flex gap-2.5">
                                                                {c.authorProfilePicture ? (
                                                                    <img
                                                                        src={c.authorProfilePicture}
                                                                        alt=""
                                                                        className="w-7 h-7 rounded-full object-cover flex-shrink-0 mt-0.5"
                                                                    />
                                                                ) : (
                                                                    <div className="w-7 h-7 rounded-full bg-gradient-to-br from-blue-500 to-cyan-500 flex-shrink-0 flex items-center justify-center text-white text-xs font-bold mt-0.5">
                                                                        {authorInitial(c.authorUsername)}
                                                                    </div>
                                                                )}
                                                                <div className="flex-1 min-w-0">
                                                                    <p className="text-sm text-slate-900 dark:text-white">
                                                                        <span className="font-medium">{c.authorUsername}</span>
                                                                        {' '}
                                                                        <span className="text-slate-500 font-normal">
                                                                            <RelativeTime value={c.createdDate} />
                                                                        </span>
                                                                    </p>
                                                                    <p className="text-sm text-slate-700 dark:text-slate-300 mt-0.5 break-words">
                                                                        {c.body}
                                                                    </p>
                                                                </div>
                                                            </li>
                                                        ))}
                                                    </ul>
                                                ) : (
                                                    <p className="text-sm text-slate-500">Chưa có bình luận.</p>
                                                )}
                                                {currentUser && (
                                                    <div className="flex gap-2.5">
                                                        {currentUser.profilePicture ? (
                                                            <img
                                                                src={currentUser.profilePicture}
                                                                alt=""
                                                                className="w-7 h-7 rounded-full object-cover flex-shrink-0 mt-1"
                                                            />
                                                        ) : (
                                                            <div className="w-7 h-7 rounded-full bg-gradient-to-br from-blue-500 to-cyan-500 flex-shrink-0 flex items-center justify-center text-white text-xs font-bold mt-1">
                                                                {authorInitial(currentUser.displayName || currentUser.username)}
                                                            </div>
                                                        )}
                                                        <div className="flex-1">
                                                            <textarea
                                                                value={commentTextByPostId[post.postId] ?? ''}
                                                                onChange={(e) => setCommentTextByPostId(prev => ({ ...prev, [post.postId]: e.target.value }))}
                                                                rows={2}
                                                                className="w-full px-3 py-2 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-sm text-slate-900 dark:text-white placeholder-slate-400 focus:border-[var(--primary)] focus:ring-1 focus:ring-[var(--primary)]/20 transition resize-none"
                                                                placeholder="Viết bình luận... (Enter để gửi)"
                                                            />
                                                            <div className="flex justify-end mt-1.5">
                                                                <button
                                                                    type="button"
                                                                    onClick={() => {
                                                                        const text = commentTextByPostId[post.postId]?.trim();
                                                                        if (!text || commentMutation.isPending) return;
                                                                        commentMutation.mutate({ postId: post.postId, body: text });
                                                                        setCommentTextByPostId(prev => ({ ...prev, [post.postId]: '' }));
                                                                    }}
                                                                    className="px-3 py-1.5 text-xs font-medium bg-[var(--primary)] text-white rounded-lg hover:opacity-90 transition disabled:opacity-50"
                                                                    disabled={!(commentTextByPostId[post.postId]?.trim()) || commentMutation.isPending}
                                                                >
                                                                    {commentMutation.isPending ? 'Đang gửi...' : 'Gửi'}
                                                                </button>
                                                            </div>
                                                        </div>
                                                    </div>
                                                )}
                                            </div>
                                        </div>
                                    )}
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
                            {trendingTopics.length > 0 ? (
                                trendingTopics.map((topic: { tagId: number; tagName: string; questionCount: number }) => (
                                    <Link key={topic.tagId} href={`/questions?tag=${encodeURIComponent(topic.tagName)}`}>
                                        <p className="text-xs font-bold text-[var(--primary)]">#{topic.tagName}</p>
                                        <p className="text-xs text-slate-500">{topic.questionCount} câu hỏi</p>
                                    </Link>
                                ))
                            ) : (
                                <p className="text-xs text-slate-500">Đang tải...</p>
                            )}
                            <Link href="/questions" className="text-xs font-bold text-slate-400 hover:text-[var(--primary)] transition-colors mt-2 text-left block">Xem thêm</Link>
                        </div>
                    </div>

                    {/* Who to Follow */}
                    <div className="bg-white dark:bg-slate-900 rounded-xl p-5 shadow-sm border border-slate-200 dark:border-slate-800">
                        <div className="flex items-center justify-between mb-4">
                            <h4 className="text-sm font-bold text-slate-900 dark:text-white">Who to follow</h4>
                            <button type="button" onClick={() => refetchWhoToFollow()} className="text-xs font-bold text-[var(--primary)] hover:underline">Refresh</button>
                        </div>
                        <div className="flex flex-col gap-5">
                            {whoToFollowUsers.length > 0 ? (
                                whoToFollowUsers.map((person: { userId: number; username: string; displayName: string | null; profilePicture: string | null }) => (
                                    <div key={person.userId} className="flex items-center justify-between">
                                        <div className="flex items-center gap-3">
                                            <Link href={`/users/${person.userId}`} className="flex items-center gap-3 min-w-0">
                                                {person.profilePicture ? (
                                                    <img src={person.profilePicture} alt="" className="h-9 w-9 rounded-full object-cover flex-shrink-0" />
                                                ) : (
                                                    <div className="h-9 w-9 rounded-full bg-gradient-to-br from-blue-500 to-indigo-500 flex items-center justify-center text-white text-xs font-bold flex-shrink-0">
                                                        {authorInitial(person.displayName || person.username)}
                                                    </div>
                                                )}
                                                <div className="min-w-0">
                                                    <p className="text-xs font-bold text-slate-900 dark:text-white truncate">{person.displayName || person.username}</p>
                                                    <p className="text-[10px] text-slate-500 truncate">@{person.username}</p>
                                                </div>
                                            </Link>
                                        </div>
                                        <button
                                            type="button"
                                            onClick={() => followUserMutation.mutate(person.userId)}
                                            disabled={followUserMutation.isPending}
                                            className="p-1.5 rounded-lg text-[var(--primary)] bg-[var(--primary)]/10 hover:bg-[var(--primary)] hover:text-white transition-all flex-shrink-0"
                                        >
                                            <span className="material-symbols-outlined text-sm">person_add</span>
                                        </button>
                                    </div>
                                ))
                            ) : (
                                <p className="text-xs text-slate-500">Đang tải hoặc không có gợi ý.</p>
                            )}
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
