import apiClient from './client';

export const votesApi = {
    voteQuestion: (questionId: number, data: { voteType: 'up' | 'down' | 'remove' }) =>
        apiClient.post(`/votes/question/${questionId}`, data).then(r => r.data),

    voteAnswer: (answerId: number, data: { voteType: 'up' | 'down' | 'remove' }) =>
        apiClient.post(`/votes/answer/${answerId}`, data).then(r => r.data),
};
