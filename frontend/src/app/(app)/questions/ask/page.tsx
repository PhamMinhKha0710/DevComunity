'use client';

import { useState, useRef, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import dynamic from 'next/dynamic';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useAuth } from '@/lib/contexts/AuthContext';
import { questionsApi } from '@/lib/api/questions.api';
import AppLayout from '@/components/AppLayout';
import AskQuestionEditor from '@/components/questions/AskQuestionEditor';

// Preview riêng dùng MDEditor.Markdown (dynamic để tránh SSR)
const MDPreview = dynamic(
    () => import('@uiw/react-md-editor').then((mod) => mod.default.Markdown),
    { ssr: false },
);

const MAX_TAGS = 5;

export default function AskQuestionPage() {
    const { user, isLoading: authLoading } = useAuth();
    const router = useRouter();
    const queryClient = useQueryClient();
    const [title, setTitle] = useState('');
    const [body, setBody] = useState('');
    const [tagList, setTagList] = useState<string[]>([]);
    const [tagInput, setTagInput] = useState('');
    const [error, setError] = useState('');
    const [colorMode, setColorMode] = useState<'light' | 'dark'>('light');
    const tagInputRef = useRef<HTMLInputElement>(null);

    useEffect(() => {
        const html = document.documentElement;
        const sync = () => setColorMode(html.classList.contains('dark') ? 'dark' : 'light');
        sync();
        const observer = new MutationObserver(sync);
        observer.observe(html, { attributes: true, attributeFilter: ['class'] });
        return () => observer.disconnect();
    }, []);

    const createMutation = useMutation({
        mutationFn: (data: { title: string; body: string; tags: string[] }) =>
            questionsApi.create(data),
        onSuccess: (data) => {
            queryClient.invalidateQueries({ queryKey: ['questions'] });
            router.push(`/questions/${data.questionId}`);
        },
        onError: (err: any) => {
            setError(err.response?.data?.message || 'Failed to create question');
        },
    });

    const isLoading = createMutation.isPending;

    const addTag = (raw: string) => {
        const name = raw.trim().replace(/^#+/, '');
        if (!name || tagList.length >= MAX_TAGS) return;
        if (tagList.some(t => t.toLowerCase() === name.toLowerCase())) return;
        setTagList(prev => [...prev, name]);
        setTagInput('');
    };

    const removeTag = (index: number) => {
        setTagList(prev => prev.filter((_, i) => i !== index));
    };

    const handleTagKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
        if (e.key === ',' || e.key === 'Enter') {
            e.preventDefault();
            addTag(tagInput);
        } else if (e.key === 'Backspace' && !tagInput && tagList.length > 0) {
            removeTag(tagList.length - 1);
        }
    };

    const handleSubmit = (e: React.FormEvent) => {
        e.preventDefault();
        setError('');
        const lastTag = tagInput.trim().replace(/^#+/, '');
        const finalTags = lastTag && tagList.length < MAX_TAGS && !tagList.some(t => t.toLowerCase() === lastTag.toLowerCase())
            ? [...tagList, lastTag]
            : tagList;
        createMutation.mutate({ title, body, tags: finalTags });
    };

    if (authLoading) {
        return (
            <AppLayout showRightSidebar={false}>
                <div className="flex items-center justify-center py-20">
                    <div className="w-10 h-10 border-4 border-[var(--primary)]/30 border-t-[var(--primary)] rounded-full animate-spin"></div>
                </div>
            </AppLayout>
        );
    }

    if (!user) {
        return (
            <AppLayout showRightSidebar={false}>
                <div className="flex flex-col items-center justify-center py-16 text-center">
                    <span className="material-symbols-outlined text-5xl text-[var(--text-muted)] mb-4">lock</span>
                    <h2 className="text-2xl font-bold text-[var(--text-primary)] mb-2">Please log in to ask a question</h2>
                    <p className="text-[var(--text-muted)] mb-6">You need to be signed in to participate in discussions</p>
                    <Link href="/auth?mode=login" className="px-6 py-3 bg-[var(--primary)] text-white rounded-xl font-medium hover:bg-[var(--primary-dark)] transition">
                        Log in to Continue
                    </Link>
                </div>
            </AppLayout>
        );
    }

    return (
        <AppLayout showRightSidebar={false}>
            {/* Header */}
            <div className="mb-6">
                <h1 className="text-2xl font-bold text-[var(--text-primary)] flex items-center gap-2">
                    <span className="material-symbols-outlined text-[var(--primary)]">help</span>
                    Ask a Question
                </h1>
                <p className="text-[var(--text-muted)]">Get help from the community by asking a well-crafted question</p>
            </div>

            {error && (
                <div className="mb-6 p-4 bg-red-500/10 border border-red-500/30 rounded-xl text-red-500 flex items-center gap-2">
                    <span className="material-symbols-outlined">error</span>
                    {error}
                </div>
            )}

            <form onSubmit={handleSubmit} className="space-y-6">
                {/* Title */}
                <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-2xl p-6">
                    <label htmlFor="title" className="block text-lg font-semibold text-[var(--text-primary)] mb-2">
                        Title
                    </label>
                    <p className="text-sm text-[var(--text-muted)] mb-3">
                        Be specific and imagine you&apos;re asking a question to another person.
                    </p>
                    <input
                        id="title"
                        type="text"
                        value={title}
                        onChange={(e) => setTitle(e.target.value)}
                        placeholder="e.g. How to center a div in CSS?"
                        required
                        className="w-full px-4 py-3 bg-[var(--bg-primary)] border border-[var(--border-color)] rounded-xl text-[var(--text-primary)] placeholder-[var(--text-muted)] focus:border-[var(--primary)] focus:ring-2 focus:ring-[var(--primary)]/20 transition"
                    />
                </div>

                {/* Body — Markdown Editor */}
                <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-2xl p-6">
                    <label className="block text-lg font-semibold text-[var(--text-primary)] mb-2">
                        What are the details of your problem?
                    </label>
                    <p className="text-sm text-[var(--text-muted)] mb-3">
                        Introduce the problem and expand on what you put in the title. Include code snippets if relevant.
                    </p>
                    <AskQuestionEditor
                        value={body}
                        onChange={setBody}
                        colorMode={colorMode}
                    />
                </div>

                {/* Tags: chip hiển thị ngay, gõ xong Enter hoặc dấu phẩy là thành tag */}
                <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-2xl p-6">
                    <label htmlFor="tags" className="block text-lg font-semibold text-[var(--text-primary)] mb-2">
                        Tags
                    </label>
                    <p className="text-sm text-[var(--text-muted)] mb-3">
                        Gõ tên tag rồi nhấn Enter hoặc dấu phẩy — tag sẽ xuất hiện bên dưới. Tối đa 5 tag.
                    </p>
                    <div className="flex flex-wrap items-center gap-2 p-3 min-h-[52px] bg-[var(--bg-primary)] border border-[var(--border-color)] rounded-xl focus-within:border-[var(--primary)] focus-within:ring-2 focus-within:ring-[var(--primary)]/20 transition">
                        {tagList.map((tag, i) => (
                            <span
                                key={`${tag}-${i}`}
                                className="inline-flex items-center gap-1 px-2.5 py-1 bg-[var(--primary)]/15 text-[var(--primary)] rounded-lg text-sm font-medium"
                            >
                                {tag}
                                <button
                                    type="button"
                                    onClick={() => removeTag(i)}
                                    className="p-0.5 rounded hover:bg-[var(--primary)]/30 transition"
                                    aria-label="Xóa tag"
                                >
                                    <span className="material-symbols-outlined text-sm">close</span>
                                </button>
                            </span>
                        ))}
                        {tagList.length < MAX_TAGS && (
                            <input
                                ref={tagInputRef}
                                id="tags"
                                type="text"
                                value={tagInput}
                                onChange={(e) => setTagInput(e.target.value)}
                                onKeyDown={handleTagKeyDown}
                                placeholder={tagList.length === 0 ? "e.g. javascript, react, node.js" : "Thêm tag..."}
                                className="flex-1 min-w-[120px] px-1 py-1.5 bg-transparent text-[var(--text-primary)] placeholder-[var(--text-muted)] outline-none"
                            />
                        )}
                    </div>
                    <div className="flex flex-wrap gap-2 mt-3">
                        {['javascript', 'react', 'python', 'csharp', 'css'].map(t => (
                            <button
                                key={t}
                                type="button"
                                onClick={() => tagList.length < MAX_TAGS && !tagList.some(x => x.toLowerCase() === t) && addTag(t)}
                                disabled={tagList.length >= MAX_TAGS || tagList.some(x => x.toLowerCase() === t)}
                                className="px-3 py-1 text-xs bg-[var(--bg-tertiary)] text-[var(--text-muted)] rounded-lg hover:bg-[var(--primary)]/20 hover:text-[var(--primary)] transition disabled:opacity-50 disabled:cursor-not-allowed"
                            >
                                + {t}
                            </button>
                        ))}
                    </div>
                </div>

                {/* Preview trước khi đăng */}
                {body.trim() && (
                    <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-2xl p-6">
                        <h3 className="text-lg font-semibold text-[var(--text-primary)] mb-2 flex items-center gap-2">
                            <span className="material-symbols-outlined text-[var(--primary)]">visibility</span>
                            Preview trước khi đăng
                        </h3>
                        <div
                            data-color-mode={colorMode}
                            className="p-4 bg-[var(--bg-primary)] border border-[var(--border-color)] rounded-xl prose-preview"
                        >
                            <MDPreview source={body} />
                        </div>
                    </div>
                )}

                {/* Submit */}
                <div className="flex items-center gap-4">
                    <button
                        type="submit"
                        disabled={isLoading}
                        className="flex items-center gap-2 px-6 py-3 bg-[var(--primary)] text-white rounded-xl font-medium hover:bg-[var(--primary-dark)] transition disabled:opacity-50"
                    >
                        {isLoading ? (
                            <>
                                <div className="w-5 h-5 border-2 border-white/30 border-t-white rounded-full animate-spin"></div>
                                Posting...
                            </>
                        ) : (
                            <>
                                <span className="material-symbols-outlined">send</span>
                                Post your question
                            </>
                        )}
                    </button>
                    <Link href="/questions" className="px-6 py-3 text-[var(--text-secondary)] hover:text-[var(--text-primary)] transition">
                        Cancel
                    </Link>
                </div>
            </form>
        </AppLayout>
    );
}