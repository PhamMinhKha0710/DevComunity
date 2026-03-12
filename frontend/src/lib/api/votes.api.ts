import apiClient from './client';

export const votesApi = {
    voteQuestion: (questionId: number, data: { voteType: 'up' | 'down' }) =>
        apiClient.post(`/votes/question/${questionId}`, data).then(r => r.data),

    voteAnswer: (answerId: number, data: { voteType: 'up' | 'down' }) =>
        apiClient.post(`/votes/answer/${answerId}`, data).then(r => r.data),

    removeQuestionVote: (questionId: number) =>
        apiClient.delete(`/votes/question/${questionId}`).then(r => r.data),

    removeAnswerVote: (answerId: number) =>
        apiClient.delete(`/votes/answer/${answerId}`).then(r => r.data),
};
