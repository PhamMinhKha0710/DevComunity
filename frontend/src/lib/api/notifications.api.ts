import apiClient from './client';

export const notificationsApi = {
    list: () =>
        apiClient.get('/Notifications').then(r => r.data),

    markAsRead: (id: number) =>
        apiClient.put(`/Notifications/${id}/read`).then(r => r.data),
};
