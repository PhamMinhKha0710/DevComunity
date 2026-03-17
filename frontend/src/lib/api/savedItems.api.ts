import apiClient from './client';

export const savedItemsApi = {
    list: () =>
        apiClient.get('/SavedItems').then(r => r.data),

    saveQuestion: (questionId: number) =>
        apiClient.post(`/SavedItems/questions/${questionId}`).then(r => r.data),

    unsaveQuestion: (questionId: number) =>
        apiClient.delete(`/SavedItems/questions/${questionId}`).then(r => r.data),

    saveAnswer: (answerId: number) =>
        apiClient.post(`/SavedItems/answers/${answerId}`).then(r => r.data),

    unsaveAnswer: (answerId: number) =>
        apiClient.delete(`/SavedItems/answers/${answerId}`).then(r => r.data),

    savePost: (postId: number) =>
        apiClient.post(`/SavedItems/posts/${postId}`).then(r => r.data),

    unsavePost: (postId: number) =>
        apiClient.delete(`/SavedItems/posts/${postId}`).then(r => r.data),

    remove: (id: number) =>
        apiClient.delete(`/SavedItems/${id}`).then(r => r.data),
};
