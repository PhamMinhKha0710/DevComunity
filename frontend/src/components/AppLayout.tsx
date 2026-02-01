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
        <div className="min-h-screen bg-[var(--bg-primary)]">
            <ModernNavbar />

            <div className="flex pt-16">
                <ModernSidebar />

                <main className="flex-1 min-w-0 p-6">
                    <div className="max-w-4xl mx-auto">
                        {children}
                    </div>
                </main>

                {showRightSidebar && <RightSidebar />}
            </div>
        </div>
    );
}
