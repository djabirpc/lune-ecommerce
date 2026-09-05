import { Link, useLocation, useParams } from 'react-router-dom';
import { CheckCircle2, PhoneCall, Truck, Wallet } from 'lucide-react';

import { OrderDetailsCard } from '../../lib/components/OrderDetailsCard';
import { formatPrice } from '../../lib/format/price';
import type { OrderDetailDto } from '../../lib/api/types';

export function OrderConfirmationPage() {
  const { orderNumber } = useParams<{ orderNumber: string }>();
  const location = useLocation();
  const order = (location.state as { order?: OrderDetailDto } | null)?.order;

  return (
    <div className="mx-auto max-w-2xl px-4 py-12">
      <div className="flex flex-col items-center text-center">
        <CheckCircle2 className="h-12 w-12 text-luna-accent" />
        <h1 className="mt-4 font-display text-4xl text-luna-black">
          {order ? `Merci ${order.firstName} !` : 'Merci pour votre commande !'}
        </h1>
        <p className="mx-auto mt-2 max-w-sm text-sm text-luna-charcoal/70">
          Votre commande <strong className="font-mono">{orderNumber}</strong> est enregistrée. Notre équipe vous appelle
          {order ? ` au ${order.phone}` : ''} pour la confirmer.
        </p>
      </div>

      {order && (
        <ul className="mt-8 grid gap-3 rounded-sm bg-luna-cream-dark p-4 text-xs text-luna-black sm:grid-cols-3">
          <li className="flex items-center gap-2">
            <PhoneCall className="h-4 w-4 text-luna-accent" /> Appel de confirmation
          </li>
          <li className="flex items-center gap-2">
            <Truck className="h-4 w-4 text-luna-accent" /> Livraison 48–72h
          </li>
          <li className="flex items-center gap-2">
            <Wallet className="h-4 w-4 text-luna-accent" /> {formatPrice(order.total)} à la réception
          </li>
        </ul>
      )}

      <div className="mt-8">
        {order ? (
          <OrderDetailsCard order={order} showTimeline={false} />
        ) : (
          <div className="rounded-sm border border-black/10 bg-white p-5 text-center text-sm text-luna-charcoal/70">
            <p className="mb-2">
              Commande <span className="font-mono">{orderNumber}</span>
            </p>
            <p>
              Consultez{' '}
              <Link to={`/track-order?orderNumber=${orderNumber}`} className="text-luna-accent-dark underline underline-offset-2">
                le suivi de commande
              </Link>{' '}
              avec votre numéro de téléphone pour voir le détail.
            </p>
          </div>
        )}
      </div>

      <div className="mt-6 flex flex-col gap-3 sm:flex-row">
        <Link
          to={`/track-order?orderNumber=${orderNumber}`}
          className="flex h-12 flex-1 items-center justify-center rounded-sm bg-luna-black text-sm font-medium text-white"
        >
          Suivre ma commande
        </Link>
        <Link
          to="/categories"
          className="flex h-12 flex-1 items-center justify-center rounded-sm border border-luna-black text-sm font-medium text-luna-black"
        >
          Continuer mes achats
        </Link>
      </div>
    </div>
  );
}
