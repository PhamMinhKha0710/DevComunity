'use client';

import { useAuth } from "@/lib/contexts/AuthContext";
import Link from "next/link";
import { useEffect, useState } from "react";
import type { Question, PaginatedResponse, Tag } from "@/types";
import apiClient from "@/lib/api/client";
import AppLayout from "@/components/AppLayout";

const stripHtml = (html: string): string => {
  if (!html) return '';
  return html.replace(/<[^>]*>/g, '').substring(0, 150);
};

export default function HomePage() {
  const { user, isAuthenticated } = useAuth();
  const [questions, setQuestions] = useState<Question[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    fetchQuestions();
  }, []);

  const fetchQuestions = async () => {
    try {
      const response = await apiClient.get<PaginatedResponse<Question>>('/questions?page=1&pageSize=10');
      setQuestions(response.data.items || []);
    } catch (error) {
      console.error('Failed to fetch questions:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffHours < 24) return `${diffHours}h ago`;
    if (diffDays < 7) return `${diffDays}d ago`;
    return date.toLocaleDateString();
  };

  return (
    <AppLayout>
      {/* Hero Stats */}
      {isAuthenticated && user && (
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-8">
          <div className="bg-[var(--bg-secondary)] border border-purple-200 dark:border-purple-900 rounded-2xl p-5 shadow-sm">
            <div className="flex items-center gap-4">
              <div className="w-12 h-12 rounded-xl bg-purple-100 dark:bg-purple-900/30 flex items-center justify-center">
                <i className="bi bi-trophy-fill text-2xl text-purple-600 dark:text-purple-400"></i>
              </div>
              <div>
                <p className="text-2xl font-bold text-[var(--text-primary)]">{user.reputationPoints || 0}</p>
                <p className="text-sm text-[var(--text-muted)]">Reputation</p>
              </div>
            </div>
          </div>
          <div className="bg-[var(--bg-secondary)] border border-blue-200 dark:border-blue-900 rounded-2xl p-5 shadow-sm">
            <div className="flex items-center gap-4">
              <div className="w-12 h-12 rounded-xl bg-blue-100 dark:bg-blue-900/30 flex items-center justify-center">
                <i className="bi bi-question-circle-fill text-2xl text-blue-600 dark:text-blue-400"></i>
              </div>
              <div>
                <p className="text-2xl font-bold text-[var(--text-primary)]">0</p>
                <p className="text-sm text-[var(--text-muted)]">Questions</p>
              </div>
            </div>
          </div>
          <div className="bg-[var(--bg-secondary)] border border-green-200 dark:border-green-900 rounded-2xl p-5 shadow-sm">
            <div className="flex items-center gap-4">
              <div className="w-12 h-12 rounded-xl bg-green-100 dark:bg-green-900/30 flex items-center justify-center">
                <i className="bi bi-chat-quote-fill text-2xl text-green-600 dark:text-green-400"></i>
              </div>
              <div>
                <p className="text-2xl font-bold text-[var(--text-primary)]">0</p>
                <p className="text-sm text-[var(--text-muted)]">Answers</p>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Welcome Banner for guests */}
      {!isAuthenticated && (
        <div className="bg-[var(--bg-secondary)] border border-[var(--primary)] border-opacity-30 rounded-2xl p-8 mb-8 shadow-sm">
          <h1 className="text-3xl font-bold text-[var(--text-primary)] mb-3">
            Welcome to DevCommunity 👋
          </h1>
          <p className="text-[var(--text-secondary)] mb-6 max-w-2xl">
            Join thousands of developers sharing knowledge, asking questions, and building together.
          </p>
          <div className="flex gap-3">
            <Link href="/register" className="px-6 py-3 bg-[var(--primary)] text-white rounded-xl font-medium hover:bg-[var(--primary-dark)] transition shadow-lg shadow-[var(--primary)]/30">
              Get Started
            </Link>
            <Link href="/questions" className="px-6 py-3 bg-[var(--bg-tertiary)] text-[var(--text-primary)] rounded-xl font-medium hover:bg-[var(--bg-hover)] transition border border-[var(--border-color)]">
              Browse Questions
            </Link>
          </div>
        </div>
      )}

      {/* Questions Feed */}
      <div className="space-y-4">
        <div className="flex items-center justify-between mb-2">
          <h2 className="text-xl font-bold text-[var(--text-primary)] flex items-center gap-2">
            <i className="bi bi-lightning-charge-fill text-yellow-400"></i>
            Recent Questions
          </h2>
          <Link href="/questions" className="text-sm text-[var(--primary)] hover:underline">
            View all →
          </Link>
        </div>

        {isLoading ? (
          <div className="flex items-center justify-center py-16">
            <div className="w-10 h-10 border-4 border-[var(--primary)]/30 border-t-[var(--primary)] rounded-full animate-spin"></div>
          </div>
        ) : questions.length === 0 ? (
          <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-2xl p-12 text-center">
            <i className="bi bi-inbox text-5xl text-[var(--text-muted)] mb-4"></i>
            <h3 className="text-lg font-semibold text-[var(--text-primary)] mb-2">No questions yet</h3>
            <p className="text-[var(--text-muted)] mb-4">Be the first to ask!</p>
            <Link href="/questions/ask" className="inline-flex items-center gap-2 px-4 py-2 bg-[var(--primary)] text-white rounded-xl">
              <i className="bi bi-plus-circle"></i> Ask Question
            </Link>
          </div>
        ) : (
          questions.map((question) => (
            <div
              key={question.questionId}
              className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-2xl p-5 hover:border-[var(--primary)]/50 transition group"
            >
              <div className="flex gap-4">
                {/* Stats */}
                <div className="hidden sm:flex flex-col items-center gap-2 min-w-[60px]">
                  <div className={`px-3 py-1.5 rounded-lg text-center ${question.score > 0 ? 'bg-green-500/20 text-green-400' : 'bg-[var(--bg-tertiary)] text-[var(--text-muted)]'}`}>
                    <span className="block text-lg font-bold">{question.score}</span>
                    <span className="text-xs">votes</span>
                  </div>
                  <div className={`px-3 py-1.5 rounded-lg text-center ${question.answerCount > 0 ? 'bg-blue-500/20 text-blue-400' : 'bg-[var(--bg-tertiary)] text-[var(--text-muted)]'}`}>
                    <span className="block text-lg font-bold">{question.answerCount}</span>
                    <span className="text-xs">answers</span>
                  </div>
                </div>

                {/* Content */}
                <div className="flex-1 min-w-0">
                  <Link
                    href={`/questions/${question.questionId}`}
                    className="text-lg font-semibold text-[var(--text-primary)] hover:text-[var(--primary)] transition line-clamp-2"
                  >
                    {question.title}
                  </Link>
                  <p className="mt-2 text-sm text-[var(--text-muted)] line-clamp-2">
                    {stripHtml(question.bodyExcerpt || question.body || '')}
                  </p>

                  {/* Tags & Meta */}
                  <div className="flex flex-wrap items-center gap-3 mt-4">
                    <div className="flex flex-wrap gap-2">
                      {question.tags?.slice(0, 4).map((tag: Tag) => (
                        <Link
                          key={tag.tagId}
                          href={`/questions?tag=${tag.tagName}`}
                          className="px-2.5 py-1 text-xs font-medium bg-[var(--primary)]/10 text-[var(--primary)] rounded-lg hover:bg-[var(--primary)]/20 transition"
                        >
                          {tag.tagName}
                        </Link>
                      ))}
                      {question.tags?.length > 4 && (
                        <span className="px-2 py-1 text-xs text-[var(--text-muted)]">+{question.tags.length - 4}</span>
                      )}
                    </div>
                    <div className="flex items-center gap-2 ml-auto text-xs text-[var(--text-muted)]">
                      <div className="w-5 h-5 rounded-full bg-gradient-to-br from-blue-500 to-purple-500 flex items-center justify-center text-white text-[10px] font-bold">
                        {question.authorUsername?.charAt(0).toUpperCase() || '?'}
                      </div>
                      <span>{question.authorUsername || 'Anonymous'}</span>
                      <span>•</span>
                      <span>{formatDate(question.createdDate)}</span>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          ))
        )}
      </div>
    </AppLayout>
  );
}
