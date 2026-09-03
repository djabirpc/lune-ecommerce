import { Link, Outlet, useNavigate } from 'react-router-dom';

import { useAdminAuth } from '../../lib/auth/AdminAuthContext';
import { ROLE_LABELS } from '../../lib/format/roleLabels';

const NAV_LINKS = [
  { to: '/admin/dashboard', label: 'Tableau de bord' },
  { to: '/admin/orders', label: 'Commandes' },
  { to: '/admin/orders/confirmation', label: 'Confirmation' },
  { to: '/admin/products', label: 'Produits' },
  { to: '/admin/inventory', label: 'Stock' },
  { to: '/admin/promotions', label: 'Promotions' },
  { to: '/admin/customers', label: 'Clients' },
  { to: '/admin/shipping', label: 'Livraison' },
  { to: '/admin/marketing', label: 'Marketing' },
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

      <div className="flex flex-1 flex-col">
        <header className="flex items-center justify-between border-b border-black/10 bg-white px-6 py-3 text-sm">
          <span className="text-luna-charcoal/70">
            {user?.firstName} {user?.lastName} · {user?.roles.map((r) => ROLE_LABELS[r] ?? r).join(', ')}
          </span>
          <button type="button" onClick={handleLogout} className="text-luna-charcoal/70 underline">
            Déconnexion
          </button>
        </header>

        <main className="flex-1 p-6">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
