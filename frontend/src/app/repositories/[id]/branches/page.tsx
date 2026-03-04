'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { useParams } from 'next/navigation';
import apiClient from '@/lib/api/client';
import AppLayout from '@/components/AppLayout';

interface Branch {
    name: string;
    isDefault: boolean;
    lastCommit?: {
        sha: string;
        message: string;
        date: string;
    };
}

export default function BranchesPage() {
    const params = useParams();
    const id = params.id as string;
    const [branches, setBranches] = useState<Branch[]>([]);
    const [isLoading, setIsLoading] = useState(true);

    useEffect(() => {
        fetchBranches();
    }, [id]);

    const fetchBranches = async () => {
        try {
            setBranches([
                { name: 'main', isDefault: true, lastCommit: { sha: 'abc1234', message: 'Initial commit', date: new Date().toISOString() } },
                { name: 'develop', isDefault: false },
            ]);
        } catch (error) {
            console.error('Failed to fetch branches:', error);
        } finally {
            setIsLoading(false);
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

    return (
        <AppLayout showRightSidebar={false}>
            {/* Breadcrumb */}
            <nav className="flex items-center gap-2 text-sm text-[#64748b] mb-4">
                <Link href="/repositories" className="hover:text-[#137fec] transition-colors">Repositories</Link>
                <span>/</span>
                <Link href={`/repositories/${id}`} className="hover:text-[#137fec] transition-colors">Repository</Link>
                <span>/</span>
                <span className="text-[#0f172a] dark:text-white font-medium">Branches</span>
            </nav>

            {/* Tabs */}
            <div className="flex gap-1 bg-[#f1f5f9] dark:bg-[var(--bg-tertiary)] rounded-xl p-1 mb-6 w-fit">
                <Link href={`/repositories/${id}`} className="flex items-center gap-1.5 px-4 py-2 rounded-lg text-sm font-bold text-[#64748b] hover:text-[#0f172a] dark:hover:text-white transition-colors">
                    <span className="material-symbols-outlined text-base">code</span>Code
                </Link>
                <Link href={`/repositories/${id}/commits`} className="flex items-center gap-1.5 px-4 py-2 rounded-lg text-sm font-bold text-[#64748b] hover:text-[#0f172a] dark:hover:text-white transition-colors">
                    <span className="material-symbols-outlined text-base">history</span>Commits
                </Link>
                <Link href={`/repositories/${id}/branches`} className="flex items-center gap-1.5 px-4 py-2 rounded-lg text-sm font-bold bg-white dark:bg-[var(--bg-secondary)] text-[#0f172a] dark:text-white shadow-sm">
                    <span className="material-symbols-outlined text-base">fork_right</span>Branches
                </Link>
            </div>

            {/* Header */}
            <div className="flex items-center justify-between mb-6">
                <h2 className="text-xl font-black tracking-tight text-[#0f172a] dark:text-white flex items-center gap-2">
                    <span className="material-symbols-outlined text-[#137fec]">fork_right</span>
                    Branches
                </h2>
                <button className="flex items-center gap-2 bg-[#137fec] text-white px-5 py-2.5 rounded-xl font-bold text-sm shadow-[0_4px_12px_rgba(19,127,236,0.2)] hover:bg-[#1170d4] hover:-translate-y-px transition-all">
                    <span className="material-symbols-outlined text-lg">add_circle</span>
                    New Branch
                </button>
            </div>

            {/* Branches List */}
            <div className="bg-white dark:bg-[var(--bg-secondary)] border border-[#e2e8f0] dark:border-[var(--border-color)] rounded-2xl overflow-hidden">
                {branches.length > 0 ? (
                    branches.map((branch, index) => (
                        <div key={branch.name} className={`p-5 flex items-center justify-between ${index > 0 ? 'border-t border-[#e2e8f0] dark:border-[var(--border-color)]' : ''}`}>
                            <div>
                                <div className="flex items-center gap-2 mb-1">
                                    <span className="material-symbols-outlined text-base text-[#137fec]">fork_right</span>
                                    <span className="font-bold text-[#0f172a] dark:text-white">{branch.name}</span>
                                    {branch.isDefault && (
                                        <span className="px-2.5 py-0.5 rounded-full text-xs font-bold bg-green-50 dark:bg-green-500/10 text-green-600 dark:text-green-400">Default</span>
                                    )}
                                </div>
                                {branch.lastCommit && (
                                    <p className="text-[#64748b] text-xs mt-1">
                                        Last commit: {branch.lastCommit.message} ({branch.lastCommit.sha.substring(0, 7)})
                                    </p>
                                )}
                            </div>
                            <div className="flex gap-2">
                                <Link href={`/repositories/${id}?branch=${branch.name}`} className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-bold border border-[#e2e8f0] dark:border-[var(--border-color)] text-[#334155] dark:text-[var(--text-secondary)] hover:bg-[#f1f5f9] dark:hover:bg-[var(--bg-tertiary)] transition">
                                    <span className="material-symbols-outlined text-sm">visibility</span>Files
                                </Link>
                                <Link href={`/repositories/${id}/commits?branch=${branch.name}`} className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-bold border border-[#e2e8f0] dark:border-[var(--border-color)] text-[#334155] dark:text-[var(--text-secondary)] hover:bg-[#f1f5f9] dark:hover:bg-[var(--bg-tertiary)] transition">
                                    <span className="material-symbols-outlined text-sm">history</span>Commits
                                </Link>
                                {!branch.isDefault && (
                                    <button className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-bold border border-red-200 dark:border-red-500/30 text-red-500 hover:bg-red-50 dark:hover:bg-red-500/10 transition">
                                        <span className="material-symbols-outlined text-sm">delete</span>Delete
                                    </button>
                                )}
                            </div>
                        </div>
                    ))
                ) : (
                    <div className="p-12 text-center">
                        <span className="material-symbols-outlined text-5xl text-[#94a3b8] mb-4 block">fork_right</span>
                        <h3 className="text-lg font-bold text-[#0f172a] dark:text-white mb-2">No branches</h3>
                        <p className="text-[#64748b]">This repository has no branches yet.</p>
                    </div>
                )}
            </div>
        </AppLayout>
    );
}
