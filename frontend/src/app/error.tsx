'use client';

export default function RootError({
    error,
    reset,
}: {
    error: Error & { digest?: string };
    reset: () => void;
}) {
    return (
        <div className="flex items-center justify-center min-h-screen bg-slate-50 dark:bg-slate-950">
            <div className="max-w-md w-full bg-white dark:bg-slate-900 rounded-2xl p-8 shadow-lg border border-slate-200 dark:border-slate-800 text-center">
                <div className="w-16 h-16 bg-red-100 dark:bg-red-900/30 rounded-full flex items-center justify-center mx-auto mb-4">
                    <span className="material-symbols-outlined text-3xl text-red-500">error</span>
                </div>
                <h2 className="text-xl font-bold text-slate-900 dark:text-white mb-2">Something went wrong</h2>
                <p className="text-sm text-slate-500 dark:text-slate-400 mb-6">
                    {error.message || 'An unexpected error occurred. Please try again.'}
                </p>
                <button
                    onClick={reset}
                    className="px-6 py-2.5 bg-[var(--primary)] text-white rounded-xl font-bold text-sm hover:bg-[var(--primary)]/90 transition"
                >
                    Try Again
                </button>
            </div>
        </div>
    );
}
