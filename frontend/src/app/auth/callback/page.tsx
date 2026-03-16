'use client';

import { useEffect, Suspense } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { useAuthStore } from '@/lib/stores/authStore';

function CallbackContent() {
    const router = useRouter();
    const searchParams = useSearchParams();
    const initialize = useAuthStore((s) => s.initialize);

    useEffect(() => {
        const success = searchParams.get('success');
        const accessToken = searchParams.get('accessToken');
        const refreshToken = searchParams.get('refreshToken');
        const error = searchParams.get('error');

        if (error) {
            router.replace(`/auth?mode=login&error=${encodeURIComponent(error)}`);
            return;
        }

        if (success === 'true' && accessToken) {
            localStorage.setItem('accessToken', accessToken);
            document.cookie = `accessToken=${accessToken}; path=/; max-age=${60 * 60 * 24 * 7}; SameSite=Lax`;
            if (refreshToken) {
                localStorage.setItem('refreshToken', refreshToken);
            }
            // Sync auth store (fetch user, set isAuthenticated) before redirect so dashboard shows immediately
            initialize().then(() => router.replace('/'));
        } else {
            router.replace('/auth?mode=login&error=' + encodeURIComponent('Authentication failed'));
        }
    }, [searchParams, router, initialize]);

    return (
        <div className="min-h-screen flex items-center justify-center bg-slate-50">
            <div className="text-center">
                <div className="w-10 h-10 border-4 border-orange-500/30 border-t-orange-500 rounded-full animate-spin mx-auto"></div>
                <p className="mt-4 text-gray-600">Signing you in...</p>
            </div>
        </div>
    );
}

export default function AuthCallbackPage() {
    return (
        <Suspense fallback={
            <div className="min-h-screen flex items-center justify-center bg-slate-50">
                <div className="w-10 h-10 border-4 border-orange-500/30 border-t-orange-500 rounded-full animate-spin mx-auto"></div>
            </div>
        }>
            <CallbackContent />
        </Suspense>
    );
}
