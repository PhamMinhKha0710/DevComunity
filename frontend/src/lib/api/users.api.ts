import apiClient from './client';

export interface UsersParams {
    search?: string;
    sortBy?: string;
    pageSize?: number;
    page?: number;
}

export const usersApi = {
    list: (params?: UsersParams) =>
        apiClient.get('/users', { params }).then(r => r.data),

    getById: (id: number | string) =>
        apiClient.get(`/users/${id}`).then(r => r.data),

    getQuestions: (userId: number | string) =>
        apiClient.get(`/users/${userId}/questions`).then(r => r.data),

    getAnswers: (userId: number | string, page = 1, pageSize = 15) =>
        apiClient.get(`/users/${userId}/answers`, { params: { page, pageSize } }).then(r => r.data),

    updateProfile: (data: FormData | Record<string, unknown>) =>
        apiClient.put('/users/profile', data).then(r => r.data),

    getTagPreferences: () =>
        apiClient.get('/users/tag-preferences').then(r => r.data),

    updateTagPreferences: (tagIds: number[]) =>
        apiClient.put('/users/tag-preferences', { tagIds }).then(r => r.data),
};
