export type MessagesKey =
    // Settings sidebar
    | 'settings.profile'
    | 'settings.account'
    | 'settings.notifications'
    | 'settings.privacy'
    | 'settings.appearance'
    | 'settings.preferences'
    // Profile fields
    | 'profile.displayName'
    | 'profile.bio'
    | 'profile.location'
    | 'profile.website'
    | 'profile.saveChanges'
    | 'profile.saved'
    // Preferences
    | 'preferences.theme'
    | 'preferences.theme.light'
    | 'preferences.theme.dark'
    | 'preferences.theme.system'
    | 'preferences.language'
    | 'preferences.language.en'
    | 'preferences.language.vi'
    // Account
    | 'account.email'
    | 'account.username'
    | 'account.changePassword'
    | 'account.currentPassword'
    | 'account.newPassword'
    | 'account.confirmPassword'
    // General
    | 'common.cancel'
    | 'common.save'
    | 'common.delete'
    | 'common.edit'
    | 'common.loading';

export const messages: Record<'en' | 'vi', Record<MessagesKey, string>> = {
    en: {
        // Settings sidebar
        'settings.profile': 'Profile',
        'settings.account': 'Account',
        'settings.notifications': 'Notifications',
        'settings.privacy': 'Privacy',
        'settings.appearance': 'Appearance',
        'settings.preferences': 'Preferences',
        // Profile fields
        'profile.displayName': 'Display Name',
        'profile.bio': 'Bio',
        'profile.location': 'Location',
        'profile.website': 'Website',
        'profile.saveChanges': 'Save Changes',
        'profile.saved': 'Saved!',
        // Preferences
        'preferences.theme': 'Theme',
        'preferences.theme.light': 'Light',
        'preferences.theme.dark': 'Dark',
        'preferences.theme.system': 'System',
        'preferences.language': 'Language',
        'preferences.language.en': 'English',
        'preferences.language.vi': 'Tiếng Việt',
        // Account
        'account.email': 'Email',
        'account.username': 'Username',
        'account.changePassword': 'Change Password',
        'account.currentPassword': 'Current Password',
        'account.newPassword': 'New Password',
        'account.confirmPassword': 'Confirm Password',
        // General
        'common.cancel': 'Cancel',
        'common.save': 'Save',
        'common.delete': 'Delete',
        'common.edit': 'Edit',
        'common.loading': 'Loading...',
    },
    vi: {
        // Settings sidebar
        'settings.profile': 'Hồ sơ',
        'settings.account': 'Tài khoản',
        'settings.notifications': 'Thông báo',
        'settings.privacy': 'Quyền riêng tư',
        'settings.appearance': 'Giao diện',
        'settings.preferences': 'Tùy chỉnh',
        // Profile fields
        'profile.displayName': 'Tên hiển thị',
        'profile.bio': 'Giới thiệu',
        'profile.location': 'Địa điểm',
        'profile.website': 'Website',
        'profile.saveChanges': 'Lưu thay đổi',
        'profile.saved': 'Đã lưu!',
        // Preferences
        'preferences.theme': 'Giao diện',
        'preferences.theme.light': 'Sáng',
        'preferences.theme.dark': 'Tối',
        'preferences.theme.system': 'Hệ thống',
        'preferences.language': 'Ngôn ngữ',
        'preferences.language.en': 'English',
        'preferences.language.vi': 'Tiếng Việt',
        // Account
        'account.email': 'Email',
        'account.username': 'Tên đăng nhập',
        'account.changePassword': 'Đổi mật khẩu',
        'account.currentPassword': 'Mật khẩu hiện tại',
        'account.newPassword': 'Mật khẩu mới',
        'account.confirmPassword': 'Xác nhận mật khẩu',
        // General
        'common.cancel': 'Hủy',
        'common.save': 'Lưu',
        'common.delete': 'Xóa',
        'common.edit': 'Sửa',
        'common.loading': 'Đang tải...',
    },
};
