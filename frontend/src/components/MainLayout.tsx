'use client';

import Navbar from './Navbar';
import Sidebar from './Sidebar';
import Footer from './Footer';

interface MainLayoutProps {
    children: React.ReactNode;
    showSidebar?: boolean;
}

export default function MainLayout({ children, showSidebar = true }: MainLayoutProps) {
    return (
        <div className="min-h-screen flex flex-col bg-gray-50 dark:bg-slate-950">
            <Navbar />

            <div className="flex flex-1">
                {showSidebar && <Sidebar />}

                <main className={`flex-1 ${showSidebar ? 'lg:ml-0' : ''}`}>
                    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-6">
                        {children}
                    </div>
                </main>
            </div>

            <Footer />
        </div>
    );
}
