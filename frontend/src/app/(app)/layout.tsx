import type { Metadata } from 'next';

export const metadata: Metadata = {
    title: {
        template: '%s | SocialTechsy',
        default: 'SocialTechsy - Developer Community Platform',
    },
    description: 'A vibrant developer community for sharing knowledge, solving technical problems, and growing your career.',
};

export default function AppGroupLayout({
    children,
}: {
    children: React.ReactNode;
}) {
    return <>{children}</>;
}
