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
  const [stats, setStats] = useState({ users: 1000, questions: 5000 });

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
    <>
      {/* Hero Banner Section */}
      <div className="hero-banner bg-gradient-primary text-white rounded-4 mb-4 p-4 p-md-5 mx-3">
        <div className="container">
          <div className="row align-items-center">
            <div className="col-lg-7">
              <h1 className="display-5 fw-bold mb-3 hero-title">
                Welcome back, {user?.displayName || user?.username || 'Guest'}!
              </h1>
              <p className="lead mb-4 hero-subtitle opacity-90">
                Find answers to your technical questions and help others in our developer community.
              </p>
              <div className="d-grid gap-2 d-md-flex">
                <Link href="/questions/ask" className="btn btn-light btn-lg px-4 text-primary rounded-pill me-md-2">
                  <i className="bi bi-plus-circle me-2"></i>Ask a Question
                </Link>
                <Link href="/questions" className="btn btn-outline-light btn-lg px-4 rounded-pill">
                  <i className="bi bi-search me-2"></i>Browse Questions
                </Link>
              </div>
            </div>
            <div className="col-lg-5 d-none d-lg-block">
              <div className="hero-illustration text-center">
                <i className="bi bi-chat-quote-fill display-1 mb-3 hero-icon"></i>
                <div className="d-flex justify-content-center position-relative">
                  <div className="stat-badge rounded-pill shadow-sm px-3 py-2 bg-white text-primary position-absolute" style={{ top: '-20px', right: '20px' }}>
                    <i className="bi bi-people-fill me-2"></i>{stats.users}+ Users
                  </div>
                  <div className="stat-badge rounded-pill shadow-sm px-3 py-2 bg-white text-primary position-absolute" style={{ bottom: '-20px', left: '20px' }}>
                    <i className="bi bi-chat-dots-fill me-2"></i>{stats.questions}+ Questions
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <div className="container">
        <div className="row g-4">
          {/* Main Content Area */}
          <div className="col-lg-8 order-2 order-lg-1">
            {/* Featured Questions Section */}
            <div className="card border-0 rounded-4 shadow-sm mb-4 hover-lift">
              <div className="card-header bg-white border-0 pt-4 pb-0 d-flex justify-content-between align-items-center">
                <div>
                  <h4 className="fw-bold mb-1">Interesting Questions</h4>
                  <p className="text-muted small">Based on your interests and activity</p>
                </div>
                <div className="dropdown">
                  <button className="btn btn-sm btn-outline-secondary rounded-pill" type="button" data-bs-toggle="dropdown">
                    <i className="bi bi-sort-down me-1"></i>Sort
                  </button>
                  <ul className="dropdown-menu dropdown-menu-end">
                    <li><a className="dropdown-item" href="#"><i className="bi bi-fire me-2"></i>Hot</a></li>
                    <li><a className="dropdown-item" href="#"><i className="bi bi-clock-history me-2"></i>Recent</a></li>
                    <li><a className="dropdown-item" href="#"><i className="bi bi-graph-up me-2"></i>Most Answered</a></li>
                  </ul>
                </div>
              </div>
              <div className="card-body p-0">
                {isLoading ? (
                  <div className="p-5 text-center">
                    <div className="spinner-border text-primary" role="status">
                      <span className="visually-hidden">Loading...</span>
                    </div>
                  </div>
                ) : questions.length > 0 ? (
                  <div className="list-group list-group-flush">
                    {questions.map((question) => (
                      <div key={question.questionId} className="list-group-item question-item p-4 border-0 border-bottom">
                        <div className="row align-items-center">
                          <div className="col-auto d-none d-sm-block">
                            <div className="question-stats d-flex flex-column align-items-center text-center" style={{ minWidth: '80px' }}>
                              <div className={`votes mb-2 py-2 px-3 rounded-pill ${question.score > 0 ? 'bg-success-subtle' : 'bg-light'}`}>
                                <span className={`fw-bold ${question.score > 0 ? 'text-success' : ''}`}>{question.score}</span>
                                <small className="d-block">votes</small>
                              </div>
                              <div className={`answers py-2 px-3 rounded-pill ${question.answerCount > 0 ? 'bg-primary-subtle' : 'bg-light'}`}>
                                <span className={`fw-bold ${question.answerCount > 0 ? 'text-primary' : ''}`}>{question.answerCount}</span>
                                <small className="d-block">answers</small>
                              </div>
                            </div>
                          </div>
                          <div className="col">
                            <div className="question-content">
                              <h5 className="mb-2 fw-bold question-title">
                                <Link href={`/questions/${question.questionId}`} className="stretched-link text-decoration-none text-dark">
                                  {question.title}
                                </Link>
                              </h5>
                              <p className="mb-3 question-excerpt text-muted">
                                {stripHtml(question.bodyExcerpt || question.body || '')}...
                              </p>
                              <div className="d-flex flex-wrap justify-content-between align-items-center">
                                <div className="tags mb-2 mb-sm-0">
                                  {question.tags?.slice(0, 4).map((tag: Tag) => (
                                    <Link key={tag.tagId} href={`/questions?tag=${tag.tagName}`} className="badge rounded-pill bg-light text-dark hover-primary me-1 text-decoration-none">
                                      {tag.tagName}
                                    </Link>
                                  ))}
                                  {question.tags?.length > 4 && (
                                    <span className="badge bg-light text-muted rounded-pill">+{question.tags.length - 4}</span>
                                  )}
                                </div>
                                <div className="user-info d-flex align-items-center small text-muted">
                                  <img src={question.authorProfilePicture || '/images/default-avatar.png'} className="avatar avatar-xs rounded-circle me-1" alt="User Avatar" style={{ width: '24px', height: '24px' }} />
                                  <span>asked {new Date(question.createdDate).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })} by <a href="#" className="text-decoration-none fw-medium">{question.authorUsername || 'Anonymous'}</a></span>
                                </div>
                              </div>
                            </div>
                          </div>
                        </div>
                      </div>
                    ))}
                  </div>
                ) : (
                  <div className="p-5 text-center empty-state">
                    <div className="empty-state-icon bg-light rounded-circle mx-auto mb-3 d-flex align-items-center justify-content-center" style={{ width: '90px', height: '90px' }}>
                      <i className="bi bi-chat-square-dots text-secondary fs-1"></i>
                    </div>
                    <h5 className="fw-bold mb-2">No questions found</h5>
                    <p className="text-muted mb-4">Be the first to ask a question and start the conversation!</p>
                    <Link href="/questions/ask" className="btn btn-primary rounded-pill px-4">
                      <i className="bi bi-plus-circle me-2"></i>Ask a Question
                    </Link>
                  </div>
                )}
                {questions.length > 0 && (
                  <div className="p-4 text-center">
                    <Link href="/questions" className="btn btn-outline-primary rounded-pill">
                      View All Questions <i className="bi bi-arrow-right ms-1"></i>
                    </Link>
                  </div>
                )}
              </div>
            </div>
          </div>

          {/* Sidebar */}
          <div className="col-lg-4 order-1 order-lg-2">
            {/* Profile & Stats Card */}
            <div className="card border-0 rounded-4 shadow-sm mb-4 hover-lift d-none d-md-block">
              {user ? (
                <div className="card-body p-4">
                  <div className="text-center mb-4">
                    <div className="position-relative d-inline-block">
                      <img src={user.profilePicture || '/images/default-avatar.png'} className="rounded-circle img-thumbnail mb-3" width="100" height="100" alt="Profile Image" />
                      <span className="position-absolute bottom-0 end-0 bg-success rounded-circle p-1 border border-white" style={{ width: '20px', height: '20px' }}></span>
                    </div>
                    <h5 className="fw-bold mb-1">{user.displayName || user.username}</h5>
                    <p className="text-muted mb-0">@{user.username}</p>
                  </div>
                  <div className="row g-3 text-center mb-4">
                    <div className="col-4">
                      <div className="p-3 rounded-4 bg-light">
                        <h5 className="fw-bold mb-0 text-primary">{user.reputationPoints || 0}</h5>
                        <small className="text-muted">Reputation</small>
                      </div>
                    </div>
                    <div className="col-4">
                      <div className="p-3 rounded-4 bg-light">
                        <h5 className="fw-bold mb-0">0</h5>
                        <small className="text-muted">Questions</small>
                      </div>
                    </div>
                    <div className="col-4">
                      <div className="p-3 rounded-4 bg-light">
                        <h5 className="fw-bold mb-0">0</h5>
                        <small className="text-muted">Answers</small>
                      </div>
                    </div>
                  </div>
                  <Link href="/profile" className="btn btn-outline-primary rounded-pill d-block">View Profile</Link>
                </div>
              ) : (
                <div className="card-body p-4 text-center">
                  <div className="mb-4">
                    <div className="empty-state-icon bg-light rounded-circle mx-auto mb-3 d-flex align-items-center justify-content-center" style={{ width: '90px', height: '90px' }}>
                      <i className="bi bi-person-circle text-secondary fs-1"></i>
                    </div>
                    <h5 className="fw-bold mb-2">Join our community</h5>
                    <p className="text-muted">Sign up to ask questions, follow topics, and engage with the community.</p>
                  </div>
                  <div className="d-grid gap-2">
                    <Link href="/register" className="btn btn-primary rounded-pill">Sign Up</Link>
                    <Link href="/login" className="btn btn-outline-secondary rounded-pill">Log In</Link>
                  </div>
                </div>
              )}
            </div>

            {/* Trending Topics Card */}
            <div className="card border-0 rounded-4 shadow-sm mb-4 hover-lift">
              <div className="card-body p-4">
                <h5 className="fw-bold card-title mb-3">Trending Topics</h5>
                <div className="trending-tags">
                  <a href="#" className="btn btn-sm btn-outline-primary rounded-pill mb-2 me-1">react</a>
                  <a href="#" className="btn btn-sm btn-outline-primary rounded-pill mb-2 me-1">javascript</a>
                  <a href="#" className="btn btn-sm btn-outline-primary rounded-pill mb-2 me-1">c#</a>
                  <a href="#" className="btn btn-sm btn-outline-primary rounded-pill mb-2 me-1">python</a>
                  <a href="#" className="btn btn-sm btn-outline-primary rounded-pill mb-2 me-1">asp.net-core</a>
                  <a href="#" className="btn btn-sm btn-outline-primary rounded-pill mb-2 me-1">sql</a>
                  <a href="#" className="btn btn-sm btn-outline-primary rounded-pill mb-2 me-1">docker</a>
                  <a href="#" className="btn btn-sm btn-outline-primary rounded-pill mb-2 me-1">next.js</a>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </>
  );
}
