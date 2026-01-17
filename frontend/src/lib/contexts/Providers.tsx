'use client';

import { AuthProvider } from '@/lib/contexts/AuthContext';
import { NotificationProvider } from '@/lib/contexts/NotificationContext';
import Header from '@/components/layout/Header';
import Footer from '@/components/layout/Footer';
import { useEffect } from 'react';

export function Providers({ children }: { children: React.ReactNode }) {
    useEffect(() => {
        // Initialize AOS when component mounts
        const initAOS = () => {
            // @ts-expect-error AOS is loaded externally
            if (typeof AOS !== 'undefined') {
                // @ts-expect-error AOS is loaded externally
                AOS.init({ duration: 600 });
            }
        };

        // Try immediately
        initAOS();

        // Also try after a short delay (in case script loads after component)
        const timeout = setTimeout(initAOS, 500);

        // Request notification permission
        if ('Notification' in window && Notification.permission === 'default') {
            Notification.requestPermission();
        }

        return () => clearTimeout(timeout);
    }, []);

    return (
        <AuthProvider>
            <NotificationProvider>
                <Header />
                <main className="flex-grow-1">
                    {children}
                </main>
                <Footer />
            </NotificationProvider>
        </AuthProvider>
    );
}
