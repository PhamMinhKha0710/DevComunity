import apiClient from './client';

export const chatApi = {
    getConversations: () =>
        apiClient.get('/chat/conversations').then(r => r.data),

    getMessages: (conversationId: number, params?: { before?: string }) =>
        apiClient.get(`/chat/conversations/${conversationId}/messages`, { params }).then(r => r.data),

    sendMessage: (conversationId: number, data: { content: string; messageType?: string; mediaUrl?: string }) =>
        apiClient.post(`/chat/conversations/${conversationId}/messages`, data).then(r => r.data),

    markAsRead: (conversationId: number) =>
        apiClient.put(`/chat/conversations/${conversationId}/read`).then(r => r.data),

    createConversation: (data: { participantIds: number[] }) =>
        apiClient.post('/chat/conversations', data).then(r => r.data),

    deleteConversation: (conversationId: number) =>
        apiClient.delete(`/chat/conversations/${conversationId}`).then(r => r.data),
};
