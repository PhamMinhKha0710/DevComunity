'use client';

import { useAuth } from "@/lib/contexts/AuthContext";
import Link from "next/link";
import { useEffect, useState } from "react";
import type { Question, PaginatedResponse, Tag } from "@/types";
import apiClient from "@/lib/api/client";

// Helper to strip HTML tags and get plain text
const stripHtml = (html: string): string => {
  if (!html) return '';
  return html.replace(/<[^>]*>/g, '').substring(0, 150);
};

export default function HomePage() {
  const { user } = useAuth();
  const [questions, setQuestions] = useState<Question[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [stats] = useState({ users: 1000, questions: 5000 });

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

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-slate-900">
      {/* Hero Banner */}
      <div className="bg-gradient-to-r from-orange-500 to-orange-600 text-white">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12 md:py-16">
          <div className="grid md:grid-cols-2 gap-8 items-center">
            <div>
              <h1 className="text-3xl md:text-4xl lg:text-5xl font-bold mb-4">
                Welcome back, {user?.displayName || user?.username || 'Developer'}!
              </h1>
              <p className="text-lg md:text-xl opacity-90 mb-6">
                Find answers to your technical questions and help others in our developer community.
              </p>
              <div className="flex flex-col sm:flex-row gap-3">
                <Link
                  href="/questions/ask"
                  className="inline-flex items-center justify-center px-6 py-3 bg-white text-orange-600 font-semibold rounded-full hover:bg-gray-100 transition"
                >
                  <i className="bi bi-plus-circle mr-2"></i>Ask a Question
                </Link>
                <Link
                  href="/questions"
                  className="inline-flex items-center justify-center px-6 py-3 border-2 border-white text-white font-semibold rounded-full hover:bg-white/10 transition"
                >
                  <i className="bi bi-search mr-2"></i>Browse Questions
                </Link>
              </div>
            </div>
            <div className="hidden md:flex justify-center relative">
              <div className="text-center">
                <i className="bi bi-code-slash text-8xl opacity-50"></i>
                <div className="absolute top-0 right-10 bg-white text-orange-600 px-4 py-2 rounded-full shadow-lg text-sm font-medium">
                  <i className="bi bi-people-fill mr-1"></i>{stats.users}+ Users
                </div>
                <div className="absolute bottom-0 left-10 bg-white text-orange-600 px-4 py-2 rounded-full shadow-lg text-sm font-medium">
                  <i className="bi bi-chat-dots-fill mr-1"></i>{stats.questions}+ Questions
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Main Content */}
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <div className="grid lg:grid-cols-3 gap-8">
          {/* Questions List */}
          <div className="lg:col-span-2">
            <div className="bg-white dark:bg-slate-800 rounded-xl shadow-sm">
              <div className="p-4 md:p-6 border-b border-gray-100 dark:border-slate-700 flex justify-between items-center">
                <div>
                  <h2 className="text-xl font-bold text-gray-900 dark:text-white">Interesting Questions</h2>
                  <p className="text-sm text-gray-500 dark:text-gray-400">Based on your interests</p>
                </div>
                <select className="text-sm border border-gray-300 dark:border-slate-600 rounded-lg px-3 py-2 bg-white dark:bg-slate-700">
                  <option>Hot</option>
                  <option>Recent</option>
                  <option>Most Voted</option>
                </select>
              </div>

              {isLoading ? (
                <div className="p-12 text-center">
                  <div className="inline-block w-8 h-8 border-4 border-orange-500 border-t-transparent rounded-full animate-spin"></div>
                </div>
              ) : questions.length > 0 ? (
                <div className="divide-y divide-gray-100 dark:divide-slate-700">
                  {questions.map((question) => (
                    <div key={question.questionId} className="p-4 md:p-6 hover:bg-gray-50 dark:hover:bg-slate-700/50 transition">
                      <div className="flex gap-4">
                        {/* Stats */}
                        <div className="hidden sm:flex flex-col items-center gap-2 text-center min-w-[70px]">
                          <div className={`px-3 py-1 rounded-full text-sm ${question.score > 0 ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-600'}`}>
                            <span className="font-bold">{question.score}</span>
                            <span className="block text-xs">votes</span>
                          </div>
                          <div className={`px-3 py-1 rounded-full text-sm ${question.answerCount > 0 ? 'bg-blue-100 text-blue-700' : 'bg-gray-100 text-gray-600'}`}>
                            <span className="font-bold">{question.answerCount}</span>
                            <span className="block text-xs">answers</span>
                          </div>
                        </div>

                        {/* Content */}
                        <div className="flex-1">
                          <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-2 hover:text-orange-500 transition">
                            <Link href={`/questions/${question.questionId}`}>
                              {question.title}
                            </Link>
                          </h3>
                          <p className="text-gray-600 dark:text-gray-400 text-sm mb-3 line-clamp-2">
                            {stripHtml(question.bodyExcerpt || question.body || '')}...
                          </p>
                          <div className="flex flex-wrap items-center justify-between gap-2">
                            <div className="flex flex-wrap gap-1">
                              {question.tags?.slice(0, 4).map((tag: Tag) => (
                                <Link
                                  key={tag.tagId}
                                  href={`/questions?tag=${tag.tagName}`}
                                  className="px-2 py-1 bg-orange-100 dark:bg-orange-900/30 text-orange-600 dark:text-orange-400 text-xs rounded-md hover:bg-orange-200 transition"
                                >
                                  {tag.tagName}
                                </Link>
                              ))}
                              {question.tags?.length > 4 && (
                                <span className="px-2 py-1 bg-gray-100 text-gray-500 text-xs rounded-md">
                                  +{question.tags.length - 4}
                                </span>
                              )}
                            </div>
                            <div className="flex items-center text-xs text-gray-500">
                              <img
                                src={question.authorProfilePicture || '/images/default-avatar.png'}
                                className="w-5 h-5 rounded-full mr-1"
                                alt=""
                              />
                              <span>
                                asked {new Date(question.createdDate).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })} by{' '}
                                <span className="text-orange-500 font-medium">{question.authorUsername || 'Anonymous'}</span>
                              </span>
                            </div>
                          </div>
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              ) : (
                <div className="p-12 text-center">
                  <div className="w-20 h-20 bg-gray-100 dark:bg-slate-700 rounded-full mx-auto mb-4 flex items-center justify-center">
                    <i className="bi bi-chat-square-dots text-4xl text-gray-400"></i>
                  </div>
                  <h3 className="text-lg font-bold text-gray-900 dark:text-white mb-2">No questions found</h3>
                  <p className="text-gray-500 mb-4">Be the first to ask a question!</p>
                  <Link href="/questions/ask" className="inline-flex items-center px-4 py-2 bg-orange-500 text-white rounded-full hover:bg-orange-600 transition">
                    <i className="bi bi-plus-circle mr-2"></i>Ask a Question
                  </Link>
                </div>
              )}

              {questions.length > 0 && (
                <div className="p-4 text-center border-t border-gray-100 dark:border-slate-700">
                  <Link href="/questions" className="inline-flex items-center text-orange-500 hover:text-orange-600 font-medium">
                    View All Questions <i className="bi bi-arrow-right ml-1"></i>
                  </Link>
                </div>
              )}
            </div>
          </div>

          {/* Sidebar */}
          <div className="space-y-6">
            {/* User Card */}
            <div className="bg-white dark:bg-slate-800 rounded-xl shadow-sm p-6">
              {user ? (
                <div className="text-center">
                  <img
                    src={user.profilePicture || '/images/default-avatar.png'}
                    className="w-20 h-20 rounded-full mx-auto mb-3 border-4 border-gray-100"
                    alt="Profile"
                  />
                  <h3 className="font-bold text-gray-900 dark:text-white">{user.displayName || user.username}</h3>
                  <p className="text-sm text-gray-500 mb-4">@{user.username}</p>
                  <div className="grid grid-cols-3 gap-2 mb-4">
                    <div className="bg-gray-50 dark:bg-slate-700 rounded-lg p-2 text-center">
                      <div className="font-bold text-orange-500">{user.reputationPoints || 0}</div>
                      <div className="text-xs text-gray-500">Reputation</div>
                    </div>
                    <div className="bg-gray-50 dark:bg-slate-700 rounded-lg p-2 text-center">
                      <div className="font-bold text-gray-900 dark:text-white">0</div>
                      <div className="text-xs text-gray-500">Questions</div>
                    </div>
                    <div className="bg-gray-50 dark:bg-slate-700 rounded-lg p-2 text-center">
                      <div className="font-bold text-gray-900 dark:text-white">0</div>
                      <div className="text-xs text-gray-500">Answers</div>
                    </div>
                  </div>
                  <Link href="/profile" className="block w-full py-2 border border-orange-500 text-orange-500 rounded-full hover:bg-orange-50 transition">
                    View Profile
                  </Link>
                </div>
              ) : (
                <div className="text-center">
                  <div className="w-20 h-20 bg-gray-100 dark:bg-slate-700 rounded-full mx-auto mb-3 flex items-center justify-center">
                    <i className="bi bi-person-circle text-4xl text-gray-400"></i>
                  </div>
                  <h3 className="font-bold text-gray-900 dark:text-white mb-2">Join our community</h3>
                  <p className="text-sm text-gray-500 mb-4">Sign up to ask questions and engage!</p>
                  <div className="space-y-2">
                    <Link href="/register" className="block w-full py-2 bg-orange-500 text-white rounded-full hover:bg-orange-600 transition">
                      Sign Up
                    </Link>
                    <Link href="/login" className="block w-full py-2 border border-gray-300 text-gray-700 dark:text-gray-300 rounded-full hover:bg-gray-50 dark:hover:bg-slate-700 transition">
                      Log In
                    </Link>
                  </div>
                </div>
              )}
            </div>

            {/* Trending Tags */}
            <div className="bg-white dark:bg-slate-800 rounded-xl shadow-sm p-6">
              <h3 className="font-bold text-gray-900 dark:text-white mb-4">Trending Topics</h3>
              <div className="flex flex-wrap gap-2">
                {['react', 'javascript', 'c#', 'python', 'asp.net-core', 'sql', 'docker', 'next.js'].map((tag) => (
                  <Link
                    key={tag}
                    href={`/questions?tag=${tag}`}
                    className="px-3 py-1 border border-orange-500 text-orange-500 text-sm rounded-full hover:bg-orange-500 hover:text-white transition"
                  >
                    {tag}
                  </Link>
                ))}
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
