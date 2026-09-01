import { Link, Outlet } from 'react-router-dom';

const NAV_LINKS = [
  { to: '/admin/dashboard', label: 'Tableau de bord' },
  { to: '/admin/orders', label: 'Commandes' },
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

      <main className="flex-1 p-6">
        <Outlet />
      </main>
    </div>
  );
}
