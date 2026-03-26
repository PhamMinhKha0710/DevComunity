'use client';

import { AuthProvider } from '@/lib/contexts/AuthContext';
import { NotificationProvider } from '@/lib/contexts/NotificationContext';
import { ChatProvider } from '@/lib/contexts/ChatContext';
import { QueryProvider } from '@/lib/api/queryClient';
import { Toaster } from 'react-hot-toast';
import { useEffect } from 'react';
import { ThemeProvider } from '@/lib/contexts/ThemeContext';
import { LocaleProvider } from '@/lib/contexts/LocaleContext';


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
                        <LocaleProvider>
                            <ThemeProvider>
                                <main className="flex-grow-1">
                                    {children}
                                </main>
                                <Toaster position="bottom-right" />
                            </ThemeProvider>
                        </LocaleProvider>
                    </NotificationProvider>
                </ChatProvider>
            </AuthProvider>
        </QueryProvider>
    );
}
