'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { useAuth } from '@/lib/contexts/AuthContext';
import { useAuthStore } from '@/lib/stores/authStore';
import { useTheme } from '@/lib/contexts/ThemeContext';
import { useLocale } from '@/lib/contexts/LocaleContext';
import apiClient from '@/lib/api/client';
import AppLayout from '@/components/AppLayout';
import { authorInitial } from '@/lib/utils';

export default function SettingsPage() {
    const { user, isLoading: authLoading, logout } = useAuth();
    const refreshCurrentUser = useAuthStore((s) => s.refreshCurrentUser);
    const { preference, setPreference } = useTheme();
    const { locale, setLocale, t } = useLocale();
    const router = useRouter();
    const [activeTab, setActiveTab] = useState('profile');
    const [isSaving, setIsSaving] = useState(false);
    const [message, setMessage] = useState({ type: '', text: '' });
    const [avatarUrl, setAvatarUrl] = useState<string>('');
    const [isUploadingAvatar, setIsUploadingAvatar] = useState(false);

    const [profile, setProfile] = useState({
        displayName: '',
        email: '',
        bio: '',
        location: '',
        website: '',
    });

    const [passwordForm, setPasswordForm] = useState({
        currentPassword: '',
        newPassword: '',
        confirmPassword: '',
        verificationCode: '',
    });
    const [codeRequested, setCodeRequested] = useState(false);
    const [isRequestingCode, setIsRequestingCode] = useState(false);

    useEffect(() => {
        if (!authLoading && !user) {
            router.push('/auth?mode=login');
        } else if (user) {
            // Initialize from auth context first
            setProfile({
                displayName: user.displayName || '',
                email: user.email || '',
                bio: user.bio || '',
                location: user.location || '',
                website: user.website || '',
            });
            setAvatarUrl(user.profilePicture || '');
            // Then fetch full profile from API to get bio/location/website
            const fetchFullProfile = async () => {
                try {
                    const res = await apiClient.get(`/users/${user.userId}`);
                    const data = res.data;
                    setProfile(prev => ({
                        ...prev,
                        bio: data.bio || prev.bio || '',
                        location: data.location || prev.location || '',
                        website: data.website || prev.website || '',
                    }));
                } catch (err) {
                    console.warn('Failed to fetch full profile:', err);
                }
            };
            fetchFullProfile();
        }
    }, [user, authLoading, router]);

    const handleAvatarUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
        const file = e.target.files?.[0];
        if (!file) return;

        const allowedTypes = ['image/jpeg', 'image/png', 'image/gif', 'image/webp'];
        if (!allowedTypes.includes(file.type)) {
            setMessage({ type: 'error', text: 'Only JPG, PNG, GIF, and WebP images are allowed.' });
            return;
        }
        if (file.size > 10 * 1024 * 1024) {
            setMessage({ type: 'error', text: 'Image must be smaller than 10 MB.' });
            return;
        }

        setIsUploadingAvatar(true);
        setMessage({ type: '', text: '' });
        try {
            const formData = new FormData();
            formData.append('file', file);
            const uploadRes = await apiClient.post('/media/upload', formData, {
                headers: { 'Content-Type': 'multipart/form-data' },
            });
            const uploadedUrl: string = uploadRes.data.url;
            setAvatarUrl(uploadedUrl);

            await apiClient.put('/users/profile', { profilePicture: uploadedUrl });
            await refreshCurrentUser();
            setMessage({ type: 'success', text: 'Avatar updated successfully.' });
        } catch (err: any) {
            setMessage({ type: 'error', text: err.response?.data?.message || 'Failed to upload avatar.' });
        } finally {
            setIsUploadingAvatar(false);
        }
    };

    const handleProfileSave = async (e: React.FormEvent) => {
        e.preventDefault();
        setIsSaving(true);
        setMessage({ type: '', text: '' });

        try {
            const payload: Record<string, string> = {};
            if (profile.displayName?.trim()) payload.displayName = profile.displayName.trim();
            if (profile.bio?.trim()) payload.bio = profile.bio.trim();
            if (profile.location?.trim()) payload.location = profile.location.trim();
            if (profile.website?.trim()) payload.website = profile.website.trim();

            if (Object.keys(payload).length === 0) {
                setMessage({ type: 'success', text: 'No changes to save' });
                setIsSaving(false);
                return;
            }

            await apiClient.put('/users/profile', payload);
            await refreshCurrentUser();
            setMessage({ type: 'success', text: 'Profile updated successfully' });
        } catch (err: any) {
            const data = err.response?.data;
            const msg = data?.message
                || (data?.errors && typeof data.errors === 'object'
                    ? Object.values(data.errors).flat().join(' ')
                    : null)
                || data?.title
                || 'Failed to update profile';
            setMessage({ type: 'error', text: msg });
        } finally {
            setIsSaving(false);
        }
    };

    const handleRequestCode = async () => {
        setIsRequestingCode(true);
        setMessage({ type: '', text: '' });
        try {
            const res = await apiClient.post('/auth/request-password-change-code');
            if (res.data.success) {
                setCodeRequested(true);
                setMessage({ type: 'success', text: 'Verification code sent to your email.' });
            } else {
                setMessage({ type: 'error', text: res.data.message || 'Failed to send code.' });
            }
        } catch (err: any) {
            setMessage({ type: 'error', text: err.response?.data?.message || 'Failed to send code.' });
        } finally {
            setIsRequestingCode(false);
        }
    };

    const handlePasswordChange = async (e: React.FormEvent) => {
        e.preventDefault();

        if (!codeRequested) {
            setMessage({ type: 'error', text: 'Please request a verification code first.' });
            return;
        }

        if (passwordForm.newPassword !== passwordForm.confirmPassword) {
            setMessage({ type: 'error', text: 'Passwords do not match' });
            return;
        }

        setIsSaving(true);
        setMessage({ type: '', text: '' });

        try {
            const res = await apiClient.post('/auth/change-password', {
                currentPassword: passwordForm.currentPassword,
                newPassword: passwordForm.newPassword,
                verificationCode: passwordForm.verificationCode,
            });
            if (res.data.success) {
                setMessage({ type: 'success', text: 'Password changed successfully.' });
                setPasswordForm({ currentPassword: '', newPassword: '', confirmPassword: '', verificationCode: '' });
                setCodeRequested(false);
            } else {
                setMessage({ type: 'error', text: res.data.message || 'Failed to change password.' });
            }
        } catch (err: any) {
            setMessage({ type: 'error', text: err.response?.data?.message || 'Failed to change password.' });
        } finally {
            setIsSaving(false);
        }
    };

    if (authLoading) {
        return (
            <AppLayout showRightSidebar={false}>
                <div className="flex items-center justify-center py-20">
                    <div className="w-10 h-10 border-4 border-[var(--primary)]/30 border-t-[var(--primary)] rounded-full animate-spin"></div>
                </div>
            </AppLayout>
        );
    }

    if (!user) return null;

    const tabs = [
        { key: 'profile', label: t('settings.profile'), icon: 'person' },
        { key: 'account', label: t('settings.account'), icon: 'security' },
        { key: 'notifications', label: t('settings.notifications'), icon: 'notifications' },
        { key: 'preferences', label: t('settings.preferences'), icon: 'tune' },
    ];

    return (
        <AppLayout showRightSidebar={false}>
            {/* Header */}
            <div className="mb-6">
                <h1 className="text-2xl font-bold text-[var(--text-primary)] flex items-center gap-2">
                    <span className="material-symbols-outlined text-[var(--primary)]">settings</span>
                    Settings
                </h1>
                <p className="text-[var(--text-muted)]">Manage your account and preferences</p>
            </div>

            <div className="grid grid-cols-1 lg:grid-cols-4 gap-6">
                {/* Sidebar */}
                <div className="lg:col-span-1">
                    <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-2xl overflow-hidden">
                        {/* User Preview */}
                        <div className="p-5 text-center border-b border-[var(--border-color)]">
                            {user.profilePicture ? (
                                <img
                                    src={user.profilePicture}
                                    alt={user.displayName || user.username}
                                    className="w-16 h-16 rounded-full object-cover border-2 border-[var(--border-color)] mx-auto mb-3"
                                />
                            ) : (
                                <div className="w-16 h-16 rounded-full bg-gradient-to-br from-blue-500 via-indigo-500 to-purple-500 flex items-center justify-center text-xl font-bold text-white mx-auto mb-3">
                                    {authorInitial(user.displayName || user.username)}
                                </div>
                            )}
                            <h4 className="font-semibold text-[var(--text-primary)]">{user.displayName || user.username}</h4>
                            <p className="text-sm text-[var(--text-muted)]">@{user.username}</p>
                        </div>

                        {/* Tab List */}
                        <div className="p-2">
                            {tabs.map((tab) => (
                                <button
                                    key={tab.key}
                                    onClick={() => setActiveTab(tab.key)}
                                    className={`w-full flex items-center gap-3 px-4 py-3 rounded-xl text-left transition ${activeTab === tab.key
                                            ? 'bg-[var(--primary)] text-white'
                                            : 'text-[var(--text-secondary)] hover:bg-[var(--bg-tertiary)]'
                                        }`}
                                >
                                    <span className="material-symbols-outlined text-[20px]">{tab.icon}</span>
                                    {tab.label}
                                </button>
                            ))}
                        </div>
                    </div>
                </div>

                {/* Content */}
                <div className="lg:col-span-3">
                    {/* Message */}
                    {message.text && (
                        <div className={`mb-4 p-4 rounded-xl flex items-center gap-2 ${message.type === 'success'
                                ? 'bg-green-500/10 border border-green-500/30 text-green-500'
                                : 'bg-red-500/10 border border-red-500/30 text-red-500'
                            }`}>
                            <span className="material-symbols-outlined">{message.type === 'success' ? 'check_circle' : 'error'}</span>
                            {message.text}
                            <button onClick={() => setMessage({ type: '', text: '' })} className="ml-auto">
                                <span className="material-symbols-outlined">close</span>
                            </button>
                        </div>
                    )}

                    {/* Profile Tab */}
                    {activeTab === 'profile' && (
                        <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-2xl">
                            <div className="p-5 border-b border-[var(--border-color)]">
                                <h3 className="text-lg font-semibold text-[var(--text-primary)]">Profile Settings</h3>
                            </div>
                            <form onSubmit={handleProfileSave} className="p-5 space-y-5">
                                {/* Avatar Upload */}
                                <div className="flex items-center gap-5">
                                    <div className="relative flex-shrink-0">
                                        {avatarUrl ? (
                                            <img
                                                src={avatarUrl}
                                                alt="Avatar"
                                                className="w-20 h-20 rounded-full object-cover border-2 border-[var(--border-color)]"
                                            />
                                        ) : (
                                            <div className="w-20 h-20 rounded-full bg-gradient-to-br from-blue-500 via-indigo-500 to-purple-500 flex items-center justify-center text-2xl font-bold text-white">
                                                {authorInitial(user.displayName || user.username)}
                                            </div>
                                        )}
                                        {isUploadingAvatar && (
                                            <div className="absolute inset-0 rounded-full bg-black/50 flex items-center justify-center">
                                                <div className="w-6 h-6 border-2 border-white/30 border-t-white rounded-full animate-spin"></div>
                                            </div>
                                        )}
                                    </div>
                                    <div>
                                        <label className="cursor-pointer inline-flex items-center gap-2 px-4 py-2 bg-[var(--primary)] text-white text-sm font-medium rounded-xl hover:bg-[var(--primary-dark)] transition disabled:opacity-50"
                                            style={{ '--primary': 'var(--primary)', '--primary-dark': 'var(--primary-dark)' } as React.CSSProperties}
                                        >
                                            <input
                                                type="file"
                                                accept="image/jpeg,image/png,image/gif,image/webp"
                                                onChange={handleAvatarUpload}
                                                disabled={isUploadingAvatar}
                                                className="sr-only"
                                            />
                                            <span className="material-symbols-outlined text-lg">upload</span>
                                            {isUploadingAvatar ? 'Uploading...' : 'Change Photo'}
                                        </label>
                                        <p className="text-xs text-[var(--text-muted)] mt-1">JPG, PNG, GIF, WebP — max 10 MB</p>
                                    </div>
                                </div>

                                <div>
                                    <label className="block text-sm font-medium text-[var(--text-secondary)] mb-2">Display Name</label>
                                    <input
                                        type="text"
                                        value={profile.displayName}
                                        onChange={(e) => setProfile({ ...profile, displayName: e.target.value })}
                                        className="w-full px-4 py-3 bg-[var(--bg-primary)] border border-[var(--border-color)] rounded-xl text-[var(--text-primary)] focus:border-[var(--primary)] focus:ring-2 focus:ring-[var(--primary)]/20 transition"
                                    />
                                </div>
                                <div>
                                    <label className="block text-sm font-medium text-[var(--text-secondary)] mb-2">Email</label>
                                    <input
                                        type="email"
                                        value={profile.email}
                                        onChange={(e) => setProfile({ ...profile, email: e.target.value })}
                                        className="w-full px-4 py-3 bg-[var(--bg-primary)] border border-[var(--border-color)] rounded-xl text-[var(--text-primary)] focus:border-[var(--primary)] focus:ring-2 focus:ring-[var(--primary)]/20 transition"
                                    />
                                </div>
                                <div>
                                    <label className="block text-sm font-medium text-[var(--text-secondary)] mb-2">Bio</label>
                                    <textarea
                                        rows={3}
                                        value={profile.bio}
                                        onChange={(e) => setProfile({ ...profile, bio: e.target.value })}
                                        placeholder="Tell us about yourself..."
                                        className="w-full px-4 py-3 bg-[var(--bg-primary)] border border-[var(--border-color)] rounded-xl text-[var(--text-primary)] placeholder-[var(--text-muted)] focus:border-[var(--primary)] focus:ring-2 focus:ring-[var(--primary)]/20 transition resize-none"
                                    />
                                </div>
                                <div className="grid sm:grid-cols-2 gap-4">
                                    <div>
                                        <label className="block text-sm font-medium text-[var(--text-secondary)] mb-2">Location</label>
                                        <input
                                            type="text"
                                            value={profile.location}
                                            onChange={(e) => setProfile({ ...profile, location: e.target.value })}
                                            placeholder="City, Country"
                                            className="w-full px-4 py-3 bg-[var(--bg-primary)] border border-[var(--border-color)] rounded-xl text-[var(--text-primary)] placeholder-[var(--text-muted)] focus:border-[var(--primary)] focus:ring-2 focus:ring-[var(--primary)]/20 transition"
                                        />
                                    </div>
                                    <div>
                                        <label className="block text-sm font-medium text-[var(--text-secondary)] mb-2">Website</label>
                                        <input
                                            type="url"
                                            value={profile.website}
                                            onChange={(e) => setProfile({ ...profile, website: e.target.value })}
                                            placeholder="https://..."
                                            className="w-full px-4 py-3 bg-[var(--bg-primary)] border border-[var(--border-color)] rounded-xl text-[var(--text-primary)] placeholder-[var(--text-muted)] focus:border-[var(--primary)] focus:ring-2 focus:ring-[var(--primary)]/20 transition"
                                        />
                                    </div>
                                </div>
                                <button
                                    type="submit"
                                    disabled={isSaving}
                                    className="px-6 py-3 bg-[var(--primary)] text-white rounded-xl font-medium hover:bg-[var(--primary-dark)] transition disabled:opacity-50"
                                >
                                    {isSaving ? 'Saving...' : 'Save Changes'}
                                </button>
                            </form>
                        </div>
                    )}

                    {/* Security Tab */}
                    {activeTab === 'account' && (
                        <div className="space-y-6">
                            <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-2xl">
                                <div className="p-5 border-b border-[var(--border-color)]">
                                    <h3 className="text-lg font-semibold text-[var(--text-primary)]">Change Password</h3>
                                </div>
                                <form onSubmit={handlePasswordChange} className="p-5 space-y-4">
                                    <div>
                                        <label className="block text-sm font-medium text-[var(--text-secondary)] mb-2">Current Password</label>
                                        <input
                                            type="password"
                                            value={passwordForm.currentPassword}
                                            onChange={(e) => setPasswordForm({ ...passwordForm, currentPassword: e.target.value })}
                                            required
                                            className="w-full px-4 py-3 bg-[var(--bg-primary)] border border-[var(--border-color)] rounded-xl text-[var(--text-primary)] focus:border-[var(--primary)] focus:ring-2 focus:ring-[var(--primary)]/20 transition"
                                        />
                                    </div>
                                    <div>
                                        <label className="block text-sm font-medium text-[var(--text-secondary)] mb-2">New Password</label>
                                        <input
                                            type="password"
                                            value={passwordForm.newPassword}
                                            onChange={(e) => setPasswordForm({ ...passwordForm, newPassword: e.target.value })}
                                            minLength={8}
                                            required
                                            className="w-full px-4 py-3 bg-[var(--bg-primary)] border border-[var(--border-color)] rounded-xl text-[var(--text-primary)] focus:border-[var(--primary)] focus:ring-2 focus:ring-[var(--primary)]/20 transition"
                                        />
                                    </div>
                                    <div>
                                        <label className="block text-sm font-medium text-[var(--text-secondary)] mb-2">Confirm Password</label>
                                        <input
                                            type="password"
                                            value={passwordForm.confirmPassword}
                                            onChange={(e) => setPasswordForm({ ...passwordForm, confirmPassword: e.target.value })}
                                            required
                                            className="w-full px-4 py-3 bg-[var(--bg-primary)] border border-[var(--border-color)] rounded-xl text-[var(--text-primary)] focus:border-[var(--primary)] focus:ring-2 focus:ring-[var(--primary)]/20 transition"
                                        />
                                    </div>

                                    {!codeRequested ? (
                                        <button
                                            type="button"
                                            onClick={handleRequestCode}
                                            disabled={isRequestingCode || !passwordForm.currentPassword || !passwordForm.newPassword}
                                            className="px-4 py-2 bg-[var(--bg-tertiary)] text-[var(--text-primary)] border border-[var(--border-color)] rounded-xl text-sm font-medium hover:bg-[var(--bg-primary)] transition disabled:opacity-50"
                                        >
                                            {isRequestingCode ? 'Sending code...' : 'Send Verification Code to Email'}
                                        </button>
                                    ) : (
                                        <div>
                                            <label className="block text-sm font-medium text-[var(--text-secondary)] mb-2">
                                                Verification Code
                                            </label>
                                            <input
                                                type="text"
                                                value={passwordForm.verificationCode}
                                                onChange={(e) => setPasswordForm({ ...passwordForm, verificationCode: e.target.value.replace(/\D/g, '').slice(0, 6) })}
                                                placeholder="6-digit code"
                                                maxLength={6}
                                                required
                                                className="w-full px-4 py-3 bg-[var(--bg-primary)] border border-[var(--border-color)] rounded-xl text-[var(--text-primary)] focus:border-[var(--primary)] focus:ring-2 focus:ring-[var(--primary)]/20 transition tracking-widest text-center font-mono"
                                            />
                                            <button
                                                type="button"
                                                onClick={handleRequestCode}
                                                className="mt-2 text-xs text-[var(--primary)] hover:underline"
                                            >
                                                Resend code
                                            </button>
                                        </div>
                                    )}

                                    <button
                                        type="submit"
                                        disabled={isSaving || !codeRequested}
                                        className="px-6 py-3 bg-[var(--primary)] text-white rounded-xl font-medium hover:bg-[var(--primary-dark)] transition disabled:opacity-50"
                                    >
                                        {isSaving ? 'Updating...' : 'Update Password'}
                                    </button>
                                </form>
                            </div>

                            <div className="bg-[var(--bg-secondary)] border border-red-500/30 rounded-2xl">
                                <div className="p-5 border-b border-red-500/30">
                                    <h3 className="text-lg font-semibold text-red-500">Danger Zone</h3>
                                </div>
                                <div className="p-5">
                                    <button
                                        onClick={logout}
                                        className="flex items-center gap-2 px-5 py-2.5 border border-red-500 text-red-500 rounded-xl hover:bg-red-500/10 transition"
                                    >
                                        <span className="material-symbols-outlined">logout</span>
                                        Sign Out
                                    </button>
                                </div>
                            </div>
                        </div>
                    )}

                    {/* Notifications Tab */}
                    {activeTab === 'notifications' && (
                        <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-2xl">
                            <div className="p-5 border-b border-[var(--border-color)]">
                                <h3 className="text-lg font-semibold text-[var(--text-primary)]">Notification Settings</h3>
                            </div>
                            <div className="p-5 space-y-4">
                                {[
                                    { id: 'email', label: 'Email notifications' },
                                    { id: 'push', label: 'Push notifications' },
                                    { id: 'answer', label: 'Notify when someone answers my question' },
                                    { id: 'mention', label: 'Notify when someone mentions me' },
                                ].map((item) => (
                                    <label key={item.id} className="flex items-center justify-between p-4 bg-[var(--bg-tertiary)] rounded-xl cursor-pointer">
                                        <span className="text-[var(--text-primary)]">{item.label}</span>
                                        <div className="relative">
                                            <input type="checkbox" defaultChecked className="sr-only peer" />
                                            <div className="w-11 h-6 bg-[var(--border-color)] rounded-full peer peer-checked:bg-[var(--primary)] transition"></div>
                                            <div className="absolute left-1 top-1 w-4 h-4 bg-white rounded-full peer-checked:translate-x-5 transition"></div>
                                        </div>
                                    </label>
                                ))}
                            </div>
                        </div>
                    )}

                    {/* Preferences Tab */}
                    {activeTab === 'preferences' && (
                        <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-2xl">
                            <div className="p-5 border-b border-[var(--border-color)]">
                                <h3 className="text-lg font-semibold text-[var(--text-primary)]">{t('settings.preferences')}</h3>
                            </div>
                            <div className="p-5 space-y-5">
                                <div>
                                    <label className="block text-sm font-medium text-[var(--text-secondary)] mb-2">{t('preferences.theme')}</label>
                                    <select
                                        value={preference}
                                        onChange={(e) => setPreference(e.target.value as 'light' | 'dark' | 'system')}
                                        className="w-full px-4 py-3 bg-[var(--bg-primary)] border border-[var(--border-color)] rounded-xl text-[var(--text-primary)] focus:border-[var(--primary)] focus:ring-2 focus:ring-[var(--primary)]/20 transition"
                                    >
                                        <option value="light">{t('preferences.theme.light')}</option>
                                        <option value="dark">{t('preferences.theme.dark')}</option>
                                        <option value="system">{t('preferences.theme.system')}</option>
                                    </select>
                                </div>
                                <div>
                                    <label className="block text-sm font-medium text-[var(--text-secondary)] mb-2">{t('preferences.language')}</label>
                                    <select
                                        value={locale}
                                        onChange={(e) => setLocale(e.target.value as 'en' | 'vi')}
                                        className="w-full px-4 py-3 bg-[var(--bg-primary)] border border-[var(--border-color)] rounded-xl text-[var(--text-primary)] focus:border-[var(--primary)] focus:ring-2 focus:ring-[var(--primary)]/20 transition"
                                    >
                                        <option value="en">{t('preferences.language.en')}</option>
                                        <option value="vi">{t('preferences.language.vi')}</option>
                                    </select>
                                </div>
                            </div>
                        </div>
                    )}
                </div>
            </div>
        </AppLayout>
    );
}
