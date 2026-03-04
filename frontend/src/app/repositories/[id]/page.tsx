'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { useParams } from 'next/navigation';
import type { Repository, RepositoryFile } from '@/types';
import apiClient from '@/lib/api/client';
import AppLayout from '@/components/AppLayout';

export default function RepositoryDetailsPage() {
    const params = useParams();
    const id = params.id as string;
    const [repository, setRepository] = useState<Repository | null>(null);
    const [files, setFiles] = useState<RepositoryFile[]>([]);
    const [readme, setReadme] = useState<string>('');
    const [isLoading, setIsLoading] = useState(true);

    useEffect(() => {
        fetchRepository();
    }, [id]);

    const fetchRepository = async () => {
        try {
            const [repoRes, filesRes] = await Promise.all([
                apiClient.get<Repository>(`/repositories/${id}`),
                apiClient.get<{ files: RepositoryFile[] }>(`/repositories/${id}/files`)
            ]);
            setRepository(repoRes.data);
            setFiles(filesRes.data.files || []);

            try {
                const readmeRes = await apiClient.get<{ content: string }>(`/repositories/${id}/files/content?path=README.md`);
                setReadme(readmeRes.data.content || '');
            } catch {
                setReadme('');
            }
        } catch (error) {
            console.error('Failed to fetch repository:', error);
        } finally {
            setIsLoading(false);
        }
    };

    const getFileIcon = (file: RepositoryFile) => {
        if (file.type === 'dir') return 'folder';
        const ext = file.name.split('.').pop()?.toLowerCase();
        switch (ext) {
            case 'js': case 'jsx': case 'ts': case 'tsx': return 'code';
            case 'json': return 'data_object';
            case 'md': return 'description';
            case 'css': case 'scss': return 'palette';
            case 'html': return 'html';
            case 'png': case 'jpg': case 'gif': return 'image';
            default: return 'draft';
        }
    };

    const getFileIconColor = (file: RepositoryFile) => {
        if (file.type === 'dir') return 'text-amber-500';
        const ext = file.name.split('.').pop()?.toLowerCase();
        switch (ext) {
            case 'js': case 'jsx': case 'ts': case 'tsx': return 'text-blue-500';
            case 'json': return 'text-amber-500';
            case 'md': return 'text-[#137fec]';
            case 'css': case 'scss': return 'text-purple-500';
            case 'html': return 'text-red-500';
            case 'png': case 'jpg': case 'gif': return 'text-green-500';
            default: return 'text-[#94a3b8]';
        }
    };

    if (isLoading) {
        return (
            <AppLayout showRightSidebar={false}>
                <div className="flex items-center justify-center py-20">
                    <div className="w-10 h-10 border-3 border-[rgba(19,127,236,0.2)] border-t-[#137fec] rounded-full animate-spin" />
                </div>
            </AppLayout>
        );
    }

    if (!repository) {
        return (
            <AppLayout showRightSidebar={false}>
                <div className="bg-white dark:bg-[var(--bg-secondary)] border border-[#e2e8f0] dark:border-[var(--border-color)] rounded-2xl p-12 text-center">
                    <span className="material-symbols-outlined text-5xl text-amber-500 mb-4 block">warning</span>
                    <h3 className="text-lg font-bold text-[#0f172a] dark:text-white mb-2">Repository not found</h3>
                    <Link href="/repositories" className="inline-flex items-center gap-2 bg-[#137fec] text-white px-5 py-2.5 rounded-xl font-bold text-sm hover:bg-[#1170d4] transition-all mt-4">
                        Back to Repositories
                    </Link>
                </div>
            </AppLayout>
        );
    }

    return (
        <AppLayout showRightSidebar={false}>
            {/* Header */}
            <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 mb-6">
                <div>
                    <div className="flex items-center gap-2 mb-1">
                        <Link href={`/users/${repository.ownerId}`} className="text-[#64748b] hover:text-[#137fec] transition-colors text-sm flex items-center gap-1.5">
                            <div className="w-5 h-5 rounded-full bg-gradient-to-br from-blue-500 to-purple-500 flex items-center justify-center text-white text-[10px] font-bold">
                                {repository.ownerUsername?.charAt(0).toUpperCase() || '?'}
                            </div>
                            {repository.ownerUsername}
                        </Link>
                        <span className="text-[#94a3b8]">/</span>
                        <h1 className="text-xl font-black text-[#0f172a] dark:text-white">{repository.repositoryName}</h1>
                        <span className={`px-2.5 py-0.5 rounded-full text-xs font-bold ${repository.visibility === 'Private' ? 'bg-red-50 dark:bg-red-500/10 text-red-600 dark:text-red-400' : 'bg-green-50 dark:bg-green-500/10 text-green-600 dark:text-green-400'}`}>
                            {repository.visibility}
                        </span>
                    </div>
                    {repository.description && (
                        <p className="text-[#475569] dark:text-[var(--text-secondary)] text-sm">{repository.description}</p>
                    )}
                </div>
                <div className="flex gap-2">
                    <button className="flex items-center gap-1.5 px-4 py-2 rounded-xl text-sm font-bold border border-[#e2e8f0] dark:border-[var(--border-color)] text-[#334155] dark:text-[var(--text-secondary)] hover:bg-[#f1f5f9] dark:hover:bg-[var(--bg-tertiary)] transition-all">
                        <span className="material-symbols-outlined text-base">visibility</span>Watch
                    </button>
                    <button className="flex items-center gap-1.5 px-4 py-2 rounded-xl text-sm font-bold border border-[#e2e8f0] dark:border-[var(--border-color)] text-[#334155] dark:text-[var(--text-secondary)] hover:bg-[#f1f5f9] dark:hover:bg-[var(--bg-tertiary)] transition-all">
                        <span className="material-symbols-outlined text-base">star</span>Star
                        <span className="bg-[#f1f5f9] dark:bg-[var(--bg-tertiary)] px-2 py-0.5 rounded-md text-xs">{repository.starsCount}</span>
                    </button>
                    <button className="flex items-center gap-1.5 px-4 py-2 rounded-xl text-sm font-bold border border-[#e2e8f0] dark:border-[var(--border-color)] text-[#334155] dark:text-[var(--text-secondary)] hover:bg-[#f1f5f9] dark:hover:bg-[var(--bg-tertiary)] transition-all">
                        <span className="material-symbols-outlined text-base">fork_right</span>Fork
                        <span className="bg-[#f1f5f9] dark:bg-[var(--bg-tertiary)] px-2 py-0.5 rounded-md text-xs">{repository.forksCount}</span>
                    </button>
                </div>
            </div>

            {/* Tabs */}
            <div className="flex gap-1 bg-[#f1f5f9] dark:bg-[var(--bg-tertiary)] rounded-xl p-1 mb-6 w-fit">
                <Link href={`/repositories/${id}`} className="flex items-center gap-1.5 px-4 py-2 rounded-lg text-sm font-bold bg-white dark:bg-[var(--bg-secondary)] text-[#0f172a] dark:text-white shadow-sm">
                    <span className="material-symbols-outlined text-base">code</span>Code
                </Link>
                <Link href={`/repositories/${id}/commits`} className="flex items-center gap-1.5 px-4 py-2 rounded-lg text-sm font-bold text-[#64748b] hover:text-[#0f172a] dark:hover:text-white transition-colors">
                    <span className="material-symbols-outlined text-base">history</span>Commits
                </Link>
                <Link href={`/repositories/${id}/branches`} className="flex items-center gap-1.5 px-4 py-2 rounded-lg text-sm font-bold text-[#64748b] hover:text-[#0f172a] dark:hover:text-white transition-colors">
                    <span className="material-symbols-outlined text-base">fork_right</span>Branches
                </Link>
            </div>

            <div className="grid lg:grid-cols-4 gap-6">
                <div className="lg:col-span-3 space-y-4">
                    {/* Branch & Clone bar */}
                    <div className="bg-white dark:bg-[var(--bg-secondary)] border border-[#e2e8f0] dark:border-[var(--border-color)] rounded-2xl p-3 flex justify-between items-center">
                        <div className="flex gap-2">
                            <button className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-sm font-bold border border-[#e2e8f0] dark:border-[var(--border-color)] text-[#334155] dark:text-[var(--text-secondary)] hover:bg-[#f1f5f9] dark:hover:bg-[var(--bg-tertiary)] transition">
                                <span className="material-symbols-outlined text-base text-[#137fec]">fork_right</span>
                                {repository.defaultBranch || 'main'}
                                <span className="material-symbols-outlined text-sm">expand_more</span>
                            </button>
                            <Link href={`/repositories/${id}/branches`} className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-sm font-bold border border-[#e2e8f0] dark:border-[var(--border-color)] text-[#334155] dark:text-[var(--text-secondary)] hover:bg-[#f1f5f9] dark:hover:bg-[var(--bg-tertiary)] transition">
                                <span className="material-symbols-outlined text-base">fork_right</span>Branches
                            </Link>
                        </div>
                        <button className="flex items-center gap-1.5 bg-green-600 text-white px-4 py-1.5 rounded-lg text-sm font-bold hover:bg-green-700 transition-all">
                            <span className="material-symbols-outlined text-base">download</span>Clone
                        </button>
                    </div>

                    {/* File Browser */}
                    <div className="bg-white dark:bg-[var(--bg-secondary)] border border-[#e2e8f0] dark:border-[var(--border-color)] rounded-2xl overflow-hidden">
                        <div className="px-4 py-3 border-b border-[#e2e8f0] dark:border-[var(--border-color)] text-[#64748b] text-sm">
                            {files.length} items
                        </div>
                        {files.sort((a, b) => {
                            if (a.type === 'dir' && b.type !== 'dir') return -1;
                            if (a.type !== 'dir' && b.type === 'dir') return 1;
                            return a.name.localeCompare(b.name);
                        }).map((file, index) => (
                            <Link
                                key={file.path}
                                href={`/repositories/${id}/files/${file.path}`}
                                className={`flex items-center gap-3 px-4 py-2.5 hover:bg-[#f8fafc] dark:hover:bg-[var(--bg-tertiary)] transition text-[#0f172a] dark:text-[var(--text-primary)] ${index > 0 ? 'border-t border-[#f1f5f9] dark:border-[var(--border-color)]' : ''}`}
                            >
                                <span className={`material-symbols-outlined text-lg ${getFileIconColor(file)}`}>{getFileIcon(file)}</span>
                                <span className="text-sm">{file.name}</span>
                                {file.size && <span className="ml-auto text-[#94a3b8] text-xs">{(file.size / 1024).toFixed(1)} KB</span>}
                            </Link>
                        ))}
                    </div>

                    {/* README */}
                    {readme && (
                        <div className="bg-white dark:bg-[var(--bg-secondary)] border border-[#e2e8f0] dark:border-[var(--border-color)] rounded-2xl overflow-hidden">
                            <div className="px-4 py-3 border-b border-[#e2e8f0] dark:border-[var(--border-color)] flex items-center gap-2 text-sm font-bold text-[#0f172a] dark:text-white">
                                <span className="material-symbols-outlined text-base">description</span>
                                README.md
                            </div>
                            <div className="p-6">
                                <div className="prose dark:prose-invert max-w-none" dangerouslySetInnerHTML={{ __html: readme }} />
                            </div>
                        </div>
                    )}
                </div>

                {/* Sidebar */}
                <div className="space-y-4">
                    <div className="bg-white dark:bg-[var(--bg-secondary)] border border-[#e2e8f0] dark:border-[var(--border-color)] rounded-2xl p-5">
                        <h3 className="text-sm font-bold text-[#0f172a] dark:text-white mb-3">About</h3>
                        <p className="text-[#475569] dark:text-[var(--text-secondary)] text-sm">{repository.description || 'No description provided'}</p>

                        {repository.cloneUrl && (
                            <div className="mt-4">
                                <p className="text-xs text-[#94a3b8] mb-1.5">Clone URL</p>
                                <div className="flex gap-1">
                                    <input type="text" value={repository.cloneUrl} readOnly className="flex-1 px-3 py-1.5 bg-[var(--bg-tertiary)] border border-[#e2e8f0] dark:border-[var(--border-color)] rounded-lg text-xs text-[var(--text-primary)] font-mono" />
                                    <button
                                        onClick={() => navigator.clipboard.writeText(repository.cloneUrl!)}
                                        className="px-2 py-1.5 rounded-lg border border-[#e2e8f0] dark:border-[var(--border-color)] text-[#64748b] hover:bg-[#f1f5f9] dark:hover:bg-[var(--bg-tertiary)] transition"
                                    >
                                        <span className="material-symbols-outlined text-sm">content_copy</span>
                                    </button>
                                </div>
                            </div>
                        )}

                        <div className="flex gap-4 mt-4 text-[#64748b] text-sm">
                            <span className="flex items-center gap-1">
                                <span className="material-symbols-outlined text-base">star</span>
                                {repository.starsCount} stars
                            </span>
                            <span className="flex items-center gap-1">
                                <span className="material-symbols-outlined text-base">fork_right</span>
                                {repository.forksCount} forks
                            </span>
                        </div>
                    </div>

                    {repository.language && (
                        <div className="bg-white dark:bg-[var(--bg-secondary)] border border-[#e2e8f0] dark:border-[var(--border-color)] rounded-2xl p-5">
                            <h3 className="text-sm font-bold text-[#0f172a] dark:text-white mb-3">Languages</h3>
                            <span className="px-3 py-1 rounded-full bg-[rgba(19,127,236,0.1)] text-xs font-bold text-[#137fec]">
                                {repository.language}
                            </span>
                        </div>
                    )}
                </div>
            </div>
        </AppLayout>
    );
}
