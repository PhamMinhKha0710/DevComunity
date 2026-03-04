'use client';

import Link from 'next/link';
import { useAuth } from '@/lib/contexts/AuthContext';

export default function RightSidebar() {
    const { isAuthenticated } = useAuth();

    return (
        <aside className="hidden xl:flex flex-col w-80 shrink-0 space-y-8">
            {/* Trending Topics */}
            <div className="bg-white dark:bg-slate-900 p-6 rounded-lg shadow-sm border border-slate-100 dark:border-slate-800">
                <h3 className="text-sm font-bold text-slate-900 dark:text-white uppercase tracking-wider mb-4">Trending Topics</h3>
                <div className="space-y-4">
                    {[
                        { tag: '#GenerativeAI', count: '2.5k discussions today' },
                        { tag: '#RustLang', count: '1.2k discussions today' },
                        { tag: '#CyberSecurity2024', count: '850 discussions today' },
                        { tag: '#TailwindCSSv4', count: '640 discussions today' },
                    ].map((topic) => (
                        <Link
                            key={topic.tag}
                            href={`/questions?tag=${topic.tag.replace('#', '')}`}
                            className="flex items-center justify-between group cursor-pointer"
                        >
                            <div>
                                <p className="text-sm font-semibold text-slate-800 dark:text-slate-200 group-hover:text-[var(--primary)] transition-colors">{topic.tag}</p>
                                <p className="text-xs text-slate-500">{topic.count}</p>
                            </div>
                            <span className="material-symbols-outlined text-slate-300 text-sm">trending_up</span>
                        </Link>
                    ))}
                </div>
                <button className="w-full mt-6 py-2 text-xs font-bold text-slate-500 dark:text-slate-400 border border-slate-200 dark:border-slate-800 rounded-lg hover:bg-slate-50 dark:hover:bg-slate-800 transition-colors">Show More</button>
            </div>

            {/* Upcoming Events */}
            <div className="bg-white dark:bg-slate-900 p-6 rounded-lg shadow-sm border border-slate-100 dark:border-slate-800">
                <h3 className="text-sm font-bold text-slate-900 dark:text-white uppercase tracking-wider mb-4">Upcoming Events</h3>
                <div className="space-y-6">
                    {[
                        { month: 'Mar', day: '15', title: 'Techsy Global Meetup 2026', location: 'Ho Chi Minh City, VN', icon: 'location_on', color: 'primary' },
                        { month: 'Mar', day: '22', title: 'AI Workshop: Practical NLP', location: 'Online Webinar', icon: 'videocam', color: 'amber' },
                        { month: 'Apr', day: '05', title: 'Open Source Contributors Night', location: 'Da Nang, VN', icon: 'location_on', color: 'emerald' },
                    ].map((event, i) => (
                        <div key={i} className="flex gap-4">
                            <div className={`flex flex-col items-center justify-center w-12 h-12 rounded-lg shrink-0 ${event.color === 'primary' ? 'bg-[var(--primary)]/10' : event.color === 'amber' ? 'bg-amber-50 dark:bg-amber-900/20' : 'bg-emerald-50 dark:bg-emerald-900/20'
                                }`}>
                                <span className={`font-bold text-xs uppercase ${event.color === 'primary' ? 'text-[var(--primary)]' : event.color === 'amber' ? 'text-amber-600' : 'text-emerald-600'
                                    }`}>{event.month}</span>
                                <span className={`font-black text-lg leading-none ${event.color === 'primary' ? 'text-[var(--primary)]' : event.color === 'amber' ? 'text-amber-600' : 'text-emerald-600'
                                    }`}>{event.day}</span>
                            </div>
                            <div className="flex-1">
                                <p className="text-sm font-bold text-slate-800 dark:text-slate-200 leading-snug">{event.title}</p>
                                <p className="text-xs text-slate-500 mt-1 flex items-center gap-1">
                                    <span className="material-symbols-outlined text-xs">{event.icon}</span> {event.location}
                                </p>
                            </div>
                        </div>
                    ))}
                </div>
                <button className="w-full mt-6 bg-[var(--primary)]/10 text-[var(--primary)] py-2 rounded-lg text-xs font-bold hover:bg-[var(--primary)]/20 transition-all">Browse All Events</button>
            </div>

            {/* Community CTA */}
            <div className="relative overflow-hidden rounded-lg bg-[var(--primary)] p-6 text-white">
                <div className="relative z-10">
                    <h4 className="font-bold text-lg leading-tight">Join the Techsy Pro community</h4>
                    <p className="text-white/80 text-xs mt-2">Get exclusive access to mentors, premium courses, and job boards.</p>
                    <button className="mt-4 bg-white text-[var(--primary)] px-4 py-2 rounded-lg text-xs font-bold shadow-lg hover:scale-105 transition-transform">Upgrade Now</button>
                </div>
                <div className="absolute -right-6 -bottom-6 w-32 h-32 bg-white/10 rounded-full blur-2xl"></div>
                <div className="absolute -left-10 -top-10 w-24 h-24 bg-white/5 rounded-full blur-xl"></div>
            </div>
        </aside>
    );
}
