import Link from 'next/link';

export default function Footer() {
    return (
        <footer className="footer-gradient text-white mt-auto">
            <div className="container py-5">
                <div className="row g-4">
                    {/* Brand Column */}
                    <div className="col-lg-4">
                        <div className="d-flex align-items-center mb-3">
                            <div className="brand-logo-container me-2">
                                <i className="bi bi-code-square fs-4"></i>
                            </div>
                            <span className="fs-4 fw-bold">DevCommunity</span>
                        </div>
                        <p className="text-white-50 mb-4">
                            Nền tảng kết nối lập trình viên, nơi chia sẻ mã nguồn và giải đáp thắc mắc kỹ thuật từ cộng đồng.
                        </p>
                        <div className="d-flex gap-2">
                            <a href="#" className="social-link"><i className="bi bi-facebook"></i></a>
                            <a href="#" className="social-link"><i className="bi bi-twitter-x"></i></a>
                            <a href="#" className="social-link"><i className="bi bi-github"></i></a>
                            <a href="#" className="social-link"><i className="bi bi-linkedin"></i></a>
                        </div>
                    </div>

                    {/* Quick Links */}
                    <div className="col-6 col-lg-2">
                        <h6 className="fw-bold mb-3">Quick Links</h6>
                        <ul className="list-unstyled footer-links">
                            <li className="mb-2"><Link href="/" className="footer-link">Home</Link></li>
                            <li className="mb-2"><Link href="/questions" className="footer-link">Questions</Link></li>
                            <li className="mb-2"><Link href="/tags" className="footer-link">Tags</Link></li>
                            <li className="mb-2"><Link href="/users" className="footer-link">Users</Link></li>
                        </ul>
                    </div>

                    {/* Resources */}
                    <div className="col-6 col-lg-2">
                        <h6 className="fw-bold mb-3">Resources</h6>
                        <ul className="list-unstyled footer-links">
                            <li className="mb-2"><a href="#" className="footer-link">Documentation</a></li>
                            <li className="mb-2"><a href="#" className="footer-link">API Reference</a></li>
                            <li className="mb-2"><a href="#" className="footer-link">Privacy Policy</a></li>
                            <li className="mb-2"><a href="#" className="footer-link">Terms of Service</a></li>
                        </ul>
                    </div>

                    {/* Contact */}
                    <div className="col-lg-4">
                        <h6 className="fw-bold mb-3">Stay Connected</h6>
                        <p className="text-white-50 small mb-3">Subscribe to our newsletter for updates</p>
                        <form className="d-flex">
                            <input
                                type="email"
                                className="form-control footer-input rounded-start"
                                placeholder="Enter your email"
                            />
                            <button type="submit" className="btn subscribe-btn">
                                <i className="bi bi-send"></i>
                            </button>
                        </form>
                        <div className="mt-4">
                            <div className="d-flex align-items-center mb-2">
                                <div className="icon-box me-3">
                                    <i className="bi bi-envelope"></i>
                                </div>
                                <span className="text-white-50 small">support@devcommunity.com</span>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            {/* Copyright */}
            <div className="border-top border-white-10">
                <div className="container py-3">
                    <div className="d-flex flex-column flex-md-row justify-content-between align-items-center">
                        <p className="text-white-50 small mb-0">
                            © 2025 DevCommunity. All rights reserved.
                        </p>
                        <div className="d-flex gap-3 mt-2 mt-md-0">
                            <a href="#" className="text-white-50 small text-decoration-none">Privacy</a>
                            <a href="#" className="text-white-50 small text-decoration-none">Terms</a>
                            <a href="#" className="text-white-50 small text-decoration-none">Cookies</a>
                        </div>
                    </div>
                </div>
            </div>
        </footer>
    );
}
