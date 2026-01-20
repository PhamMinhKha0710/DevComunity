import type { Metadata } from "next";
import { Inter } from "next/font/google";
import { Providers } from "@/lib/contexts/Providers";
import Navbar from "@/components/Navbar";
import Footer from "@/components/Footer";
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
    <html lang="en" data-theme="light" suppressHydrationWarning>
      <head>
        {/* Google Fonts */}
        <link rel="preconnect" href="https://fonts.googleapis.com" />
        <link rel="preconnect" href="https://fonts.gstatic.com" crossOrigin="anonymous" />
        <link href="https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700&family=Fira+Code:wght@400;500;600&display=swap" rel="stylesheet" />

        {/* Bootstrap Icons (icons only, no framework) */}
        <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.1/font/bootstrap-icons.css" rel="stylesheet" />

        {/* Prism.js for code highlighting */}
        <link href="https://cdnjs.cloudflare.com/ajax/libs/prism/1.29.0/themes/prism-tomorrow.min.css" rel="stylesheet" />
      </head>
      <body className={`${inter.variable} flex flex-col min-h-screen bg-gray-50 dark:bg-slate-900`}>
        <Providers>
          <Navbar />
          <main className="flex-1">
            {children}
          </main>
          <Footer />
        </Providers>
      </body>
    </html>
  );
}

