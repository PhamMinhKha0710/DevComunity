export default function RootLoading() {
    return (
        <div className="min-h-screen bg-slate-50 dark:bg-slate-950">
            {/* Navbar skeleton */}
            <div className="h-16 border-b border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 px-6 flex items-center justify-between animate-pulse">
                <div className="flex items-center gap-4">
                    <div className="h-8 w-8 bg-slate-200 dark:bg-slate-800 rounded-lg" />
                    <div className="h-4 w-32 bg-slate-200 dark:bg-slate-800 rounded" />
                </div>
                <div className="flex items-center gap-3">
                    <div className="h-9 w-48 bg-slate-100 dark:bg-slate-800 rounded-xl hidden sm:block" />
                    <div className="h-9 w-9 bg-slate-200 dark:bg-slate-800 rounded-full" />
                    <div className="h-9 w-9 bg-slate-200 dark:bg-slate-800 rounded-full" />
                </div>
            </div>

            <div className="flex">
                {/* Sidebar skeleton */}
                <div className="hidden lg:flex w-64 border-r border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 flex-col p-4 gap-3 animate-pulse">
                    {Array.from({ length: 7 }).map((_, i) => (
                        <div key={i} className="flex items-center gap-3 p-2.5 rounded-xl">
                            <div className="h-5 w-5 bg-slate-200 dark:bg-slate-800 rounded" />
                            <div className="h-4 bg-slate-200 dark:bg-slate-800 rounded" style={{ width: `${60 + Math.random() * 40}%` }} />
                        </div>
                    ))}
                </div>

                {/* Content skeleton */}
                <div className="flex-1 p-6 lg:p-8 max-w-5xl animate-pulse">
                    <div className="h-7 w-48 bg-slate-200 dark:bg-slate-800 rounded-lg mb-2" />
                    <div className="h-4 w-72 bg-slate-100 dark:bg-slate-800/50 rounded mb-8" />

                    <div className="space-y-4">
                        {Array.from({ length: 4 }).map((_, i) => (
                            <div key={i} className="bg-white dark:bg-slate-900 rounded-xl p-5 border border-slate-200 dark:border-slate-800">
                                <div className="h-5 w-3/4 bg-slate-200 dark:bg-slate-800 rounded mb-3" />
                                <div className="h-4 w-full bg-slate-100 dark:bg-slate-800/50 rounded mb-2" />
                                <div className="h-4 w-2/3 bg-slate-100 dark:bg-slate-800/50 rounded mb-4" />
                                <div className="flex gap-3">
                                    <div className="h-6 w-16 bg-slate-100 dark:bg-slate-800/50 rounded-full" />
                                    <div className="h-6 w-16 bg-slate-100 dark:bg-slate-800/50 rounded-full" />
                                    <div className="h-6 w-20 bg-slate-100 dark:bg-slate-800/50 rounded-full" />
                                </div>
                            </div>
                        ))}
                    </div>
                </div>
            </div>
        </div>
    );
}
