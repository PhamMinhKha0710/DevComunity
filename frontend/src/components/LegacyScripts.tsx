'use client';

import Script from 'next/script';
import { useEffect } from 'react';
import { useAuth } from '@/lib/contexts/AuthContext';

/**
 * Component to load legacy JavaScript files from public/js/
 * These scripts provide additional UI effects and handlers
 */
export function LegacyScripts() {
    const { isAuthenticated } = useAuth();

    useEffect(() => {
        // Initialize AOS when component mounts
        if (typeof window !== 'undefined' && (window as any).AOS) {
            (window as any).AOS.init({
                duration: 600,
                once: true,
            });
        }
    }, []);

    return (
        <>
            {/* Theme and UI Effects */}
            <Script src="/js/theme-switcher.js" strategy="lazyOnload" />
            <Script src="/js/theme-fix.js" strategy="lazyOnload" />

            {/* Vote Effects */}
            <Script src="/js/vote-handler.js" strategy="lazyOnload" />
            <Script src="/js/vote-notification-fixed.js" strategy="lazyOnload" />

            {/* Comment Features */}
            <Script src="/js/comment-form.js" strategy="lazyOnload" />
            <Script src="/js/comment-handlers.js" strategy="lazyOnload" />

            {/* View Counter and Activity */}
            <Script src="/js/view-counter-fixed.js" strategy="lazyOnload" />
            <Script src="/js/activity-handler.js" strategy="lazyOnload" />

            {/* SignalR Scripts - Only load when authenticated */}
            {isAuthenticated && (
                <>
                    <Script src="/js/signalr-loader.js" strategy="lazyOnload" />
                    <Script src="/js/signalr-connection-check.js" strategy="lazyOnload" />
                    <Script src="/js/question-realtime-client.js" strategy="lazyOnload" />
                    <Script src="/js/chat-client.js" strategy="lazyOnload" />
                    <Script src="/js/notification-service-fixed.js" strategy="lazyOnload" />
                    <Script src="/js/presence-handler.js" strategy="lazyOnload" />
                </>
            )}

            {/* Search and Tags */}
            <Script src="/js/search-autocomplete.js" strategy="lazyOnload" />
            <Script src="/js/tag-search.js" strategy="lazyOnload" />
            <Script src="/js/tag-suggestions.js" strategy="lazyOnload" />

            {/* Markdown and Attachments */}
            <Script src="/js/markdown-editor-complete.js" strategy="lazyOnload" />
            <Script src="/js/image-preview-handler.js" strategy="lazyOnload" />
            <Script src="/js/question-attachments.js" strategy="lazyOnload" />
            <Script src="/js/answer-attachments.js" strategy="lazyOnload" />
            <Script src="/js/attachment-viewer.js" strategy="lazyOnload" />

            {/* Gamification */}
            <Script src="/js/badge-progress-client.js" strategy="lazyOnload" />
            <Script src="/js/reputation-sync.js" strategy="lazyOnload" />

            {/* Saved Items */}
            <Script src="/js/saved-items.js" strategy="lazyOnload" />

            {/* Main Site Script (load last) */}
            <Script src="/js/site.js" strategy="lazyOnload" />
        </>
    );
}

export default LegacyScripts;
