'use client';

import ModernNavbar from './ModernNavbar';
import ModernSidebar from './ModernSidebar';
import RightSidebar from './RightSidebar';
import ModernFooter from './ModernFooter';

interface AppLayoutProps {
    children: React.ReactNode;
    showRightSidebar?: boolean;
}

export default function AppLayout({ children, showRightSidebar = true }: AppLayoutProps) {
    return (
        <div className="min-h-screen flex flex-col bg-[var(--bg-primary)]">
            <ModernNavbar />

            <div className="flex flex-1 pt-16">
                <ModernSidebar />

                <main className="flex-1 min-w-0 p-6 flex flex-col">
                    <div className="max-w-4xl mx-auto w-full flex-1">
                        {children}
                    </div>

                    {/* Spacer to push footer down if content is short */}
                    <div className="mt-12"></div>
                </main>

                {showRightSidebar && <RightSidebar />}
            </div>

            <ModernFooter />
        </div>
    );
}
