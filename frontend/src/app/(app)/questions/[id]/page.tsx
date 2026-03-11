"use client";

import { useState } from "react";
import { useParams } from "next/navigation";
import Link from "next/link";
import dynamic from "next/dynamic";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useAuth } from "@/lib/contexts/AuthContext";
import { questionsApi } from "@/lib/api/questions.api";
import { answersApi } from "@/lib/api/answers.api";
import { votesApi } from "@/lib/api/votes.api";
import AppLayout from "@/components/AppLayout";
import RelativeTime from "@/components/RelativeTime";
import type { Question, Answer } from "@/types";

const MarkdownContent = dynamic(() => import("@/components/MarkdownContent"), {
  loading: () => (
    <div className="animate-pulse h-20 bg-slate-100 dark:bg-slate-800 rounded" />
  ),
});

export default function QuestionDetailPage() {
  const { id } = useParams();
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const [newAnswer, setNewAnswer] = useState("");

  const { data: question, isLoading } = useQuery<Question>({
    queryKey: ["question", id],
    queryFn: () => questionsApi.getById(id as string),
    enabled: !!id,
  });

  const { data: answersRaw } = useQuery({
    queryKey: ["answers", id],
    queryFn: () => answersApi.listByQuestion(id as string),
    enabled: !!id,
  });

  const answers: Answer[] = Array.isArray(answersRaw)
    ? answersRaw
    : answersRaw?.items || [];

  const voteMutation = useMutation({
    mutationFn: ({
      type,
      targetType,
      targetId,
    }: {
      type: "up" | "down";
      targetType: "question" | "answer";
      targetId: number;
    }) => {
      const voteType = type; // 'up' | 'down'
      return targetType === "question"
        ? votesApi.voteQuestion(targetId, { voteType })
        : votesApi.voteAnswer(targetId, { voteType });
    },
    onSuccess: () => {
      // Cập nhật lại chi tiết + danh sách liên quan
      queryClient.invalidateQueries({ queryKey: ["question", id] });
      queryClient.invalidateQueries({ queryKey: ["answers", id] });
      queryClient.invalidateQueries({ queryKey: ["questions"] });
    },
  });

  const handleVote = (
    type: "up" | "down",
    targetType: "question" | "answer",
    targetId: number,
  ) => {
    if (!user) return;

    // Không cho self-vote để tránh 400 từ backend
    if (targetType === "question" && user.userId === question?.authorId) {
      return;
    }
    if (targetType === "answer") {
      const targetAnswer = answers.find(a => a.answerId === targetId);
      if (targetAnswer && targetAnswer.authorId === user.userId) {
        return;
      }
    }
    voteMutation.mutate({ type, targetType, targetId });
  };

  const answerMutation = useMutation({
    mutationFn: (body: string) =>
      answersApi.create({ questionId: Number(id), body }),
    onSuccess: () => {
      setNewAnswer("");
      queryClient.invalidateQueries({ queryKey: ["answers", id] });
      queryClient.invalidateQueries({ queryKey: ["question", id] });
    },
  });

  const isSubmitting = answerMutation.isPending;

  const handleSubmitAnswer = (e: React.FormEvent) => {
    e.preventDefault();
    if (!newAnswer.trim() || !user) return;
    answerMutation.mutate(newAnswer);
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString(undefined, {
      year: "numeric",
      month: "long",
      day: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    });
  };

  if (isLoading) {
    return (
      <AppLayout>
        <div className="flex items-center justify-center py-20">
          <div className="w-10 h-10 border-4 border-[var(--primary)]/30 border-t-[var(--primary)] rounded-full animate-spin"></div>
        </div>
      </AppLayout>
    );
  }

  if (!question) {
    return (
      <AppLayout>
        <div className="text-center py-20">
          <h2 className="text-2xl font-bold text-[var(--text-primary)] mb-4">
            Question not found
          </h2>
          <Link href="/" className="text-[var(--primary)] hover:underline">
            ← Back to home
          </Link>
        </div>
      </AppLayout>
    );
  }

  return (
    <AppLayout>
      <div className="max-w-4xl mx-auto">
        {/* Question */}
        <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-2xl p-6 mb-6 shadow-sm">
          {/* Header */}
          <div className="mb-6 border-b border-[var(--border-color)] pb-4">
            <div className="flex items-start justify-between gap-4 mb-4">
              <div className="flex-1 min-w-0">
                <h1 className="text-2xl sm:text-3xl font-bold text-[var(--text-primary)] leading-tight">
                  {question.title}
                </h1>
              </div>
              <div className="flex flex-col items-end gap-2">
                {question.status === "Solved" && (
                  <span className="px-3 py-1 bg-green-500/10 text-green-500 border border-green-500/20 rounded-full text-sm font-medium whitespace-nowrap">
                    <i className="bi bi-check-circle-fill mr-1"></i> Solved
                  </span>
                )}
                {user && user.userId === question.authorId && (
                  <Link
                    href={`/questions/${question.questionId}/edit`}
                    className="inline-flex items-center gap-1 text-xs font-medium text-[var(--primary)] hover:text-[var(--primary-dark)]"
                  >
                    <span className="material-symbols-outlined text-sm">edit</span>
                    Edit question
                  </Link>
                )}
              </div>
            </div>
            <div className="flex flex-wrap items-center gap-4 text-sm text-[var(--text-muted)]">
              <span className="flex items-center gap-1">
                <i className="bi bi-clock"></i>
              <RelativeTime value={question.createdDate} fallback={formatDate(question.createdDate)} />
              </span>
              <span className="flex items-center gap-1">
                <i className="bi bi-eye"></i>
                {question.viewCount} views
              </span>
            </div>
          </div>

          <div className="flex gap-6">
            {/* Voting */}
            <div className="flex flex-col items-center gap-2">
              <button
                onClick={() =>
                  handleVote("up", "question", question.questionId)
                }
                disabled={!!user && user.userId === question.authorId}
                className={`w-10 h-10 rounded-full flex items-center justify-center transition ${
                  question.userVoteType === "up"
                    ? "bg-[var(--primary)] text-white"
                    : "bg-[var(--bg-tertiary)] text-[var(--text-muted)] hover:bg-[var(--bg-hover)]"
                }`}
                title="Upvote"
              >
                <span className="material-symbols-outlined text-xl">
                  expand_less
                </span>
              </button>
              <span className="text-xl font-bold text-[var(--text-primary)]">
                {question.score}
              </span>
              <button
                onClick={() =>
                  handleVote("down", "question", question.questionId)
                }
                disabled={!!user && user.userId === question.authorId}
                className={`w-10 h-10 rounded-full flex items-center justify-center transition ${
                  question.userVoteType === "down"
                    ? "bg-red-500 text-white"
                    : "bg-[var(--bg-tertiary)] text-[var(--text-muted)] hover:bg-[var(--bg-hover)]"
                }`}
                title="Downvote"
              >
                <span className="material-symbols-outlined text-xl">
                  expand_more
                </span>
              </button>
              <button
                className="mt-2 w-8 h-8 rounded-full bg-[var(--bg-tertiary)] text-[var(--text-muted)] hover:text-[var(--primary)] flex items-center justify-center transition"
                title="Save"
              >
                <i
                  className={`bi ${question.isSaved ? "bi-bookmark-fill text-[var(--primary)]" : "bi-bookmark"}`}
                ></i>
              </button>
            </div>

            {/* Content */}
            <div className="flex-1 min-w-0">
              <MarkdownContent
                content={question.body}
                className="mb-6 text-[var(--text-secondary)]"
              />

              <div className="flex flex-wrap gap-2 mb-6">
                {question.tags?.map((tag) => (
                  <Link
                    key={tag.tagId}
                    href={`/tags?search=${tag.tagName}`}
                    className="px-3 py-1 bg-[var(--primary)]/10 text-[var(--primary)] rounded-lg text-sm font-medium hover:bg-[var(--primary)] hover:text-white transition"
                  >
                    #{tag.tagName}
                  </Link>
                ))}
              </div>

              <div className="flex items-center justify-between pt-4 border-t border-[var(--border-color)]">
                <div className="flex gap-4">
                  <button className="text-[var(--text-muted)] hover:text-[var(--text-primary)] text-sm font-medium transition">
                    All questions
                  </button>
                </div>
                <div className="flex items-center gap-3 bg-[var(--bg-tertiary)] p-3 rounded-xl border border-[var(--border-color)]">
                  <div className="w-10 h-10 rounded-lg bg-gradient-to-br from-blue-500 to-cyan-500 flex items-center justify-center text-white font-bold">
                    {question.authorUsername?.charAt(0).toUpperCase()}
                  </div>
                  <div className="text-sm">
                    <div className="text-[var(--text-muted)]">Asked by</div>
                    <Link
                      href={`/users/${question.authorId}`}
                      className="font-semibold text-[var(--primary)] hover:underline"
                    >
                      {question.authorUsername}
                    </Link>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Answers Section */}
        <div className="mb-8">
          <h2 className="text-xl font-bold text-[var(--text-primary)] mb-4 flex items-center gap-2">
            <i className="bi bi-chat-left-text-fill text-[var(--primary)]"></i>
            {answers.length} Answers
          </h2>

          <div className="space-y-4">
            {answers.map((answer) => (
              <div
                key={answer.answerId}
                id={`answer-${answer.answerId}`}
                className={`bg-[var(--bg-secondary)] border rounded-2xl p-6 transition ${
                  answer.isAccepted
                    ? "border-green-500/50 shadow-[0_0_15px_rgba(34,197,94,0.1)]"
                    : "border-[var(--border-color)]"
                }`}
              >
                <div className="flex gap-6">
                  <div className="flex flex-col items-center gap-2">
                    <button
                      onClick={() =>
                        handleVote("up", "answer", answer.answerId)
                      }
                      className={`w-10 h-10 rounded-full flex items-center justify-center transition ${
                        answer.userVoteType === "up"
                          ? "bg-[var(--primary)] text-white"
                          : "bg-[var(--bg-tertiary)] text-[var(--text-muted)] hover:bg-[var(--bg-hover)]"
                      }`}
                    >
                      <span className="material-symbols-outlined text-xl">
                        expand_less
                      </span>
                    </button>
                    <span className="text-xl font-bold text-[var(--text-primary)]">
                      {answer.score}
                    </span>
                    <button
                      onClick={() =>
                        handleVote("down", "answer", answer.answerId)
                      }
                      className={`w-10 h-10 rounded-full flex items-center justify-center transition ${
                        answer.userVoteType === "down"
                          ? "bg-red-500 text-white"
                          : "bg-[var(--bg-tertiary)] text-[var(--text-muted)] hover:bg-[var(--bg-hover)]"
                      }`}
                    >
                      <span className="material-symbols-outlined text-xl">
                        expand_more
                      </span>
                    </button>
                    {answer.isAccepted && (
                      <div
                        className="mt-2 w-10 h-10 rounded-full bg-green-500 text-white flex items-center justify-center"
                        title="Accepted Answer"
                      >
                        <i className="bi bi-check-lg text-2xl"></i>
                      </div>
                    )}
                  </div>

                  <div className="flex-1 min-w-0">
                    <div
                      className="prose dark:prose-invert max-w-none mb-4 text-[var(--text-secondary)]"
                      dangerouslySetInnerHTML={{ __html: answer.body }}
                    />

                    <div className="flex items-center justify-between pt-4 border-t border-[var(--border-color)]">
                      <RelativeTime
                        value={answer.createdDate}
                        prefix="Trả lời "
                        className="text-xs text-[var(--text-muted)]"
                      />
                      <div className="flex items-center gap-2">
                        <div className="w-6 h-6 rounded bg-gradient-to-br from-purple-500 to-pink-500 flex items-center justify-center text-white text-xs font-bold">
                          {answer.authorUsername?.charAt(0).toUpperCase()}
                        </div>
                        <Link
                          href={`/users/${answer.authorId}`}
                          className="text-sm font-medium text-[var(--primary)] hover:underline"
                        >
                          {answer.authorUsername}
                        </Link>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Add Answer Form */}
        {user ? (
          <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-2xl p-6 shadow-sm">
            <h3 className="text-lg font-semibold text-[var(--text-primary)] mb-4">
              Your Answer
            </h3>
            <form onSubmit={handleSubmitAnswer}>
              <div className="mb-4">
                <textarea
                  value={newAnswer}
                  onChange={(e) => setNewAnswer(e.target.value)}
                  rows={6}
                  className="w-full p-4 bg-[var(--bg-tertiary)] border border-[var(--border-color)] rounded-xl text-[var(--text-primary)] placeholder-[var(--text-muted)] focus:border-[var(--primary)] focus:ring-2 focus:ring-[var(--primary)]/20 transition resize-y min-h-[150px]"
                  placeholder="Write your answer here. Markdown is supported."
                  required
                />
              </div>
              <div className="flex justify-end">
                <button
                  type="submit"
                  disabled={isSubmitting}
                  className="px-6 py-3 bg-[var(--primary)] text-white rounded-xl font-medium hover:bg-[var(--primary-dark)] transition disabled:opacity-50 flex items-center gap-2"
                >
                  {isSubmitting ? (
                    <>
                      <i className="bi bi-arrow-clockwise animate-spin"></i>{" "}
                      Posting...
                    </>
                  ) : (
                    <>
                      <i className="bi bi-send-fill"></i> Post Answer
                    </>
                  )}
                </button>
              </div>
            </form>
          </div>
        ) : (
          <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-2xl p-8 text-center">
            <i className="bi bi-lock-fill text-4xl text-[var(--text-muted)] mb-3"></i>
            <h3 className="text-lg font-semibold text-[var(--text-primary)] mb-2">
              Join the discussion
            </h3>
            <p className="text-[var(--text-muted)] mb-6">
              Log in or sign up to leave an answer
            </p>
            <div className="flex justify-center gap-4">
              <Link
                href="/login"
                className="px-6 py-2 bg-[var(--primary)] text-white rounded-xl font-medium hover:bg-[var(--primary-dark)] transition"
              >
                Log In
              </Link>
              <Link
                href="/register"
                className="px-6 py-2 bg-[var(--bg-tertiary)] text-[var(--text-primary)] rounded-xl font-medium border border-[var(--border-color)] hover:border-[var(--primary)] transition"
              >
                Sign Up
              </Link>
            </div>
          </div>
        )}
      </div>
    </AppLayout>
  );
}
