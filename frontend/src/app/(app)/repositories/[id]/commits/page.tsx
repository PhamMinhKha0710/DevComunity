'use client';

import Link from 'next/link';
import { useParams } from 'next/navigation';
import { useQuery } from '@tanstack/react-query';
import type { RepositoryCommit } from '@/types';
import { repositoriesApi } from '@/lib/api/repositories.api';
import AppLayout from '@/components/AppLayout';

export default function CommitsPage() {
    const params = useParams();
    const id = params.id as string;

    const { data: commitsData, isLoading } = useQuery({
        queryKey: ['repository', id, 'commits'],
        queryFn: () => repositoriesApi.getCommits(id),
    });

    const commits: RepositoryCommit[] = commitsData?.items || [];

    const formatDate = (dateString: string) => {
        const date = new Date(dateString);
        return date.toLocaleDateString('en-US', {
            year: 'numeric', month: 'short', day: 'numeric',
            hour: '2-digit', minute: '2-digit'
        });
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

    return (
        <AppLayout showRightSidebar={false}>
            {/* Breadcrumb */}
            <nav className="flex items-center gap-2 text-sm text-[#64748b] mb-4">
                <Link href="/repositories" className="hover:text-[#137fec] transition-colors">Repositories</Link>
                <span>/</span>
                <Link href={`/repositories/${id}`} className="hover:text-[#137fec] transition-colors">Repository</Link>
                <span>/</span>
                <span className="text-[#0f172a] dark:text-white font-medium">Commits</span>
            </nav>

            {/* Tabs */}
            <div className="flex gap-1 bg-[#f1f5f9] dark:bg-[var(--bg-tertiary)] rounded-xl p-1 mb-6 w-fit">
                <Link href={`/repositories/${id}`} className="flex items-center gap-1.5 px-4 py-2 rounded-lg text-sm font-bold text-[#64748b] hover:text-[#0f172a] dark:hover:text-white transition-colors">
                    <span className="material-symbols-outlined text-base">code</span>Code
                </Link>
                <Link href={`/repositories/${id}/commits`} className="flex items-center gap-1.5 px-4 py-2 rounded-lg text-sm font-bold bg-white dark:bg-[var(--bg-secondary)] text-[#0f172a] dark:text-white shadow-sm">
                    <span className="material-symbols-outlined text-base">history</span>Commits
                </Link>
                <Link href={`/repositories/${id}/branches`} className="flex items-center gap-1.5 px-4 py-2 rounded-lg text-sm font-bold text-[#64748b] hover:text-[#0f172a] dark:hover:text-white transition-colors">
                    <span className="material-symbols-outlined text-base">fork_right</span>Branches
                </Link>
            </div>

            <h2 className="text-xl font-black tracking-tight text-[#0f172a] dark:text-white flex items-center gap-2 mb-6">
                <span className="material-symbols-outlined text-[#137fec]">history</span>
                Commit History
            </h2>

            {/* Commits List */}
            <div className="bg-white dark:bg-[var(--bg-secondary)] border border-[#e2e8f0] dark:border-[var(--border-color)] rounded-2xl overflow-hidden">
                {commits.length > 0 ? (
                    commits.map((commit, index) => (
                        <div key={commit.sha} className={`p-5 flex items-center gap-4 ${index > 0 ? 'border-t border-[#e2e8f0] dark:border-[var(--border-color)]' : ''}`}>
                            <div className="w-10 h-10 rounded-full bg-gradient-to-br from-blue-500 to-purple-500 flex items-center justify-center text-white text-sm font-bold shrink-0">
                                {commit.authorName?.charAt(0).toUpperCase() || '?'}
                            </div>
                            <div className="flex-1 min-w-0">
                                <p className="font-bold text-[#0f172a] dark:text-white text-sm truncate">{commit.message}</p>
                                <div className="flex items-center gap-2 text-[#64748b] text-xs mt-1">
                                    <span className="font-medium">{commit.authorName}</span>
                                    <span>committed {formatDate(commit.committedDate)}</span>
                                </div>
                            </div>
                            <div className="flex gap-2 items-center shrink-0">
                                {commit.additions !== undefined && commit.deletions !== undefined && (
                                    <span className="text-xs">
                                        <span className="text-green-500 font-bold">+{commit.additions}</span>
                                        {' / '}
                                        <span className="text-red-500 font-bold">-{commit.deletions}</span>
                                    </span>
                                )}
                                <code className="bg-[#f1f5f9] dark:bg-[var(--bg-tertiary)] px-2.5 py-1 rounded-lg text-xs font-mono text-[#334155] dark:text-[var(--text-secondary)]">{commit.sha.substring(0, 7)}</code>
                                <button
                                    onClick={() => navigator.clipboard.writeText(commit.sha)}
                                    className="w-7 h-7 rounded-lg flex items-center justify-center text-[#94a3b8] hover:bg-[#f1f5f9] dark:hover:bg-[var(--bg-tertiary)] transition"
                                >
                                    <span className="material-symbols-outlined text-sm">content_copy</span>
                                </button>
                            </div>
                        </div>
                    ))
                ) : (
                    <div className="p-12 text-center">
                        <span className="material-symbols-outlined text-5xl text-[#94a3b8] mb-4 block">history</span>
                        <h3 className="text-lg font-bold text-[#0f172a] dark:text-white mb-2">No commits yet</h3>
                        <p className="text-[#64748b]">This repository has no commits.</p>
                    </div>
                )}
            </div>
        </AppLayout>
    );
}
