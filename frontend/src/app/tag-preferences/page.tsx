'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useAuth } from '@/lib/contexts/AuthContext';
import type { Tag } from '@/types';
import apiClient from '@/lib/api/client';

interface TagPreference {
    tagId: number;
    tagName: string;
    type: 'watched' | 'ignored';
}

export default function TagPreferencesPage() {
    const { user, isLoading: authLoading } = useAuth();
    const router = useRouter();
    const [allTags, setAllTags] = useState<Tag[]>([]);
    const [watchedTags, setWatchedTags] = useState<string[]>([]);
    const [ignoredTags, setIgnoredTags] = useState<string[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [searchTerm, setSearchTerm] = useState('');
    const [activeTab, setActiveTab] = useState<'watched' | 'ignored'>('watched');

    useEffect(() => {
        if (!authLoading && !user) {
            router.push('/login');
        } else if (user) {
            fetchData();
        }
    }, [user, authLoading, router]);

    const fetchData = async () => {
        try {
            const [tagsRes, prefsRes] = await Promise.all([
                apiClient.get<{ items: Tag[] }>('/tags?pageSize=100'),
                apiClient.get<{ watched: string[]; ignored: string[] }>('/users/tag-preferences')
            ]);
            setAllTags(tagsRes.data.items || []);
            setWatchedTags(prefsRes.data.watched || []);
            setIgnoredTags(prefsRes.data.ignored || []);
        } catch (error) {
            console.error('Failed to fetch tag preferences:', error);
            // Use mock data for demo
            setAllTags([
                { tagId: 1, tagName: 'javascript', usageCount: 1500 },
                { tagId: 2, tagName: 'react', usageCount: 1200 },
                { tagId: 3, tagName: 'typescript', usageCount: 800 },
                { tagId: 4, tagName: 'node.js', usageCount: 700 },
                { tagId: 5, tagName: 'python', usageCount: 600 },
                { tagId: 6, tagName: 'csharp', usageCount: 500 },
                { tagId: 7, tagName: 'html', usageCount: 450 },
                { tagId: 8, tagName: 'css', usageCount: 400 },
            ]);
        } finally {
            setIsLoading(false);
        }
    };

    const toggleTag = (tagName: string, type: 'watched' | 'ignored') => {
        if (type === 'watched') {
            if (watchedTags.includes(tagName)) {
                setWatchedTags(watchedTags.filter(t => t !== tagName));
            } else {
                setWatchedTags([...watchedTags, tagName]);
                setIgnoredTags(ignoredTags.filter(t => t !== tagName)); // Remove from ignored
            }
        } else {
            if (ignoredTags.includes(tagName)) {
                setIgnoredTags(ignoredTags.filter(t => t !== tagName));
            } else {
                setIgnoredTags([...ignoredTags, tagName]);
                setWatchedTags(watchedTags.filter(t => t !== tagName)); // Remove from watched
            }
        }
    };

    const savePreferences = async () => {
        try {
            await apiClient.put('/users/tag-preferences', { watched: watchedTags, ignored: ignoredTags });
            // Show success message
        } catch (error) {
            console.error('Failed to save preferences:', error);
        }
    };

    const filteredTags = allTags.filter(tag =>
        tag.tagName.toLowerCase().includes(searchTerm.toLowerCase())
    );

    if (authLoading || isLoading) {
        return (
            <div className="container py-5 text-center">
                <div className="spinner-border text-primary" role="status">
                    <span className="visually-hidden">Loading...</span>
                </div>
            </div>
        );
    }

    if (!user) return null;

    return (
        <div className="container py-4">
            <div className="row">
                <div className="col-lg-8">
                    {/* Header */}
                    <div className="mb-4">
                        <h1 className="fw-bold mb-2">
                            <i className="bi bi-tags-fill me-2 text-primary"></i>Tag Preferences
                        </h1>
                        <p className="text-muted">
                            Customize your feed by watching tags you're interested in or ignoring tags you don't want to see.
                        </p>
                    </div>

                    {/* Tabs */}
                    <ul className="nav nav-tabs mb-4">
                        <li className="nav-item">
                            <button
                                className={`nav-link ${activeTab === 'watched' ? 'active' : ''}`}
                                onClick={() => setActiveTab('watched')}
                            >
                                <i className="bi bi-eye me-1 text-success"></i>
                                Watched Tags ({watchedTags.length})
                            </button>
                        </li>
                        <li className="nav-item">
                            <button
                                className={`nav-link ${activeTab === 'ignored' ? 'active' : ''}`}
                                onClick={() => setActiveTab('ignored')}
                            >
                                <i className="bi bi-eye-slash me-1 text-danger"></i>
                                Ignored Tags ({ignoredTags.length})
                            </button>
                        </li>
                    </ul>

                    {/* Tab Content */}
                    <div className="card border-0 shadow-sm rounded-4 mb-4">
                        <div className="card-body">
                            {activeTab === 'watched' ? (
                                <>
                                    <div className="alert alert-success d-flex align-items-start mb-4">
                                        <i className="bi bi-info-circle-fill me-2 mt-1"></i>
                                        <div>
                                            <strong>Watched Tags</strong>
                                            <p className="mb-0 small">Questions with these tags will be highlighted in your feed and you'll receive notifications for new questions.</p>
                                        </div>
                                    </div>

                                    {watchedTags.length > 0 ? (
                                        <div className="d-flex flex-wrap gap-2 mb-4">
                                            {watchedTags.map((tag) => (
                                                <span key={tag} className="badge bg-success-subtle text-success d-flex align-items-center gap-1 px-3 py-2 rounded-pill">
                                                    <i className="bi bi-eye"></i>
                                                    {tag}
                                                    <button
                                                        className="btn-close btn-close-white ms-1"
                                                        style={{ fontSize: '0.5rem' }}
                                                        onClick={() => toggleTag(tag, 'watched')}
                                                    ></button>
                                                </span>
                                            ))}
                                        </div>
                                    ) : (
                                        <p className="text-muted text-center py-3">No watched tags yet. Add tags below to watch them.</p>
                                    )}
                                </>
                            ) : (
                                <>
                                    <div className="alert alert-danger d-flex align-items-start mb-4">
                                        <i className="bi bi-info-circle-fill me-2 mt-1"></i>
                                        <div>
                                            <strong>Ignored Tags</strong>
                                            <p className="mb-0 small">Questions with these tags will be hidden from your feed.</p>
                                        </div>
                                    </div>

                                    {ignoredTags.length > 0 ? (
                                        <div className="d-flex flex-wrap gap-2 mb-4">
                                            {ignoredTags.map((tag) => (
                                                <span key={tag} className="badge bg-danger-subtle text-danger d-flex align-items-center gap-1 px-3 py-2 rounded-pill">
                                                    <i className="bi bi-eye-slash"></i>
                                                    {tag}
                                                    <button
                                                        className="btn-close btn-close-white ms-1"
                                                        style={{ fontSize: '0.5rem' }}
                                                        onClick={() => toggleTag(tag, 'ignored')}
                                                    ></button>
                                                </span>
                                            ))}
                                        </div>
                                    ) : (
                                        <p className="text-muted text-center py-3">No ignored tags. Add tags below to ignore them.</p>
                                    )}
                                </>
                            )}
                        </div>
                    </div>

                    {/* Search & Add */}
                    <div className="card border-0 shadow-sm rounded-4">
                        <div className="card-header bg-white border-0 pt-4">
                            <h5 className="fw-bold mb-0">Add Tags</h5>
                        </div>
                        <div className="card-body">
                            <div className="input-group mb-4">
                                <span className="input-group-text bg-light border-end-0">
                                    <i className="bi bi-search"></i>
                                </span>
                                <input
                                    type="text"
                                    className="form-control border-start-0"
                                    placeholder="Search tags..."
                                    value={searchTerm}
                                    onChange={(e) => setSearchTerm(e.target.value)}
                                />
                            </div>

                            <div className="row g-2">
                                {filteredTags.map((tag) => (
                                    <div key={tag.tagId} className="col-md-6 col-lg-4">
                                        <div className="d-flex align-items-center justify-content-between p-2 border rounded-3">
                                            <div>
                                                <span className="badge bg-primary-subtle text-primary me-2">{tag.tagName}</span>
                                                <small className="text-muted">×{tag.usageCount}</small>
                                            </div>
                                            <div className="btn-group btn-group-sm">
                                                <button
                                                    className={`btn ${watchedTags.includes(tag.tagName) ? 'btn-success' : 'btn-outline-success'}`}
                                                    onClick={() => toggleTag(tag.tagName, 'watched')}
                                                    title="Watch"
                                                >
                                                    <i className="bi bi-eye"></i>
                                                </button>
                                                <button
                                                    className={`btn ${ignoredTags.includes(tag.tagName) ? 'btn-danger' : 'btn-outline-danger'}`}
                                                    onClick={() => toggleTag(tag.tagName, 'ignored')}
                                                    title="Ignore"
                                                >
                                                    <i className="bi bi-eye-slash"></i>
                                                </button>
                                            </div>
                                        </div>
                                    </div>
                                ))}
                            </div>
                        </div>
                    </div>

                    {/* Save Button */}
                    <div className="d-flex justify-content-end mt-4">
                        <button className="btn btn-primary rounded-pill px-4" onClick={savePreferences}>
                            <i className="bi bi-check-lg me-2"></i>Save Preferences
                        </button>
                    </div>
                </div>

                {/* Sidebar */}
                <div className="col-lg-4">
                    <div className="card border-0 shadow-sm rounded-4">
                        <div className="card-header bg-white border-0 pt-4">
                            <h5 className="fw-bold mb-0"><i className="bi bi-lightbulb me-2 text-warning"></i>Tips</h5>
                        </div>
                        <div className="card-body">
                            <ul className="list-unstyled mb-0">
                                <li className="mb-3 d-flex">
                                    <i className="bi bi-check-circle-fill text-success me-2 mt-1"></i>
                                    <span className="small">Watch tags you want to follow closely</span>
                                </li>
                                <li className="mb-3 d-flex">
                                    <i className="bi bi-check-circle-fill text-success me-2 mt-1"></i>
                                    <span className="small">Ignore tags for topics you're not interested in</span>
                                </li>
                                <li className="mb-3 d-flex">
                                    <i className="bi bi-check-circle-fill text-success me-2 mt-1"></i>
                                    <span className="small">Your preferences affect your homepage feed</span>
                                </li>
                                <li className="d-flex">
                                    <i className="bi bi-info-circle-fill text-primary me-2 mt-1"></i>
                                    <span className="small">You can always change these later</span>
                                </li>
                            </ul>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
}
