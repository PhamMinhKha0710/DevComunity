'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { useAuth } from '@/lib/contexts/AuthContext';
import apiClient from '@/lib/api/client';

type Tab = 'profile' | 'account' | 'notifications' | 'preferences';

export default function SettingsPage() {
    const { user, isLoading: authLoading, logout } = useAuth();
    const router = useRouter();
    const [activeTab, setActiveTab] = useState<Tab>('profile');
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
            router.push('/login');
        } else if (user) {
            setProfile({
                displayName: user.displayName || '',
                email: user.email || '',
                bio: '',
                location: '',
                website: '',
            });
        }
    }, [user, authLoading, router]);

    const handleProfileSave = async (e: React.FormEvent) => {
        e.preventDefault();
        setIsSaving(true);
        setMessage({ type: '', text: '' });

        try {
            await apiClient.put('/users/profile', profile);
            setMessage({ type: 'success', text: 'Profile updated successfully' });
        } catch (err: unknown) {
            const error = err as { response?: { data?: { message?: string } } };
            setMessage({ type: 'error', text: error.response?.data?.message || 'Failed to update profile' });
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
        } catch (err: unknown) {
            const error = err as { response?: { data?: { message?: string } } };
            setMessage({ type: 'error', text: error.response?.data?.message || 'Failed to change password' });
        } finally {
            setIsSaving(false);
        }
    };

    if (authLoading) {
        return (
            <div className="min-h-screen flex items-center justify-center bg-gray-50 dark:bg-slate-900">
                <div className="inline-block w-10 h-10 border-4 border-orange-500 border-t-transparent rounded-full animate-spin"></div>
            </div>
        );
    }

    if (!user) return null;

    const tabs = [
        { key: 'profile' as Tab, icon: 'bi-person', label: 'Profile' },
        { key: 'account' as Tab, icon: 'bi-shield-lock', label: 'Account & Security' },
        { key: 'notifications' as Tab, icon: 'bi-bell', label: 'Notifications' },
        { key: 'preferences' as Tab, icon: 'bi-sliders', label: 'Preferences' },
    ];

    return (
        <div className="min-h-screen bg-gray-50 dark:bg-slate-900 py-8">
            <div className="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8">
                <div className="grid lg:grid-cols-4 gap-8">
                    {/* Sidebar */}
                    <div className="lg:col-span-1">
                        <div className="bg-white dark:bg-slate-800 rounded-xl shadow-sm overflow-hidden">
                            <div className="p-6 text-center border-b border-gray-100 dark:border-slate-700">
                                <img
                                    src={user.profilePicture || '/images/default-avatar.png'}
                                    className="w-20 h-20 rounded-full mx-auto mb-3 border-4 border-gray-100 dark:border-slate-700"
                                    alt=""
                                />
                                <h3 className="font-bold text-gray-900 dark:text-white">{user.displayName || user.username}</h3>
                                <p className="text-sm text-gray-500">@{user.username}</p>
                            </div>
                            <nav className="p-2">
                                {tabs.map(({ key, icon, label }) => (
                                    <button
                                        key={key}
                                        onClick={() => setActiveTab(key)}
                                        className={`w-full flex items-center px-4 py-3 rounded-lg text-left transition ${activeTab === key
                                                ? 'bg-orange-50 dark:bg-orange-900/20 text-orange-600'
                                                : 'text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-slate-700'
                                            }`}
                                    >
                                        <i className={`bi ${icon} mr-3`}></i>{label}
                                    </button>
                                ))}
                            </nav>
                        </div>
                    </div>

                    {/* Main Content */}
                    <div className="lg:col-span-3">
                        {message.text && (
                            <div className={`mb-6 px-4 py-3 rounded-lg flex items-center ${message.type === 'success'
                                    ? 'bg-green-50 dark:bg-green-900/20 text-green-700 dark:text-green-400 border border-green-200 dark:border-green-800'
                                    : 'bg-red-50 dark:bg-red-900/20 text-red-700 dark:text-red-400 border border-red-200 dark:border-red-800'
                                }`}>
                                <i className={`bi ${message.type === 'success' ? 'bi-check-circle' : 'bi-exclamation-circle'} mr-2`}></i>
                                {message.text}
                                <button onClick={() => setMessage({ type: '', text: '' })} className="ml-auto">
                                    <i className="bi bi-x"></i>
                                </button>
                            </div>
                        )}

                        {activeTab === 'profile' && (
                            <div className="bg-white dark:bg-slate-800 rounded-xl shadow-sm p-6">
                                <h2 className="text-xl font-bold text-gray-900 dark:text-white mb-6">Profile Settings</h2>
                                <form onSubmit={handleProfileSave} className="space-y-6">
                                    <div>
                                        <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">Display Name</label>
                                        <input
                                            type="text"
                                            className="w-full px-4 py-3 border border-gray-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-orange-500 focus:border-transparent"
                                            value={profile.displayName}
                                            onChange={(e) => setProfile({ ...profile, displayName: e.target.value })}
                                        />
                                    </div>
                                    <div>
                                        <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">Email</label>
                                        <input
                                            type="email"
                                            className="w-full px-4 py-3 border border-gray-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-orange-500 focus:border-transparent"
                                            value={profile.email}
                                            onChange={(e) => setProfile({ ...profile, email: e.target.value })}
                                        />
                                    </div>
                                    <div>
                                        <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">Bio</label>
                                        <textarea
                                            rows={3}
                                            className="w-full px-4 py-3 border border-gray-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-orange-500 focus:border-transparent resize-none"
                                            value={profile.bio}
                                            onChange={(e) => setProfile({ ...profile, bio: e.target.value })}
                                            placeholder="Tell us about yourself..."
                                        />
                                    </div>
                                    <div className="grid md:grid-cols-2 gap-6">
                                        <div>
                                            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">Location</label>
                                            <input
                                                type="text"
                                                className="w-full px-4 py-3 border border-gray-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-orange-500 focus:border-transparent"
                                                value={profile.location}
                                                onChange={(e) => setProfile({ ...profile, location: e.target.value })}
                                                placeholder="City, Country"
                                            />
                                        </div>
                                        <div>
                                            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">Website</label>
                                            <input
                                                type="url"
                                                className="w-full px-4 py-3 border border-gray-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-orange-500 focus:border-transparent"
                                                value={profile.website}
                                                onChange={(e) => setProfile({ ...profile, website: e.target.value })}
                                                placeholder="https://..."
                                            />
                                        </div>
                                    </div>
                                    <button
                                        type="submit"
                                        disabled={isSaving}
                                        className="px-6 py-3 bg-orange-500 text-white font-medium rounded-lg hover:bg-orange-600 disabled:opacity-50 transition"
                                    >
                                        {isSaving ? 'Saving...' : 'Save Changes'}
                                    </button>
                                </form>
                            </div>
                        )}

                        {activeTab === 'account' && (
                            <div className="bg-white dark:bg-slate-800 rounded-xl shadow-sm p-6">
                                <h2 className="text-xl font-bold text-gray-900 dark:text-white mb-6">Account & Security</h2>

                                <h3 className="font-semibold text-gray-900 dark:text-white mb-4">Change Password</h3>
                                <form onSubmit={handlePasswordChange} className="space-y-4 mb-8">
                                    <div>
                                        <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">Current Password</label>
                                        <input
                                            type="password"
                                            className="w-full px-4 py-3 border border-gray-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-orange-500 focus:border-transparent"
                                            value={passwordForm.currentPassword}
                                            onChange={(e) => setPasswordForm({ ...passwordForm, currentPassword: e.target.value })}
                                            required
                                        />
                                    </div>
                                    <div>
                                        <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">New Password</label>
                                        <input
                                            type="password"
                                            className="w-full px-4 py-3 border border-gray-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-orange-500 focus:border-transparent"
                                            value={passwordForm.newPassword}
                                            onChange={(e) => setPasswordForm({ ...passwordForm, newPassword: e.target.value })}
                                            minLength={6}
                                            required
                                        />
                                    </div>
                                    <div>
                                        <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">Confirm New Password</label>
                                        <input
                                            type="password"
                                            className="w-full px-4 py-3 border border-gray-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-orange-500 focus:border-transparent"
                                            value={passwordForm.confirmPassword}
                                            onChange={(e) => setPasswordForm({ ...passwordForm, confirmPassword: e.target.value })}
                                            required
                                        />
                                    </div>
                                    <button
                                        type="submit"
                                        disabled={isSaving}
                                        className="px-6 py-3 bg-orange-500 text-white font-medium rounded-lg hover:bg-orange-600 disabled:opacity-50 transition"
                                    >
                                        {isSaving ? 'Updating...' : 'Update Password'}
                                    </button>
                                </form>

                                <div className="border-t border-gray-200 dark:border-slate-700 pt-6">
                                    <h3 className="font-semibold text-red-600 mb-4">Danger Zone</h3>
                                    <button
                                        onClick={logout}
                                        className="px-6 py-3 border border-red-500 text-red-500 font-medium rounded-lg hover:bg-red-50 dark:hover:bg-red-900/20 transition"
                                    >
                                        <i className="bi bi-box-arrow-right mr-2"></i>Sign Out
                                    </button>
                                </div>
                            </div>
                        )}

                        {activeTab === 'notifications' && (
                            <div className="bg-white dark:bg-slate-800 rounded-xl shadow-sm p-6">
                                <h2 className="text-xl font-bold text-gray-900 dark:text-white mb-6">Notification Settings</h2>
                                <div className="space-y-4">
                                    {[
                                        { id: 'emailNotif', label: 'Email notifications', defaultChecked: true },
                                        { id: 'pushNotif', label: 'Push notifications', defaultChecked: true },
                                        { id: 'answerNotif', label: 'Notify when someone answers my question', defaultChecked: true },
                                        { id: 'mentionNotif', label: 'Notify when someone mentions me', defaultChecked: true },
                                    ].map(({ id, label, defaultChecked }) => (
                                        <label key={id} className="flex items-center justify-between p-4 bg-gray-50 dark:bg-slate-700 rounded-lg cursor-pointer">
                                            <span className="text-gray-700 dark:text-gray-300">{label}</span>
                                            <input type="checkbox" defaultChecked={defaultChecked} className="w-5 h-5 text-orange-500 rounded focus:ring-orange-500" />
                                        </label>
                                    ))}
                                </div>
                            </div>
                        )}

                        {activeTab === 'preferences' && (
                            <div className="bg-white dark:bg-slate-800 rounded-xl shadow-sm p-6">
                                <h2 className="text-xl font-bold text-gray-900 dark:text-white mb-6">Preferences</h2>
                                <div className="space-y-6">
                                    <div>
                                        <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">Theme</label>
                                        <select className="w-full px-4 py-3 border border-gray-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-orange-500 focus:border-transparent">
                                            <option value="light">Light</option>
                                            <option value="dark">Dark</option>
                                            <option value="system">System</option>
                                        </select>
                                    </div>
                                    <div>
                                        <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">Language</label>
                                        <select className="w-full px-4 py-3 border border-gray-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-orange-500 focus:border-transparent">
                                            <option value="en">English</option>
                                            <option value="vi">Tiếng Việt</option>
                                        </select>
                                    </div>
                                </div>
                            </div>
                        )}
                    </div>
                </div>
            </div>
        </div>
    );
}
