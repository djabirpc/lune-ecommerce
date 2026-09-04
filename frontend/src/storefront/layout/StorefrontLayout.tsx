import { useEffect, useState } from 'react';
import { Link, Outlet, useLocation } from 'react-router-dom';

import { useCart } from '../../lib/cart/CartContext';
import { captureAttributionOnLoad } from '../../lib/marketing/attribution';
import { initPixels, trackEvent } from '../../lib/marketing/pixels';

const NAV_LINKS = [
  { to: '/categories', label: 'Catégories' },
  { to: '/promotions', label: 'Promotions' },
  { to: '/track-order', label: 'Suivre ma commande' },
];

const INSTAGRAM_URL = import.meta.env.VITE_INSTAGRAM_URL as string | undefined;
const TIKTOK_URL = import.meta.env.VITE_TIKTOK_URL as string | undefined;

function MenuIcon() {
  return (
    <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" aria-hidden="true">
      <path d="M3 6h18M3 12h18M3 18h18" strokeLinecap="round" />
    </svg>
  );
}

function CloseIcon() {
  return (
    <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" aria-hidden="true">
      <path d="M5 5l14 14M19 5L5 19" strokeLinecap="round" />
    </svg>
  );
}

function BagIcon() {
  return (
    <svg width="21" height="21" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" aria-hidden="true">
      <path d="M6 8h12l-1 12.5a1 1 0 0 1-1 .9H8a1 1 0 0 1-1-.9L6 8Z" strokeLinejoin="round" />
      <path d="M9 8V6a3 3 0 0 1 6 0v2" strokeLinecap="round" />
    </svg>
  );
}

export function StorefrontLayout() {
  const { itemCount } = useCart();
  const location = useLocation();
  const [isMenuOpen, setIsMenuOpen] = useState(false);

  useEffect(() => {
    captureAttributionOnLoad();
    initPixels();
  }, []);

  useEffect(() => {
    trackEvent('PAGE_VIEW');
  }, [location.pathname]);

  useEffect(() => {
    setIsMenuOpen(false);
  }, [location.pathname]);

  useEffect(() => {
    document.body.style.overflow = isMenuOpen ? 'hidden' : '';
    return () => {
      document.body.style.overflow = '';
    };
  }, [isMenuOpen]);

  return (
    <div className="flex min-h-screen flex-col bg-white">
      <div className="bg-luna-black py-2 text-center text-[11px] tracking-wide text-white sm:text-xs">
        Livraison partout en Algérie • Paiement à la livraison
      </div>

      <header className="sticky top-0 z-40 border-b border-black/5 bg-white/95 backdrop-blur supports-[backdrop-filter]:bg-white/80">
        <div className="mx-auto flex max-w-6xl items-center justify-between px-4 py-3.5 sm:px-6 lg:px-8">
          <button
            type="button"
            onClick={() => setIsMenuOpen(true)}
            className="-ml-2 p-2 text-luna-black sm:hidden"
            aria-label="Ouvrir le menu"
          >
            <MenuIcon />
          </button>

          <Link to="/" className="font-display text-2xl italic tracking-tight text-luna-black">
            Luna.
          </Link>

          <nav className="hidden items-center gap-8 text-sm font-medium text-luna-charcoal sm:flex">
            {NAV_LINKS.map((link) => (
              <Link key={link.to} to={link.to} className="transition-colors hover:text-luna-accent">
                {link.label}
              </Link>
            ))}
          </nav>

          <Link to="/cart" className="relative -mr-2 p-2 text-luna-black" aria-label="Voir le panier">
            <BagIcon />
            {itemCount > 0 && (
              <span className="absolute right-0.5 top-0.5 flex h-4 w-4 items-center justify-center rounded-full bg-luna-accent text-[10px] font-semibold text-white">
                {itemCount > 9 ? '9+' : itemCount}
              </span>
            )}
          </Link>
        </div>
      </header>

      {/* Mobile nav drawer */}
      <div
        className={`fixed inset-0 z-50 transition-opacity sm:hidden ${
          isMenuOpen ? 'pointer-events-auto opacity-100' : 'pointer-events-none opacity-0'
        }`}
        aria-hidden={!isMenuOpen}
      >
        <div className="absolute inset-0 bg-luna-black/40" onClick={() => setIsMenuOpen(false)} />
        <div
          className={`absolute inset-y-0 left-0 flex w-72 max-w-[85vw] flex-col bg-white shadow-xl transition-transform duration-300 ${
            isMenuOpen ? 'translate-x-0' : '-translate-x-full'
          }`}
        >
          <div className="flex items-center justify-between border-b border-black/5 px-5 py-4">
            <span className="font-display text-xl italic">Luna.</span>
            <button type="button" onClick={() => setIsMenuOpen(false)} className="p-2 text-luna-black" aria-label="Fermer le menu">
              <CloseIcon />
            </button>
          </div>
          <nav className="flex flex-col gap-1 px-3 py-4 text-base">
            {NAV_LINKS.map((link) => (
              <Link key={link.to} to={link.to} className="rounded-lg px-3 py-3 text-luna-charcoal hover:bg-luna-cream">
                {link.label}
              </Link>
            ))}
          </nav>
        </div>
      </div>

      <main className="flex-1">
        <Outlet />
      </main>

      <footer className="border-t border-black/5 bg-luna-cream">
        <div className="mx-auto grid max-w-6xl gap-8 px-4 py-12 sm:grid-cols-3 sm:px-6 lg:px-8">
          <div>
            <p className="font-display text-xl italic">Luna.</p>
            <p className="mt-2 text-sm text-luna-charcoal/70">La mode qui vous ressemble.</p>
            {(INSTAGRAM_URL || TIKTOK_URL) && (
              <div className="mt-4 flex gap-3 text-sm">
                {INSTAGRAM_URL && (
                  <a href={INSTAGRAM_URL} target="_blank" rel="noreferrer" className="text-luna-charcoal/70 hover:text-luna-accent">
                    Instagram
                  </a>
                )}
                {TIKTOK_URL && (
                  <a href={TIKTOK_URL} target="_blank" rel="noreferrer" className="text-luna-charcoal/70 hover:text-luna-accent">
                    TikTok
                  </a>
                )}
              </div>
            )}
          </div>

          <div>
            <p className="text-xs font-semibold uppercase tracking-wide text-luna-charcoal/50">Boutique</p>
            <div className="mt-3 flex flex-col gap-2 text-sm text-luna-charcoal/70">
              <Link to="/categories" className="hover:text-luna-accent">Catégories</Link>
              <Link to="/promotions" className="hover:text-luna-accent">Promotions</Link>
              <Link to="/track-order" className="hover:text-luna-accent">Suivre ma commande</Link>
            </div>
          </div>

          <div>
            <p className="text-xs font-semibold uppercase tracking-wide text-luna-charcoal/50">Livraison &amp; paiement</p>
            <div className="mt-3 flex flex-col gap-2 text-sm text-luna-charcoal/70">
              <span>Livraison partout en Algérie</span>
              <span>Paiement à la livraison</span>
              <span>Échange facile sous 7 jours</span>
            </div>
          </div>
        </div>

        <div className="border-t border-black/5 px-4 py-5 text-center text-xs text-luna-charcoal/50">
          © {new Date().getFullYear()} Luna. Tous droits réservés.
        </div>
      </footer>
    </div>
  );
}
