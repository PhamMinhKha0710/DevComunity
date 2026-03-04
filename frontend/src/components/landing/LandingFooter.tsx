import Link from 'next/link';
import Image from 'next/image';

export default function LandingFooter() {
  return (
    <footer className="landing-footer">
      <div className="landing-footer-grid">
        {/* Brand */}
        <div className="landing-footer-brand">
          <div className="landing-brand">
            <div className="landing-brand-icon">
              <Image src="/logo.png" alt="SocialTechsy" width={28} height={28} />
            </div>
            <h2 className="landing-brand-text">SocialTechsy</h2>
          </div>
          <p className="landing-footer-desc">
            Empowering developers to build the future through collaborative learning and knowledge sharing.
          </p>
        </div>

        {/* Community */}
        <div className="landing-footer-column">
          <h4 className="landing-footer-heading">Community</h4>
          <ul className="landing-footer-links">
            <li><Link href="/questions">Questions</Link></li>
            <li><Link href="/tags">Tags</Link></li>
            <li><Link href="/users">Users</Link></li>
            <li><Link href="#">Help Center</Link></li>
          </ul>
        </div>

        {/* Company */}
        <div className="landing-footer-column">
          <h4 className="landing-footer-heading">Company</h4>
          <ul className="landing-footer-links">
            <li><Link href="#">About Us</Link></li>
            <li><Link href="#">Careers</Link></li>
            <li><Link href="#">Privacy Policy</Link></li>
            <li><Link href="#">Terms of Service</Link></li>
          </ul>
        </div>

        {/* Follow Us */}
        <div className="landing-footer-column">
          <h4 className="landing-footer-heading">Follow Us</h4>
          <div className="landing-footer-social">
            <a href="#" className="landing-social-icon">
              <span className="material-symbols-outlined">public</span>
            </a>
            <a href="#" className="landing-social-icon">
              <span className="material-symbols-outlined">alternate_email</span>
            </a>
            <a href="#" className="landing-social-icon">
              <span className="material-symbols-outlined">code</span>
            </a>
          </div>
        </div>
      </div>

      {/* Bottom bar */}
      <div className="landing-footer-bottom">
        <p>&copy; 2026 SocialTechsy Community. All rights reserved.</p>
        <div className="landing-footer-bottom-links">
          <a href="#">Status</a>
          <a href="#">Security</a>
          <a href="#">API</a>
        </div>
      </div>
    </footer>
  );
}
