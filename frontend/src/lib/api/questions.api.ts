import apiClient from './client';

export interface QuestionsParams {
    page?: number;
    pageSize?: number;
    sortBy?: string;
    tag?: string;
    search?: string;
}

export const questionsApi = {
    list: (params?: QuestionsParams) =>
        apiClient.get('/questions', { params }).then(r => r.data),

    getById: (id: number | string) =>
        apiClient.get(`/questions/${id}`).then(r => r.data),

    create: (data: { title: string; body: string; tags?: string[] }) =>
        apiClient.post('/questions', data).then(r => r.data),

    update: (id: number | string, data: { title: string; body: string; tags?: string[] }) =>
        apiClient.put(`/questions/${id}`, data).then(r => r.data),

    delete: (id: number | string) =>
        apiClient.delete(`/questions/${id}`).then(r => r.data),
};
