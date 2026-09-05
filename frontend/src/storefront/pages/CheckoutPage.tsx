import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { zodResolver } from '@hookform/resolvers/zod';
import { useForm, useWatch } from 'react-hook-form';
import { z } from 'zod';
import { Loader2, ShieldCheck, Truck, Wallet } from 'lucide-react';

import { ordersApi } from '../../lib/api/orders';
import { shippingRatesApi } from '../../lib/api/shipping';
import { ApiError } from '../../lib/api/client';
import { useCart } from '../../lib/cart/CartContext';
import { formatPrice } from '../../lib/format/price';
import { DELIVERY_TYPE_LABELS } from '../../lib/format/orderLabels';
import { getStoredAttribution } from '../../lib/marketing/attribution';
import { trackEvent } from '../../lib/marketing/pixels';
import { ALGERIAN_WILAYAS } from '../../lib/data/wilayas';
import { getSavedCustomerInfo, saveCustomerInfo } from '../../lib/customer/savedCustomerInfo';
import { rememberOrder } from '../../lib/orders/localOrderHistory';

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

const inputClass = 'h-12 w-full rounded-sm border border-black/15 px-3.5 text-sm outline-none transition focus:border-luna-black';

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
    reset,
    formState: { errors },
  } = useForm<CheckoutFormValues>({
    resolver: zodResolver(checkoutSchema),
    defaultValues: { deliveryType: 'HomeDelivery' },
  });

  useEffect(() => {
    const saved = getSavedCustomerInfo();
    if (saved) {
      reset({
        firstName: saved.firstName,
        lastName: saved.lastName,
        phone: saved.phone,
        wilaya: saved.wilaya,
        commune: saved.commune,
        address: saved.address,
        deliveryType: 'HomeDelivery',
      });
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

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
      saveCustomerInfo({
        firstName: values.firstName,
        lastName: values.lastName,
        phone: values.phone,
        wilaya: values.wilaya,
        commune: values.commune,
        address: values.address,
      });
      rememberOrder(order.orderNumber, values.phone);
      clear();
      navigate(`/order-confirmation/${order.orderNumber}`, { state: { order } });
    } catch (error) {
      setSubmitError(error instanceof ApiError ? error.message : 'Une erreur est survenue. Veuillez réessayer.');
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div className="mx-auto max-w-5xl px-4 py-8 pb-8 sm:px-6 lg:px-8">
      <h1 className="font-display text-4xl text-luna-black">Finaliser la commande</h1>
      <p className="mt-2 text-sm text-luna-charcoal/70">Paiement à la livraison — vous payez le livreur à la réception.</p>

      <form onSubmit={handleSubmit(onSubmit)} className="mt-8 grid gap-8 lg:grid-cols-[1fr_20rem]">
        <div className="space-y-5">
          <div className="grid gap-4 sm:grid-cols-2">
            <div>
              <label className="mb-1 block text-sm font-medium text-luna-black">Prénom</label>
              <input {...register('firstName')} className={inputClass} autoComplete="given-name" />
              {errors.firstName && <p className="mt-1 text-xs text-red-600">{errors.firstName.message}</p>}
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium text-luna-black">Nom</label>
              <input {...register('lastName')} className={inputClass} autoComplete="family-name" />
              {errors.lastName && <p className="mt-1 text-xs text-red-600">{errors.lastName.message}</p>}
            </div>
          </div>

          <div>
            <label className="mb-1 block text-sm font-medium text-luna-black">Téléphone</label>
            <input {...register('phone')} placeholder="0551234567" className={inputClass} inputMode="numeric" autoComplete="tel" />
            {errors.phone && <p className="mt-1 text-xs text-red-600">{errors.phone.message}</p>}
          </div>

          <div className="grid gap-4 sm:grid-cols-2">
            <div>
              <label className="mb-1 block text-sm font-medium text-luna-black">Wilaya</label>
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
              <label className="mb-1 block text-sm font-medium text-luna-black">Commune</label>
              <input {...register('commune')} className={inputClass} />
              {errors.commune && <p className="mt-1 text-xs text-red-600">{errors.commune.message}</p>}
            </div>
          </div>

          <div>
            <label className="mb-1 block text-sm font-medium text-luna-black">Adresse de livraison</label>
            <input {...register('address')} placeholder="Cité, rue, immeuble, repère…" className={inputClass} />
            {errors.address && <p className="mt-1 text-xs text-red-600">{errors.address.message}</p>}
          </div>

          <div>
            <p className="eyebrow mb-2">Type de livraison</p>
            <div className="grid grid-cols-2 gap-2">
              {(Object.keys(DELIVERY_TYPE_LABELS) as (keyof typeof DELIVERY_TYPE_LABELS)[]).map((type) => (
                <label
                  key={type}
                  className="flex h-12 cursor-pointer items-center justify-center gap-2 rounded-sm border border-black/15 px-3 text-sm has-[:checked]:border-luna-black has-[:checked]:bg-luna-black has-[:checked]:text-white"
                >
                  <input type="radio" value={type} {...register('deliveryType')} className="sr-only" />
                  {DELIVERY_TYPE_LABELS[type]}
                </label>
              ))}
            </div>
          </div>

          <div>
            <label className="mb-1 block text-sm font-medium text-luna-black">Note pour le livreur (facultatif)</label>
            <textarea
              {...register('notes')}
              placeholder="Appeler avant de venir, livrer après 16h…"
              rows={3}
              className="w-full rounded-sm border border-black/15 px-3.5 py-2.5 text-sm outline-none transition focus:border-luna-black"
            />
          </div>

          <ul className="grid gap-3 rounded-sm bg-luna-cream-dark p-4 text-xs text-luna-black sm:grid-cols-3">
            <li className="flex items-center gap-2">
              <Wallet className="h-4 w-4 text-luna-accent" /> Paiement à la réception
            </li>
            <li className="flex items-center gap-2">
              <Truck className="h-4 w-4 text-luna-accent" /> Livraison 48–72h
            </li>
            <li className="flex items-center gap-2">
              <ShieldCheck className="h-4 w-4 text-luna-accent" /> Échange sous 7 jours
            </li>
          </ul>

          {submitError && <p className="text-sm text-red-600">{submitError}</p>}
        </div>

        <aside className="h-fit rounded-sm border border-black/10 bg-white p-5 lg:sticky lg:top-24">
          <h2 className="font-display text-xl text-luna-black">Votre commande</h2>
          <ul className="mt-4 space-y-3">
            {items.map((item) => (
              <li key={item.variantId} className="flex gap-3">
                <div className="h-16 w-12 shrink-0 overflow-hidden rounded-sm bg-luna-cream-dark">
                  {item.imageUrl && <img src={item.imageUrl} alt={item.productName} className="h-full w-full object-cover" />}
                </div>
                <div className="min-w-0 flex-1 text-xs">
                  <p className="line-clamp-1 text-luna-black">{item.productName}</p>
                  <p className="text-luna-charcoal/60">
                    {item.color} · {item.size} · x{item.quantity}
                  </p>
                </div>
                <span className="text-xs font-medium text-luna-black">{formatPrice(item.unitPrice * item.quantity)}</span>
              </li>
            ))}
          </ul>

          <dl className="mt-5 space-y-2 border-t border-black/10 pt-4 text-sm">
            <div className="flex justify-between">
              <dt className="text-luna-charcoal/60">Sous-total</dt>
              <dd>{formatPrice(subtotal)}</dd>
            </div>
            <div className="flex justify-between">
              <dt className="text-luna-charcoal/60">Livraison</dt>
              <dd>
                {shippingError ? (
                  <span className="text-red-600">Indisponible</span>
                ) : shippingCost === null ? (
                  'Selon la wilaya'
                ) : (
                  formatPrice(shippingCost)
                )}
              </dd>
            </div>
            <div className="flex justify-between border-t border-black/10 pt-3 text-base font-medium text-luna-black">
              <dt>Total à payer</dt>
              <dd>{formatPrice(subtotal + (shippingCost ?? 0))}</dd>
            </div>
          </dl>

          <div className="mt-4">
            <label className="mb-1 block text-sm font-medium text-luna-black">Code promo (facultatif)</label>
            <input
              value={couponCode}
              onChange={(e) => setCouponCode(e.target.value.toUpperCase())}
              placeholder="CODE2024"
              className={inputClass}
            />
          </div>

          <button
            type="submit"
            disabled={isSubmitting}
            className="mt-5 flex h-12 w-full items-center justify-center rounded-sm bg-luna-black text-sm font-medium text-white transition hover:bg-luna-charcoal disabled:opacity-40"
          >
            {isSubmitting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
            {isSubmitting ? 'Envoi...' : 'Confirmer la commande'}
          </button>
          <p className="mt-3 text-center text-xs text-luna-charcoal/60">Aucun paiement en ligne. Nous vous appelons pour confirmer.</p>
        </aside>
      </form>
    </div>
  );
}
