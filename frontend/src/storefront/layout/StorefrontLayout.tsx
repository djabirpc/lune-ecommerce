import { useEffect, useState } from 'react';
import { Link, Outlet, useLocation } from 'react-router-dom';
import { Menu, X, Search, Heart, ShoppingBag, AtSign, Phone, Truck, ShieldCheck, RotateCcw } from 'lucide-react';
import { useTranslation } from 'react-i18next';

import { useCart } from '../../lib/cart/CartContext';
import { useFavorites } from '../../lib/favorites/FavoritesContext';
import { captureAttributionOnLoad } from '../../lib/marketing/attribution';
import { initPixels, trackEvent } from '../../lib/marketing/pixels';
import { catalogApi } from '../../lib/api/catalog';
import { useQuery } from '@tanstack/react-query';
import { MobileTabBar } from './MobileTabBar';
import { LanguageSwitcher } from '../../lib/i18n/LanguageSwitcher';

const NAV_LINKS_HIDDEN_BAR = [/^\/product\//, /^\/cart$/, /^\/checkout$/];

const INSTAGRAM_URL = import.meta.env.VITE_INSTAGRAM_URL as string | undefined;
const TIKTOK_URL = import.meta.env.VITE_TIKTOK_URL as string | undefined;
const FACEBOOK_URL = import.meta.env.VITE_FACEBOOK_URL as string | undefined;
const PHONE_NUMBER = import.meta.env.VITE_STORE_PHONE as string | undefined;

export function StorefrontLayout() {
  const { t } = useTranslation();
  const { itemCount } = useCart();
  const { favorites } = useFavorites();
  const location = useLocation();
  const [isMenuOpen, setIsMenuOpen] = useState(false);

  const { data: categories } = useQuery({
    queryKey: ['nav-categories'],
    queryFn: () => catalogApi.getCategories(),
  });

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

  const showsTabBar = !NAV_LINKS_HIDDEN_BAR.some((pattern) => pattern.test(location.pathname));

  return (
    <div className="flex min-h-screen flex-col bg-luna-cream">
      <header className="sticky top-0 z-40 border-b border-black/10 bg-luna-cream/90 backdrop-blur supports-[backdrop-filter]:bg-luna-cream/80">
        <div className="bg-luna-black py-2 text-center text-[11px] tracking-[0.16em] text-white uppercase">
          {t('layout.announcementBar')}
        </div>
        <div className="mx-auto flex h-14 max-w-6xl items-center gap-3 px-4 sm:px-6 lg:px-8">
          <button
            type="button"
            onClick={() => setIsMenuOpen(true)}
            className="-ms-2 p-2 text-luna-black sm:hidden"
            aria-label={t('layout.openMenu')}
          >
            <Menu className="h-5 w-5" />
          </button>

          <Link to="/" className="font-display text-2xl tracking-[0.25em] text-luna-black uppercase sm:text-3xl">
            Luna
          </Link>

          <nav className="ms-8 hidden items-center gap-6 text-sm sm:flex">
            {categories?.slice(0, 5).map((c) => (
              <Link key={c.id} to={`/category/${c.slug}`} className="transition-colors hover:text-luna-accent">
                {c.name}
              </Link>
            ))}
            <Link to="/promotions" className="text-luna-accent transition-opacity hover:opacity-70">
              {t('layout.promos')}
            </Link>
          </nav>

          <div className="ms-auto flex items-center gap-1">
            <LanguageSwitcher className="hidden sm:flex" />
            <Link to="/categories" className="p-2 text-luna-black" aria-label={t('layout.browseShop')}>
              <Search className="h-5 w-5" />
            </Link>
            <Link to="/favoris" className="relative hidden p-2 text-luna-black sm:block" aria-label={t('layout.favorites')}>
              <Heart className="h-5 w-5" />
              {favorites.length > 0 && <span className="absolute top-0.5 end-0.5 h-2 w-2 rounded-full bg-luna-accent" />}
            </Link>
            <Link to="/cart" className="relative p-2 text-luna-black" aria-label={t('layout.viewCart')}>
              <ShoppingBag className="h-5 w-5" />
              {itemCount > 0 && (
                <span className="absolute -top-0.5 -end-0.5 flex h-4 min-w-4 items-center justify-center rounded-full bg-luna-accent px-1 text-[10px] font-medium text-white">
                  {itemCount > 9 ? '9+' : itemCount}
                </span>
              )}
            </Link>
          </div>
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
          className={`absolute inset-y-0 start-0 flex w-72 max-w-[85vw] flex-col bg-white shadow-xl transition-transform duration-300 ${
            isMenuOpen ? 'translate-x-0' : '-translate-x-full rtl:translate-x-full'
          }`}
        >
          <div className="flex items-center justify-between border-b border-black/10 px-5 py-4">
            <span className="font-display text-xl tracking-[0.2em] uppercase">Luna</span>
            <button type="button" onClick={() => setIsMenuOpen(false)} className="p-2 text-luna-black" aria-label={t('layout.closeMenu')}>
              <X className="h-5 w-5" />
            </button>
          </div>
          <nav className="flex flex-col gap-1 px-3 py-6 text-lg">
            <span className="eyebrow px-3 mb-2">{t('layout.collections')}</span>
            <Link to="/" className="rounded-lg px-3 py-2 text-luna-charcoal hover:bg-luna-cream">
              {t('layout.home')}
            </Link>
            {categories?.map((c) => (
              <Link key={c.id} to={`/category/${c.slug}`} className="rounded-lg px-3 py-2 text-luna-charcoal hover:bg-luna-cream">
                {c.name}
              </Link>
            ))}
            <Link to="/promotions" className="rounded-lg px-3 py-2 text-luna-accent hover:bg-luna-cream">
              {t('layout.promotions')}
            </Link>
            <Link to="/orders" className="rounded-lg px-3 py-2 text-luna-charcoal hover:bg-luna-cream">
              {t('layout.myOrders')}
            </Link>
            <Link to="/track-order" className="rounded-lg px-3 py-2 text-luna-charcoal hover:bg-luna-cream">
              {t('layout.trackOrder')}
            </Link>
            <Link to="/favoris" className="rounded-lg px-3 py-2 text-luna-charcoal hover:bg-luna-cream">
              {t('layout.myFavorites')}
            </Link>
            <Link to="/account" className="rounded-lg px-3 py-2 text-luna-charcoal hover:bg-luna-cream">
              {t('layout.myAccount')}
            </Link>
            <div className="mt-4 border-t border-black/10 px-3 pt-4">
              <LanguageSwitcher />
            </div>
          </nav>
        </div>
      </div>

      <main className={`flex-1 ${showsTabBar ? 'pb-16 sm:pb-0' : ''}`}>
        <Outlet />
      </main>

      <MobileTabBar />

      <footer className="mt-8 border-t border-black/10 bg-luna-cream-dark/60">
        <div className="mx-auto grid max-w-6xl gap-8 border-b border-black/10 px-4 py-8 sm:grid-cols-3 sm:px-6 lg:px-8">
          <Feature icon={<Truck className="h-5 w-5" />} title={t('layout.footer.deliveryTitle')} text={t('layout.footer.deliveryText')} />
          <Feature icon={<ShieldCheck className="h-5 w-5" />} title={t('layout.footer.codTitle')} text={t('layout.footer.codText')} />
          <Feature icon={<RotateCcw className="h-5 w-5" />} title={t('layout.footer.exchangeTitle')} text={t('layout.footer.exchangeText')} />
        </div>

        <div className="mx-auto grid max-w-6xl gap-8 px-4 py-10 sm:grid-cols-3 sm:px-6 lg:px-8">
          <div>
            <span className="font-display text-2xl tracking-[0.25em] text-luna-black uppercase">Luna</span>
            <p className="mt-3 max-w-xs text-sm text-luna-charcoal/70">{t('layout.footer.tagline')}</p>
            {(INSTAGRAM_URL || TIKTOK_URL || FACEBOOK_URL || PHONE_NUMBER) && (
              <div className="mt-4 flex flex-wrap gap-3 text-sm">
                {INSTAGRAM_URL && (
                  <a href={INSTAGRAM_URL} target="_blank" rel="noreferrer" className="flex items-center gap-1.5 rounded-full border border-black/10 px-3 py-1.5 text-luna-black">
                    <AtSign className="h-3.5 w-3.5" /> Instagram
                  </a>
                )}
                {TIKTOK_URL && (
                  <a href={TIKTOK_URL} target="_blank" rel="noreferrer" className="flex items-center gap-1.5 rounded-full border border-black/10 px-3 py-1.5 text-luna-black">
                    <AtSign className="h-3.5 w-3.5" /> TikTok
                  </a>
                )}
                {FACEBOOK_URL && (
                  <a href={FACEBOOK_URL} target="_blank" rel="noreferrer" className="flex items-center gap-1.5 rounded-full border border-black/10 px-3 py-1.5 text-luna-black">
                    <AtSign className="h-3.5 w-3.5" /> Facebook
                  </a>
                )}
                {PHONE_NUMBER && (
                  <a href={`tel:${PHONE_NUMBER}`} className="flex items-center gap-1.5 rounded-full border border-black/10 px-3 py-1.5 text-luna-black">
                    <Phone className="h-3.5 w-3.5" /> {t('layout.footer.call')}
                  </a>
                )}
              </div>
            )}
          </div>

          <div>
            <span className="eyebrow">{t('layout.collections')}</span>
            <ul className="mt-3 space-y-2 text-sm">
              {categories?.map((c) => (
                <li key={c.id}>
                  <Link to={`/category/${c.slug}`} className="text-luna-charcoal/70 hover:text-luna-black">
                    {c.name}
                  </Link>
                </li>
              ))}
            </ul>
          </div>

          <div>
            <span className="eyebrow">{t('layout.footer.help')}</span>
            <ul className="mt-3 space-y-2 text-sm text-luna-charcoal/70">
              <li>
                <Link to="/track-order" className="hover:text-luna-black">
                  {t('layout.footer.trackMyOrder')}
                </Link>
              </li>
              <li>
                <Link to="/promotions" className="hover:text-luna-black">
                  {t('layout.promotions')}
                </Link>
              </li>
            </ul>
          </div>
        </div>

        <div className="border-t border-black/10 px-4 py-5 text-center text-xs text-luna-charcoal/60">
          {t('layout.footer.copyright', { year: new Date().getFullYear() })}
        </div>
      </footer>
    </div>
  );
}

function Feature({ icon, title, text }: { icon: React.ReactNode; title: string; text: string }) {
  return (
    <div className="flex gap-3">
      <span className="text-luna-accent">{icon}</span>
      <div>
        <p className="text-sm font-medium text-luna-black">{title}</p>
        <p className="text-xs text-luna-charcoal/60">{text}</p>
      </div>
    </div>
  );
}
