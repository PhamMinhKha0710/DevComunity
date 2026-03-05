'use client';

import ModernNavbar from './ModernNavbar';
import ModernSidebar from './ModernSidebar';
import RightSidebar from './RightSidebar';

interface AppLayoutProps {
    children: React.ReactNode;
    showRightSidebar?: boolean;
}

export default function AppLayout({ children, showRightSidebar = true }: AppLayoutProps) {
    return (
        <div className="flex min-h-screen bg-[#f6f7f8] dark:bg-[#101922]">
            {/* Fixed Sidebar */}
            <ModernSidebar />

            {/* Main Content Area */}
            <div className="flex-1 flex flex-col overflow-hidden">
                {/* Top Header */}
                <ModernNavbar />

                {/* Scrollable Content */}
                <main className="flex-1 overflow-y-auto p-8">
                    <div className="max-w-7xl mx-auto flex gap-8">
                        {/* Main Feed */}
                        <div className="flex-1 min-w-0 space-y-8">
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
