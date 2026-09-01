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
        <Link to="/categories" className="rounded-full bg-luna-black px-6 py-3 text-sm text-white">
          Découvrir la collection
        </Link>
      </div>
    );
  }

  return (
    <div className="px-4 py-8">
      <h1 className="mb-6 text-xl font-semibold">Panier</h1>

      <div className="flex flex-col divide-y divide-black/5">
        {items.map((item) => (
          <div key={item.variantId} className="flex gap-4 py-4">
            <div className="h-20 w-16 shrink-0 overflow-hidden rounded bg-luna-cream">
              {item.imageUrl && <img src={item.imageUrl} alt={item.productName} className="h-full w-full object-cover" />}
            </div>

            <div className="flex flex-1 flex-col gap-1">
              <Link to={`/product/${item.productSlug}`} className="text-sm font-medium">
                {item.productName}
              </Link>
              <p className="text-xs text-luna-charcoal/60">
                {item.color} / {item.size}
              </p>
              <p className="text-sm">{formatPrice(item.unitPrice)}</p>

              <div className="mt-1 flex items-center gap-3">
                <select
                  value={item.quantity}
                  onChange={(e) => setQuantity(item.variantId, Number(e.target.value))}
                  className="rounded border border-black/20 px-2 py-1 text-sm"
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
                  className="text-xs text-luna-charcoal/60 underline"
                >
                  Retirer
                </button>
              </div>
            </div>

            <div className="text-sm font-medium">{formatPrice(item.unitPrice * item.quantity)}</div>
          </div>
        ))}
      </div>

      <div className="mt-6 flex items-center justify-between border-t border-black/10 pt-4">
        <span className="text-sm font-medium">Sous-total</span>
        <span className="text-lg font-semibold">{formatPrice(subtotal)}</span>
      </div>

      <button
        type="button"
        onClick={() => navigate('/checkout')}
        className="mt-6 w-full rounded-full bg-luna-black px-6 py-3 text-sm text-white"
      >
        Commander
      </button>
    </div>
  );
}
