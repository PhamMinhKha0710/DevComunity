import apiClient from './client';

export interface RepositoriesParams {
    page?: number;
    pageSize?: number;
    ownerId?: number;
    search?: string;
}

export const repositoriesApi = {
    list: (params?: RepositoriesParams) =>
        apiClient.get('/repositories', { params }).then(r => r.data),

    getById: (id: number | string) =>
        apiClient.get(`/repositories/${id}`).then(r => r.data),

    getFiles: (id: number | string, params?: { path?: string }) =>
        apiClient.get(`/repositories/${id}/files`, { params }).then(r => r.data),

    getFileContent: (id: number | string, path: string) =>
        apiClient.get(`/repositories/${id}/files/content`, { params: { path } }).then(r => r.data),

    getCommits: (id: number | string) =>
        apiClient.get(`/repositories/${id}/commits`).then(r => r.data),

    create: (data: FormData) =>
        apiClient.post('/repositories', data).then(r => r.data),
};
