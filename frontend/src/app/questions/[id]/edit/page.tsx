'use client';

import { useEffect, useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
import Link from 'next/link';
import { useAuth } from '@/lib/contexts/AuthContext';
import type { Question } from '@/types';
import apiClient from '@/lib/api/client';
import AppLayout from '@/components/AppLayout';

export default function EditQuestionPage() {
    const params = useParams();
    const router = useRouter();
    const { user } = useAuth();
    const questionId = params.id as string;

    const [isLoading, setIsLoading] = useState(true);
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [error, setError] = useState('');
    const [formData, setFormData] = useState({
        title: '',
        body: '',
        tags: '',
    });

    useEffect(() => {
        fetchQuestion();
    }, [questionId]);

    const fetchQuestion = async () => {
        try {
            const response = await apiClient.get<Question>(`/questions/${questionId}`);
            const question = response.data;
            setFormData({
                title: question.title,
                body: question.body,
                tags: question.tags?.map(t => t.tagName).join(', ') || '',
            });
        } catch (err) {
            setError('Failed to load question');
        } finally {
            setIsLoading(false);
        }
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setIsSubmitting(true);
        setError('');

        try {
            await apiClient.put(`/questions/${questionId}`, {
                title: formData.title,
                body: formData.body,
                tags: formData.tags.split(',').map(t => t.trim()).filter(Boolean),
            });
            router.push(`/questions/${questionId}`);
        } catch (err: any) {
            setError(err.response?.data?.message || 'Failed to update question');
        } finally {
            setIsSubmitting(false);
        }
    };

    const insertMarkdown = (syntax: string) => {
        const textarea = document.querySelector('textarea[name="body"]') as HTMLTextAreaElement;
        if (!textarea) return;

        const start = textarea.selectionStart;
        const end = textarea.selectionEnd;
        const text = textarea.value;
        const before = text.substring(0, start);
        const selection = text.substring(start, end);
        const after = text.substring(end);

        let newText = '';
        if (selection) {
            if (syntax.includes('**')) {
                newText = before + `**${selection}**` + after;
            } else if (syntax.includes('*')) {
                newText = before + `*${selection}*` + after;
            } else if (syntax.includes('`')) {
                newText = before + `\`${selection}\`` + after;
            } else {
                newText = before + syntax + selection + after;
            }
        } else {
            newText = before + syntax + after;
        }

        setFormData({ ...formData, body: newText });
    };

    const toolbarButtons = [
        { icon: 'format_bold', action: '**bold**', title: 'Bold' },
        { icon: 'format_italic', action: '*italic*', title: 'Italic' },
        { icon: 'title', action: '# ', title: 'Heading 1' },
        { icon: 'title', action: '## ', title: 'Heading 2' },
        { icon: 'code', action: '`code`', title: 'Code' },
        { icon: 'link', action: '[Link](url)', title: 'Link' },
        { icon: 'format_list_bulleted', action: '- ', title: 'Bullet List' },
        { icon: 'format_list_numbered', action: '1. ', title: 'Numbered List' },
    ];

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
                <Link href="/questions" className="hover:text-[#137fec] transition-colors">Questions</Link>
                <span>/</span>
                <Link href={`/questions/${questionId}`} className="hover:text-[#137fec] transition-colors">Question</Link>
                <span>/</span>
                <span className="text-[#0f172a] dark:text-white font-medium">Edit</span>
            </nav>

            <div className="grid lg:grid-cols-3 gap-6">
                {/* Main Form */}
                <div className="lg:col-span-2">
                    <div className="bg-white dark:bg-[var(--bg-secondary)] border border-[#e2e8f0] dark:border-[var(--border-color)] rounded-2xl overflow-hidden">
                        <div className="bg-[#137fec] text-white px-6 py-4">
                            <h1 className="text-lg font-bold flex items-center gap-2">
                                <span className="material-symbols-outlined">edit</span>
                                Edit Your Question
                            </h1>
                        </div>
                        <div className="p-6">
                            {error && (
                                <div className="mb-4 p-4 bg-red-50 dark:bg-red-500/10 border border-red-200 dark:border-red-500/30 rounded-xl text-red-600 dark:text-red-400 text-sm flex items-center gap-2">
                                    <span className="material-symbols-outlined text-lg">error</span>
                                    {error}
                                </div>
                            )}

                            <form onSubmit={handleSubmit} className="space-y-6">
                                {/* Title */}
                                <div>
                                    <label className="block text-sm font-bold text-[#334155] dark:text-[var(--text-secondary)] mb-2 flex items-center gap-1">
                                        <span className="material-symbols-outlined text-[#137fec] text-base">title</span>Title
                                    </label>
                                    <input
                                        type="text"
                                        placeholder="What's your question? Be specific."
                                        value={formData.title}
                                        onChange={(e) => setFormData({ ...formData, title: e.target.value })}
                                        required
                                        className="w-full px-4 py-3 bg-[var(--bg-tertiary)] border border-[#e2e8f0] dark:border-[var(--border-color)] rounded-xl text-[var(--text-primary)] placeholder-[#94a3b8] focus:border-[#137fec] focus:ring-2 focus:ring-[rgba(19,127,236,0.1)] outline-none transition text-lg"
                                    />
                                    <p className="text-xs text-[#94a3b8] mt-2 flex items-center gap-1">
                                        <span className="material-symbols-outlined text-[#137fec] text-sm">info</span>
                                        Be specific and imagine you&apos;re asking a question to another person.
                                    </p>
                                </div>

                                {/* Body with Markdown Toolbar */}
                                <div>
                                    <label className="block text-sm font-bold text-[#334155] dark:text-[var(--text-secondary)] mb-2 flex items-center gap-1">
                                        <span className="material-symbols-outlined text-[#137fec] text-base">notes</span>Body
                                    </label>
                                    <div className="border border-[#e2e8f0] dark:border-[var(--border-color)] rounded-xl overflow-hidden">
                                        <div className="bg-[#f8fafc] dark:bg-[var(--bg-tertiary)] p-2 border-b border-[#e2e8f0] dark:border-[var(--border-color)] flex items-center gap-1">
                                            {toolbarButtons.map((btn, i) => (
                                                <button
                                                    key={i}
                                                    type="button"
                                                    title={btn.title}
                                                    className="w-8 h-8 rounded-lg flex items-center justify-center text-[#64748b] hover:bg-[#e2e8f0] dark:hover:bg-[var(--border-color)] hover:text-[#0f172a] dark:hover:text-white transition"
                                                    onClick={() => insertMarkdown(btn.action)}
                                                >
                                                    <span className="material-symbols-outlined text-lg">{btn.icon}</span>
                                                </button>
                                            ))}
                                        </div>
                                        <textarea
                                            name="body"
                                            rows={12}
                                            placeholder="Include all the information someone would need to answer your question"
                                            value={formData.body}
                                            onChange={(e) => setFormData({ ...formData, body: e.target.value })}
                                            required
                                            className="w-full px-4 py-3 bg-transparent text-[var(--text-primary)] placeholder-[#94a3b8] outline-none resize-none"
                                        />
                                    </div>
                                    <p className="text-xs text-[#94a3b8] mt-2 flex items-center gap-1">
                                        <span className="material-symbols-outlined text-[#137fec] text-sm">description</span>
                                        Supports Markdown formatting.
                                    </p>
                                </div>

                                {/* Tags */}
                                <div>
                                    <label className="block text-sm font-bold text-[#334155] dark:text-[var(--text-secondary)] mb-2 flex items-center gap-1">
                                        <span className="material-symbols-outlined text-[#137fec] text-base">sell</span>Tags
                                    </label>
                                    <div className="flex items-center gap-0 border border-[#e2e8f0] dark:border-[var(--border-color)] rounded-xl overflow-hidden">
                                        <span className="px-3 py-3 bg-[#f8fafc] dark:bg-[var(--bg-tertiary)] text-[#94a3b8] border-r border-[#e2e8f0] dark:border-[var(--border-color)]">
                                            <span className="material-symbols-outlined text-lg">sell</span>
                                        </span>
                                        <input
                                            type="text"
                                            placeholder="e.g. javascript, react, node.js (comma separated)"
                                            value={formData.tags}
                                            onChange={(e) => setFormData({ ...formData, tags: e.target.value })}
                                            className="flex-1 px-4 py-3 bg-transparent text-[var(--text-primary)] placeholder-[#94a3b8] outline-none"
                                        />
                                    </div>
                                    <p className="text-xs text-[#94a3b8] mt-2 flex items-center gap-1">
                                        <span className="material-symbols-outlined text-[#137fec] text-sm">info</span>
                                        Add up to 5 tags to describe what your question is about.
                                    </p>
                                </div>

                                {/* Actions */}
                                <div className="flex justify-end gap-3 pt-4 border-t border-[#e2e8f0] dark:border-[var(--border-color)]">
                                    <Link href={`/questions/${questionId}`} className="px-5 py-2.5 rounded-xl font-bold text-sm border border-[#e2e8f0] dark:border-[var(--border-color)] text-[#334155] dark:text-[var(--text-secondary)] hover:bg-[#f1f5f9] dark:hover:bg-[var(--bg-tertiary)] transition-all flex items-center gap-1">
                                        <span className="material-symbols-outlined text-base">close</span>Cancel
                                    </Link>
                                    <button
                                        type="submit"
                                        disabled={isSubmitting}
                                        className="flex items-center gap-2 bg-green-600 text-white px-6 py-2.5 rounded-xl font-bold text-sm hover:bg-green-700 transition-all disabled:opacity-50 disabled:cursor-not-allowed"
                                    >
                                        {isSubmitting ? (
                                            <>
                                                <div className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                                                Saving...
                                            </>
                                        ) : (
                                            <>
                                                <span className="material-symbols-outlined text-base">check</span>Save Changes
                                            </>
                                        )}
                                    </button>
                                </div>
                            </form>
                        </div>
                    </div>
                </div>

                {/* Sidebar Tips */}
                <div className="space-y-4">
                    <div className="bg-white dark:bg-[var(--bg-secondary)] border border-[#e2e8f0] dark:border-[var(--border-color)] rounded-2xl overflow-hidden">
                        <div className="bg-blue-500 text-white px-5 py-3">
                            <h3 className="font-bold text-sm flex items-center gap-2">
                                <span className="material-symbols-outlined text-base">lightbulb</span>Editing Tips
                            </h3>
                        </div>
                        <div className="p-4 space-y-3">
                            {[
                                { icon: 'wb_sunny', title: 'Improve clarity', desc: 'Make your question clearer and more focused.' },
                                { icon: 'checklist', title: 'Add relevant details', desc: 'Include any additional context that could help.' },
                                { icon: 'format_paragraph', title: 'Use proper formatting', desc: 'Format code, use headings for readability.' },
                            ].map(tip => (
                                <div key={tip.title} className="flex gap-3 p-3 bg-[#f8fafc] dark:bg-[var(--bg-tertiary)] rounded-xl">
                                    <div className="w-9 h-9 rounded-full bg-[#137fec] flex items-center justify-center text-white shrink-0">
                                        <span className="material-symbols-outlined text-base">{tip.icon}</span>
                                    </div>
                                    <div>
                                        <p className="font-bold text-sm text-[#0f172a] dark:text-white">{tip.title}</p>
                                        <p className="text-xs text-[#64748b]">{tip.desc}</p>
                                    </div>
                                </div>
                            ))}
                        </div>
                    </div>

                    <div className="bg-white dark:bg-[var(--bg-secondary)] border border-[#e2e8f0] dark:border-[var(--border-color)] rounded-2xl overflow-hidden">
                        <div className="bg-green-600 text-white px-5 py-3">
                            <h3 className="font-bold text-sm flex items-center gap-2">
                                <span className="material-symbols-outlined text-base">check_circle</span>Editing Etiquette
                            </h3>
                        </div>
                        <div className="p-4">
                            <div className="p-3 bg-blue-50 dark:bg-blue-500/10 border border-blue-200 dark:border-blue-500/30 rounded-xl text-sm text-blue-700 dark:text-blue-300 flex items-center gap-2 mb-3">
                                <span className="material-symbols-outlined text-base">info</span>
                                Editing after receiving answers should clarify, not change meaning.
                            </div>
                            <div className="space-y-2">
                                {[
                                    { ok: true, text: 'Clarify ambiguous points' },
                                    { ok: true, text: 'Fix typos and grammar' },
                                    { ok: false, text: 'Completely change the meaning' },
                                    { ok: false, text: 'Invalidate existing answers' },
                                ].map(item => (
                                    <div key={item.text} className="flex items-center gap-2 text-sm">
                                        <span className={`material-symbols-outlined text-base ${item.ok ? 'text-green-500' : 'text-red-500'}`}>
                                            {item.ok ? 'check_circle' : 'cancel'}
                                        </span>
                                        <span className="text-[#475569] dark:text-[var(--text-secondary)]">{item.text}</span>
                                    </div>
                                ))}
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </AppLayout>
    );
}
