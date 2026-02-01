'use client';

import Link from 'next/link';

export default function ModernFooter() {
    const currentYear = new Date().getFullYear();

    return (
        <footer className="bg-[var(--bg-secondary)] border-t border-[var(--border-color)]">
            {/* Main Footer - Instagram Style (Compact) */}
            <div className="max-w-7xl mx-auto px-6 py-8">
                <div className="flex flex-col md:flex-row items-center justify-between gap-6">

                    {/* Brand */}
                    <div className="flex items-center gap-3">
                        <span className="text-2xl">🚀</span>
                        <div>
                            <span className="font-bold text-[var(--text-primary)]">Dev<span className="text-[var(--primary)]">Community</span></span>
                            <p className="text-xs text-[var(--text-muted)]">Where developers connect & grow</p>
                        </div>
                    </div>

                    {/* Links - Instagram Style Horizontal */}
                    <nav className="flex flex-wrap items-center justify-center gap-4 md:gap-6 text-sm text-[var(--text-muted)]">
                        <Link href="/about" className="hover:text-[var(--text-primary)] transition-colors">About</Link>
                        <Link href="/questions" className="hover:text-[var(--text-primary)] transition-colors">Questions</Link>
                        <Link href="/tags" className="hover:text-[var(--text-primary)] transition-colors">Tags</Link>
                        <Link href="/users" className="hover:text-[var(--text-primary)] transition-colors">Users</Link>
                        <Link href="/help" className="hover:text-[var(--text-primary)] transition-colors">Help</Link>
                        <Link href="/privacy" className="hover:text-[var(--text-primary)] transition-colors">Privacy</Link>
                        <Link href="/terms" className="hover:text-[var(--text-primary)] transition-colors">Terms</Link>
                    </nav>

                    {/* Social Icons */}
                    <div className="flex items-center gap-4">
                        <a href="#" className="text-[var(--text-muted)] hover:text-[var(--primary)] transition-colors">
                            <i className="bi bi-github text-lg"></i>
                        </a>
                        <a href="#" className="text-[var(--text-muted)] hover:text-[var(--primary)] transition-colors">
                            <i className="bi bi-twitter-x text-lg"></i>
                        </a>
                        <a href="#" className="text-[var(--text-muted)] hover:text-[var(--primary)] transition-colors">
                            <i className="bi bi-discord text-lg"></i>
                        </a>
                    </div>
                </div>
            </div>

            {/* Copyright Bar */}
            <div className="border-t border-[var(--border-color)]">
                <div className="max-w-7xl mx-auto px-6 py-4">
                    <p className="text-center text-xs text-[var(--text-muted)]">
                        &copy; {currentYear} DevCommunity. Made with <span className="text-red-500">♥</span> for developers worldwide.
                    </p>
                </div>
            </div>
        </footer>
    );
}
