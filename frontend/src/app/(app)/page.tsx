'use client';

import { useCallback, useEffect } from "react";
import { useAuth } from "@/lib/contexts/AuthContext";
import Link from "next/link";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import type { Question, Tag } from "@/types";
import { questionsApi } from "@/lib/api/questions.api";
import { useHub } from "@/lib/signalr/useHub";
import { HubConnectionState } from "@microsoft/signalr";
import AppLayout from "@/components/AppLayout";
import LandingPage from "@/components/landing/LandingPage";
import RelativeTime from "@/components/RelativeTime";
import { authorInitial } from "@/lib/utils";

const stripHtml = (html: string): string => {
  if (!html) return '';
  return html.replace(/<[^>]*>/g, '').substring(0, 150);
};

export default function HomePage() {
  const { user, isAuthenticated, isLoading: authLoading } = useAuth();
  const queryClient = useQueryClient();
  const questionHub = useHub("question");

  const { data, isLoading } = useQuery({
    queryKey: ['questions', { page: 1, pageSize: 10 }],
    queryFn: () => questionsApi.list({ page: 1, pageSize: 10 }),
    enabled: isAuthenticated,
  });

  const questions: Question[] = data?.items || [];
  const questionIds = questions.map((q) => q.questionId).join(',');

  // Join question rooms to receive real-time like updates
  useEffect(() => {
    if (!isAuthenticated || questionHub.connectionState !== HubConnectionState.Connected || questions.length === 0) return;
    const ids = questions.map((q) => q.questionId);
    ids.forEach((questionId) => {
      questionHub.invoke("JoinQuestion", questionId).catch(() => {});
    });
    return () => {
      ids.forEach((questionId) => {
        questionHub.invoke("LeaveQuestion", questionId).catch(() => {});
      });
    };
  }, [isAuthenticated, questionHub.connectionState, questionHub, questionIds, questions.length]);

  const handleVoteChanged = useCallback(
    (payload: { targetType: string; targetId: number; likeCount: number }) => {
      if (payload.targetType !== "question") return;
      queryClient.setQueriesData(
        { queryKey: ["questions"] },
        (old: { items?: Question[] } | undefined) => {
          if (!old?.items) return old;
          return {
            ...old,
            items: old.items.map((q) =>
              q.questionId === payload.targetId ? { ...q, score: payload.likeCount } : q
            ),
          };
        }
      );
    },
    [queryClient]
  );

  useEffect(() => {
    questionHub.on("VoteChanged", handleVoteChanged);
    return () => {
      questionHub.off("VoteChanged", handleVoteChanged);
    };
  }, [questionHub, handleVoteChanged]);

  if (authLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-[#f6f7f8] dark:bg-[#101922]">
        <div className="w-10 h-10 border-4 border-[var(--primary)]/30 border-t-[var(--primary)] rounded-full animate-spin"></div>
      </div>
    );
  }

  if (!isAuthenticated) {
    return <LandingPage />;
  }

  return (
    <AppLayout>
      {/* Welcome Section */}
      <div>
        <h2 className="text-xl sm:text-2xl font-extrabold text-slate-900 dark:text-white tracking-tight">
          Welcome back, {user?.displayName || user?.username || 'Developer'}!
        </h2>
        <p className="text-slate-500 mt-1">Here is what&apos;s happening in your network today.</p>
      </div>

      {/* Stats Cards */}
      {user && (
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          <div className="bg-white dark:bg-slate-900 p-6 rounded-lg shadow-sm border border-slate-100 dark:border-slate-800">
            <div className="flex items-center justify-between mb-4">
              <span className="material-symbols-outlined text-[var(--primary)] bg-[var(--primary)]/10 p-2 rounded-lg">stars</span>
              <span className="text-xs font-bold text-emerald-600 bg-emerald-50 dark:bg-emerald-900/30 px-2 py-1 rounded-full">+12%</span>
            </div>
            <p className="text-slate-500 text-sm font-medium">Reputation</p>
            <p className="text-2xl font-bold text-slate-900 dark:text-white mt-1">{(user.reputationPoints || 0).toLocaleString()}</p>
          </div>
          <div className="bg-white dark:bg-slate-900 p-6 rounded-lg shadow-sm border border-slate-100 dark:border-slate-800">
            <div className="flex items-center justify-between mb-4">
              <span className="material-symbols-outlined text-amber-500 bg-amber-50 dark:bg-amber-900/20 p-2 rounded-lg">help_center</span>
              <span className="text-xs font-bold text-emerald-600 bg-emerald-50 dark:bg-emerald-900/30 px-2 py-1 rounded-full">+2%</span>
            </div>
            <p className="text-slate-500 text-sm font-medium">Questions Asked</p>
            <p className="text-2xl font-bold text-slate-900 dark:text-white mt-1">0</p>
          </div>
          <div className="bg-white dark:bg-slate-900 p-6 rounded-lg shadow-sm border border-slate-100 dark:border-slate-800">
            <div className="flex items-center justify-between mb-4">
              <span className="material-symbols-outlined text-indigo-500 bg-indigo-50 dark:bg-indigo-900/20 p-2 rounded-lg">forum</span>
              <span className="text-xs font-bold text-emerald-600 bg-emerald-50 dark:bg-emerald-900/30 px-2 py-1 rounded-full">+5%</span>
            </div>
            <p className="text-slate-500 text-sm font-medium">Answers Given</p>
            <p className="text-2xl font-bold text-slate-900 dark:text-white mt-1">0</p>
          </div>
        </div>
      )}

      {/* Recent Questions */}
      <section>
        <div className="flex items-center justify-between mb-6">
          <h3 className="text-lg font-bold text-slate-900 dark:text-white">Recent Questions from your Network</h3>
          <Link href="/questions" className="text-[var(--primary)] text-sm font-semibold hover:underline">View all</Link>
        </div>

        {isLoading ? (
          <div className="flex items-center justify-center py-16">
            <div className="w-10 h-10 border-4 border-[var(--primary)]/30 border-t-[var(--primary)] rounded-full animate-spin"></div>
          </div>
        ) : questions.length === 0 ? (
          <div className="bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-lg p-12 text-center shadow-sm">
            <span className="material-symbols-outlined text-5xl text-slate-300 mb-4 block">inbox</span>
            <h3 className="text-lg font-semibold text-slate-900 dark:text-white mb-2">No questions yet</h3>
            <p className="text-slate-500 mb-4">Be the first to ask!</p>
            <Link href="/questions/ask" className="inline-flex items-center gap-2 px-4 py-2 bg-[var(--primary)] text-white rounded-lg text-sm font-semibold">
              <span className="material-symbols-outlined text-sm">add</span> Ask Question
            </Link>
          </div>
        ) : (
          <div className="space-y-4">
            {questions.map((question) => (
              <Link
                key={question.questionId}
                href={`/questions/${question.questionId}`}
                className="block bg-white dark:bg-slate-900 p-5 rounded-lg shadow-sm border border-slate-100 dark:border-slate-800 hover:border-[var(--primary)]/50 transition-all cursor-pointer group"
              >
                <div className="flex gap-4">
                  <div className="flex-1">
                    {/* Author */}
                    <div className="flex items-center gap-2 mb-2">
                      {question.authorProfilePicture ? (
                        <img
                          src={question.authorProfilePicture}
                          alt={question.authorUsername || ''}
                          className="w-6 h-6 rounded-full object-cover"
                        />
                      ) : (
                        <div className="w-6 h-6 rounded-full bg-gradient-to-br from-blue-500 to-purple-500 flex items-center justify-center text-white text-[10px] font-bold">
                          {authorInitial(question.authorUsername)}
                        </div>
                      )}
                      <span className="text-xs font-semibold text-slate-700 dark:text-slate-300">
                        {question.authorUsername || 'Anonymous'}
                      </span>
                      <RelativeTime value={question.createdDate} prefix="• " className="text-xs text-slate-400" />
                    </div>

                    {/* Title */}
                    <h4 className="text-base font-bold text-slate-900 dark:text-white group-hover:text-[var(--primary)] transition-colors line-clamp-2">
                      {question.title}
                    </h4>

                    {/* Tags */}
                    <div className="flex flex-wrap gap-2 mt-3">
                      {question.tags?.slice(0, 3).map((tag: Tag) => (
                        <span
                          key={tag.tagId}
                          className="px-2 py-1 bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-400 text-[10px] font-bold rounded uppercase tracking-wider"
                        >
                          {tag.tagName}
                        </span>
                      ))}
                    </div>
                  </div>
                </div>

                {/* Stats Bar */}
                <div className="flex items-center justify-between mt-4 pt-4 border-t border-slate-50 dark:border-slate-800">
                  <div className="flex items-center gap-4 text-slate-500 text-xs font-medium">
                    <div className="flex items-center gap-1">
                      <span className="material-symbols-outlined text-sm">thumb_up</span>
                      {question.score || 0}
                    </div>
                    <div className="flex items-center gap-1">
                      <span className="material-symbols-outlined text-sm">comment</span>
                      {question.answerCount || 0}
                    </div>
                    <div className="flex items-center gap-1">
                      <span className="material-symbols-outlined text-sm">visibility</span>
                      {question.viewCount || 0}
                    </div>
                  </div>
                  <span className="text-slate-400 hover:text-[var(--primary)] transition-colors">
                    <span className="material-symbols-outlined text-lg">bookmark</span>
                  </span>
                </div>
              </Link>
            ))}
          </div>
        )}
      </section>
    </AppLayout>
  );
}