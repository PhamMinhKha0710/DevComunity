'use client';

import { createContext, useContext, useEffect, useState, useCallback, useRef } from 'react';

export type ThemePreference = 'light' | 'dark' | 'system';

interface ThemeContextValue {
    preference: ThemePreference;
    resolvedIsDark: boolean;
    setPreference: (pref: ThemePreference) => void;
}

const ThemeContext = createContext<ThemeContextValue | null>(null);

const STORAGE_KEY = 'theme-preference';

function resolveIsDark(pref: ThemePreference): boolean {
    if (pref === 'system') {
        if (typeof window === 'undefined') return false;
        return window.matchMedia('(prefers-color-scheme: dark)').matches;
    }
    return pref === 'dark';
}

export function ThemeProvider({ children }: { children: React.ReactNode }) {
    const [preference, setPreferenceState] = useState<ThemePreference>('system');
    const [resolvedIsDark, setResolvedIsDark] = useState(false);
    const mediaQueryRef = useRef<MediaQueryList | null>(null);

    // Apply class to <html>
    const applyTheme = useCallback((isDark: boolean) => {
        if (isDark) {
            document.documentElement.classList.add('dark');
        } else {
            document.documentElement.classList.remove('dark');
        }
    }, []);

    useEffect(() => {
        // Read preference from storage
        const stored = localStorage.getItem(STORAGE_KEY) as ThemePreference | null;
        const initial: ThemePreference = stored ?? 'system';
        setPreferenceState(initial);
        applyTheme(resolveIsDark(initial));
        setResolvedIsDark(resolveIsDark(initial));

        // Listen to system preference changes
        if (initial === 'system') {
            const mq = window.matchMedia('(prefers-color-scheme: dark)');
            mediaQueryRef.current = mq;
            const handler = (e: MediaQueryListEvent) => {
                setResolvedIsDark(e.matches);
                applyTheme(e.matches);
            };
            mq.addEventListener('change', handler);
        }
    }, [applyTheme]);

    // When preference changes, update class + storage
    const setPreference = useCallback((pref: ThemePreference) => {
        setPreferenceState(pref);
        localStorage.setItem(STORAGE_KEY, pref);
        const isDark = resolveIsDark(pref);
        setResolvedIsDark(isDark);
        applyTheme(isDark);

        // Swap system listener if needed
        if (pref === 'system') {
            if (mediaQueryRef.current) {
                mediaQueryRef.current.removeEventListener('change', () => {});
            }
            const mq = window.matchMedia('(prefers-color-scheme: dark)');
            mediaQueryRef.current = mq;
            const handler = (e: MediaQueryListEvent) => {
                setResolvedIsDark(e.matches);
                applyTheme(e.matches);
            };
            mq.addEventListener('change', handler);
        }
    }, [applyTheme]);

    return (
        <ThemeContext.Provider value={{ preference, resolvedIsDark, setPreference }}>
            {children}
        </ThemeContext.Provider>
    );
}

export function useTheme(): ThemeContextValue {
    const ctx = useContext(ThemeContext);
    if (!ctx) throw new Error('useTheme must be used inside ThemeProvider');
    return ctx;
}
