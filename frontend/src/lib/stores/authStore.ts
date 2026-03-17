import { create } from 'zustand';
import type { User, LoginRequest, RegisterRequest, AuthResponse } from '@/types';
import apiClient from '@/lib/api/client';

function setTokenCookie(token: string) {
    document.cookie = `accessToken=${token}; path=/; max-age=${60 * 60 * 24 * 7}; SameSite=Lax`;
}

function removeTokenCookie() {
    document.cookie = 'accessToken=; path=/; max-age=0';
}

interface AuthState {
    user: User | null;
    isAuthenticated: boolean;
    isLoading: boolean;
    setUser: (user: User | null) => void;
    initialize: () => Promise<void>;
    refreshCurrentUser: () => Promise<void>;
    login: (data: LoginRequest) => Promise<AuthResponse>;
    register: (data: RegisterRequest) => Promise<AuthResponse>;
    logout: () => void;
    externalLogin: (provider: 'google' | 'github' | 'facebook') => Promise<AuthResponse>;
}

export const useAuthStore = create<AuthState>((set) => ({
    user: null,
    isAuthenticated: false,
    isLoading: true,

    setUser: (user) => set({ user, isAuthenticated: !!user }),

    refreshCurrentUser: async () => {
        try {
            const response = await apiClient.get<User>('/auth/me');
            set({ user: response.data });
        } catch {
            // If refresh fails, keep current user state
        }
    },

    initialize: async () => {
        const token = typeof window !== 'undefined' ? localStorage.getItem('accessToken') : null;
        if (!token) {
            removeTokenCookie();
            set({ isLoading: false });
            return;
        }
        try {
            const response = await apiClient.get<User>('/auth/me');
            setTokenCookie(token);
            set({ user: response.data, isAuthenticated: true, isLoading: false });
        } catch {
            localStorage.removeItem('accessToken');
            localStorage.removeItem('refreshToken');
            removeTokenCookie();
            set({ user: null, isAuthenticated: false, isLoading: false });
        }
    },

    login: async (data) => {
        const response = await apiClient.post<AuthResponse>('/auth/login', data);
        if (response.data.success && response.data.accessToken) {
            localStorage.setItem('accessToken', response.data.accessToken);
            setTokenCookie(response.data.accessToken);
            if (response.data.refreshToken) {
                localStorage.setItem('refreshToken', response.data.refreshToken);
            }
            if (response.data.user) {
                set({ user: response.data.user, isAuthenticated: true });
            }
        }
        return response.data;
    },

    register: async (data) => {
        const response = await apiClient.post<AuthResponse>('/auth/register', data);
        if (response.data.success && response.data.accessToken) {
            localStorage.setItem('accessToken', response.data.accessToken);
            setTokenCookie(response.data.accessToken);
            if (response.data.refreshToken) {
                localStorage.setItem('refreshToken', response.data.refreshToken);
            }
            if (response.data.user) {
                set({ user: response.data.user, isAuthenticated: true });
            }
        }
        return response.data;
    },

    externalLogin: async (provider) => {
        // Redirect to backend OAuth endpoint
        window.location.href = `/api/auth/external-login/${provider}`;
        // This won't return since the page will redirect
        return new Promise<AuthResponse>((resolve) => {
            // Placeholder - will never execute
            resolve({ success: false, message: 'Redirecting...' });
        });
    },

    logout: () => {
        localStorage.removeItem('accessToken');
        localStorage.removeItem('refreshToken');
        removeTokenCookie();
        set({ user: null, isAuthenticated: false });
        apiClient.post('/auth/logout').catch(() => {});
    },
}));
