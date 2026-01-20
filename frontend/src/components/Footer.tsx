import Link from 'next/link';

export default function Footer() {
    const currentYear = new Date().getFullYear();

    return (
        <footer className="bg-slate-900 text-white mt-auto">
            <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
                <div className="grid grid-cols-1 md:grid-cols-4 gap-8">
                    {/* Brand */}
                    <div className="col-span-1 md:col-span-2">
                        <Link href="/" className="flex items-center gap-2 mb-4">
                            <div className="w-10 h-10 bg-gradient-to-br from-orange-500 to-orange-600 rounded-lg flex items-center justify-center">
                                <i className="bi bi-code-slash text-white text-xl"></i>
                            </div>
                            <span className="font-bold text-xl">
                                Dev<span className="text-orange-500">Community</span>
                            </span>
                        </Link>
                        <p className="text-gray-400 max-w-sm">
                            Nền tảng kết nối lập trình viên, nơi chia sẻ mã nguồn và giải đáp thắc mắc kỹ thuật từ cộng đồng.
                        </p>
                        <div className="flex gap-4 mt-4">
                            <a href="#" className="w-10 h-10 bg-slate-800 rounded-full flex items-center justify-center hover:bg-orange-500 transition">
                                <i className="bi bi-github"></i>
                            </a>
                            <a href="#" className="w-10 h-10 bg-slate-800 rounded-full flex items-center justify-center hover:bg-orange-500 transition">
                                <i className="bi bi-twitter-x"></i>
                            </a>
                            <a href="#" className="w-10 h-10 bg-slate-800 rounded-full flex items-center justify-center hover:bg-orange-500 transition">
                                <i className="bi bi-discord"></i>
                            </a>
                        </div>
                    </div>

                    {/* Quick Links */}
                    <div>
                        <h3 className="font-semibold text-lg mb-4">Quick Links</h3>
                        <ul className="space-y-2">
                            <li>
                                <Link href="/questions" className="text-gray-400 hover:text-orange-500 transition">
                                    Questions
                                </Link>
                            </li>
                            <li>
                                <Link href="/tags" className="text-gray-400 hover:text-orange-500 transition">
                                    Tags
                                </Link>
                            </li>
                            <li>
                                <Link href="/users" className="text-gray-400 hover:text-orange-500 transition">
                                    Users
                                </Link>
                            </li>
                            <li>
                                <Link href="/repositories" className="text-gray-400 hover:text-orange-500 transition">
                                    Repositories
                                </Link>
                            </li>
                        </ul>
                    </div>

                    {/* Resources */}
                    <div>
                        <h3 className="font-semibold text-lg mb-4">Resources</h3>
                        <ul className="space-y-2">
                            <li>
                                <Link href="/about" className="text-gray-400 hover:text-orange-500 transition">
                                    About Us
                                </Link>
                            </li>
                            <li>
                                <Link href="/help" className="text-gray-400 hover:text-orange-500 transition">
                                    Help Center
                                </Link>
                            </li>
                            <li>
                                <Link href="/privacy" className="text-gray-400 hover:text-orange-500 transition">
                                    Privacy Policy
                                </Link>
                            </li>
                            <li>
                                <Link href="/terms" className="text-gray-400 hover:text-orange-500 transition">
                                    Terms of Service
                                </Link>
                            </li>
                        </ul>
                    </div>
                </div>

                {/* Bottom */}
                <div className="border-t border-slate-800 mt-8 pt-8 flex flex-col md:flex-row justify-between items-center">
                    <p className="text-gray-400 text-sm">
                        © {currentYear} DevCommunity. All rights reserved.
                    </p>
                    <p className="text-gray-400 text-sm mt-2 md:mt-0">
                        Made with <span className="text-red-500">❤</span> by developers, for developers.
                    </p>
                </div>
            </div>
        </footer>
    );
}
