import apiClient from './client';

export const answersApi = {
    listByQuestion: (questionId: number | string) =>
        apiClient.get(`/answers/question/${questionId}`).then(r => r.data),

    create: (data: { questionId: number; body: string }) =>
        apiClient.post('/answers', data).then(r => r.data),
};
