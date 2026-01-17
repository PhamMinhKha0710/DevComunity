import type { Metadata } from "next";
import { Inter } from "next/font/google";
import { Providers } from "@/lib/contexts/Providers";
import "./globals.css";

const inter = Inter({
  variable: "--font-inter",
  subsets: ["latin"],
  weight: ["300", "400", "500", "600", "700"],
});

export const metadata: Metadata = {
  title: "DevCommunity - Developer Community Platform",
  description: "DevCommunity - Nền tảng kết nối lập trình viên, nơi chia sẻ mã nguồn và giải đáp thắc mắc kỹ thuật từ cộng đồng",
  keywords: "community, developers, programming, coding, questions, answers, repository, git",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en" data-theme="light" data-bs-theme="light">
      <head>
        {/* Google Fonts */}
        <link rel="preconnect" href="https://fonts.googleapis.com" />
        <link rel="preconnect" href="https://fonts.gstatic.com" crossOrigin="anonymous" />
        <link href="https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700&family=Fira+Code:wght@400;500;600&display=swap" rel="stylesheet" />

        {/* Bootstrap CSS */}
        <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.min.css" rel="stylesheet" />
        <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.1/font/bootstrap-icons.css" rel="stylesheet" />

        {/* Prism.js for code highlighting */}
        <link href="https://cdnjs.cloudflare.com/ajax/libs/prism/1.29.0/themes/prism-tomorrow.min.css" rel="stylesheet" />

        {/* AOS Animation */}
        <link href="https://cdn.jsdelivr.net/npm/aos@2.3.4/dist/aos.css" rel="stylesheet" />

        {/* Custom CSS */}
        <link href="/css/site.css" rel="stylesheet" />
        <link href="/css/themes.css" rel="stylesheet" />
        <link href="/css/fast-animations.css" rel="stylesheet" />
        <link href="/css/questions.css" rel="stylesheet" />
        <link href="/css/repositories.css" rel="stylesheet" />
        <link href="/css/tags-modern.css" rel="stylesheet" />
        <link href="/css/signalr-styles.css" rel="stylesheet" />
        <link href="/css/badge-animations.css" rel="stylesheet" />
        <link href="/css/search-suggestions.css" rel="stylesheet" />
        <link href="/css/home.css" rel="stylesheet" />
        <link href="/css/reputation-animations.css" rel="stylesheet" />

        {/* Bootstrap JS */}
        <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/js/bootstrap.bundle.min.js" async></script>

        {/* AOS Animation */}
        <script src="https://cdn.jsdelivr.net/npm/aos@2.3.4/dist/aos.js" async></script>
      </head>
      <body className={`${inter.variable} d-flex flex-column`}>
        <Providers>{children}</Providers>
      </body>
    </html>
  );
}
