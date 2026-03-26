import apiClient from './client';
import type { AuthResponse } from '@/types';

interface ForgotPasswordOtpResponse {
    success: boolean;
    message?: string;
}

export const forgotPasswordApi = {
    requestCode: (email: string) =>
        apiClient.post<ForgotPasswordOtpResponse>('/auth/forgot-password/request-code', { email }).then(r => r.data),

    confirmCode: (data: { email: string; code: string; newPassword: string; confirmPassword: string }) =>
        apiClient.post<AuthResponse>('/auth/forgot-password/confirm', data).then(r => r.data),
};
