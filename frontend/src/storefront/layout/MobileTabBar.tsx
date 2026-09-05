import { Link, useLocation } from 'react-router-dom';
import { Home, LayoutGrid, Tag, ShoppingBag, User } from 'lucide-react';

import { useCart } from '../../lib/cart/CartContext';

const TAB_CLASS = 'flex flex-1 flex-col items-center gap-1 py-2 text-[10px] text-luna-charcoal/60';
const ACTIVE_CLASS = `${TAB_CLASS} text-luna-black`;

// Hidden on the focused single-item conversion flow, where the page's own sticky CTA bar owns the
// bottom of the screen instead (ProductPage, CartPage, CheckoutPage).
const HIDDEN_ON = [/^\/product\//, /^\/cart$/, /^\/checkout$/];

export function MobileTabBar() {
  const location = useLocation();
  const { itemCount } = useCart();

  if (HIDDEN_ON.some((pattern) => pattern.test(location.pathname))) {
    return null;
  }

  const isActive = (path: string) => (path === '/' ? location.pathname === '/' : location.pathname.startsWith(path));

  return (
    <nav
      aria-label="Navigation mobile"
      className="fixed inset-x-0 bottom-0 z-40 border-t border-black/10 bg-white/95 pb-[env(safe-area-inset-bottom)] backdrop-blur sm:hidden"
    >
      <div className="flex">
        <Link to="/" className={isActive('/') ? ACTIVE_CLASS : TAB_CLASS}>
          <Home className="h-5 w-5" /> Accueil
        </Link>
        <Link to="/categories" className={isActive('/categories') || isActive('/category') ? ACTIVE_CLASS : TAB_CLASS}>
          <LayoutGrid className="h-5 w-5" /> Boutique
        </Link>
        <Link to="/promotions" className={isActive('/promotions') ? ACTIVE_CLASS : TAB_CLASS}>
          <Tag className="h-5 w-5" /> Promos
        </Link>
        <Link to="/cart" className={`${isActive('/cart') ? ACTIVE_CLASS : TAB_CLASS} relative`}>
          <ShoppingBag className="h-5 w-5" />
          {itemCount > 0 && (
            <span className="absolute top-0.5 right-[26%] flex h-4 min-w-4 items-center justify-center rounded-full bg-luna-accent px-1 text-[10px] text-white">
              {itemCount > 9 ? '9+' : itemCount}
            </span>
          )}
          Panier
        </Link>
        <Link to="/account" className={isActive('/account') ? ACTIVE_CLASS : TAB_CLASS}>
          <User className="h-5 w-5" /> Compte
        </Link>
      </div>
    </nav>
  );
}
