import { Link, Outlet } from 'react-router-dom';

const NAV_LINKS = [
  { to: '/categories', label: 'Catégories' },
  { to: '/promotions', label: 'Promotions' },
  { to: '/track-order', label: 'Suivre ma commande' },
];

export function StorefrontLayout() {
  return (
    <div className="flex min-h-screen flex-col bg-white">
      <div className="bg-luna-black py-2 text-center text-xs text-white">
        Livraison partout en Algérie • Paiement à la livraison
      </div>

      <header className="flex items-center justify-between border-b border-black/5 px-4 py-4">
        <Link to="/" className="text-lg font-semibold tracking-wide">
          Luna.
        </Link>

        <nav className="flex items-center gap-4 text-sm">
          {NAV_LINKS.map((link) => (
            <Link key={link.to} to={link.to} className="hover:text-luna-accent">
              {link.label}
            </Link>
          ))}
          <Link to="/cart" className="hover:text-luna-accent">
            Panier
          </Link>
        </nav>
      </header>

      <main className="flex-1">
        <Outlet />
      </main>

      <footer className="border-t border-black/5 px-4 py-8 text-center text-xs text-luna-charcoal/60">
        © {new Date().getFullYear()} Luna. Tous droits réservés.
      </footer>
    </div>
  );
}
