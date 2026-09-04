import { Link, useNavigate } from 'react-router-dom';

import { useCart } from '../../lib/cart/CartContext';
import { formatPrice } from '../../lib/format/price';

export function CartPage() {
  const { items, removeItem, setQuantity, subtotal } = useCart();
  const navigate = useNavigate();

  if (items.length === 0) {
    return (
      <div className="flex flex-col items-center gap-4 px-4 py-24 text-center">
        <p className="text-sm text-luna-charcoal/70">Votre panier est vide.</p>
        <Link to="/categories" className="rounded-full bg-luna-black px-6 py-3.5 text-sm font-medium text-white">
          Découvrir la collection
        </Link>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-2xl px-4 py-8 pb-28 sm:pb-8">
      <h1 className="mb-6 font-display text-2xl italic text-luna-black">Panier</h1>

      <div className="flex flex-col divide-y divide-black/5">
        {items.map((item) => (
          <div key={item.variantId} className="flex gap-4 py-4">
            <div className="h-24 w-20 shrink-0 overflow-hidden rounded-lg bg-luna-cream">
              {item.imageUrl && <img src={item.imageUrl} alt={item.productName} className="h-full w-full object-cover" />}
            </div>

            <div className="flex flex-1 flex-col gap-1">
              <Link to={`/product/${item.productSlug}`} className="text-sm font-medium text-luna-black">
                {item.productName}
              </Link>
              <p className="text-xs text-luna-charcoal/60">
                {item.color} / {item.size}
              </p>
              <p className="text-sm text-luna-charcoal/80">{formatPrice(item.unitPrice)}</p>

              <div className="mt-1 flex items-center gap-3">
                <select
                  value={item.quantity}
                  onChange={(e) => setQuantity(item.variantId, Number(e.target.value))}
                  className="rounded-lg border border-black/15 px-2 py-1 text-sm"
                >
                  {Array.from({ length: Math.min(10, item.availableQuantity) }, (_, i) => i + 1).map((n) => (
                    <option key={n} value={n}>
                      {n}
                    </option>
                  ))}
                </select>
                <button
                  type="button"
                  onClick={() => removeItem(item.variantId)}
                  className="text-xs text-luna-charcoal/50 underline underline-offset-2"
                >
                  Retirer
                </button>
              </div>
            </div>

            <div className="text-sm font-medium text-luna-black">{formatPrice(item.unitPrice * item.quantity)}</div>
          </div>
        ))}
      </div>

      <div className="mt-6 flex items-center justify-between border-t border-black/10 pt-4">
        <span className="text-sm font-medium">Sous-total</span>
        <span className="text-lg font-semibold">{formatPrice(subtotal)}</span>
      </div>
      <p className="mt-1 text-xs text-luna-charcoal/50">Livraison calculée à l'étape suivante.</p>

      <button
        type="button"
        onClick={() => navigate('/checkout')}
        className="mt-6 hidden w-full rounded-full bg-luna-black px-6 py-3.5 text-sm font-medium text-white transition hover:bg-luna-charcoal sm:block"
      >
        Commander
      </button>

      {/* Sticky mobile CTA */}
      <div className="fixed inset-x-0 bottom-0 z-30 border-t border-black/10 bg-white/95 p-3 backdrop-blur sm:hidden">
        <button
          type="button"
          onClick={() => navigate('/checkout')}
          className="w-full rounded-full bg-luna-black px-6 py-3.5 text-sm font-medium text-white"
        >
          Commander — {formatPrice(subtotal)}
        </button>
      </div>
    </div>
  );
}
