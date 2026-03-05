'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useAuth } from '@/lib/contexts/AuthContext';
import type { Repository, PaginatedResponse } from '@/types';
import apiClient from '@/lib/api/client';
import AppLayout from '@/components/AppLayout';

export default function MyRepositoriesPage() {
    const { user, isLoading: authLoading } = useAuth();
    const router = useRouter();
    const [repositories, setRepositories] = useState<Repository[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [menuOpen, setMenuOpen] = useState<number | null>(null);

    useEffect(() => {
        if (!authLoading && !user) {
            router.push('/login');
        } else if (user) {
            fetchMyRepositories();
        }
    }, [user, authLoading, router]);

    const fetchMyRepositories = async () => {
        try {
            const response = await apiClient.get<PaginatedResponse<Repository>>(`/repositories?ownerId=${user?.userId}`);
            setRepositories(response.data.items || []);
        } catch (error) {
            console.error('Failed to fetch repositories:', error);
        } finally {
            setIsLoading(false);
        }
    };

    if (authLoading || isLoading) {
        return (
            <AppLayout showRightSidebar={false}>
                <div className="flex items-center justify-center py-20">
                    <div className="w-10 h-10 border-3 border-[rgba(19,127,236,0.2)] border-t-[#137fec] rounded-full animate-spin" />
                </div>
            </AppLayout>
        );
    }

    if (!user) return null;

    return (
        <AppLayout showRightSidebar={false}>
            {/* Breadcrumb */}
            <nav className="flex items-center gap-2 text-sm text-[#64748b] mb-4">
                <Link href="/repositories" className="hover:text-[#137fec] transition-colors">Repositories</Link>
                <span>/</span>
                <span className="text-[#0f172a] dark:text-white font-medium">My Repositories</span>
            </nav>

            {/* Header */}
            <div className="flex items-center justify-between mb-6">
                <div>
                    <h1 className="text-2xl font-black tracking-tight text-[#0f172a] dark:text-white flex items-center gap-2">
                        <span className="material-symbols-outlined text-[#137fec]">person</span>
                        My Repositories
                    </h1>
                    <p className="text-[#64748b] text-sm mt-1">Manage your code repositories</p>
                </div>
                <Link href="/repositories/create" className="flex items-center gap-2 bg-[#137fec] text-white px-5 py-2.5 rounded-xl font-bold text-sm shadow-[0_4px_12px_rgba(19,127,236,0.2)] hover:bg-[#1170d4] hover:-translate-y-px transition-all">
                    <span className="material-symbols-outlined text-lg">add_circle</span>
                    New Repository
                </Link>
            </div>

            {/* Repository List */}
            {repositories.length > 0 ? (
                <div className="bg-white dark:bg-[var(--bg-secondary)] border border-[#e2e8f0] dark:border-[var(--border-color)] rounded-2xl overflow-hidden">
                    {repositories.map((repo, index) => (
                        <div key={repo.repositoryId} className={`p-5 flex items-center gap-4 hover:bg-[#f8fafc] dark:hover:bg-[var(--bg-tertiary)] transition ${index > 0 ? 'border-t border-[#e2e8f0] dark:border-[var(--border-color)]' : ''}`}>
                            <div className="flex-1 min-w-0">
                                <div className="flex items-center gap-2 mb-1.5">
                                    <span className={`material-symbols-outlined text-base ${repo.visibility === 'Private' ? 'text-amber-500' : 'text-green-500'}`}>
                                        {repo.visibility === 'Private' ? 'lock' : 'lock_open'}
                                    </span>
                                    <Link href={`/repositories/${repo.repositoryId}`} className="text-lg font-bold text-[#0f172a] dark:text-white hover:text-[#137fec] transition-colors truncate">
                                        {repo.repositoryName}
                                    </Link>
                                    <span className={`px-2.5 py-0.5 rounded-full text-xs font-bold ${repo.visibility === 'Private' ? 'bg-red-50 dark:bg-red-500/10 text-red-600 dark:text-red-400' : 'bg-green-50 dark:bg-green-500/10 text-green-600 dark:text-green-400'}`}>
                                        {repo.visibility}
                                    </span>
                                </div>
                                <p className="text-[#475569] dark:text-[var(--text-secondary)] text-sm mb-2 truncate">
                                    {repo.description || <span className="italic text-[#94a3b8]">No description</span>}
                                </p>
                                <div className="flex gap-4 text-[#64748b] text-xs">
                                    {repo.language && (
                                        <span className="flex items-center gap-1">
                                            <span className="w-2 h-2 rounded-full bg-[#137fec]" />
                                            {repo.language}
                                        </span>
                                    )}
                                    <span className="flex items-center gap-1">
                                        <span className="material-symbols-outlined text-sm">star</span>
                                        {repo.starsCount || 0}
                                    </span>
                                    <span className="flex items-center gap-1">
                                        <span className="material-symbols-outlined text-sm">fork_right</span>
                                        {repo.forksCount || 0}
                                    </span>
                                    <span>Updated {new Date(repo.updatedDate || repo.createdDate).toLocaleDateString()}</span>
                                </div>
                            </div>

                            {/* Actions menu */}
                            <div className="relative">
                                <button
                                    onClick={() => setMenuOpen(menuOpen === repo.repositoryId ? null : repo.repositoryId)}
                                    className="w-8 h-8 rounded-lg flex items-center justify-center text-[#94a3b8] hover:bg-[#f1f5f9] dark:hover:bg-[var(--bg-tertiary)] transition"
                                >
                                    <span className="material-symbols-outlined text-lg">more_horiz</span>
                                </button>
                                {menuOpen === repo.repositoryId && (
                                    <div className="absolute right-0 top-10 bg-white dark:bg-[var(--bg-secondary)] border border-[#e2e8f0] dark:border-[var(--border-color)] rounded-xl shadow-lg py-1 min-w-[160px] z-10">
                                        <Link href={`/repositories/${repo.repositoryId}`} className="flex items-center gap-2 px-4 py-2 text-sm text-[#334155] dark:text-[var(--text-secondary)] hover:bg-[#f1f5f9] dark:hover:bg-[var(--bg-tertiary)] transition">
                                            <span className="material-symbols-outlined text-base">visibility</span>
                                            View
                                        </Link>
                                        <Link href={`/repositories/${repo.repositoryId}/settings`} className="flex items-center gap-2 px-4 py-2 text-sm text-[#334155] dark:text-[var(--text-secondary)] hover:bg-[#f1f5f9] dark:hover:bg-[var(--bg-tertiary)] transition">
                                            <span className="material-symbols-outlined text-base">settings</span>
                                            Settings
                                        </Link>
                                        <div className="border-t border-[#e2e8f0] dark:border-[var(--border-color)] my-1" />
                                        <button className="flex items-center gap-2 px-4 py-2 text-sm text-red-500 hover:bg-red-50 dark:hover:bg-red-500/10 w-full text-left transition">
                                            <span className="material-symbols-outlined text-base">delete</span>
                                            Delete
                                        </button>
                                    </div>
                                )}
                            </div>
                        </div>
                    ))}
                </div>
            ) : (
                <div className="bg-white dark:bg-[var(--bg-secondary)] border border-[#e2e8f0] dark:border-[var(--border-color)] rounded-2xl p-12 text-center">
                    <span className="material-symbols-outlined text-5xl text-[#94a3b8] mb-4 block">inventory_2</span>
                    <h3 className="text-lg font-bold text-[#0f172a] dark:text-white mb-2">You don&apos;t have any repositories yet</h3>
                    <p className="text-[#64748b] mb-4">Create your first repository to start sharing code.</p>
                    <Link href="/repositories/create" className="inline-flex items-center gap-2 bg-[#137fec] text-white px-5 py-2.5 rounded-xl font-bold text-sm shadow-[0_4px_12px_rgba(19,127,236,0.2)] hover:bg-[#1170d4] transition-all">
                        <span className="material-symbols-outlined text-lg">add_circle</span>
                        Create your first repository
                    </Link>
                </div>
            )}
        </AppLayout>
    );
}
