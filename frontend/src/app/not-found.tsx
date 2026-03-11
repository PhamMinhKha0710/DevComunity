import Link from 'next/link';

export default function NotFound() {
    return (
        <div className="flex items-center justify-center min-h-screen bg-slate-50 dark:bg-slate-950">
            <div className="max-w-md w-full text-center">
                <div className="text-8xl font-black text-slate-200 dark:text-slate-800 mb-4">404</div>
                <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-2">Page Not Found</h2>
                <p className="text-sm text-slate-500 dark:text-slate-400 mb-8">
                    The page you&apos;re looking for doesn&apos;t exist or has been moved.
                </p>
                <Link
                    href="/"
                    className="inline-flex items-center gap-2 px-6 py-2.5 bg-[var(--primary)] text-white rounded-xl font-bold text-sm hover:bg-[var(--primary)]/90 transition"
                >
                    <span className="material-symbols-outlined text-sm">home</span>
                    Back to Home
                </Link>
            </div>
        </div>
    );
}
