'use client';

import Link from 'next/link';
import { useEffect, useState } from 'react';
import type { Question, PaginatedResponse, Tag } from '@/types';
import apiClient from '@/lib/api/client';
import RelativeTime from '@/components/RelativeTime';

const stripHtml = (html: string): string => {
  if (!html) return '';
  return html.replace(/<[^>]*>/g, '').substring(0, 200);
};

const FALLBACK_QUESTIONS = [
  {
    questionId: -1,
    title: 'How to optimize React context for large state objects to prevent re-renders?',
    body: "I'm building a dashboard with massive data states. My entire app re-renders every time a single value in the context provider updates. Are there best practices...",
    score: 42, answerCount: 12, viewCount: 0, hasAcceptedAnswer: false,
    createdDate: new Date(Date.now() - 2 * 3600000).toISOString(), status: 'open',
    authorUsername: 'dev_alex',
    tags: [{ tagId: 1, tagName: 'react', usageCount: 0 }, { tagId: 2, tagName: 'javascript', usageCount: 0 }, { tagId: 3, tagName: 'performance', usageCount: 0 }],
  },
  {
    questionId: -2,
    title: 'Best way to handle authentication with Next.js 14 Server Actions?',
    body: "Transitioning from Pages router to App router and I'm confused about the middleware vs server action security flow for JWT tokens...",
    score: 89, answerCount: 5, viewCount: 0, hasAcceptedAnswer: false,
    createdDate: new Date(Date.now() - 5 * 3600000).toISOString(), status: 'open',
    authorUsername: 'sarah_codes',
    tags: [{ tagId: 4, tagName: 'nextjs', usageCount: 0 }, { tagId: 5, tagName: 'auth', usageCount: 0 }],
  },
];

export default function QuestionsFeed() {
  const [questions, setQuestions] = useState<Question[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [filter, setFilter] = useState<'newest' | 'hot'>('newest');

  useEffect(() => {
    fetchQuestions();
  }, []);

  const fetchQuestions = async () => {
    try {
      const response = await apiClient.get<PaginatedResponse<Question>>('/questions?page=1&pageSize=5');
      const items = response.data.items || [];
      setQuestions(items.length > 0 ? items : FALLBACK_QUESTIONS as unknown as Question[]);
    } catch {
      setQuestions(FALLBACK_QUESTIONS as unknown as Question[]);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="landing-questions">
      <div className="landing-questions-header">
        <h2 className="landing-section-title">Recent Questions</h2>
        <div className="landing-filter-buttons">
          <button
            className={`landing-filter-btn ${filter === 'newest' ? 'active' : ''}`}
            onClick={() => setFilter('newest')}
          >Newest</button>
          <button
            className={`landing-filter-btn ${filter === 'hot' ? 'active' : ''}`}
            onClick={() => setFilter('hot')}
          >Hot</button>
        </div>
      </div>

      {isLoading ? (
        <div className="landing-loading">
          <div className="landing-spinner" />
        </div>
      ) : (
        <div className="landing-questions-list">
          {questions.slice(0, 5).map((q) => (
            <div key={q.questionId} className="landing-question-card">
              <div className="landing-question-stats">
                <div className="landing-stat">
                  <p className="landing-stat-number">{q.score}</p>
                  <p className="landing-stat-label">Votes</p>
                </div>
                <div className={`landing-stat ${q.answerCount > 0 ? 'has-answers' : ''}`}>
                  <p className="landing-stat-number">{q.answerCount}</p>
                  <p className="landing-stat-label">Answers</p>
                </div>
              </div>
              <div className="landing-question-content">
                <Link
                  href={q.questionId > 0 ? `/questions/${q.questionId}` : '/auth?mode=register'}
                  className="landing-question-title"
                >
                  {q.title}
                </Link>
                <p className="landing-question-excerpt">
                  {stripHtml(q.bodyExcerpt || q.body || '')}
                </p>
                <div className="landing-question-meta">
                  <div className="landing-tag-list">
                    {q.tags?.slice(0, 4).map((tag: Tag) => (
                      <span key={tag.tagId} className="landing-tag">#{tag.tagName}</span>
                    ))}
                  </div>
                  <div className="landing-question-author">
                    <div className="landing-author-avatar">
                      {q.authorUsername?.charAt(0).toUpperCase() || '?'}
                    </div>
                    <span className="landing-author-name">{q.authorUsername || 'Anonymous'}</span>
                    <RelativeTime
                      value={q.createdDate}
                      prefix="asked "
                      className="landing-author-time"
                    />
                  </div>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      <Link href="/questions" className="landing-load-more">
        View Older Questions
      </Link>
    </div>
  );
}
