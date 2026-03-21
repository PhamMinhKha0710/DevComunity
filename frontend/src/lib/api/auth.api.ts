import apiClient from './client';
import type { User, LoginRequest, RegisterRequest, AuthResponse } from '@/types';

export const authApi = {
    me: () =>
        apiClient.get<User>('/auth/me').then(r => r.data),

    login: (data: LoginRequest) =>
        apiClient.post<AuthResponse>('/auth/login', data).then(r => r.data),

    register: (data: RegisterRequest) =>
        apiClient.post<AuthResponse>('/auth/register', data).then(r => r.data),

    logout: () =>
        apiClient.post('/auth/logout').catch(() => {}),

    forgotPassword: (email: string) =>
        apiClient.post('/auth/forgot-password', { email }).then(r => r.data),

    resetPassword: (data: { email: string; token: string; newPassword: string }) =>
        apiClient.post('/auth/reset-password', data).then(r => r.data),

    changePassword: (data: { currentPassword: string; newPassword: string }) =>
        apiClient.post('/auth/change-password', data).then(r => r.data),

    externalLogin: (provider: 'google' | 'github' | 'facebook') => {
        // Redirect directly to backend to keep OAuth correlation cookies on same domain
        const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5122';
        window.location.href = `${apiUrl}/api/ExternalAuth/login/${provider}`;
        return Promise.resolve({ success: true, message: 'Redirecting...' });
    },

    exchangeCode: (code: string) =>
        apiClient.post<AuthResponse>('/auth/exchange', { code }).then(r => r.data),
};
