'use client';

import { useState } from 'react';
import { authorInitial } from '@/lib/utils';

const TRENDING_TOPICS = [
  { name: '#Typescript_5.4', posts: '1.2k posts this week' },
  { name: '#TailwindCSS_v4', posts: '850 posts this week' },
  { name: '#AI_Agents', posts: '2.4k posts this week' },
  { name: '#Docker_Swarm', posts: '320 posts this week' },
];

const TOP_CONTRIBUTORS = [
  { name: 'Marcus.dev', karma: '12.4k Karma', rank: 1, color: '#eab308' },
  { name: 'Elena_Q', karma: '9.1k Karma', rank: 2, color: '#9ca3af' },
  { name: 'ProCoder_88', karma: '7.8k Karma', rank: 3, color: '#d97706' },
];

export default function LandingSidebar() {
  const [email, setEmail] = useState('');

  return (
    <aside className="landing-sidebar">
      {/* Trending Topics */}
      <section className="landing-sidebar-card">
        <h3 className="landing-sidebar-title">
          <span className="material-symbols-outlined text-primary">trending_up</span>
          Trending Topics
        </h3>
        <ul className="landing-trending-list">
          {TRENDING_TOPICS.map((topic) => (
            <li key={topic.name} className="landing-trending-item">
              <div>
                <p className="landing-trending-name">{topic.name}</p>
                <p className="landing-trending-posts">{topic.posts}</p>
              </div>
              <span className="material-symbols-outlined landing-chevron">chevron_right</span>
            </li>
          ))}
        </ul>
      </section>

      {/* Top Contributors */}
      <section className="landing-sidebar-card">
        <h3 className="landing-sidebar-title">
          <span className="material-symbols-outlined text-primary">emoji_events</span>
          Top Contributors
        </h3>
        <div className="landing-contributors">
          {TOP_CONTRIBUTORS.map((c) => (
            <div key={c.name} className="landing-contributor">
              <div className="landing-contributor-avatar-wrapper">
                <div className="landing-contributor-avatar">
                  {authorInitial(c.name)}
                </div>
                <div className="landing-rank-badge" style={{ background: c.color }}>
                  {c.rank}
                </div>
              </div>
              <div className="landing-contributor-info">
                <p className="landing-contributor-name">{c.name}</p>
                <p className="landing-contributor-karma">{c.karma}</p>
              </div>
              <button className="landing-follow-btn">Follow</button>
            </div>
          ))}
        </div>
        <button className="landing-see-leaderboard">See Leaderboard</button>
      </section>

      {/* Newsletter */}
      <section className="landing-newsletter">
        <h3 className="landing-newsletter-title">Weekly Roundup</h3>
        <p className="landing-newsletter-desc">
          Get the best questions and technical articles in your inbox.
        </p>
        <form className="landing-newsletter-form" onSubmit={(e) => e.preventDefault()}>
          <input
            type="email"
            className="landing-newsletter-input"
            placeholder="Your email address"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
          />
          <button type="submit" className="landing-newsletter-btn">Subscribe</button>
        </form>
      </section>
    </aside>
  );
}
