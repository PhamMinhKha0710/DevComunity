import apiClient from './client';

export const searchApi = {
    search: (q: string) =>
        apiClient.get('/Search', { params: { q } }).then(r => r.data),
};
