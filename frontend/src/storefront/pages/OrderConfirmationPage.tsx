import { Link, useLocation, useParams } from 'react-router-dom';

import { OrderDetailsCard } from '../../lib/components/OrderDetailsCard';
import type { OrderDetailDto } from '../../lib/api/types';

export function OrderConfirmationPage() {
  const { orderNumber } = useParams<{ orderNumber: string }>();
  const location = useLocation();
  const order = (location.state as { order?: OrderDetailDto } | null)?.order;

  return (
    <div className="mx-auto max-w-lg px-4 py-10">
      <div className="mb-6 text-center">
        <h1 className="text-xl font-semibold text-luna-black">Merci pour votre commande !</h1>
        <p className="mt-1 text-sm text-luna-charcoal/70">
          Un agent vous contactera bientôt au numéro fourni pour confirmer votre commande.
        </p>
      </div>

      {order ? (
        <OrderDetailsCard order={order} />
      ) : (
        <div className="rounded-lg border border-black/10 p-4 text-center text-sm text-luna-charcoal/70">
          <p className="mb-2">
            Commande <span className="font-mono">{orderNumber}</span>
          </p>
          <p>
            Consultez <Link to="/track-order" className="underline">le suivi de commande</Link> avec votre numéro de
            téléphone pour voir le détail.
          </p>
        </div>
      )}

      <div className="mt-6 text-center">
        <Link to="/categories" className="text-sm underline">
          Continuer mes achats
        </Link>
      </div>
    </div>
  );
}
