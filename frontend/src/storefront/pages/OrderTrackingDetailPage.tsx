import { Link, useParams } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';

import { ordersApi } from '../../lib/api/orders';
import { findPhoneForOrder } from '../../lib/orders/localOrderHistory';
import { OrderDetailsCard } from '../../lib/components/OrderDetailsCard';
import { PagePlaceholder } from '../../lib/components/PagePlaceholder';

export function OrderTrackingDetailPage() {
  const { orderNumber } = useParams<{ orderNumber: string }>();
  const phone = orderNumber ? findPhoneForOrder(orderNumber) : undefined;

  const { data: order, isLoading, isError } = useQuery({
    queryKey: ['order-tracking', orderNumber, phone],
    queryFn: () => ordersApi.track(orderNumber!, phone!),
    enabled: Boolean(orderNumber && phone),
  });

  if (!orderNumber) {
    return <PagePlaceholder title="Commande introuvable" />;
  }

  // No phone remembered on this device (different browser, cleared storage, ...) — fall back to
  // the manual, phone-verified lookup form rather than guessing/exposing the order without proof.
  if (!phone) {
    return (
      <div className="mx-auto flex max-w-md flex-col items-center px-4 py-24 text-center">
        <h1 className="font-display text-3xl text-luna-black">Confirmez votre numéro</h1>
        <p className="mt-2 text-sm text-luna-charcoal/70">
          Cette commande n'est pas associée à cet appareil. Entrez votre numéro de téléphone pour la retrouver.
        </p>
        <Link
          to={`/track-order?orderNumber=${orderNumber}`}
          className="mt-6 rounded-sm bg-luna-black px-7 py-3 text-sm text-white"
        >
          Suivre ma commande
        </Link>
      </div>
    );
  }

  if (isLoading) {
    return <div className="px-4 py-24 text-center text-sm text-luna-charcoal/60">Chargement…</div>;
  }

  if (isError || !order) {
    return <PagePlaceholder title="Commande introuvable" />;
  }

  return (
    <div className="mx-auto max-w-2xl px-4 py-8">
      <Link to="/orders" className="text-xs text-luna-charcoal/60 hover:text-luna-black">
        ← Mes commandes
      </Link>
      <h1 className="mt-2 font-display text-4xl text-luna-black">{order.orderNumber}</h1>
      <p className="mt-1 text-sm text-luna-charcoal/70">
        Passée le {new Date(order.createdAtUtc).toLocaleDateString('fr-FR')} · Paiement à la livraison
      </p>

      <div className="mt-8">
        <OrderDetailsCard order={order} />
      </div>
    </div>
  );
}
