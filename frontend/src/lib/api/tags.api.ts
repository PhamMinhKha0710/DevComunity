import apiClient from './client';

export const tagsApi = {
    list: (params?: { page?: number; pageSize?: number; sortBy?: string; search?: string }) =>
        apiClient.get('/tags', { params }).then(r => r.data),

    getPreferences: () =>
        apiClient.get('/tags/preferences').then(r => r.data),

    follow: (tagId: number) =>
        apiClient.post(`/tags/${tagId}/follow`).then(r => r.data),

    ignore: (tagId: number) =>
        apiClient.post(`/tags/${tagId}/ignore`).then(r => r.data),

    removePreference: (tagId: number) =>
        apiClient.delete(`/tags/${tagId}/preference`).then(r => r.data),
};
