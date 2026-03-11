'use client';

import { useRelativeTime } from '@/lib/hooks/useRelativeTime';

interface RelativeTimeProps {
    value: string | Date | null | undefined;
    prefix?: string;
    fallback?: string;
    className?: string;
}

export default function RelativeTime({ value, prefix, fallback, className }: RelativeTimeProps) {
    const text = useRelativeTime(value);

    if (!value && !fallback) {
        return null;
    }

    const finalText = text || fallback || '';
    const content = prefix ? `${prefix}${finalText}` : finalText;

    return <span className={className}>{content}</span>;
}

