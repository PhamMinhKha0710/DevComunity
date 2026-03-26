'use client';

import { createContext, useContext, useEffect, ReactNode } from 'react';
import { useAuthStore } from '@/lib/stores/authStore';
import { forgotPasswordApi } from '@/lib/api/forgot-password.api';
import type { User, LoginRequest, RegisterRequest, AuthResponse } from '@/types';

interface AuthContextType {
    user: User | null;
    isAuthenticated: boolean;
    isLoading: boolean;
    login: (data: LoginRequest) => Promise<AuthResponse>;
    register: (data: RegisterRequest) => Promise<AuthResponse>;
    logout: () => void;
    externalLogin: (provider: 'google' | 'github' | 'facebook') => Promise<AuthResponse>;
    forgotPassword: (email: string) => Promise<{ success: boolean; message: string }>;
    forgotPasswordOtp: {
        requestCode: (email: string) => Promise<{ success: boolean; message?: string }>;
        confirmCode: (data: { email: string; code: string; newPassword: string; confirmPassword: string }) => Promise<AuthResponse>;
    };
    resetPassword: (data: any) => Promise<AuthResponse>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
    const store = useAuthStore();

    useEffect(() => {
        store.initialize();
    }, []);

    return (
        <AuthContext.Provider
            value={{
                user: store.user,
                isAuthenticated: store.isAuthenticated,
                isLoading: store.isLoading,
                login: store.login,
                register: store.register,
                logout: store.logout,
                externalLogin: store.externalLogin,
                forgotPassword: store.forgotPassword,
                forgotPasswordOtp: {
                    requestCode: forgotPasswordApi.requestCode,
                    confirmCode: forgotPasswordApi.confirmCode,
                },
                resetPassword: store.resetPassword,
            }}
        >
            {children}
        </AuthContext.Provider>
    );
}

export function useAuth() {
    const context = useContext(AuthContext);
    if (context === undefined) {
        throw new Error('useAuth must be used within an AuthProvider');
    }
    return context;
}
