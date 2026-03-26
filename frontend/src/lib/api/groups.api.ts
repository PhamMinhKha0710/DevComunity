import apiClient from './client';

export const groupsApi = {
    list: () =>
        apiClient.get('/Groups').then(r => r.data),

    getById: (id: number | string) =>
        apiClient.get(`/Groups/${id}`).then(r => r.data),

    getMyGroups: () =>
        apiClient.get('/Groups/my').then(r => r.data),

    isMember: (groupId: number | string) =>
        apiClient.get(`/Groups/${groupId}/isMember`).then(r => r.data),

    create: (data: { name: string; description: string; isPrivate: boolean }) =>
        apiClient.post('/Groups', data).then(r => r.data),

    join: (groupId: number | string) =>
        apiClient.post(`/Groups/${groupId}/join`).then(r => r.data),

    leave: (groupId: number | string) =>
        apiClient.post(`/Groups/${groupId}/leave`).then(r => r.data),

    update: (
        groupId: number | string,
        data: { name?: string; description?: string | null; isPrivate?: boolean }
    ) => apiClient.put(`/Groups/${groupId}`, data).then((r) => r.data),

    delete: (groupId: number | string) =>
        apiClient.delete(`/Groups/${groupId}`).then((r) => r.data),
};
