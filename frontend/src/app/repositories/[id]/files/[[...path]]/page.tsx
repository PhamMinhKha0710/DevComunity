'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { useParams } from 'next/navigation';
import type { RepositoryFile } from '@/types';
import apiClient from '@/lib/api/client';

export default function FileViewPage() {
    const params = useParams();
    const id = params.id as string;
    const pathSegments = params.path as string[] || [];
    const filePath = pathSegments.join('/');

    const [content, setContent] = useState<string>('');
    const [fileInfo, setFileInfo] = useState<RepositoryFile | null>(null);
    const [files, setFiles] = useState<RepositoryFile[]>([]);
    const [isDirectory, setIsDirectory] = useState(false);
    const [isLoading, setIsLoading] = useState(true);

    useEffect(() => {
        fetchContent();
    }, [id, filePath]);

    const fetchContent = async () => {
        try {
            // First try to get as file content
            const response = await apiClient.get<{ content?: string; files?: RepositoryFile[] }>(
                `/repositories/${id}/files/content?path=${encodeURIComponent(filePath)}`
            );

            if (response.data.files) {
                // It's a directory
                setIsDirectory(true);
                setFiles(response.data.files);
            } else {
                // It's a file
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
        if (file.type === 'dir') return 'bi-folder-fill text-warning';
        const ext = file.name.split('.').pop()?.toLowerCase();
        switch (ext) {
            case 'js':
            case 'jsx':
            case 'ts':
            case 'tsx': return 'bi-file-earmark-code text-info';
            case 'json': return 'bi-file-earmark-code text-warning';
            case 'md': return 'bi-markdown text-primary';
            case 'css':
            case 'scss': return 'bi-filetype-css';
            case 'html': return 'bi-filetype-html text-danger';
            default: return 'bi-file-earmark text-muted';
        }
    };

    const getLanguage = (filename: string) => {
        const ext = filename.split('.').pop()?.toLowerCase();
        switch (ext) {
            case 'js':
            case 'jsx': return 'javascript';
            case 'ts':
            case 'tsx': return 'typescript';
            case 'json': return 'json';
            case 'css': return 'css';
            case 'html': return 'html';
            case 'md': return 'markdown';
            case 'py': return 'python';
            case 'cs': return 'csharp';
            default: return 'plaintext';
        }
    };

    if (isLoading) {
        return (
            <div className="container py-5 text-center">
                <div className="spinner-border text-primary" role="status">
                    <span className="visually-hidden">Loading...</span>
                </div>
            </div>
        );
    }

    // Build breadcrumb parts
    const breadcrumbParts = filePath.split('/').filter(Boolean);

    return (
        <div className="container py-4">
            {/* Breadcrumb */}
            <nav aria-label="breadcrumb" className="mb-4">
                <ol className="breadcrumb">
                    <li className="breadcrumb-item"><Link href="/repositories">Repositories</Link></li>
                    <li className="breadcrumb-item"><Link href={`/repositories/${id}`}>Repository</Link></li>
                    {breadcrumbParts.map((part, index) => {
                        const partPath = breadcrumbParts.slice(0, index + 1).join('/');
                        const isLast = index === breadcrumbParts.length - 1;
                        return isLast ? (
                            <li key={part} className="breadcrumb-item active">{part}</li>
                        ) : (
                            <li key={part} className="breadcrumb-item">
                                <Link href={`/repositories/${id}/files/${partPath}`}>{part}</Link>
                            </li>
                        );
                    })}
                </ol>
            </nav>

            {isDirectory ? (
                // Directory listing
                <div className="card border-0 shadow-sm rounded-4">
                    <div className="card-header bg-white">
                        <i className="bi bi-folder-fill text-warning me-2"></i>
                        {filePath || 'Root'}
                    </div>
                    <div className="list-group list-group-flush">
                        {pathSegments.length > 0 && (
                            <Link
                                href={`/repositories/${id}/files/${breadcrumbParts.slice(0, -1).join('/')}`}
                                className="list-group-item list-group-item-action d-flex align-items-center py-2"
                            >
                                <i className="bi bi-arrow-90deg-up me-3 text-muted"></i>
                                <span>..</span>
                            </Link>
                        )}
                        {files.sort((a, b) => {
                            if (a.type === 'dir' && b.type !== 'dir') return -1;
                            if (a.type !== 'dir' && b.type === 'dir') return 1;
                            return a.name.localeCompare(b.name);
                        }).map((file) => (
                            <Link
                                key={file.path}
                                href={`/repositories/${id}/files/${file.path}`}
                                className="list-group-item list-group-item-action d-flex align-items-center py-2"
                            >
                                <i className={`bi ${getFileIcon(file)} me-3`}></i>
                                <span>{file.name}</span>
                            </Link>
                        ))}
                    </div>
                </div>
            ) : (
                // File content
                <div className="card border-0 shadow-sm rounded-4">
                    <div className="card-header bg-white d-flex justify-content-between align-items-center">
                        <div>
                            <i className="bi bi-file-earmark-code me-2"></i>
                            {pathSegments[pathSegments.length - 1] || 'File'}
                        </div>
                        <div className="d-flex gap-2">
                            <button className="btn btn-sm btn-outline-secondary rounded-pill">
                                <i className="bi bi-pencil me-1"></i>Edit
                            </button>
                            <button className="btn btn-sm btn-outline-secondary rounded-pill">
                                <i className="bi bi-download me-1"></i>Download
                            </button>
                        </div>
                    </div>
                    <div className="card-body p-0">
                        <pre className="m-0 p-4 bg-light" style={{ maxHeight: '600px', overflow: 'auto' }}>
                            <code>{content}</code>
                        </pre>
                    </div>
                </div>
            )}
        </div>
    );
}
