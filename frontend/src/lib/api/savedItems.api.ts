import apiClient from './client';

export const savedItemsApi = {
    list: () =>
        apiClient.get('/SavedItems').then(r => r.data),

    remove: (id: number) =>
        apiClient.delete(`/SavedItems/${id}`).then(r => r.data),
};
