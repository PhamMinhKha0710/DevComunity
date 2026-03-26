"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { useParams } from "next/navigation";
import Link from "next/link";
import dynamic from "next/dynamic";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useAuth } from "@/lib/contexts/AuthContext";
import { useHub } from "@/lib/signalr/useHub";
import { HubConnectionState } from "@microsoft/signalr";
import { questionsApi } from "@/lib/api/questions.api";
import { answersApi } from "@/lib/api/answers.api";
import { votesApi } from "@/lib/api/votes.api";
import { savedItemsApi } from "@/lib/api/savedItems.api";
import { commentsApi } from "@/lib/api/comments.api";
import AppLayout from "@/components/AppLayout";
import RelativeTime from "@/components/RelativeTime";
import type { Question, Answer, Comment } from "@/types";
import { authorInitial } from "@/lib/utils";

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
  const answerFormRef = useRef<HTMLDivElement>(null);
  const questionHub = useHub("question");

  // Join/leave question room for realtime vote updates
  useEffect(() => {
    if (!id || questionHub.connectionState !== HubConnectionState.Connected) return;

    const questionId = Number(id);
    questionHub.invoke("JoinQuestion", questionId).catch(() => {});

    return () => {
      questionHub.invoke("LeaveQuestion", questionId).catch(() => {});
    };
  }, [id, questionHub.connectionState]);

  // Listen to VoteChanged events from other users
  const handleVoteChanged = useCallback(
    (data: { targetType: string; targetId: number; likeCount: number; likedByUserId: number }) => {
      if (data.targetType === "question") {
        queryClient.setQueryData<Question>(["question", id], (prev) => {
          if (!prev) return prev;
          return { ...prev, score: data.likeCount };
        });
      } else if (data.targetType === "answer") {
        queryClient.setQueryData(["answers", id], (prev: unknown) => {
          if (!prev) return prev;
          const raw = prev as { items?: Answer[] };
          const items = raw.items || (prev as Answer[]);
          const updated = (Array.isArray(items) ? items : []).map((a: Answer) =>
            a.answerId === data.targetId ? { ...a, score: data.likeCount } : a
          );
          return raw.items ? { ...raw, items: updated } : updated;
        });
      }
    },
    [id, queryClient]
  );

  const handleNewComment = useCallback(
    (data: { targetType: string; targetId: number; comment: Comment }) => {
      if (data.targetType === "answer") {
        queryClient.setQueryData(["answers", id], (prev: unknown) => {
          if (!prev) return prev;
          const raw = prev as { items?: Answer[] };
          const items = raw.items || (prev as Answer[]);
          const updated = (Array.isArray(items) ? items : []).map((a: Answer) => {
            if (a.answerId === data.targetId) {
              const existingComments = a.comments || [];
              if (!existingComments.find(c => c.commentId === data.comment.commentId)) {
                return { ...a, comments: [...existingComments, data.comment] };
              }
            }
            return a;
          });
          return raw.items ? { ...raw, items: updated } : updated;
        });
      }
    },
    [id, queryClient]
  );

  const handleAnswerUpdated = useCallback(
    (data: { answerId: number; body: string }) => {
      queryClient.setQueryData(["answers", id], (prev: unknown) => {
        if (!prev) return prev;
        const raw = prev as { items?: Answer[] };
        const items = raw.items || (prev as Answer[]);
        const updated = (Array.isArray(items) ? items : []).map((a: Answer) =>
          a.answerId === data.answerId ? { ...a, body: data.body } : a
        );
        return raw.items ? { ...raw, items: updated } : updated;
      });
    },
    [id, queryClient]
  );

  const handleAnswerAccepted = useCallback(
    (data: { answerId: number }) => {
      queryClient.setQueryData(["answers", id], (prev: unknown) => {
        if (!prev) return prev;
        const raw = prev as { items?: Answer[] };
        const items = raw.items || (prev as Answer[]);
        const updated = (Array.isArray(items) ? items : []).map((a: Answer) =>
          a.answerId === data.answerId ? { ...a, isAccepted: true } : { ...a, isAccepted: false }
        );
        return raw.items ? { ...raw, items: updated } : updated;
      });
      queryClient.setQueryData<Question>(["question", id], (prev) => {
        if (!prev) return prev;
        return { ...prev, status: "Solved" };
      });
    },
    [id, queryClient]
  );

  const handleNewAnswer = useCallback(
    (data: { answer: Answer }) => {
      queryClient.setQueryData(["answers", id], (prev: unknown) => {
        if (!prev) return prev;
        const raw = prev as { items?: Answer[] };
        const items = raw.items || (prev as Answer[]);
        if (items.find(a => a.answerId === data.answer.answerId)) {
          return prev;
        }
        const updated = [...(Array.isArray(items) ? items : []), data.answer];
        return raw.items ? { ...raw, items: updated } : updated;
      });
      queryClient.setQueryData<Question>(["question", id], (prev) => {
        if (!prev) return prev;
        return { ...prev, answerCount: (prev.answerCount || 0) + 1 };
      });
    },
    [id, queryClient]
  );

  useEffect(() => {
    questionHub.on("VoteChanged", handleVoteChanged);
    questionHub.on("NewComment", handleNewComment);
    questionHub.on("AnswerUpdated", handleAnswerUpdated);
    questionHub.on("AnswerAccepted", handleAnswerAccepted);
    questionHub.on("NewAnswer", handleNewAnswer);
    return () => {
      questionHub.off("VoteChanged", handleVoteChanged);
      questionHub.off("NewComment", handleNewComment);
      questionHub.off("AnswerUpdated", handleAnswerUpdated);
      questionHub.off("AnswerAccepted", handleAnswerAccepted);
      questionHub.off("NewAnswer", handleNewAnswer);
    };
  }, [questionHub, handleVoteChanged, handleNewComment, handleAnswerUpdated, handleAnswerAccepted, handleNewAnswer]);

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

  const likeMutation = useMutation({
    mutationFn: ({
      targetType,
      targetId,
      isLiked,
      voteType,
    }: {
      targetType: "question" | "answer";
      targetId: number;
      isLiked: boolean;
      voteType: 'up' | 'down';
    }) => {
      if (isLiked) {
        return targetType === "question"
          ? votesApi.removeQuestionVote(targetId)
          : votesApi.removeAnswerVote(targetId);
      }
      return targetType === "question"
        ? votesApi.voteQuestion(targetId, { voteType })
        : votesApi.voteAnswer(targetId, { voteType });
    },
    onMutate: async ({ targetType, targetId, isLiked, voteType }) => {
      const delta = isLiked ? -1 : 1;
      const newVote = isLiked ? null : "up";

      if (targetType === "question") {
        await queryClient.cancelQueries({ queryKey: ["question", id] });
        const prev = queryClient.getQueryData<Question>(["question", id]);
        if (prev) {
          queryClient.setQueryData<Question>(["question", id], {
            ...prev,
            score: prev.score + delta,
            userVoteType: newVote as Question["userVoteType"],
          });
        }
        return { prev };
      } else {
        await queryClient.cancelQueries({ queryKey: ["answers", id] });
        const prev = queryClient.getQueryData(["answers", id]);
        if (prev) {
          const raw = prev as { items?: Answer[] };
          const items = raw.items || (prev as Answer[]);
          const updated = (Array.isArray(items) ? items : []).map((a: Answer) =>
            a.answerId === targetId
              ? { ...a, score: a.score + delta, userVoteType: newVote as Answer["userVoteType"] }
              : a,
          );
          queryClient.setQueryData(["answers", id], raw.items ? { ...raw, items: updated } : updated);
        }
        return { prev };
      }
    },
    onError: (_err, vars, context) => {
      if (vars.targetType === "question" && context?.prev) {
        queryClient.setQueryData(["question", id], context.prev);
      } else if (context?.prev) {
        queryClient.setQueryData(["answers", id], context.prev);
      }
    },
    onSuccess: (_data, vars) => {
      const { targetType, targetId, isLiked, voteType } = vars;
      const newUserVote = isLiked ? null : voteType;

      if (targetType === "question") {
        const response = _data as { score: number; userVote?: string };
        const newScore = response.score;
        const newVoteType = newUserVote === "up" ? "up" : null;

        queryClient.setQueryData<Question>(["question", id], (prev) => {
          if (!prev) return prev;
          return { ...prev, score: newScore, userVoteType: newVoteType as Question["userVoteType"] };
        });
      } else {
        const response = _data as { score: number; userVote?: string };
        const newScore = response.score;
        const newVoteType = newUserVote === "up" ? "up" : null;

        queryClient.setQueryData(["answers", id], (prev: unknown) => {
          if (!prev) return prev;
          const raw = prev as { items?: Answer[] };
          const items = raw.items || (prev as Answer[]);
          const updated = (Array.isArray(items) ? items : []).map((a: Answer) =>
            a.answerId === targetId
              ? { ...a, score: newScore, userVoteType: newVoteType as Answer["userVoteType"] }
              : a,
          );
          return raw.items ? { ...raw, items: updated } : updated;
        });
      }
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: ["questions"] });
    },
  });

  const handleLike = (
    targetType: "question" | "answer",
    targetId: number,
    voteType: 'up' | 'down' = 'up'
  ) => {
    if (!user) return;

    if (targetType === "question" && user.userId === question?.authorId) return;
    if (targetType === "answer") {
      const targetAnswer = answers.find(a => a.answerId === targetId);
      if (targetAnswer && targetAnswer.authorId === user.userId) return;
    }

    const currentVote =
      targetType === "question"
        ? question?.userVoteType
        : answers.find(a => a.answerId === targetId)?.userVoteType;

    const isLiked = currentVote === voteType;

    likeMutation.mutate({ targetType, targetId, isLiked, voteType });
  };

  const acceptMutation = useMutation({
    mutationFn: (answerId: number) => answersApi.accept(answerId, Number(id)),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["answers", id] });
      queryClient.invalidateQueries({ queryKey: ["question", id] });
    },
  });

  const handleAccept = (answerId: number) => {
    if (!user || user.userId !== question?.authorId) return;
    acceptMutation.mutate(answerId);
  };

  const saveMutation = useMutation({
    mutationFn: ({ questionId, isSaved }: { questionId: number; isSaved: boolean }) =>
      isSaved
        ? savedItemsApi.unsaveQuestion(questionId)
        : savedItemsApi.saveQuestion(questionId),
    onMutate: async ({ isSaved }) => {
      await queryClient.cancelQueries({ queryKey: ["question", id] });
      const prev = queryClient.getQueryData<Question>(["question", id]);
      if (prev) {
        queryClient.setQueryData<Question>(["question", id], {
          ...prev,
          isSaved: !isSaved,
        });
      }
      return { prev };
    },
    onError: (_err, _vars, context) => {
      if (context?.prev) queryClient.setQueryData(["question", id], context.prev);
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: ["question", id] });
    },
  });

  const handleSave = () => {
    if (!user || !question) return;
    saveMutation.mutate({ questionId: question.questionId, isSaved: question.isSaved });
  };

  const scrollToAnswerForm = () => {
    answerFormRef.current?.scrollIntoView({ behavior: "smooth", block: "center" });
  };

  const [replyingTo, setReplyingTo] = useState<number | null>(null);
  const [replyText, setReplyText] = useState("");
  const replyInputRef = useRef<HTMLTextAreaElement>(null);

  const toggleReply = (answerId: number) => {
    if (replyingTo === answerId) {
      setReplyingTo(null);
      setReplyText("");
    } else {
      setReplyingTo(answerId);
      setReplyText("");
      setTimeout(() => replyInputRef.current?.focus(), 50);
    }
  };

  const commentMutation = useMutation({
    mutationFn: ({ answerId, body }: { answerId: number; body: string }) =>
      commentsApi.addToAnswer(answerId, body),
    onSuccess: () => {
      setReplyingTo(null);
      setReplyText("");
      queryClient.invalidateQueries({ queryKey: ["answers", id] });
    },
  });

  const handleSubmitComment = (answerId: number) => {
    if (!replyText.trim() || !user) return;
    commentMutation.mutate({ answerId, body: replyText });
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

            {/* Tags - ngay dưới meta, dễ thấy */}
            {(question.tags?.length ?? 0) > 0 && (
              <div className="mt-3 flex flex-wrap items-center gap-2">
                <span className="text-sm font-medium text-[var(--text-muted)] mr-1">Tags:</span>
                {question.tags.map((tag) => (
                  <Link
                    key={tag.tagId}
                    href={`/questions?tag=${tag.tagName}`}
                    className="inline-flex items-center gap-1 px-2.5 py-1 bg-[var(--primary)]/10 text-[var(--primary)] rounded-md text-sm font-medium hover:bg-[var(--primary)] hover:text-white transition"
                  >
                    #{tag.tagName}
                  </Link>
                ))}
              </div>
            )}
          </div>

          <div>
            <MarkdownContent
              content={question.body}
              className="mb-6 text-[var(--text-secondary)]"
            />

            {/* Like count */}
            {question.score > 0 && (
              <div className="flex items-center gap-1.5 mb-3 text-sm text-[var(--text-muted)]">
                <span className="w-5 h-5 rounded-full bg-[var(--primary)] flex items-center justify-center">
                  <span className="material-symbols-outlined text-white text-xs">thumb_up</span>
                </span>
                {question.score}
              </div>
            )}

            {/* Action bar */}
            <div className="flex flex-wrap sm:flex-nowrap items-center border-t border-b border-[var(--border-color)] py-1 mb-4">
              <button
                onClick={() => handleLike("question", question.questionId, "up")}
                disabled={!!user && user.userId === question.authorId}
                data-testid="question-upvote"
                className={`flex-1 flex items-center justify-center gap-2 py-2.5 rounded-lg font-medium text-sm transition hover:bg-[var(--bg-tertiary)] ${
                  question.userVoteType === "up"
                    ? "text-[var(--primary)]"
                    : "text-[var(--text-muted)]"
                }`}
              >
                <span className="material-symbols-outlined text-xl">
                  {question.userVoteType === "up" ? "thumb_up" : "thumb_up_off_alt"}
                </span>
                {question.score > 0 && <span>{question.score}</span>}
                Thích
              </button>
              <button
                onClick={() => handleLike("question", question.questionId, "down")}
                disabled={!!user && user.userId === question.authorId}
                data-testid="question-downvote"
                className={`flex-1 flex items-center justify-center gap-2 py-2.5 rounded-lg font-medium text-sm transition hover:bg-[var(--bg-tertiary)] ${
                  question.userVoteType === "down"
                    ? "text-red-500"
                    : "text-[var(--text-muted)]"
                }`}
              >
                <span className="material-symbols-outlined text-xl rotate-180">
                  {question.userVoteType === "down" ? "thumb_up" : "thumb_up_off_alt"}
                </span>
                Ghét
              </button>
              <button
                onClick={scrollToAnswerForm}
                data-testid="question-comment"
                className="flex-1 flex items-center justify-center gap-2 py-2.5 rounded-lg font-medium text-sm text-[var(--text-muted)] transition hover:bg-[var(--bg-tertiary)]"
              >
                <span className="material-symbols-outlined text-xl">comment</span>
                Bình luận
              </button>
              <button
                onClick={handleSave}
                className={`flex-1 flex items-center justify-center gap-2 py-2.5 rounded-lg font-medium text-sm transition hover:bg-[var(--bg-tertiary)] ${
                  question.isSaved ? "text-[var(--primary)]" : "text-[var(--text-muted)]"
                }`}
              >
                <span className="material-symbols-outlined text-xl">
                  {question.isSaved ? "bookmark" : "bookmark_border"}
                </span>
                {question.isSaved ? "Đã lưu" : "Lưu"}
              </button>
            </div>

            <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-3">
              <Link href="/questions" className="text-[var(--text-muted)] hover:text-[var(--text-primary)] text-sm font-medium transition">
                Tất cả câu hỏi
              </Link>
              <div className="flex items-center gap-3 bg-[var(--bg-tertiary)] p-3 rounded-xl border border-[var(--border-color)]">
                {question.authorProfilePicture ? (
                  <img
                    src={question.authorProfilePicture}
                    alt={question.authorUsername || ""}
                    className="w-10 h-10 rounded-lg object-cover"
                  />
                ) : (
                  <div className="w-10 h-10 rounded-lg bg-gradient-to-br from-blue-500 to-cyan-500 flex items-center justify-center text-white font-bold">
                    {authorInitial(question.authorUsername)}
                  </div>
                )}
                <div className="text-sm">
                  <div className="text-[var(--text-muted)]">Hỏi bởi</div>
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
                <div>
                  {answer.isAccepted && (
                    <div className="flex items-center gap-2 mb-3 px-3 py-2 bg-green-500/10 border border-green-500/20 rounded-lg text-green-600 text-sm font-medium">
                      <span className="material-symbols-outlined text-base">check_circle</span>
                      Câu trả lời được chấp nhận
                    </div>
                  )}

                  <MarkdownContent
                    content={answer.body}
                    className="mb-4 text-[var(--text-secondary)]"
                  />

                  {/* Like count */}
                  {answer.score > 0 && (
                    <div className="flex items-center gap-1.5 mb-3 text-sm text-[var(--text-muted)]">
                      <span className="w-5 h-5 rounded-full bg-[var(--primary)] flex items-center justify-center">
                        <span className="material-symbols-outlined text-white text-xs">thumb_up</span>
                      </span>
                      {answer.score}
                    </div>
                  )}

                  {/* Action bar */}
                  <div className="flex items-center border-t border-[var(--border-color)] pt-2 mb-3">
                    <button
                      onClick={() => handleLike("answer", answer.answerId, "up")}
                      data-testid={`answer-upvote-${answer.answerId}`}
                      className={`flex items-center gap-1.5 px-3 py-2 rounded-lg font-medium text-sm transition hover:bg-[var(--bg-tertiary)] ${
                        answer.userVoteType === "up"
                          ? "text-[var(--primary)]"
                          : "text-[var(--text-muted)]"
                      }`}
                    >
                      <span className="material-symbols-outlined text-lg">
                        {answer.userVoteType === "up" ? "thumb_up" : "thumb_up_off_alt"}
                      </span>
                      {answer.score > 0 && <span>{answer.score}</span>}
                    </button>
                    <button
                      onClick={() => handleLike("answer", answer.answerId, "down")}
                      data-testid={`answer-downvote-${answer.answerId}`}
                      className={`flex items-center gap-1.5 px-3 py-2 rounded-lg font-medium text-sm transition hover:bg-[var(--bg-tertiary)] ${
                        answer.userVoteType === "down"
                          ? "text-red-500"
                          : "text-[var(--text-muted)]"
                      }`}
                    >
                      <span className="material-symbols-outlined text-lg rotate-180">
                        {answer.userVoteType === "down" ? "thumb_up" : "thumb_up_off_alt"}
                      </span>
                    </button>
                    {user && user.userId === question.authorId && !answer.isAccepted && (
                      <button
                        onClick={() => handleAccept(answer.answerId)}
                        data-testid={`answer-accept-${answer.answerId}`}
                        disabled={acceptMutation.isPending}
                        className="flex items-center gap-1.5 px-4 py-2 bg-green-500/10 text-green-600 rounded-lg font-medium text-sm hover:bg-green-500/20 transition disabled:opacity-50"
                      >
                        <span className="material-symbols-outlined text-lg">check_circle</span>
                        Accept Answer
                      </button>
                    )}
                    <button
                      onClick={() => user && toggleReply(answer.answerId)}
                      className={`flex items-center gap-1.5 px-4 py-2 rounded-lg font-medium text-sm transition hover:bg-[var(--bg-tertiary)] ${
                        replyingTo === answer.answerId
                          ? "text-[var(--primary)]"
                          : "text-[var(--text-muted)]"
                      }`}
                    >
                      <span className="material-symbols-outlined text-lg">reply</span>
                      Phản hồi
                      {answer.comments?.length > 0 && (
                        <span className="text-xs">({answer.comments.length})</span>
                      )}
                    </button>
                  </div>

                  {/* Comments list */}
                  {answer.comments?.length > 0 && (
                    <div className="ml-4 pl-4 border-l-2 border-[var(--border-color)] mb-3 space-y-3">
                      {answer.comments.map((comment: Comment) => (
                        <div key={comment.commentId} className="flex gap-2.5">
                          {comment.authorProfilePicture ? (
                            <img
                              src={comment.authorProfilePicture}
                              alt={comment.authorUsername || ""}
                              className="w-6 h-6 rounded-full object-cover flex-shrink-0 mt-0.5"
                            />
                          ) : (
                            <div className="w-6 h-6 rounded-full bg-gradient-to-br from-amber-500 to-orange-500 flex-shrink-0 flex items-center justify-center text-white text-[10px] font-bold mt-0.5">
                              {authorInitial(comment.authorUsername)}
                            </div>
                          )}
                          <div className="flex-1 min-w-0">
                            <div className="bg-[var(--bg-tertiary)] rounded-xl px-3 py-2">
                              <Link
                                href={`/users/${comment.authorId}`}
                                className="text-xs font-semibold text-[var(--primary)] hover:underline"
                              >
                                {comment.authorUsername}
                              </Link>
                              <p className="text-sm text-[var(--text-secondary)] mt-0.5 whitespace-pre-wrap break-words">
                                {comment.body}
                              </p>
                            </div>
                            <RelativeTime
                              value={comment.createdDate}
                              className="text-[10px] text-[var(--text-muted)] ml-3 mt-0.5"
                            />
                          </div>
                        </div>
                      ))}
                    </div>
                  )}

                  {/* Reply form */}
                  {replyingTo === answer.answerId && user && (
                    <div className="ml-4 pl-4 border-l-2 border-[var(--primary)]/30 mb-3">
                      <div className="flex gap-2.5">
                        {user.profilePicture ? (
                          <img
                            src={user.profilePicture}
                            alt={user.displayName || user.username}
                            className="w-7 h-7 rounded-full object-cover flex-shrink-0 mt-1"
                          />
                        ) : (
                          <div className="w-7 h-7 rounded-full bg-gradient-to-br from-blue-500 to-cyan-500 flex-shrink-0 flex items-center justify-center text-white text-xs font-bold mt-1">
                            {authorInitial(user.displayName || user.username)}
                          </div>
                        )}
                        <div className="flex-1">
                          <textarea
                            ref={replyInputRef}
                            value={replyText}
                            onChange={(e) => setReplyText(e.target.value)}
                            onKeyDown={(e) => {
                              if (e.key === "Enter" && !e.shiftKey) {
                                e.preventDefault();
                                handleSubmitComment(answer.answerId);
                              }
                            }}
                            rows={2}
                            className="w-full px-3 py-2 bg-[var(--bg-tertiary)] border border-[var(--border-color)] rounded-xl text-sm text-[var(--text-primary)] placeholder-[var(--text-muted)] focus:border-[var(--primary)] focus:ring-1 focus:ring-[var(--primary)]/20 transition resize-none"
                            placeholder="Viết phản hồi... (Enter để gửi)"
                          />
                          <div className="flex items-center justify-end gap-2 mt-1.5">
                            <button
                              onClick={() => { setReplyingTo(null); setReplyText(""); }}
                              className="px-3 py-1 text-xs font-medium text-[var(--text-muted)] hover:text-[var(--text-primary)] rounded-lg transition"
                            >
                              Hủy
                            </button>
                            <button
                              onClick={() => handleSubmitComment(answer.answerId)}
                              disabled={!replyText.trim() || commentMutation.isPending}
                              className="px-3 py-1 text-xs font-medium bg-[var(--primary)] text-white rounded-lg hover:bg-[var(--primary-dark)] transition disabled:opacity-50"
                            >
                              {commentMutation.isPending ? "Đang gửi..." : "Gửi"}
                            </button>
                          </div>
                        </div>
                      </div>
                    </div>
                  )}

                  <div className="flex items-center justify-between">
                    <RelativeTime
                      value={answer.createdDate}
                      prefix="Trả lời "
                      className="text-xs text-[var(--text-muted)]"
                    />
                    <div className="flex items-center gap-2">
                      {answer.authorProfilePicture ? (
                        <img
                          src={answer.authorProfilePicture}
                          alt={answer.authorUsername || ""}
                          className="w-6 h-6 rounded object-cover"
                        />
                      ) : (
                        <div className="w-6 h-6 rounded bg-gradient-to-br from-purple-500 to-pink-500 flex items-center justify-center text-white text-xs font-bold">
                          {authorInitial(answer.authorUsername)}
                        </div>
                      )}
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
            ))}
          </div>
        </div>

        {/* Add Answer Form */}
        {user ? (
          <div ref={answerFormRef} className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-2xl p-6 shadow-sm">
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
                href="/auth?mode=login"
                className="px-6 py-2 bg-[var(--primary)] text-white rounded-xl font-medium hover:bg-[var(--primary-dark)] transition"
              >
                Log In
              </Link>
              <Link
                href="/auth?mode=register"
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
