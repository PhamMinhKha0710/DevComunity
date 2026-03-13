import type { Metadata } from 'next';

export const metadata: Metadata = {
    title: {
        template: '%s | SocialTechsy',
        default: 'Sign In | SocialTechsy',
    },
    description: 'Sign in or create an account on SocialTechsy to join the developer community.',
};

export default function AuthLayout({
    children,
}: {
    children: React.ReactNode;
}) {
    return (
        <div className="min-h-screen bg-[var(--bg-primary)]">
            {children}
        </div>
    );
}
