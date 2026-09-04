import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { zodResolver } from '@hookform/resolvers/zod';
import { useForm, useWatch } from 'react-hook-form';
import { z } from 'zod';

import { ordersApi } from '../../lib/api/orders';
import { shippingRatesApi } from '../../lib/api/shipping';
import { ApiError } from '../../lib/api/client';
import { useCart } from '../../lib/cart/CartContext';
import { formatPrice } from '../../lib/format/price';
import { DELIVERY_TYPE_LABELS } from '../../lib/format/orderLabels';
import { getStoredAttribution } from '../../lib/marketing/attribution';
import { trackEvent } from '../../lib/marketing/pixels';
import { ALGERIAN_WILAYAS } from '../../lib/data/wilayas';

const checkoutSchema = z.object({
  firstName: z.string().min(1, 'Le prénom est requis.').max(100),
  lastName: z.string().min(1, 'Le nom est requis.').max(100),
  phone: z.string().regex(/^0[0-9]{9}$/, 'Numéro de téléphone algérien invalide (10 chiffres, commence par 0).'),
  wilaya: z.string().min(1, 'La wilaya est requise.').max(100),
  commune: z.string().min(1, 'La commune est requise.').max(100),
  address: z.string().min(1, "L'adresse est requise.").max(500),
  deliveryType: z.enum(['HomeDelivery', 'StopDesk']),
  notes: z.string().max(1000).optional(),
});

type CheckoutFormValues = z.infer<typeof checkoutSchema>;

const inputClass =
  'w-full rounded-lg border border-black/15 px-3.5 py-2.5 text-sm outline-none transition focus:border-luna-black';

export function CheckoutPage() {
  const { items, subtotal, clear } = useCart();
  const navigate = useNavigate();
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [couponCode, setCouponCode] = useState('');

  const {
    register,
    handleSubmit,
    control,
    formState: { errors },
  } = useForm<CheckoutFormValues>({
    resolver: zodResolver(checkoutSchema),
    defaultValues: { deliveryType: 'HomeDelivery' },
  });

  const wilaya = useWatch({ control, name: 'wilaya' });
  const deliveryType = useWatch({ control, name: 'deliveryType' });
  const [shippingCost, setShippingCost] = useState<number | null>(null);
  const [shippingError, setShippingError] = useState<string | null>(null);

  useEffect(() => {
    if (items.length > 0) {
      trackEvent('INITIATE_CHECKOUT', { value: subtotal, currency: 'DZD', num_items: items.length });
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  useEffect(() => {
    if (!wilaya) {
      setShippingCost(null);
      setShippingError(null);
      return;
    }

    let cancelled = false;
    shippingRatesApi
      .getQuote(wilaya, deliveryType)
      .then((quote) => {
        if (!cancelled) {
          setShippingCost(quote.price);
          setShippingError(null);
        }
      })
      .catch(() => {
        if (!cancelled) {
          setShippingCost(null);
          setShippingError('Livraison indisponible pour cette wilaya. Contactez-nous pour vérifier.');
        }
      });

    return () => {
      cancelled = true;
    };
  }, [wilaya, deliveryType]);

  if (items.length === 0) {
    return (
      <div className="px-4 py-24 text-center text-sm text-luna-charcoal/70">
        Votre panier est vide. Ajoutez des articles avant de commander.
      </div>
    );
  }

  async function onSubmit(values: CheckoutFormValues) {
    setSubmitError(null);
    setIsSubmitting(true);
    try {
      const order = await ordersApi.create({
        ...values,
        notes: values.notes || null,
        items: items.map((item) => ({ productVariantId: item.variantId, quantity: item.quantity })),
        couponCode: couponCode.trim() || null,
        marketingAttribution: getStoredAttribution(),
      });
      trackEvent('ORDER_CREATED', { value: order.total, currency: 'DZD', order_id: order.orderNumber });
      clear();
      navigate(`/order-confirmation/${order.orderNumber}`, { state: { order } });
    } catch (error) {
      setSubmitError(error instanceof ApiError ? error.message : 'Une erreur est survenue. Veuillez réessayer.');
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div className="mx-auto max-w-4xl px-4 py-8 sm:px-6 lg:px-8">
      <h1 className="mb-6 font-display text-2xl italic text-luna-black">Finaliser la commande</h1>

      <div className="grid gap-8 sm:grid-cols-5">
        <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-4 sm:order-1 sm:col-span-3">
          <h2 className="text-xs font-semibold uppercase tracking-wide text-luna-charcoal/50">Coordonnées &amp; livraison</h2>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="mb-1 block text-sm font-medium">Prénom</label>
              <input {...register('firstName')} className={inputClass} />
              {errors.firstName && <p className="mt-1 text-xs text-red-600">{errors.firstName.message}</p>}
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium">Nom</label>
              <input {...register('lastName')} className={inputClass} />
              {errors.lastName && <p className="mt-1 text-xs text-red-600">{errors.lastName.message}</p>}
            </div>
          </div>

          <div>
            <label className="mb-1 block text-sm font-medium">Téléphone</label>
            <input {...register('phone')} placeholder="0551234567" className={inputClass} inputMode="numeric" />
            {errors.phone && <p className="mt-1 text-xs text-red-600">{errors.phone.message}</p>}
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="mb-1 block text-sm font-medium">Wilaya</label>
              <select {...register('wilaya')} defaultValue="" className={inputClass}>
                <option value="" disabled>
                  Sélectionnez une wilaya
                </option>
                {ALGERIAN_WILAYAS.map((w) => (
                  <option key={w} value={w}>
                    {w}
                  </option>
                ))}
              </select>
              {errors.wilaya && <p className="mt-1 text-xs text-red-600">{errors.wilaya.message}</p>}
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium">Commune</label>
              <input {...register('commune')} className={inputClass} />
              {errors.commune && <p className="mt-1 text-xs text-red-600">{errors.commune.message}</p>}
            </div>
          </div>

          <div>
            <label className="mb-1 block text-sm font-medium">Adresse</label>
            <input {...register('address')} className={inputClass} />
            {errors.address && <p className="mt-1 text-xs text-red-600">{errors.address.message}</p>}
          </div>

          <div>
            <label className="mb-2 block text-sm font-medium">Type de livraison</label>
            <div className="grid grid-cols-2 gap-2">
              {(Object.keys(DELIVERY_TYPE_LABELS) as (keyof typeof DELIVERY_TYPE_LABELS)[]).map((type) => (
                <label
                  key={type}
                  className="flex cursor-pointer items-center justify-center gap-2 rounded-lg border border-black/15 px-3 py-2.5 text-sm has-[:checked]:border-luna-black has-[:checked]:bg-luna-black has-[:checked]:text-white"
                >
                  <input type="radio" value={type} {...register('deliveryType')} className="sr-only" />
                  {DELIVERY_TYPE_LABELS[type]}
                </label>
              ))}
            </div>
          </div>

          <div>
            <label className="mb-1 block text-sm font-medium">Notes (facultatif)</label>
            <textarea {...register('notes')} rows={2} className={inputClass} />
          </div>

          {submitError && <p className="text-sm text-red-600">{submitError}</p>}

          <button
            type="submit"
            disabled={isSubmitting}
            className="mt-2 rounded-full bg-luna-black px-6 py-3.5 text-sm font-medium text-white transition hover:bg-luna-charcoal disabled:opacity-40"
          >
            {isSubmitting ? 'Envoi...' : 'Confirmer la commande — Paiement à la livraison'}
          </button>
        </form>

        <div className="sm:order-2 sm:col-span-2">
          <div className="rounded-2xl bg-luna-cream p-5 sm:sticky sm:top-24">
            <h2 className="mb-3 text-xs font-semibold uppercase tracking-wide text-luna-charcoal/50">Résumé</h2>
            <div className="flex flex-col divide-y divide-black/5">
              {items.map((item) => (
                <div key={item.variantId} className="flex items-center justify-between py-2 text-sm">
                  <span className="pr-2 text-luna-charcoal/80">
                    {item.productName} ({item.color}/{item.size}) × {item.quantity}
                  </span>
                  <span className="shrink-0 font-medium">{formatPrice(item.unitPrice * item.quantity)}</span>
                </div>
              ))}
            </div>
            <div className="mt-3 flex items-center justify-between border-t border-black/10 pt-3 text-sm font-medium">
              <span>Sous-total</span>
              <span>{formatPrice(subtotal)}</span>
            </div>
            <div className="mt-1 flex items-center justify-between text-sm text-luna-charcoal/60">
              <span>Livraison</span>
              {shippingError ? (
                <span className="text-red-600">{shippingError}</span>
              ) : shippingCost === null ? (
                <span>Sélectionnez une wilaya</span>
              ) : (
                <span>{formatPrice(shippingCost)}</span>
              )}
            </div>
            {shippingCost !== null && (
              <div className="mt-2 flex items-center justify-between border-t border-black/10 pt-2 text-base font-semibold">
                <span>Total estimé</span>
                <span>{formatPrice(subtotal + shippingCost)}</span>
              </div>
            )}

            <div className="mt-4">
              <label className="mb-1 block text-sm font-medium">Code promo (facultatif)</label>
              <input
                value={couponCode}
                onChange={(e) => setCouponCode(e.target.value.toUpperCase())}
                placeholder="CODE2024"
                className={`${inputClass} bg-white`}
              />
              <p className="mt-1 text-xs text-luna-charcoal/50">La réduction sera appliquée et affichée sur la confirmation.</p>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
