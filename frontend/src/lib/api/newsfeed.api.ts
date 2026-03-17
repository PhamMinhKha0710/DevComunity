import apiClient from './client';

export const newsfeedApi = {
    list: () =>
        apiClient.get('/Newsfeed').then(r => r.data),

    getGroupFeed: (groupId: number | string) =>
        apiClient.get(`/Newsfeed/groups/${groupId}`).then(r => r.data),

    createPost: (data: { content: string; groupId?: number | null }) =>
        apiClient.post('/Newsfeed/posts', data).then(r => r.data),

    likePost: (postId: number) =>
        apiClient.post(`/Newsfeed/posts/${postId}/like`).then(r => r.data),

    unlikePost: (postId: number) =>
        apiClient.delete(`/Newsfeed/posts/${postId}/like`).then(r => r.data),
};
