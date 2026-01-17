'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { useParams } from 'next/navigation';
import type { Repository, RepositoryFile } from '@/types';
import apiClient from '@/lib/api/client';

export default function RepositoryDetailsPage() {
    const params = useParams();
    const id = params.id as string;
    const [repository, setRepository] = useState<Repository | null>(null);
    const [files, setFiles] = useState<RepositoryFile[]>([]);
    const [readme, setReadme] = useState<string>('');
    const [isLoading, setIsLoading] = useState(true);
    const [activeTab, setActiveTab] = useState<'code' | 'readme'>('code');

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

            // Try to fetch README
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
            case 'scss': return 'bi-filetype-css text-purple';
            case 'html': return 'bi-filetype-html text-danger';
            case 'png':
            case 'jpg':
            case 'gif': return 'bi-file-earmark-image text-success';
            default: return 'bi-file-earmark text-muted';
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

    if (!repository) {
        return (
            <div className="container py-5 text-center">
                <i className="bi bi-exclamation-triangle fs-1 text-warning mb-3"></i>
                <h3>Repository not found</h3>
                <Link href="/repositories" className="btn btn-primary mt-3">Back to Repositories</Link>
            </div>
        );
    }

    return (
        <div className="container py-4">
            {/* Header */}
            <div className="d-flex align-items-center justify-content-between mb-4">
                <div>
                    <div className="d-flex align-items-center mb-2">
                        <Link href={`/users/${repository.ownerId}`} className="text-decoration-none text-muted">
                            <img src={repository.ownerProfilePicture || '/images/default-avatar.png'} className="rounded-circle me-2" width="24" height="24" alt="" />
                            {repository.ownerUsername}
                        </Link>
                        <span className="text-muted mx-2">/</span>
                        <h4 className="mb-0 fw-bold">{repository.repositoryName}</h4>
                        <span className={`badge ms-2 rounded-pill ${repository.visibility === 'Private' ? 'bg-danger' : 'bg-success'}`}>
                            {repository.visibility}
                        </span>
                    </div>
                    {repository.description && (
                        <p className="text-muted mb-0">{repository.description}</p>
                    )}
                </div>
                <div className="d-flex gap-2">
                    <button className="btn btn-sm btn-outline-secondary rounded-pill">
                        <i className="bi bi-eye me-1"></i>Watch
                    </button>
                    <button className="btn btn-sm btn-outline-secondary rounded-pill">
                        <i className="bi bi-star me-1"></i>Star <span className="badge bg-secondary ms-1">{repository.starsCount}</span>
                    </button>
                    <button className="btn btn-sm btn-outline-secondary rounded-pill">
                        <i className="bi bi-diagram-2 me-1"></i>Fork <span className="badge bg-secondary ms-1">{repository.forksCount}</span>
                    </button>
                </div>
            </div>

            {/* Tabs */}
            <ul className="nav nav-tabs mb-4">
                <li className="nav-item">
                    <Link href={`/repositories/${id}`} className="nav-link active">
                        <i className="bi bi-code-square me-1"></i>Code
                    </Link>
                </li>
                <li className="nav-item">
                    <Link href={`/repositories/${id}/commits`} className="nav-link">
                        <i className="bi bi-clock-history me-1"></i>Commits
                    </Link>
                </li>
                <li className="nav-item">
                    <Link href={`/repositories/${id}/branches`} className="nav-link">
                        <i className="bi bi-diagram-3 me-1"></i>Branches
                    </Link>
                </li>
            </ul>

            <div className="row">
                <div className="col-lg-9">
                    {/* Branch & Clone */}
                    <div className="card border-0 shadow-sm rounded-4 mb-4">
                        <div className="card-body d-flex justify-content-between align-items-center p-3">
                            <div className="d-flex gap-2">
                                <div className="dropdown">
                                    <button className="btn btn-sm btn-outline-secondary rounded-pill" data-bs-toggle="dropdown">
                                        <i className="bi bi-git me-1"></i>{repository.defaultBranch || 'main'} <i className="bi bi-chevron-down ms-1"></i>
                                    </button>
                                    <ul className="dropdown-menu">
                                        <li><a className="dropdown-item" href="#">main</a></li>
                                        <li><a className="dropdown-item" href="#">develop</a></li>
                                    </ul>
                                </div>
                                <Link href={`/repositories/${id}/branches`} className="btn btn-sm btn-outline-secondary rounded-pill">
                                    <i className="bi bi-diagram-3 me-1"></i>Branches
                                </Link>
                            </div>
                            <div className="d-flex gap-2">
                                <button className="btn btn-sm btn-success rounded-pill">
                                    <i className="bi bi-download me-1"></i>Clone
                                </button>
                            </div>
                        </div>
                    </div>

                    {/* File Browser */}
                    <div className="card border-0 shadow-sm rounded-4 mb-4">
                        <div className="card-header bg-white d-flex justify-content-between align-items-center">
                            <span className="text-muted small">{files.length} items</span>
                        </div>
                        <div className="list-group list-group-flush">
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
                                    {file.size && <span className="ms-auto text-muted small">{(file.size / 1024).toFixed(1)} KB</span>}
                                </Link>
                            ))}
                        </div>
                    </div>

                    {/* README */}
                    {readme && (
                        <div className="card border-0 shadow-sm rounded-4">
                            <div className="card-header bg-white">
                                <i className="bi bi-book me-2"></i>README.md
                            </div>
                            <div className="card-body">
                                <div className="markdown-body" dangerouslySetInnerHTML={{ __html: readme }} />
                            </div>
                        </div>
                    )}
                </div>

                {/* Sidebar */}
                <div className="col-lg-3">
                    <div className="card border-0 shadow-sm rounded-4 mb-4">
                        <div className="card-body">
                            <h6 className="fw-bold mb-3">About</h6>
                            <p className="text-muted small">{repository.description || 'No description provided'}</p>

                            {repository.cloneUrl && (
                                <div className="mb-3">
                                    <small className="text-muted d-block mb-1">Clone URL</small>
                                    <div className="input-group input-group-sm">
                                        <input type="text" className="form-control" value={repository.cloneUrl} readOnly />
                                        <button className="btn btn-outline-secondary" onClick={() => navigator.clipboard.writeText(repository.cloneUrl!)}>
                                            <i className="bi bi-clipboard"></i>
                                        </button>
                                    </div>
                                </div>
                            )}

                            <div className="d-flex gap-3 text-muted small">
                                <span><i className="bi bi-star me-1"></i>{repository.starsCount} stars</span>
                                <span><i className="bi bi-diagram-2 me-1"></i>{repository.forksCount} forks</span>
                            </div>
                        </div>
                    </div>

                    {repository.language && (
                        <div className="card border-0 shadow-sm rounded-4">
                            <div className="card-body">
                                <h6 className="fw-bold mb-3">Languages</h6>
                                <div className="d-flex align-items-center">
                                    <span className="badge bg-primary rounded-pill">{repository.language}</span>
                                </div>
                            </div>
                        </div>
                    )}
                </div>
            </div>
        </div>
    );
}
