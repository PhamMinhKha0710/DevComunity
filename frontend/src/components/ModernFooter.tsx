'use client';

import Link from 'next/link';

export default function ModernFooter() {
    const currentYear = new Date().getFullYear();

    return (
        <footer className="bg-[var(--bg-secondary)] border-t border-[var(--border-color)] text-[var(--text-secondary)]">
            {/* Top Section */}
            <div className="max-w-7xl mx-auto px-6 py-12 lg:py-16">
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-12 gap-12 lg:gap-8">

                    {/* Brand */}
                    <div className="lg:col-span-4">
                        <Link href="/" className="flex items-center gap-2 mb-6">
                            <span className="text-3xl">🚀</span>
                            <span className="font-bold text-2xl text-[var(--text-primary)]">
                                Dev<span className="text-[var(--primary)]">Community</span>
                            </span>
                        </Link>
                        <p className="text-[var(--text-muted)] mb-6 max-w-sm leading-relaxed">
                            The premier platform for developers to connect, share knowledge, and build their careers. Join the revolution today.
                        </p>
                        <div className="flex gap-4">
                            <a href="#" className="w-10 h-10 rounded-xl bg-[var(--bg-tertiary)] hover:bg-[var(--primary)] text-[var(--text-muted)] hover:text-white flex items-center justify-center transition-all duration-300 shadow-sm hover:shadow-lg hover:shadow-[var(--primary)]/30 hover:-translate-y-1">
                                <i className="bi bi-github text-xl"></i>
                            </a>
                            <a href="#" className="w-10 h-10 rounded-xl bg-[var(--bg-tertiary)] hover:bg-blue-400 text-[var(--text-muted)] hover:text-white flex items-center justify-center transition-all duration-300 shadow-sm hover:shadow-lg hover:shadow-blue-400/30 hover:-translate-y-1">
                                <i className="bi bi-twitter-x text-xl"></i>
                            </a>
                            <a href="#" className="w-10 h-10 rounded-xl bg-[var(--bg-tertiary)] hover:bg-indigo-500 text-[var(--text-muted)] hover:text-white flex items-center justify-center transition-all duration-300 shadow-sm hover:shadow-lg hover:shadow-indigo-500/30 hover:-translate-y-1">
                                <i className="bi bi-discord text-xl"></i>
                            </a>
                            <a href="#" className="w-10 h-10 rounded-xl bg-[var(--bg-tertiary)] hover:bg-blue-600 text-[var(--text-muted)] hover:text-white flex items-center justify-center transition-all duration-300 shadow-sm hover:shadow-lg hover:shadow-blue-600/30 hover:-translate-y-1">
                                <i className="bi bi-linkedin text-xl"></i>
                            </a>
                        </div>
                    </div>

                    {/* Links Column 1 */}
                    <div className="lg:col-span-2">
                        <h4 className="font-bold text-[var(--text-primary)] mb-6 text-lg">Platform</h4>
                        <ul className="space-y-4">
                            <li><Link href="/questions" className="hover:text-[var(--primary)] transition-colors">Questions</Link></li>
                            <li><Link href="/tags" className="hover:text-[var(--primary)] transition-colors">Tags</Link></li>
                            <li><Link href="/users" className="hover:text-[var(--primary)] transition-colors">Users</Link></li>
                            <li><Link href="/groups" className="hover:text-[var(--primary)] transition-colors">Communities</Link></li>
                        </ul>
                    </div>

                    {/* Links Column 2 */}
                    <div className="lg:col-span-2">
                        <h4 className="font-bold text-[var(--text-primary)] mb-6 text-lg">Company</h4>
                        <ul className="space-y-4">
                            <li><Link href="/about" className="hover:text-[var(--primary)] transition-colors">About Us</Link></li>
                            <li><Link href="/careers" className="hover:text-[var(--primary)] transition-colors">Careers</Link></li>
                            <li><Link href="/blog" className="hover:text-[var(--primary)] transition-colors">Blog</Link></li>
                            <li><Link href="/contact" className="hover:text-[var(--primary)] transition-colors">Contact</Link></li>
                        </ul>
                    </div>

                    {/* Newsletter */}
                    <div className="lg:col-span-4">
                        <h4 className="font-bold text-[var(--text-primary)] mb-6 text-lg">Stay Updated</h4>
                        <p className="text-[var(--text-muted)] mb-4">
                            Get the latest developer news, tips, and updates delivered straight to your inbox.
                        </p>
                        <form className="relative">
                            <input
                                type="email"
                                placeholder="Enter your email"
                                className="w-full pl-5 pr-12 py-3 bg-[var(--bg-primary)] border border-[var(--border-color)] rounded-xl focus:border-[var(--primary)] focus:ring-2 focus:ring-[var(--primary)]/20 text-[var(--text-primary)] placeholder-[var(--text-muted)] outline-none transition-all"
                            />
                            <button type="button" className="absolute right-2 top-2 p-1.5 bg-[var(--primary)] text-white rounded-lg hover:bg-[var(--primary-dark)] transition-colors shadow-lg shadow-[var(--primary)]/20">
                                <i className="bi bi-send-fill text-sm"></i>
                            </button>
                        </form>
                    </div>
                </div>
            </div>

            {/* Bottom Section */}
            <div className="border-t border-[var(--border-color)] bg-[var(--bg-tertiary)]/50">
                <div className="max-w-7xl mx-auto px-6 py-6 flex flex-col md:flex-row justify-between items-center gap-4 text-sm font-medium">
                    <p className="text-[var(--text-muted)]">
                        &copy; {currentYear} DevCommunity. All rights reserved.
                    </p>
                    <div className="flex gap-8">
                        <Link href="/privacy" className="text-[var(--text-muted)] hover:text-[var(--primary)] transition-colors">Privacy Policy</Link>
                        <Link href="/terms" className="text-[var(--text-muted)] hover:text-[var(--primary)] transition-colors">Terms of Service</Link>
                        <Link href="/cookies" className="text-[var(--text-muted)] hover:text-[var(--primary)] transition-colors">Cookie Settings</Link>
                    </div>
                </div>
            </div>
        </footer>
    );
}
