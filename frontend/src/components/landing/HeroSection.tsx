'use client';

import Link from 'next/link';

export default function HeroSection() {
  return (
    <section className="landing-hero">
      <div className="landing-hero-content">
        <div className="landing-hero-badge">
          <span className="landing-ping-wrapper">
            <span className="landing-ping" />
            <span className="landing-ping-dot" />
          </span>
          Join 50k+ Developers
        </div>

        <div className="landing-hero-text">
          <h1 className="landing-hero-title">
            Welcome to <span className="text-primary">SocialTechsy</span>
          </h1>
          <p className="landing-hero-desc">
            A vibrant developer community for sharing knowledge, solving complex
            technical problems, and growing your career together.
          </p>
        </div>

        <div className="landing-hero-buttons">
          <Link href="/auth?mode=register" className="landing-btn-hero-primary">
            Get Started
            <span className="material-symbols-outlined" style={{ fontSize: '18px' }}>arrow_forward</span>
          </Link>
          <Link href="/questions" className="landing-btn-hero-secondary">
            Browse Questions
          </Link>
        </div>

        <div className="landing-hero-social-proof">
          <div className="landing-avatars">
            <div className="landing-avatar" style={{ background: 'linear-gradient(135deg,#667eea,#764ba2)' }}>A</div>
            <div className="landing-avatar" style={{ background: 'linear-gradient(135deg,#f093fb,#f5576c)' }}>B</div>
            <div className="landing-avatar" style={{ background: 'linear-gradient(135deg,#4facfe,#00f2fe)' }}>C</div>
            <div className="landing-avatar-count">+12k</div>
          </div>
          <p className="landing-social-proof-text">Active developers online right now</p>
        </div>
      </div>

      <div className="landing-hero-visual">
        <div className="landing-hero-glow" />
        <div className="landing-hero-image-wrapper">
          <div className="landing-hero-image">
            <div className="landing-code-lines">
              <div className="landing-code-line" style={{ width: '60%', background: '#60a5fa' }} />
              <div className="landing-code-line" style={{ width: '80%', background: '#818cf8' }} />
              <div className="landing-code-line" style={{ width: '45%', background: '#34d399' }} />
              <div className="landing-code-line" style={{ width: '70%', background: '#f472b6' }} />
              <div className="landing-code-line" style={{ width: '55%', background: '#fbbf24' }} />
              <div className="landing-code-line" style={{ width: '75%', background: '#60a5fa' }} />
              <div className="landing-code-line" style={{ width: '40%', background: '#a78bfa' }} />
              <div className="landing-code-line" style={{ width: '65%', background: '#34d399' }} />
            </div>
          </div>
          <div className="landing-hero-badge-live">
            <div className="landing-live-dot" />
            <p>Live Discussion: WebAssembly vs JS</p>
          </div>
        </div>
      </div>
    </section>
  );
}
