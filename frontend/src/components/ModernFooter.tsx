'use client';

import Link from 'next/link';

export default function ModernFooter() {
    const currentYear = new Date().getFullYear();

    return (
        <footer className="bg-white dark:bg-[#101922] border-t border-slate-200 dark:border-slate-800 mt-20">
            <div className="max-w-[1440px] mx-auto px-6 md:px-10 py-12">
                <div className="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-5 gap-8">
                    {/* Brand */}
                    <div className="col-span-2">
                        <div className="flex items-center gap-3 text-[var(--primary)] mb-6">
                            <span className="material-symbols-outlined text-3xl font-bold">code_blocks</span>
                            <h2 className="text-[var(--text-primary)] text-xl font-bold tracking-tight">SocialTechsy</h2>
                        </div>
                        <p className="text-slate-500 dark:text-slate-400 text-sm max-w-xs mb-6 leading-relaxed">
                            The world&apos;s largest community for developers to learn, share knowledge, and build their careers.
                        </p>
                        <div className="flex gap-4">
                            <span className="material-symbols-outlined text-slate-400 hover:text-[var(--primary)] cursor-pointer transition-colors">social_leaderboard</span>
                            <span className="material-symbols-outlined text-slate-400 hover:text-[var(--primary)] cursor-pointer transition-colors">alternate_email</span>
                            <span className="material-symbols-outlined text-slate-400 hover:text-[var(--primary)] cursor-pointer transition-colors">hub</span>
                        </div>
                    </div>

                    {/* Platform */}
                    <div>
                        <h4 className="font-bold text-[var(--text-primary)] mb-4">Platform</h4>
                        <ul className="space-y-2 text-sm text-slate-500 dark:text-slate-400">
                            <li><Link href="/questions" className="hover:text-[var(--primary)] transition-colors">Questions</Link></li>
                            <li><Link href="/tags" className="hover:text-[var(--primary)] transition-colors">Tags</Link></li>
                            <li><Link href="/users" className="hover:text-[var(--primary)] transition-colors">Users</Link></li>
                            <li><Link href="/groups" className="hover:text-[var(--primary)] transition-colors">Companies</Link></li>
                        </ul>
                    </div>

                    {/* Company */}
                    <div>
                        <h4 className="font-bold text-[var(--text-primary)] mb-4">Company</h4>
                        <ul className="space-y-2 text-sm text-slate-500 dark:text-slate-400">
                            <li><a href="#" className="hover:text-[var(--primary)] transition-colors">About Us</a></li>
                            <li><a href="#" className="hover:text-[var(--primary)] transition-colors">Press</a></li>
                            <li><a href="#" className="hover:text-[var(--primary)] transition-colors">Work Here</a></li>
                            <li><a href="#" className="hover:text-[var(--primary)] transition-colors">Legal</a></li>
                        </ul>
                    </div>

                    {/* Support */}
                    <div>
                        <h4 className="font-bold text-[var(--text-primary)] mb-4">Support</h4>
                        <ul className="space-y-2 text-sm text-slate-500 dark:text-slate-400">
                            <li><a href="#" className="hover:text-[var(--primary)] transition-colors">Help Center</a></li>
                            <li><a href="#" className="hover:text-[var(--primary)] transition-colors">Contact Us</a></li>
                            <li><a href="#" className="hover:text-[var(--primary)] transition-colors">API Docs</a></li>
                        </ul>
                    </div>
                </div>

                {/* Bottom bar */}
                <div className="mt-12 pt-8 border-t border-slate-200 dark:border-slate-800 flex flex-col md:flex-row justify-between items-center gap-4">
                    <p className="text-xs text-slate-400">&copy; {currentYear} SocialTechsy Inc. All rights reserved.</p>
                    <div className="flex gap-6 text-xs text-slate-400">
                        <a href="#" className="hover:text-[var(--primary)] transition-colors">Privacy Policy</a>
                        <a href="#" className="hover:text-[var(--primary)] transition-colors">Terms of Service</a>
                        <a href="#" className="hover:text-[var(--primary)] transition-colors">Cookie Settings</a>
                    </div>
                </div>
            </div>
        </footer>
    );
}
