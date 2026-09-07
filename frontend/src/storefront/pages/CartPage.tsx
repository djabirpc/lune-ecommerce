import { Link, useNavigate } from 'react-router-dom';
import { Minus, Plus, Trash2, ShoppingBag } from 'lucide-react';
import { useTranslation } from 'react-i18next';

import { useCart } from '../../lib/cart/CartContext';
import { formatPrice } from '../../lib/format/price';

export function CartPage() {
  const { t } = useTranslation();
  const { items, removeItem, setQuantity, subtotal, itemCount } = useCart();
  const navigate = useNavigate();

  if (items.length === 0) {
    return (
      <div className="mx-auto flex max-w-md flex-col items-center px-4 py-24 text-center">
        <ShoppingBag className="h-10 w-10 text-luna-charcoal/40" />
        <h1 className="mt-4 font-display text-3xl text-luna-black">{t('cart.empty.title')}</h1>
        <p className="mt-2 text-sm text-luna-charcoal/70">{t('cart.empty.subtitle')}</p>
        <Link to="/categories" className="mt-6 rounded-sm bg-luna-black px-7 py-3 text-sm text-white">
          {t('cart.empty.cta')}
        </Link>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-5xl px-4 py-8 pb-28 sm:pb-8">
      <h1 className="font-display text-4xl text-luna-black">{t('cart.title', { count: itemCount })}</h1>

      <div className="mt-6 grid gap-8 lg:grid-cols-[1fr_20rem]">
        <ul className="divide-y divide-black/10 border-y border-black/10">
          {items.map((item) => (
            <li key={item.variantId} className="flex gap-4 py-4">
              <Link to={`/product/${item.productSlug}`} className="h-28 w-20 shrink-0 overflow-hidden rounded-sm bg-luna-cream-dark">
                {item.imageUrl && <img src={item.imageUrl} alt={item.productName} className="h-full w-full object-cover" />}
              </Link>
              <div className="flex min-w-0 flex-1 flex-col">
                <div className="flex items-start justify-between gap-3">
                  <div className="min-w-0">
                    <Link to={`/product/${item.productSlug}`} className="line-clamp-1 text-sm text-luna-black">
                      {item.productName}
                    </Link>
                    <p className="mt-0.5 text-xs text-luna-charcoal/60">{t('cart.colorSize', { color: item.color, size: item.size })}</p>
                  </div>
                  <button
                    onClick={() => removeItem(item.variantId)}
                    aria-label={t('cart.removeItem')}
                    className="p-1 text-luna-charcoal/50"
                  >
                    <Trash2 className="h-4 w-4" />
                  </button>
                </div>
                <div className="mt-auto flex items-center justify-between pt-3">
                  <div className="flex items-center rounded-sm border border-black/15">
                    <button
                      className="p-2"
                      aria-label={t('product.decrease')}
                      onClick={() => setQuantity(item.variantId, item.quantity - 1)}
                    >
                      <Minus className="h-3.5 w-3.5" />
                    </button>
                    <span className="w-7 text-center text-sm">{item.quantity}</span>
                    <button
                      className="p-2"
                      aria-label={t('product.increase')}
                      onClick={() => setQuantity(item.variantId, item.quantity + 1)}
                    >
                      <Plus className="h-3.5 w-3.5" />
                    </button>
                  </div>
                  <span className="text-sm font-medium text-luna-black">{formatPrice(item.unitPrice * item.quantity)}</span>
                </div>
              </div>
            </li>
          ))}
        </ul>

        <aside className="h-fit rounded-sm border border-black/10 bg-white p-5 lg:sticky lg:top-24">
          <h2 className="font-display text-xl text-luna-black">{t('cart.summary.title')}</h2>
          <dl className="mt-4 space-y-2 text-sm">
            <div className="flex justify-between">
              <dt className="text-luna-charcoal/60">{t('cart.summary.subtotal')}</dt>
              <dd>{formatPrice(subtotal)}</dd>
            </div>
            <div className="flex justify-between">
              <dt className="text-luna-charcoal/60">{t('cart.summary.shipping')}</dt>
              <dd>{t('cart.summary.shippingCalculated')}</dd>
            </div>
            <div className="flex justify-between border-t border-black/10 pt-3 text-base font-medium text-luna-black">
              <dt>{t('cart.summary.total')}</dt>
              <dd>{formatPrice(subtotal)}</dd>
            </div>
          </dl>
          <button
            type="button"
            onClick={() => navigate('/checkout')}
            className="mt-5 h-12 w-full rounded-sm bg-luna-black text-sm font-medium text-white transition hover:bg-luna-charcoal"
          >
            {t('cart.orderCod')}
          </button>
          <p className="mt-3 text-center text-xs text-luna-charcoal/60">{t('cart.noOnlinePayment')}</p>
        </aside>
      </div>

      {/* Sticky mobile CTA */}
      <div className="fixed inset-x-0 bottom-0 z-30 border-t border-black/10 bg-white/95 p-3 backdrop-blur sm:hidden">
        <button
          type="button"
          onClick={() => navigate('/checkout')}
          className="w-full rounded-sm bg-luna-black px-6 py-3.5 text-sm font-medium text-white"
        >
          {t('cart.orderWithPrice', { price: formatPrice(subtotal) })}
        </button>
      </div>
    </div>
  );
}
