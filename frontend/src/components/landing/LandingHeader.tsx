'use client';

import Link from 'next/link';
import Image from 'next/image';

export default function LandingHeader() {
  return (
    <header className="landing-header">
      <div className="landing-header-inner">
        <Link href="/" className="landing-brand">
          <div className="landing-brand-icon">
            <Image src="/logo.png" alt="SocialTechsy" width={28} height={28} priority />
          </div>
          <h2 className="landing-brand-text">SocialTechsy</h2>
        </Link>

        <nav className="landing-nav">
          <Link href="/questions" className="landing-nav-link">Feed</Link>
          <Link href="/questions" className="landing-nav-link">Questions</Link>
          <Link href="/tags" className="landing-nav-link">Tags</Link>
          <Link href="/users" className="landing-nav-link">Contributors</Link>
        </nav>

        <div className="landing-header-actions">
          <Link href="/login" className="landing-btn-text">Log In</Link>
          <Link href="/register" className="landing-btn-primary">Sign Up</Link>
        </div>
      </div>
    </header>
  );
}
