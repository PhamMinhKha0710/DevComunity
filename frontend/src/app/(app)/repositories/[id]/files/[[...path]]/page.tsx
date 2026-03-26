'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useParams } from 'next/navigation';
import type { RepositoryFile } from '@/types';
import apiClient from '@/lib/api/client';
import AppLayout from '@/components/AppLayout';

export default function FileViewPage() {
    const params = useParams();
    const router = useRouter();
    const id = params.id as string;
    const isValidId = id && id !== '0' && !isNaN(Number(id));
    const pathSegments = params.path as string[] || [];
    const filePath = pathSegments.join('/');

    useEffect(() => {
        if (!isValidId) {
            router.replace('/repositories');
        }
    }, [isValidId, router]);

    const [content, setContent] = useState<string>('');
    const [fileInfo, setFileInfo] = useState<RepositoryFile | null>(null);
    const [files, setFiles] = useState<RepositoryFile[]>([]);
    const [isDirectory, setIsDirectory] = useState(false);
    const [isLoading, setIsLoading] = useState(true);

    useEffect(() => {
        if (isValidId) fetchContent();
    }, [id, filePath]);

    const fetchContent = async () => {
        try {
            const response = await apiClient.get<{ content?: string; files?: RepositoryFile[] }>(
                `/repositories/${id}/files/content?path=${encodeURIComponent(filePath)}`
            );

            if (response.data.files) {
                setIsDirectory(true);
                setFiles(response.data.files);
            } else {
                setIsDirectory(false);
                setContent(response.data.content || '');
            }
        } catch (error) {
            console.error('Failed to fetch content:', error);
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

    const breadcrumbParts = filePath.split('/').filter(Boolean);

    return (
        <AppLayout showRightSidebar={false}>
            {/* Breadcrumb */}
            <nav className="flex items-center gap-2 text-sm text-[#64748b] mb-6 flex-wrap">
                <Link href="/repositories" className="hover:text-[#137fec] transition-colors">Repositories</Link>
                <span>/</span>
                <Link href={`/repositories/${id}`} className="hover:text-[#137fec] transition-colors">Repository</Link>
                {breadcrumbParts.map((part, index) => {
                    const partPath = breadcrumbParts.slice(0, index + 1).join('/');
                    const isLast = index === breadcrumbParts.length - 1;
                    return (
                        <span key={part} className="flex items-center gap-2">
                            <span>/</span>
                            {isLast ? (
                                <span className="text-[#0f172a] dark:text-white font-medium">{part}</span>
                            ) : (
                                <Link href={`/repositories/${id}/files/${partPath}`} className="hover:text-[#137fec] transition-colors">{part}</Link>
                            )}
                        </span>
                    );
                })}
            </nav>

            {isDirectory ? (
                <div className="bg-white dark:bg-[var(--bg-secondary)] border border-[#e2e8f0] dark:border-[var(--border-color)] rounded-2xl overflow-hidden">
                    <div className="px-4 py-3 border-b border-[#e2e8f0] dark:border-[var(--border-color)] flex items-center gap-2 text-sm font-bold text-[#0f172a] dark:text-white">
                        <span className="material-symbols-outlined text-amber-500 text-base">folder</span>
                        {filePath || 'Root'}
                    </div>
                    {pathSegments.length > 0 && (
                        <Link
                            href={`/repositories/${id}/files/${breadcrumbParts.slice(0, -1).join('/')}`}
                            className="flex items-center gap-3 px-4 py-2.5 hover:bg-[#f8fafc] dark:hover:bg-[var(--bg-tertiary)] transition text-[#64748b] border-b border-[#f1f5f9] dark:border-[var(--border-color)]"
                        >
                            <span className="material-symbols-outlined text-lg">arrow_upward</span>
                            <span className="text-sm">..</span>
                        </Link>
                    )}
                    {files.sort((a, b) => {
                        if (a.type === 'dir' && b.type !== 'dir') return -1;
                        if (a.type !== 'dir' && b.type === 'dir') return 1;
                        return a.name.localeCompare(b.name);
                    }).map((file, index) => (
                        <Link
                            key={file.path}
                            href={`/repositories/${id}/files/${file.path}`}
                            className={`flex items-center gap-3 px-4 py-2.5 hover:bg-[#f8fafc] dark:hover:bg-[var(--bg-tertiary)] transition text-[#0f172a] dark:text-[var(--text-primary)] ${index > 0 || pathSegments.length > 0 ? 'border-t border-[#f1f5f9] dark:border-[var(--border-color)]' : ''}`}
                        >
                            <span className={`material-symbols-outlined text-lg ${getFileIconColor(file)}`}>{getFileIcon(file)}</span>
                            <span className="text-sm">{file.name}</span>
                        </Link>
                    ))}
                </div>
            ) : (
                <div className="bg-white dark:bg-[var(--bg-secondary)] border border-[#e2e8f0] dark:border-[var(--border-color)] rounded-2xl overflow-hidden">
                    <div className="px-4 py-3 border-b border-[#e2e8f0] dark:border-[var(--border-color)] flex items-center justify-between">
                        <div className="flex items-center gap-2 text-sm font-bold text-[#0f172a] dark:text-white">
                            <span className="material-symbols-outlined text-blue-500 text-base">code</span>
                            {pathSegments[pathSegments.length - 1] || 'File'}
                        </div>
                        <div className="flex gap-2">
                            <button className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-bold border border-[#e2e8f0] dark:border-[var(--border-color)] text-[#334155] dark:text-[var(--text-secondary)] hover:bg-[#f1f5f9] dark:hover:bg-[var(--bg-tertiary)] transition">
                                <span className="material-symbols-outlined text-sm">edit</span>Edit
                            </button>
                            <button className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-bold border border-[#e2e8f0] dark:border-[var(--border-color)] text-[#334155] dark:text-[var(--text-secondary)] hover:bg-[#f1f5f9] dark:hover:bg-[var(--bg-tertiary)] transition">
                                <span className="material-symbols-outlined text-sm">download</span>Download
                            </button>
                        </div>
                    </div>
                    <pre className="m-0 p-4 bg-[#f8fafc] dark:bg-[var(--bg-tertiary)] overflow-auto text-sm font-mono text-[#334155] dark:text-[var(--text-secondary)]" style={{ maxHeight: '600px' }}>
                        <code>{content}</code>
                    </pre>
                </div>
            )}
        </AppLayout>
    );
}
