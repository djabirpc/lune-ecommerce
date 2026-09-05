import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { Heart, Package, User } from 'lucide-react';

import { ordersApi } from '../../lib/api/orders';
import { catalogApi } from '../../lib/api/catalog';
import { getOrderHistory } from '../../lib/orders/localOrderHistory';
import { getSavedCustomerInfo } from '../../lib/customer/savedCustomerInfo';
import { useFavorites } from '../../lib/favorites/FavoritesContext';
import { formatPrice } from '../../lib/format/price';
import { ORDER_STATUS_LABELS } from '../../lib/format/orderLabels';
import { ProductCard } from '../../lib/components/ProductCard';

export function AccountPage() {
  const customer = getSavedCustomerInfo();
  const history = getOrderHistory().slice(0, 3);
  const { favorites } = useFavorites();

  const { data: recentOrders } = useQuery({
    queryKey: ['account-recent-orders', history.map((h) => h.orderNumber).join(',')],
    queryFn: async () => {
      const results = await Promise.allSettled(history.map((entry) => ordersApi.track(entry.orderNumber, entry.phone)));
      return results.filter((r) => r.status === 'fulfilled').map((r) => r.value);
    },
    enabled: history.length > 0,
  });

  const { data: products } = useQuery({
    queryKey: ['products', { pageSize: 100 }],
    queryFn: () => catalogApi.getProducts({ pageSize: 100 }),
    enabled: favorites.length > 0,
  });
  const favoriteProducts = (products?.items ?? []).filter((p) => favorites.includes(p.id));

  return (
    <div className="mx-auto max-w-5xl px-4 py-8">
      <p className="eyebrow">Mon espace</p>
      <h1 className="mt-1 font-display text-4xl text-luna-black">Mon compte</h1>

      <section className="mt-8 rounded-sm border border-black/10 bg-white p-5">
        <h2 className="flex items-center gap-2 font-display text-xl text-luna-black">
          <User className="h-4 w-4" /> Informations de livraison
        </h2>
        {customer ? (
          <div className="mt-3 text-sm text-luna-black">
            <p>
              {customer.firstName} {customer.lastName}
            </p>
            <p className="text-luna-charcoal/60">{customer.phone}</p>
            <p className="text-luna-charcoal/60">
              {customer.address}, {customer.commune}, {customer.wilaya}
            </p>
          </div>
        ) : (
          <p className="mt-3 text-sm text-luna-charcoal/60">Vos informations seront enregistrées lors de votre première commande.</p>
        )}
      </section>

      <section className="mt-6 rounded-sm border border-black/10 bg-white p-5">
        <div className="flex items-center justify-between">
          <h2 className="flex items-center gap-2 font-display text-xl text-luna-black">
            <Package className="h-4 w-4" /> Dernières commandes
          </h2>
          <Link to="/orders" className="text-xs text-luna-charcoal/60 underline underline-offset-2 hover:text-luna-black">
            Tout voir
          </Link>
        </div>
        {!recentOrders || recentOrders.length === 0 ? (
          <p className="mt-3 text-sm text-luna-charcoal/60">Aucune commande pour le moment.</p>
        ) : (
          <ul className="mt-3 divide-y divide-black/10">
            {recentOrders.map((order) => (
              <li key={order.id} className="flex items-center justify-between gap-3 py-3 text-sm">
                <div>
                  <Link to={`/orders/${order.orderNumber}`} className="text-luna-black">
                    {order.orderNumber}
                  </Link>
                  <p className="text-xs text-luna-charcoal/60">
                    {new Date(order.createdAtUtc).toLocaleDateString('fr-FR')} · {ORDER_STATUS_LABELS[order.status]}
                  </p>
                </div>
                <span className="text-luna-black">{formatPrice(order.total)}</span>
              </li>
            ))}
          </ul>
        )}
      </section>

      <section className="mt-6">
        <h2 className="flex items-center gap-2 font-display text-xl text-luna-black">
          <Heart className="h-4 w-4" /> Mes favoris
        </h2>
        {favoriteProducts.length === 0 ? (
          <p className="mt-3 text-sm text-luna-charcoal/60">Ajoutez vos coups de cœur en touchant le cœur sur un produit.</p>
        ) : (
          <div className="mt-4 grid grid-cols-2 gap-4 md:grid-cols-4">
            {favoriteProducts.map((p) => (
              <ProductCard key={p.id} product={p} />
            ))}
          </div>
        )}
      </section>
    </div>
  );
}
