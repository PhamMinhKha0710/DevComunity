import apiClient from './client';

export const newsfeedApi = {
    list: (filter?: string) =>
        apiClient.get('/Newsfeed', { params: { filter } }).then(r => r.data),

    getGroupFeed: (groupId: number | string) =>
        apiClient.get(`/Newsfeed/groups/${groupId}`).then(r => r.data),

    createPost: (data: { content: string; groupId?: number | null; mediaUrls?: string | null; visibility?: number }) =>
        apiClient.post('/Newsfeed/posts', data).then(r => r.data),

    updatePost: (postId: number, data: { content: string; mediaUrls?: string | null }) =>
        apiClient.put(`/Newsfeed/posts/${postId}`, data).then(r => r.data),

    deletePost: (postId: number) =>
        apiClient.delete(`/Newsfeed/posts/${postId}`).then(r => r.data),

    likePost: (postId: number) =>
        apiClient.post(`/Newsfeed/posts/${postId}/like`).then(r => r.data),

    unlikePost: (postId: number) =>
        apiClient.delete(`/Newsfeed/posts/${postId}/like`).then(r => r.data),
};
