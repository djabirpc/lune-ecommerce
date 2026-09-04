import { Link, useLocation, useParams } from 'react-router-dom';

import { OrderDetailsCard } from '../../lib/components/OrderDetailsCard';
import type { OrderDetailDto } from '../../lib/api/types';

export function OrderConfirmationPage() {
  const { orderNumber } = useParams<{ orderNumber: string }>();
  const location = useLocation();
  const order = (location.state as { order?: OrderDetailDto } | null)?.order;

  return (
    <div className="mx-auto max-w-lg px-4 py-12">
      <div className="mb-8 text-center">
        <div className="mx-auto mb-4 flex h-14 w-14 items-center justify-center rounded-full bg-luna-rose text-2xl text-luna-accent-dark">
          ✓
        </div>
        <h1 className="font-display text-2xl italic text-luna-black">Merci pour votre commande !</h1>
        <p className="mx-auto mt-2 max-w-xs text-sm text-luna-charcoal/70">
          Un agent vous contactera bientôt au numéro fourni pour confirmer votre commande.
        </p>
      </div>

      {order ? (
        <OrderDetailsCard order={order} />
      ) : (
        <div className="rounded-2xl border border-black/10 p-5 text-center text-sm text-luna-charcoal/70">
          <p className="mb-2">
            Commande <span className="font-mono">{orderNumber}</span>
          </p>
          <p>
            Consultez <Link to="/track-order" className="text-luna-accent-dark underline underline-offset-2">le suivi de commande</Link> avec votre numéro de
            téléphone pour voir le détail.
          </p>
        </div>
      )}

      <div className="mt-8 text-center">
        <Link to="/categories" className="text-sm font-medium text-luna-black underline underline-offset-4 hover:text-luna-accent">
          Continuer mes achats
        </Link>
      </div>
    </div>
  );
}
