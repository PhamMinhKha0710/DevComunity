'use client';

import { useState } from 'react';
import ModernNavbar from './ModernNavbar';
import ModernSidebar from './ModernSidebar';
import RightSidebar from './RightSidebar';
import MobileNav from './MobileNav';

interface AppLayoutProps {
    children: React.ReactNode;
    showRightSidebar?: boolean;
}

export default function AppLayout({ children, showRightSidebar = true }: AppLayoutProps) {
    const [mobileNavOpen, setMobileNavOpen] = useState(false);

    return (
        <div className="flex min-h-screen bg-[#f6f7f8] dark:bg-[#101922]">
            {/* Fixed Sidebar - desktop only */}
            <ModernSidebar />

            {/* Mobile Navigation Drawer */}
            <MobileNav isOpen={mobileNavOpen} onClose={() => setMobileNavOpen(false)} />

            {/* Main Content Area */}
            <div className="flex-1 flex flex-col overflow-hidden">
                {/* Top Header */}
                <ModernNavbar onMobileMenuClick={() => setMobileNavOpen(true)} />

                {/* Scrollable Content */}
                <main className="flex-1 overflow-y-auto p-4 sm:p-6 lg:p-8">
                    <div className="max-w-7xl mx-auto flex gap-4 sm:gap-6 lg:gap-8">
                        {/* Main Feed */}
                        <div className="flex-1 min-w-0 space-y-4 sm:space-y-6 lg:space-y-8">
                            {children}
                        </div>

                        {/* Right Sidebar */}
                        {showRightSidebar && <RightSidebar />}
                    </div>
                </main>
            </div>
        </div>
    );
}
