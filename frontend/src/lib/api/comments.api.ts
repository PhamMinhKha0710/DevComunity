import apiClient from './client';

export const commentsApi = {
    addToQuestion: (questionId: number, body: string) =>
        apiClient.post(`/comments/question/${questionId}`, { body }).then(r => r.data),

    addToAnswer: (answerId: number, body: string) =>
        apiClient.post(`/comments/answer/${answerId}`, { body }).then(r => r.data),

    addToPost: (postId: number, body: string) =>
        apiClient.post(`/comments/post/${postId}`, { body }).then(r => r.data),

    listByPost: (postId: number) =>
        apiClient.get(`/comments/post/${postId}`).then(r => r.data),

    update: (commentId: number, body: string) =>
        apiClient.put(`/comments/${commentId}`, { body }).then(r => r.data),

    remove: (commentId: number) =>
        apiClient.delete(`/comments/${commentId}`).then(r => r.data),
};
