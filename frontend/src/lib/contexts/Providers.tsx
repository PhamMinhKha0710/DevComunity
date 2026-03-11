'use client';

import { AuthProvider } from '@/lib/contexts/AuthContext';
import { NotificationProvider } from '@/lib/contexts/NotificationContext';
import { ChatProvider } from '@/lib/contexts/ChatContext';
import { QueryProvider } from '@/lib/api/queryClient';
import { Toaster } from 'react-hot-toast';
import { useEffect } from 'react';


export function Providers({ children }: { children: React.ReactNode }) {
    useEffect(() => {
        if ('Notification' in window && Notification.permission === 'default') {
            Notification.requestPermission();
        }
    }, []);

    return (
        <QueryProvider>
            <AuthProvider>
                <ChatProvider>
                    <NotificationProvider>
                        <main className="flex-grow-1">
                            {children}
                        </main>
                        <Toaster position="bottom-right" />
                    </NotificationProvider>
                </ChatProvider>
            </AuthProvider>
        </QueryProvider>
    );
}
