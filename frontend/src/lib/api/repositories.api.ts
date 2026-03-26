import type { Repository } from '@/types';
import apiClient from './client';

export interface RepositoriesParams {
    page?: number;
    pageSize?: number;
    ownerId?: number;
    search?: string;
}

type ApiRepositoryRecord = Record<string, unknown>;

function normalizeRepository(r: ApiRepositoryRecord): Repository {
    const repositoryId = Number(r.repositoryId);
    const name = (r.repositoryName ?? r.name) as string | undefined;
    const isPrivate = Boolean(r.isPrivate ?? (r.visibility === 'Private'));
    const created = r.createdDate as string | undefined;
    const updated = (r.lastUpdatedDate ?? r.updatedDate) as string | undefined;

    return {
        repositoryId: Number.isFinite(repositoryId) ? repositoryId : 0,
        repositoryName: name ?? '',
        description: r.description as string | undefined,
        visibility: isPrivate ? 'Private' : 'Public',
        defaultBranch: (r.defaultBranch as string) ?? 'main',
        ownerId: Number(r.ownerId) || 0,
        ownerUsername: (r.ownerUsername as string) ?? '',
        ownerProfilePicture: r.ownerProfilePicture as string | undefined,
        giteaRepoId: r.giteaRepoId as number | undefined,
        cloneUrl: r.cloneUrl as string | undefined,
        createdDate: created ?? new Date().toISOString(),
        updatedDate: updated,
        starsCount: Number(r.starCount ?? r.starsCount) || 0,
        forksCount: Number(r.forkCount ?? r.forksCount) || 0,
        language: r.language as string | undefined,
    };
}

function normalizeListPayload(data: { items?: ApiRepositoryRecord[] } & Record<string, unknown>) {
    const items = (data.items ?? []).map((item) => normalizeRepository(item));
    return { ...data, items };
}

export const repositoriesApi = {
    list: async (params?: RepositoriesParams) => {
        const data = await apiClient.get('/repositories', { params }).then((r) => r.data);
        return normalizeListPayload(data);
    },

    getById: async (id: number | string) => {
        const data = await apiClient.get(`/repositories/${id}`).then((r) => r.data);
        return normalizeRepository(data as ApiRepositoryRecord);
    },

    getFiles: (id: number | string, params?: { path?: string }) =>
        apiClient.get(`/repositories/${id}/files`, { params }).then((r) => r.data),

    getFileContent: (id: number | string, path: string) =>
        apiClient.get(`/repositories/${id}/files/content`, { params: { path } }).then((r) => r.data),

    getCommits: (id: number | string) =>
        apiClient.get(`/repositories/${id}/commits`).then((r) => r.data),

    create: (data: FormData) =>
        apiClient.post('/repositories', data).then((r) => r.data),
};
