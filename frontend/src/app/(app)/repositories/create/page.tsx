'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { useQueryClient } from '@tanstack/react-query';
import { useAuth } from '@/lib/contexts/AuthContext';
import type { CreateRepositoryRequest } from '@/types';
import apiClient from '@/lib/api/client';
import AppLayout from '@/components/AppLayout';
import { authorInitial } from '@/lib/utils';

export default function CreateRepositoryPage() {
    const { user, isLoading: authLoading } = useAuth();
    const router = useRouter();
    const queryClient = useQueryClient();
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [error, setError] = useState('');
    const [formData, setFormData] = useState<CreateRepositoryRequest>({
        name: '',
        description: '',
        isPrivate: false,
    });

    if (authLoading) {
        return (
            <AppLayout showRightSidebar={false}>
                <div className="flex items-center justify-center py-20">
                    <div className="w-10 h-10 border-3 border-[rgba(19,127,236,0.2)] border-t-[#137fec] rounded-full animate-spin" />
                </div>
            </AppLayout>
        );
    }

    if (!user) {
        router.push('/auth?mode=login');
        return null;
    }

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setIsSubmitting(true);
        setError('');

        try {
            const response = await apiClient.post('/repositories', formData);
            await queryClient.invalidateQueries({ queryKey: ['repositories'] });
            const rawId = response.data?.repositoryId ?? response.data?.id;
            const newRepoId = typeof rawId === 'number' ? rawId : Number(rawId);
            if (Number.isFinite(newRepoId) && newRepoId > 0) {
                router.push(`/repositories/${newRepoId}`);
            } else {
                router.push('/repositories');
            }
        } catch (err: any) {
            setError(err.response?.data?.message || 'Failed to create repository');
        } finally {
            setIsSubmitting(false);
        }
    };

    return (
        <AppLayout showRightSidebar={false}>
            {/* Breadcrumb */}
            <nav className="flex items-center gap-2 text-sm text-[#64748b] mb-4">
                <Link href="/repositories" className="hover:text-[#137fec] transition-colors">Repositories</Link>
                <span>/</span>
                <span className="text-[#0f172a] dark:text-white font-medium">Create New</span>
            </nav>

            {/* Header */}
            <div className="mb-6">
                <h1 className="text-2xl font-black tracking-tight text-[#0f172a] dark:text-white flex items-center gap-2">
                    <span className="material-symbols-outlined text-[#137fec]">add_circle</span>
                    Create a new repository
                </h1>
                <p className="text-[#64748b] text-sm mt-1">A repository contains all project files, including the revision history.</p>
            </div>

            {/* Form */}
            <div className="bg-white dark:bg-[var(--bg-secondary)] border border-[#e2e8f0] dark:border-[var(--border-color)] rounded-2xl">
                <div className="p-6">
                    {error && (
                        <div className="mb-4 p-4 bg-red-50 dark:bg-red-500/10 border border-red-200 dark:border-red-500/30 rounded-xl text-red-600 dark:text-red-400 text-sm flex items-center gap-2">
                            <span className="material-symbols-outlined text-lg">error</span>
                            {error}
                        </div>
                    )}

                    <form onSubmit={handleSubmit} className="space-y-6">
                        {/* Owner + Name */}
                        <div>
                            <label className="block text-sm font-bold text-[#334155] dark:text-[var(--text-secondary)] mb-2">Owner / Repository name *</label>
                            <div className="flex items-center gap-3">
                                <div className="flex items-center gap-2 bg-[#f1f5f9] dark:bg-[var(--bg-tertiary)] rounded-full px-4 py-2.5 shrink-0">
                                    <div className="w-6 h-6 rounded-full bg-gradient-to-br from-blue-500 to-purple-500 flex items-center justify-center text-white text-xs font-bold">
                                        {authorInitial(user.displayName || user.username)}
                                    </div>
                                    <span className="text-sm font-medium text-[#0f172a] dark:text-white">{user.username}</span>
                                </div>
                                <span className="text-[#94a3b8] text-lg">/</span>
                                <input
                                    type="text"
                                    placeholder="repository-name"
                                    value={formData.name}
                                    onChange={(e) => setFormData({ ...formData, name: e.target.value.toLowerCase().replace(/\s+/g, '-') })}
                                    required
                                    pattern="[a-z0-9\-]+"
                                    title="Only lowercase letters, numbers, and hyphens are allowed"
                                    className="flex-1 px-4 py-2.5 bg-[var(--bg-tertiary)] border border-[#e2e8f0] dark:border-[var(--border-color)] rounded-xl text-[var(--text-primary)] placeholder-[#94a3b8] focus:border-[#137fec] focus:ring-2 focus:ring-[rgba(19,127,236,0.1)] outline-none transition"
                                />
                            </div>
                            <p className="text-xs text-[#94a3b8] mt-2">Great repository names are short and memorable.</p>
                        </div>

                        {/* Description */}
                        <div>
                            <label className="block text-sm font-bold text-[#334155] dark:text-[var(--text-secondary)] mb-2">Description (optional)</label>
                            <textarea
                                rows={3}
                                placeholder="A short description of what this repository is about..."
                                value={formData.description}
                                onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                                className="w-full px-4 py-3 bg-[var(--bg-tertiary)] border border-[#e2e8f0] dark:border-[var(--border-color)] rounded-xl text-[var(--text-primary)] placeholder-[#94a3b8] focus:border-[#137fec] focus:ring-2 focus:ring-[rgba(19,127,236,0.1)] outline-none transition resize-none"
                            />
                        </div>

                        {/* Visibility */}
                        <div>
                            <label className="block text-sm font-bold text-[#334155] dark:text-[var(--text-secondary)] mb-3">Visibility</label>
                            <div className="flex flex-col gap-3">
                                <label
                                    className={`flex items-center gap-4 p-4 rounded-xl border-2 cursor-pointer transition-all ${!formData.isPrivate ? 'border-[#137fec] bg-[rgba(19,127,236,0.05)]' : 'border-[#e2e8f0] dark:border-[var(--border-color)] hover:border-[#cbd5e1]'}`}
                                    onClick={() => setFormData({ ...formData, isPrivate: false })}
                                >
                                    <span className="material-symbols-outlined text-2xl text-green-500">lock_open</span>
                                    <div>
                                        <p className="font-bold text-[#0f172a] dark:text-white text-sm">Public</p>
                                        <p className="text-[#64748b] text-xs">Anyone on the internet can see this repository.</p>
                                    </div>
                                </label>
                                <label
                                    className={`flex items-center gap-4 p-4 rounded-xl border-2 cursor-pointer transition-all ${formData.isPrivate ? 'border-[#137fec] bg-[rgba(19,127,236,0.05)]' : 'border-[#e2e8f0] dark:border-[var(--border-color)] hover:border-[#cbd5e1]'}`}
                                    onClick={() => setFormData({ ...formData, isPrivate: true })}
                                >
                                    <span className="material-symbols-outlined text-2xl text-amber-500">lock</span>
                                    <div>
                                        <p className="font-bold text-[#0f172a] dark:text-white text-sm">Private</p>
                                        <p className="text-[#64748b] text-xs">You choose who can see and commit to this repository.</p>
                                    </div>
                                </label>
                            </div>
                        </div>

                        <div className="border-t border-[#e2e8f0] dark:border-[var(--border-color)] pt-6 flex justify-between">
                            <Link href="/repositories" className="px-5 py-2.5 rounded-xl font-bold text-sm border border-[#e2e8f0] dark:border-[var(--border-color)] text-[#334155] dark:text-[var(--text-secondary)] hover:bg-[#f1f5f9] dark:hover:bg-[var(--bg-tertiary)] transition-all">
                                Cancel
                            </Link>
                            <button
                                type="submit"
                                disabled={isSubmitting || !formData.name}
                                className="flex items-center gap-2 bg-[#137fec] text-white px-6 py-2.5 rounded-xl font-bold text-sm shadow-[0_4px_12px_rgba(19,127,236,0.2)] hover:bg-[#1170d4] transition-all disabled:opacity-50 disabled:cursor-not-allowed"
                            >
                                {isSubmitting ? (
                                    <>
                                        <div className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                                        Creating...
                                    </>
                                ) : (
                                    <>
                                        <span className="material-symbols-outlined text-lg">add_circle</span>
                                        Create repository
                                    </>
                                )}
                            </button>
                        </div>
                    </form>
                </div>
            </div>
        </AppLayout>
    );
}
