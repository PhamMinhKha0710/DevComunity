'use client';

import { createContext, useContext, useState, useEffect, useCallback } from 'react';
import { messages, MessagesKey } from '@/lib/i18n/messages';

export type Locale = 'en' | 'vi';

interface LocaleContextValue {
    locale: Locale;
    setLocale: (locale: Locale) => void;
    t: (key: MessagesKey) => string;
}

const LocaleContext = createContext<LocaleContextValue | null>(null);

const STORAGE_KEY = 'app-locale';

export function LocaleProvider({ children }: { children: React.ReactNode }) {
    const [locale, setLocaleState] = useState<Locale>('en');

    useEffect(() => {
        const stored = localStorage.getItem(STORAGE_KEY) as Locale | null;
        const initial: Locale = stored ?? 'en';
        setLocaleState(initial);
        document.documentElement.lang = initial;
    }, []);

    const setLocale = useCallback((loc: Locale) => {
        setLocaleState(loc);
        localStorage.setItem(STORAGE_KEY, loc);
        document.documentElement.lang = loc;
    }, []);

    const t = useCallback((key: MessagesKey): string => {
        return messages[locale][key] ?? key;
    }, [locale]);

    return (
        <LocaleContext.Provider value={{ locale, setLocale, t }}>
            {children}
        </LocaleContext.Provider>
    );
}

export function useLocale(): LocaleContextValue {
    const ctx = useContext(LocaleContext);
    if (!ctx) throw new Error('useLocale must be used inside LocaleProvider');
    return ctx;
}
