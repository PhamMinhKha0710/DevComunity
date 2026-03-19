import apiClient from './client';

export const followApi = {
    getFollowing: (userId: number) =>
        apiClient.get(`/Follow/following/${userId}`).then(r => r.data),

    follow: (targetId: number) =>
        apiClient.post(`/Follow/${targetId}`).then(r => r.data),

    unfollow: (targetId: number) =>
        apiClient.delete(`/Follow/${targetId}`).then(r => r.data),
};

export const friendshipApi = {
    getFriends: () =>
        apiClient.get('/Friendship/friends').then(r => r.data),

    getPending: () =>
        apiClient.get('/Friendship/pending').then(r => r.data),

    sendRequest: (targetUserId: number) =>
        apiClient.post(`/Friendship/request/${targetUserId}`).then(r => r.data),

    accept: (requestId: number) =>
        apiClient.put(`/Friendship/accept/${requestId}`).then(r => r.data),

    reject: (requestId: number) =>
        apiClient.put(`/Friendship/reject/${requestId}`).then(r => r.data),

    getSuggestions: (limit = 5) =>
        apiClient.get(`/Friendship/suggestions?limit=${limit}`).then(r => r.data),

    getNetworkGrowth: (days = 28) =>
        apiClient.get(`/Friendship/analytics/growth?days=${days}`).then(r => r.data),
};
