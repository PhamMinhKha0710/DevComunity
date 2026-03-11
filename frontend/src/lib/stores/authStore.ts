import { create } from 'zustand';
import type { User, LoginRequest, RegisterRequest, AuthResponse } from '@/types';
import apiClient from '@/lib/api/client';

interface AuthState {
    user: User | null;
    isAuthenticated: boolean;
    isLoading: boolean;
    setUser: (user: User | null) => void;
    initialize: () => Promise<void>;
    login: (data: LoginRequest) => Promise<AuthResponse>;
    register: (data: RegisterRequest) => Promise<AuthResponse>;
    logout: () => void;
}

export const useAuthStore = create<AuthState>((set) => ({
    user: null,
    isAuthenticated: false,
    isLoading: true,

    setUser: (user) => set({ user, isAuthenticated: !!user }),

    initialize: async () => {
        const token = typeof window !== 'undefined' ? localStorage.getItem('accessToken') : null;
        if (!token) {
            set({ isLoading: false });
            return;
        }
        try {
            const response = await apiClient.get<User>('/auth/me');
            set({ user: response.data, isAuthenticated: true, isLoading: false });
        } catch {
            localStorage.removeItem('accessToken');
            localStorage.removeItem('refreshToken');
            set({ user: null, isAuthenticated: false, isLoading: false });
        }
    },

    login: async (data) => {
        const response = await apiClient.post<AuthResponse>('/auth/login', data);
        if (response.data.success && response.data.accessToken) {
            localStorage.setItem('accessToken', response.data.accessToken);
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
            if (response.data.refreshToken) {
                localStorage.setItem('refreshToken', response.data.refreshToken);
            }
            if (response.data.user) {
                set({ user: response.data.user, isAuthenticated: true });
            }
        }
        return response.data;
    },

    logout: () => {
        localStorage.removeItem('accessToken');
        localStorage.removeItem('refreshToken');
        set({ user: null, isAuthenticated: false });
        apiClient.post('/auth/logout').catch(() => {});
    },
}));
