'use client';

import { useState } from 'react';
import Link from 'next/link';
import { useSearchParams } from 'next/navigation';
import { Suspense } from 'react';
import { useQuery } from '@tanstack/react-query';
import type { Repository } from '@/types';
import { repositoriesApi } from '@/lib/api/repositories.api';
import AppLayout from '@/components/AppLayout';
import RelativeTime from '@/components/RelativeTime';
import { authorInitial } from '@/lib/utils';

function RepositoriesContent() {
    const searchParams = useSearchParams();
    const [search, setSearch] = useState(searchParams.get('search') || '');

    const { data: repoData, isLoading, refetch: fetchRepositories } = useQuery({
        queryKey: ['repositories', search],
        queryFn: () => repositoriesApi.list({ page: 1, pageSize: 20, search: search || undefined }),
    });

    const repositories: Repository[] = repoData?.items || [];

    const gradients = [
        'from-blue-600 to-cyan-500',
        'from-purple-600 to-pink-500',
        'from-green-600 to-emerald-500',
        'from-indigo-600 to-violet-500',
        'from-orange-500 to-amber-500',
    ];

    return (
        <AppLayout>
            {/* Header */}
            <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
                <div>
                    <h1 className="text-2xl md:text-3xl font-black tracking-tight text-[#0f172a] dark:text-white flex items-center gap-2">
                        <span className="material-symbols-outlined text-[#137fec]">inventory_2</span>
                        Code Repositories
                    </h1>
                    <p className="text-[#64748b] text-sm mt-1">Explore and share code with the community</p>
                </div>
                <div className="flex gap-2">
                    <Link href="/repositories/create" className="flex items-center gap-2 bg-[#137fec] text-white px-5 py-2.5 rounded-xl font-bold text-sm shadow-[0_4px_12px_rgba(19,127,236,0.2)] hover:bg-[#1170d4] hover:-translate-y-px transition-all">
                        <span className="material-symbols-outlined text-lg">add_circle</span>
                        Create Repository
                    </Link>
                    <Link href="/repositories/my" className="flex items-center gap-2 px-5 py-2.5 rounded-xl font-bold text-sm border border-[#e2e8f0] dark:border-[var(--border-color)] text-[#334155] dark:text-[var(--text-secondary)] hover:bg-[#f1f5f9] dark:hover:bg-[var(--bg-tertiary)] transition-all">
                        <span className="material-symbols-outlined text-lg">person</span>
                        My Repositories
                    </Link>
                </div>
            </div>

            {/* Search */}
            <div className="bg-white dark:bg-[var(--bg-secondary)] border border-[#e2e8f0] dark:border-[var(--border-color)] rounded-2xl p-4">
                <form onSubmit={(e) => { e.preventDefault(); fetchRepositories(); }} className="flex gap-3">
                    <div className="flex-1 relative">
                        <span className="material-symbols-outlined absolute left-4 top-1/2 -translate-y-1/2 text-[#94a3b8]">search</span>
                        <input
                            type="text"
                            placeholder="Search repositories by name, description or language..."
                            value={search}
                            onChange={(e) => setSearch(e.target.value)}
                            className="w-full pl-12 pr-4 py-3 bg-[var(--bg-tertiary)] border border-[#e2e8f0] dark:border-[var(--border-color)] rounded-xl text-[var(--text-primary)] placeholder-[#94a3b8] focus:border-[#137fec] transition"
                        />
                    </div>
                    <button type="submit" className="px-5 py-3 bg-[#137fec] text-white rounded-xl font-bold text-sm hover:bg-[#1170d4] transition-all">
                        Search
                    </button>
                </form>
            </div>

            {/* Repository Grid */}
            {isLoading ? (
                <div className="flex items-center justify-center py-16">
                    <div className="w-10 h-10 border-3 border-[rgba(19,127,236,0.2)] border-t-[#137fec] rounded-full animate-spin" />
                </div>
            ) : repositories.length > 0 ? (
                <div className="grid md:grid-cols-2 gap-4">
                    {repositories.map((repo, index) => (
                        <div key={repo.repositoryId} className="bg-white dark:bg-[var(--bg-secondary)] border border-[#e2e8f0] dark:border-[var(--border-color)] rounded-2xl overflow-hidden hover:border-[rgba(19,127,236,0.5)] transition-all group">
                            {/* Gradient accent */}
                            <div className={`h-1.5 bg-gradient-to-r ${gradients[index % gradients.length]}`} />
                            <div className="p-5">
                                {/* Owner */}
                                <div className="flex items-center gap-2 mb-3">
                                    <div className={`w-7 h-7 rounded-full bg-gradient-to-br ${gradients[index % gradients.length]} flex items-center justify-center text-white text-xs font-bold`}>
                                        {authorInitial(repo.ownerUsername)}
                                    </div>
                                    <Link href={`/users/${repo.ownerId}`} className="text-sm text-[#64748b] hover:text-[#137fec] transition-colors">
                                        {repo.ownerUsername}
                                    </Link>
                                </div>

                                {/* Name */}
                                <Link href={`/repositories/${repo.repositoryId}`} className="text-lg font-bold text-[#0f172a] dark:text-white hover:text-[#137fec] dark:hover:text-[#137fec] transition-colors">
                                    {repo.repositoryName}
                                </Link>

                                {/* Description */}
                                <p className="text-[#475569] dark:text-[var(--text-secondary)] text-sm mt-2 line-clamp-2">
                                    {repo.description || <span className="italic text-[#94a3b8]">No description provided</span>}
                                </p>

                                {/* Badges */}
                                <div className="flex flex-wrap gap-2 mt-4">
                                    <span className="px-3 py-1 rounded-full bg-[#f1f5f9] dark:bg-[var(--bg-tertiary)] text-xs font-bold text-[#64748b]">
                                        {repo.defaultBranch || 'main'}
                                    </span>
                                    <span className={`px-3 py-1 rounded-full text-xs font-bold ${repo.visibility === 'Private'
                                        ? 'bg-red-50 dark:bg-red-500/10 text-red-600 dark:text-red-400'
                                        : 'bg-green-50 dark:bg-green-500/10 text-green-600 dark:text-green-400'
                                        }`}>
                                        {repo.visibility}
                                    </span>
                                    {repo.language && (
                                        <span className="px-3 py-1 rounded-full bg-[rgba(19,127,236,0.1)] text-xs font-bold text-[#137fec]">
                                            {repo.language}
                                        </span>
                                    )}
                                </div>

                                {/* Footer stats */}
                                <div className="flex items-center justify-between mt-4 pt-4 border-t border-[#e2e8f0] dark:border-[var(--border-color)]">
                                    <div className="flex gap-4 text-[#64748b] text-sm">
                                        <span className="flex items-center gap-1">
                                            <span className="material-symbols-outlined text-base">star</span>
                                            {repo.starsCount || 0}
                                        </span>
                                        <span className="flex items-center gap-1">
                                            <span className="material-symbols-outlined text-base">fork_right</span>
                                            {repo.forksCount || 0}
                                        </span>
                                    </div>
                                    <RelativeTime
                                        value={repo.updatedDate || repo.createdDate}
                                        prefix="Updated "
                                        className="text-xs text-[#94a3b8]"
                                    />
                                </div>
                            </div>
                        </div>
                    ))}
                </div>
            ) : (
                <div className="bg-white dark:bg-[var(--bg-secondary)] border border-[#e2e8f0] dark:border-[var(--border-color)] rounded-2xl p-12 text-center">
                    <span className="material-symbols-outlined text-5xl text-[#94a3b8] mb-4 block">inventory_2</span>
                    <h3 className="text-lg font-bold text-[#0f172a] dark:text-white mb-2">No repositories found</h3>
                    <p className="text-[#64748b] mb-4">
                        {search ? 'Try a different search term' : 'Be the first to create a repository!'}
                    </p>
                    <Link href="/repositories/create" className="inline-flex items-center gap-2 bg-[#137fec] text-white px-5 py-2.5 rounded-xl font-bold text-sm shadow-[0_4px_12px_rgba(19,127,236,0.2)] hover:bg-[#1170d4] transition-all">
                        <span className="material-symbols-outlined text-lg">add_circle</span>
                        Create Repository
                    </Link>
                </div>
            )}
        </AppLayout>
    );
}

export default function RepositoriesPage() {
    return (
        <Suspense fallback={
            <AppLayout>
                <div className="flex items-center justify-center py-20">
                    <div className="w-10 h-10 border-3 border-[rgba(19,127,236,0.2)] border-t-[#137fec] rounded-full animate-spin" />
                </div>
            </AppLayout>
        }>
            <RepositoriesContent />
        </Suspense>
    );
}
