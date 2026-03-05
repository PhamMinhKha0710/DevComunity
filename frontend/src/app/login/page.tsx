'use client';

import { useEffect } from 'react';
import { useRouter } from 'next/navigation';

export default function LoginPage() {
    const router = useRouter();

    useEffect(() => {
        router.replace('/auth?mode=login');
    }, [router]);

    return (
        <div className="min-h-screen flex items-center justify-center bg-slate-900">
            <div className="w-8 h-8 border-4 border-[var(--primary)]/30 border-t-[var(--primary)] rounded-full animate-spin"></div>
        </div>
    );
}
