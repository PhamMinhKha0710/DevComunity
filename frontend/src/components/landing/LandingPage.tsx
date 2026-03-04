'use client';

import LandingHeader from './LandingHeader';
import HeroSection from './HeroSection';
import QuestionsFeed from './QuestionsFeed';
import LandingSidebar from './LandingSidebar';
import LandingFooter from './LandingFooter';

export default function LandingPage() {
  return (
    <div className="landing-root">
      <LandingHeader />

      <main className="landing-main">
        <HeroSection />

        <div className="landing-content-grid">
          <QuestionsFeed />
          <LandingSidebar />
        </div>
      </main>

      <LandingFooter />
    </div>
  );
}
