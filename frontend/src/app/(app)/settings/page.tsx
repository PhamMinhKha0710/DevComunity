'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { useAuth } from '@/lib/contexts/AuthContext';
import apiClient from '@/lib/api/client';
import AppLayout from '@/components/AppLayout';

export default function SettingsPage() {
    const { user, isLoading: authLoading, logout } = useAuth();
    const router = useRouter();
    const [activeTab, setActiveTab] = useState('profile');
    const [isSaving, setIsSaving] = useState(false);
    const [message, setMessage] = useState({ type: '', text: '' });

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
    });

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

    const handleProfileSave = async (e: React.FormEvent) => {
        e.preventDefault();
        setIsSaving(true);
        setMessage({ type: '', text: '' });

        try {
            await apiClient.put('/users/profile', profile);
            setMessage({ type: 'success', text: 'Profile updated successfully' });
        } catch (err: any) {
            setMessage({ type: 'error', text: err.response?.data?.message || 'Failed to update profile' });
        } finally {
            setIsSaving(false);
        }
    };

    const handlePasswordChange = async (e: React.FormEvent) => {
        e.preventDefault();

        if (passwordForm.newPassword !== passwordForm.confirmPassword) {
            setMessage({ type: 'error', text: 'Passwords do not match' });
            return;
        }

        setIsSaving(true);
        setMessage({ type: '', text: '' });

        try {
            await apiClient.post('/auth/change-password', {
                currentPassword: passwordForm.currentPassword,
                newPassword: passwordForm.newPassword,
            });
            setMessage({ type: 'success', text: 'Password changed successfully' });
            setPasswordForm({ currentPassword: '', newPassword: '', confirmPassword: '' });
        } catch (err: any) {
            setMessage({ type: 'error', text: err.response?.data?.message || 'Failed to change password' });
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
        { key: 'profile', label: 'Profile', icon: 'person' },
        { key: 'account', label: 'Security', icon: 'security' },
        { key: 'notifications', label: 'Notifications', icon: 'notifications' },
        { key: 'preferences', label: 'Preferences', icon: 'tune' },
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

            <div className="grid lg:grid-cols-4 gap-6">
                {/* Sidebar */}
                <div className="lg:col-span-1">
                    <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-2xl overflow-hidden">
                        {/* User Preview */}
                        <div className="p-5 text-center border-b border-[var(--border-color)]">
                            <div className="w-16 h-16 rounded-full bg-gradient-to-br from-blue-500 via-indigo-500 to-purple-500 p-0.5 mx-auto mb-3">
                                <div className="w-full h-full rounded-full bg-[var(--bg-secondary)] flex items-center justify-center text-xl font-bold text-[var(--text-primary)]">
                                    {user.displayName?.charAt(0).toUpperCase() || user.username?.charAt(0).toUpperCase() || '?'}
                                </div>
                            </div>
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
                                            minLength={6}
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
                                    <button
                                        type="submit"
                                        disabled={isSaving}
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
                                <h3 className="text-lg font-semibold text-[var(--text-primary)]">Preferences</h3>
                            </div>
                            <div className="p-5 space-y-5">
                                <div>
                                    <label className="block text-sm font-medium text-[var(--text-secondary)] mb-2">Theme</label>
                                    <select className="w-full px-4 py-3 bg-[var(--bg-primary)] border border-[var(--border-color)] rounded-xl text-[var(--text-primary)] focus:border-[var(--primary)] focus:ring-2 focus:ring-[var(--primary)]/20 transition">
                                        <option value="light">Light</option>
                                        <option value="dark">Dark</option>
                                        <option value="system">System</option>
                                    </select>
                                </div>
                                <div>
                                    <label className="block text-sm font-medium text-[var(--text-secondary)] mb-2">Language</label>
                                    <select className="w-full px-4 py-3 bg-[var(--bg-primary)] border border-[var(--border-color)] rounded-xl text-[var(--text-primary)] focus:border-[var(--primary)] focus:ring-2 focus:ring-[var(--primary)]/20 transition">
                                        <option value="en">English</option>
                                        <option value="vi">Tiếng Việt</option>
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
