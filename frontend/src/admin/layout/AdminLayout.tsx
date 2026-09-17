import { Link, Outlet, useNavigate } from 'react-router-dom';

import { useAdminAuth } from '../../lib/auth/AdminAuthContext';
import { ROLE_LABELS } from '../../lib/format/roleLabels';

const NAV_LINKS = [
  { to: '/admin/dashboard', label: 'Tableau de bord' },
  { to: '/admin/orders', label: 'Commandes' },
  { to: '/admin/orders/confirmation', label: 'Confirmation' },
  { to: '/admin/categories', label: 'Catégories' },
  { to: '/admin/products', label: 'Produits' },
  { to: '/admin/inventory', label: 'Stock' },
  { to: '/admin/suppliers', label: 'Fournisseurs' },
  { to: '/admin/promotions', label: 'Promotions' },
  { to: '/admin/customers', label: 'Clients' },
  { to: '/admin/shipping', label: 'Livraison' },
  { to: '/admin/marketing', label: 'Marketing' },
  { to: '/admin/homepage', label: "Page d'accueil" },
  { to: '/admin/users', label: 'Utilisateurs' },
  { to: '/admin/settings', label: 'Paramètres' },
];

export function AdminLayout() {
  const { user, logout } = useAdminAuth();
  const navigate = useNavigate();

  async function handleLogout() {
    await logout();
    navigate('/admin/login', { replace: true });
  }

  return (
    <div className="flex min-h-screen bg-luna-cream">
      <aside className="hidden w-56 shrink-0 border-r border-black/10 bg-white p-4 sm:block">
        <div className="mb-6 text-lg font-semibold">Luna Admin</div>
        <nav className="flex flex-col gap-1 text-sm">
          {NAV_LINKS.map((link) => (
            <Link key={link.to} to={link.to} className="rounded px-3 py-2 hover:bg-luna-cream">
              {link.label}
            </Link>
          ))}
        </nav>
      </aside>

      {/* min-w-0 overrides the flex item's default min-width:auto — without it, a wide child (e.g.
          OrdersPage's table, even inside its own overflow-x-auto wrapper) can force this whole column
          wider than the viewport instead of scrolling internally, which on mobile also drags any
          position:fixed overlay's "100vw" (like Drawer's) off past the right edge of the screen with it. */}
      <div className="flex min-w-0 flex-1 flex-col">
        <header className="flex items-center justify-between border-b border-black/10 bg-white px-6 py-3 text-sm">
          <span className="text-luna-charcoal/70">
            {user?.firstName} {user?.lastName} · {user?.roles.map((r) => ROLE_LABELS[r] ?? r).join(', ')}
          </span>
          <button type="button" onClick={handleLogout} className="text-luna-charcoal/70 underline">
            Déconnexion
          </button>
        </header>

        <main className="min-w-0 flex-1 p-6">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
