import type { Metadata } from 'next';

export const metadata: Metadata = {
    title: 'Chat | SocialTechsy',
    description: 'Real-time messaging and collaboration on SocialTechsy.',
};

export default function ChatGroupLayout({
    children,
}: {
    children: React.ReactNode;
}) {
    return (
        <div className="min-h-screen bg-slate-50 dark:bg-slate-950">
            {children}
        </div>
    );
}
