'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { useAuth } from '@/lib/contexts/AuthContext';
import apiClient from '@/lib/api/client';

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
            <div className="container py-5 text-center">
                <div className="spinner-border text-primary" role="status">
                    <span className="visually-hidden">Loading...</span>
                </div>
            </div>
        );
    }

    if (!user) return null;

    return (
        <div className="container py-4">
            <div className="row">
                {/* Sidebar */}
                <div className="col-lg-3 mb-4">
                    <div className="card border-0 shadow-sm rounded-4">
                        <div className="card-body p-0">
                            <div className="text-center p-4 border-bottom">
                                <img
                                    src={user.profilePicture || '/images/default-avatar.png'}
                                    className="rounded-circle mb-3"
                                    width="80"
                                    height="80"
                                    alt=""
                                />
                                <h6 className="fw-bold mb-0">{user.displayName || user.username}</h6>
                                <small className="text-muted">@{user.username}</small>
                            </div>
                            <div className="list-group list-group-flush">
                                <button
                                    className={`list-group-item list-group-item-action ${activeTab === 'profile' ? 'active' : ''}`}
                                    onClick={() => setActiveTab('profile')}
                                >
                                    <i className="bi bi-person me-2"></i>Profile
                                </button>
                                <button
                                    className={`list-group-item list-group-item-action ${activeTab === 'account' ? 'active' : ''}`}
                                    onClick={() => setActiveTab('account')}
                                >
                                    <i className="bi bi-shield-lock me-2"></i>Account & Security
                                </button>
                                <button
                                    className={`list-group-item list-group-item-action ${activeTab === 'notifications' ? 'active' : ''}`}
                                    onClick={() => setActiveTab('notifications')}
                                >
                                    <i className="bi bi-bell me-2"></i>Notifications
                                </button>
                                <button
                                    className={`list-group-item list-group-item-action ${activeTab === 'preferences' ? 'active' : ''}`}
                                    onClick={() => setActiveTab('preferences')}
                                >
                                    <i className="bi bi-sliders me-2"></i>Preferences
                                </button>
                            </div>
                        </div>
                    </div>
                </div>

                {/* Main Content */}
                <div className="col-lg-9">
                    {message.text && (
                        <div className={`alert alert-${message.type === 'success' ? 'success' : 'danger'} alert-dismissible fade show`} role="alert">
                            <i className={`bi bi-${message.type === 'success' ? 'check-circle' : 'exclamation-circle'} me-2`}></i>
                            {message.text}
                            <button type="button" className="btn-close" onClick={() => setMessage({ type: '', text: '' })}></button>
                        </div>
                    )}

                    {activeTab === 'profile' && (
                        <div className="card border-0 shadow-sm rounded-4">
                            <div className="card-header bg-white border-0 pt-4">
                                <h5 className="fw-bold mb-0">Profile Settings</h5>
                            </div>
                            <div className="card-body">
                                <form onSubmit={handleProfileSave}>
                                    <div className="mb-4">
                                        <label className="form-label fw-semibold">Display Name</label>
                                        <input
                                            type="text"
                                            className="form-control"
                                            value={profile.displayName}
                                            onChange={(e) => setProfile({ ...profile, displayName: e.target.value })}
                                        />
                                    </div>
                                    <div className="mb-4">
                                        <label className="form-label fw-semibold">Email</label>
                                        <input
                                            type="email"
                                            className="form-control"
                                            value={profile.email}
                                            onChange={(e) => setProfile({ ...profile, email: e.target.value })}
                                        />
                                    </div>
                                    <div className="mb-4">
                                        <label className="form-label fw-semibold">Bio</label>
                                        <textarea
                                            className="form-control"
                                            rows={3}
                                            value={profile.bio}
                                            onChange={(e) => setProfile({ ...profile, bio: e.target.value })}
                                            placeholder="Tell us about yourself..."
                                        />
                                    </div>
                                    <div className="row">
                                        <div className="col-md-6 mb-4">
                                            <label className="form-label fw-semibold">Location</label>
                                            <input
                                                type="text"
                                                className="form-control"
                                                value={profile.location}
                                                onChange={(e) => setProfile({ ...profile, location: e.target.value })}
                                                placeholder="City, Country"
                                            />
                                        </div>
                                        <div className="col-md-6 mb-4">
                                            <label className="form-label fw-semibold">Website</label>
                                            <input
                                                type="url"
                                                className="form-control"
                                                value={profile.website}
                                                onChange={(e) => setProfile({ ...profile, website: e.target.value })}
                                                placeholder="https://..."
                                            />
                                        </div>
                                    </div>
                                    <button type="submit" className="btn btn-primary rounded-pill px-4" disabled={isSaving}>
                                        {isSaving ? 'Saving...' : 'Save Changes'}
                                    </button>
                                </form>
                            </div>
                        </div>
                    )}

                    {activeTab === 'account' && (
                        <div className="card border-0 shadow-sm rounded-4">
                            <div className="card-header bg-white border-0 pt-4">
                                <h5 className="fw-bold mb-0">Account & Security</h5>
                            </div>
                            <div className="card-body">
                                <h6 className="fw-semibold mb-3">Change Password</h6>
                                <form onSubmit={handlePasswordChange}>
                                    <div className="mb-3">
                                        <label className="form-label">Current Password</label>
                                        <input
                                            type="password"
                                            className="form-control"
                                            value={passwordForm.currentPassword}
                                            onChange={(e) => setPasswordForm({ ...passwordForm, currentPassword: e.target.value })}
                                            required
                                        />
                                    </div>
                                    <div className="mb-3">
                                        <label className="form-label">New Password</label>
                                        <input
                                            type="password"
                                            className="form-control"
                                            value={passwordForm.newPassword}
                                            onChange={(e) => setPasswordForm({ ...passwordForm, newPassword: e.target.value })}
                                            minLength={6}
                                            required
                                        />
                                    </div>
                                    <div className="mb-4">
                                        <label className="form-label">Confirm New Password</label>
                                        <input
                                            type="password"
                                            className="form-control"
                                            value={passwordForm.confirmPassword}
                                            onChange={(e) => setPasswordForm({ ...passwordForm, confirmPassword: e.target.value })}
                                            required
                                        />
                                    </div>
                                    <button type="submit" className="btn btn-primary rounded-pill px-4" disabled={isSaving}>
                                        {isSaving ? 'Updating...' : 'Update Password'}
                                    </button>
                                </form>

                                <hr className="my-4" />

                                <h6 className="fw-semibold mb-3 text-danger">Danger Zone</h6>
                                <button className="btn btn-outline-danger rounded-pill" onClick={logout}>
                                    <i className="bi bi-box-arrow-right me-2"></i>Sign Out
                                </button>
                            </div>
                        </div>
                    )}

                    {activeTab === 'notifications' && (
                        <div className="card border-0 shadow-sm rounded-4">
                            <div className="card-header bg-white border-0 pt-4">
                                <h5 className="fw-bold mb-0">Notification Settings</h5>
                            </div>
                            <div className="card-body">
                                <div className="form-check form-switch mb-3">
                                    <input className="form-check-input" type="checkbox" id="emailNotif" defaultChecked />
                                    <label className="form-check-label" htmlFor="emailNotif">Email notifications</label>
                                </div>
                                <div className="form-check form-switch mb-3">
                                    <input className="form-check-input" type="checkbox" id="pushNotif" defaultChecked />
                                    <label className="form-check-label" htmlFor="pushNotif">Push notifications</label>
                                </div>
                                <div className="form-check form-switch mb-3">
                                    <input className="form-check-input" type="checkbox" id="answerNotif" defaultChecked />
                                    <label className="form-check-label" htmlFor="answerNotif">Notify when someone answers my question</label>
                                </div>
                                <div className="form-check form-switch">
                                    <input className="form-check-input" type="checkbox" id="mentionNotif" defaultChecked />
                                    <label className="form-check-label" htmlFor="mentionNotif">Notify when someone mentions me</label>
                                </div>
                            </div>
                        </div>
                    )}

                    {activeTab === 'preferences' && (
                        <div className="card border-0 shadow-sm rounded-4">
                            <div className="card-header bg-white border-0 pt-4">
                                <h5 className="fw-bold mb-0">Preferences</h5>
                            </div>
                            <div className="card-body">
                                <div className="mb-4">
                                    <label className="form-label fw-semibold">Theme</label>
                                    <select className="form-select">
                                        <option value="light">Light</option>
                                        <option value="dark">Dark</option>
                                        <option value="system">System</option>
                                    </select>
                                </div>
                                <div className="mb-4">
                                    <label className="form-label fw-semibold">Language</label>
                                    <select className="form-select">
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
    );
}
