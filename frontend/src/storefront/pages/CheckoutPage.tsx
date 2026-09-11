import { useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { zodResolver } from '@hookform/resolvers/zod';
import { useForm, useWatch } from 'react-hook-form';
import { useQuery } from '@tanstack/react-query';
import { z } from 'zod';
import { Loader2, ShieldCheck, Truck, Wallet } from 'lucide-react';
import { useTranslation } from 'react-i18next';

import { ordersApi } from '../../lib/api/orders';
import { shippingRatesApi } from '../../lib/api/shipping';
import { promotionsApi } from '../../lib/api/promotions';
import { ApiError } from '../../lib/api/client';
import { useCart } from '../../lib/cart/CartContext';
import { formatPrice } from '../../lib/format/price';
import { useDeliveryTypeLabels } from '../../lib/format/orderLabels';
import { estimateCartDiscount } from '../../lib/promotions/estimate';
import { getStoredAttribution } from '../../lib/marketing/attribution';
import { trackEvent } from '../../lib/marketing/pixels';
import { ALGERIAN_WILAYAS } from '../../lib/data/wilayas';
import { getSavedCustomerInfo, saveCustomerInfo } from '../../lib/customer/savedCustomerInfo';
import { rememberOrder } from '../../lib/orders/localOrderHistory';

function createCheckoutSchema(t: (key: string) => string) {
  return z.object({
    firstName: z.string().trim().min(1, t('checkout.validation.firstNameRequired')).max(100),
    lastName: z.string().trim().min(1, t('checkout.validation.lastNameRequired')).max(100),
    phone: z.string().regex(/^0[0-9]{9}$/, t('checkout.validation.phoneInvalid')),
    wilaya: z.string().trim().min(1, t('checkout.validation.wilayaRequired')).max(100),
    commune: z.string().trim().min(1, t('checkout.validation.communeRequired')).max(100),
    address: z.string().trim().max(500).optional(),
    deliveryType: z.enum(['HomeDelivery', 'StopDesk']),
    notes: z.string().trim().max(1000).optional(),
  });
}

type CheckoutFormValues = z.infer<ReturnType<typeof createCheckoutSchema>>;

const inputClass = 'h-12 w-full rounded-sm border border-black/15 px-3.5 text-sm outline-none transition focus:border-luna-black';

export function CheckoutPage() {
  const { t } = useTranslation();
  const deliveryTypeLabels = useDeliveryTypeLabels();
  const { items, subtotal, clear } = useCart();
  const navigate = useNavigate();
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [staleCartItems, setStaleCartItems] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [couponCode, setCouponCode] = useState('');

  const checkoutSchema = useMemo(() => createCheckoutSchema(t), [t]);

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
        address: saved.address ?? undefined,
        deliveryType: 'HomeDelivery',
      });
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const wilaya = useWatch({ control, name: 'wilaya' });
  const deliveryType = useWatch({ control, name: 'deliveryType' });
  const [shippingCost, setShippingCost] = useState<number | null>(null);
  const [shippingError, setShippingError] = useState<string | null>(null);

  const { data: activePromotions } = useQuery({
    queryKey: ['active-promotions'],
    queryFn: () => promotionsApi.getActive(),
  });
  const discount = estimateCartDiscount(items, activePromotions ?? []);
  const estimatedTotal = subtotal - discount.discountTotal + (shippingCost ?? 0);

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
          setShippingError(t('checkout.shippingUnavailable'));
        }
      });

    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [wilaya, deliveryType]);

  if (items.length === 0) {
    return (
      <div className="px-4 py-24 text-center text-sm text-luna-charcoal/70">{t('checkout.emptyCart')}</div>
    );
  }

  async function onSubmit(values: CheckoutFormValues) {
    setSubmitError(null);
    setStaleCartItems(false);
    setIsSubmitting(true);
    try {
      const order = await ordersApi.create({
        ...values,
        address: values.address || null,
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
        address: values.address || null,
      });
      rememberOrder(order.orderNumber, values.phone);
      clear();
      navigate(`/order-confirmation/${order.orderNumber}`, { state: { order } });
    } catch (error) {
      setSubmitError(error instanceof ApiError ? error.message : t('common.error'));
      // A 404 here means at least one cart item's variant no longer exists (e.g. the catalog
      // changed since it was added) — the cart itself is unusable and needs to be cleared, not
      // just resubmitted, so offer that directly instead of leaving the customer stuck.
      setStaleCartItems(error instanceof ApiError && error.status === 404);
    } finally {
      setIsSubmitting(false);
    }
  }

  function handleClearStaleCart() {
    clear();
    navigate('/categories');
  }

  return (
    <div className="mx-auto max-w-5xl px-4 py-8 pb-8 sm:px-6 lg:px-8">
      <h1 className="font-display text-4xl text-luna-black">{t('checkout.title')}</h1>
      <p className="mt-2 text-sm text-luna-charcoal/70">{t('checkout.subtitle')}</p>

      <form onSubmit={handleSubmit(onSubmit)} className="mt-8 grid gap-8 lg:grid-cols-[1fr_20rem]">
        <div className="space-y-5">
          <div className="grid gap-4 sm:grid-cols-2">
            <div>
              <label className="mb-1 block text-sm font-medium text-luna-black">{t('checkout.firstName')}</label>
              <input {...register('firstName')} className={inputClass} autoComplete="given-name" />
              {errors.firstName && <p className="mt-1 text-xs text-red-600">{errors.firstName.message}</p>}
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium text-luna-black">{t('checkout.lastName')}</label>
              <input {...register('lastName')} className={inputClass} autoComplete="family-name" />
              {errors.lastName && <p className="mt-1 text-xs text-red-600">{errors.lastName.message}</p>}
            </div>
          </div>

          <div>
            <label className="mb-1 block text-sm font-medium text-luna-black">{t('checkout.phone')}</label>
            <input {...register('phone')} placeholder="0551234567" className={inputClass} inputMode="numeric" autoComplete="tel" />
            {errors.phone && <p className="mt-1 text-xs text-red-600">{errors.phone.message}</p>}
          </div>

          <div className="grid gap-4 sm:grid-cols-2">
            <div>
              <label className="mb-1 block text-sm font-medium text-luna-black">{t('checkout.wilaya')}</label>
              <select {...register('wilaya')} defaultValue="" className={inputClass}>
                <option value="" disabled>
                  {t('checkout.selectWilaya')}
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
              <label className="mb-1 block text-sm font-medium text-luna-black">{t('checkout.commune')}</label>
              <input {...register('commune')} className={inputClass} />
              {errors.commune && <p className="mt-1 text-xs text-red-600">{errors.commune.message}</p>}
            </div>
          </div>

          <div>
            <label className="mb-1 block text-sm font-medium text-luna-black">
              {t('checkout.address')} <span className="font-normal text-luna-charcoal/50">({t('checkout.optional')})</span>
            </label>
            <input {...register('address')} placeholder={t('checkout.addressPlaceholder')} className={inputClass} />
            {errors.address && <p className="mt-1 text-xs text-red-600">{errors.address.message}</p>}
          </div>

          <div>
            <p className="eyebrow mb-2">{t('checkout.deliveryType')}</p>
            <div className="grid grid-cols-2 gap-2">
              {(Object.keys(deliveryTypeLabels) as (keyof typeof deliveryTypeLabels)[]).map((type) => (
                <label
                  key={type}
                  className="flex h-12 cursor-pointer items-center justify-center gap-2 rounded-sm border border-black/15 px-3 text-sm has-[:checked]:border-luna-black has-[:checked]:bg-luna-black has-[:checked]:text-white"
                >
                  <input type="radio" value={type} {...register('deliveryType')} className="sr-only" />
                  {deliveryTypeLabels[type]}
                </label>
              ))}
            </div>
          </div>

          <div>
            <label className="mb-1 block text-sm font-medium text-luna-black">{t('checkout.notes')}</label>
            <textarea
              {...register('notes')}
              placeholder={t('checkout.notesPlaceholder')}
              rows={3}
              className="w-full rounded-sm border border-black/15 px-3.5 py-2.5 text-sm outline-none transition focus:border-luna-black"
            />
          </div>

          <ul className="grid gap-3 rounded-sm bg-luna-cream-dark p-4 text-xs text-luna-black sm:grid-cols-3">
            <li className="flex items-center gap-2">
              <Wallet className="h-4 w-4 text-luna-accent" /> {t('checkout.info.cod')}
            </li>
            <li className="flex items-center gap-2">
              <Truck className="h-4 w-4 text-luna-accent" /> {t('checkout.info.delivery')}
            </li>
            <li className="flex items-center gap-2">
              <ShieldCheck className="h-4 w-4 text-luna-accent" /> {t('checkout.info.exchange')}
            </li>
          </ul>

          {submitError && (
            <div className="rounded-sm bg-red-50 p-3 text-sm text-red-600">
              <p>{submitError}</p>
              {staleCartItems && (
                <button
                  type="button"
                  onClick={handleClearStaleCart}
                  className="mt-2 underline underline-offset-2 hover:text-red-800"
                >
                  {t('checkout.clearStaleCart')}
                </button>
              )}
            </div>
          )}
        </div>

        <aside className="h-fit rounded-sm border border-black/10 bg-white p-5 lg:sticky lg:top-24">
          <h2 className="font-display text-xl text-luna-black">{t('checkout.summary.title')}</h2>
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
              <dt className="text-luna-charcoal/60">{t('cart.summary.subtotal')}</dt>
              <dd>{formatPrice(subtotal)}</dd>
            </div>
            {discount.discountTotal > 0 && (
              <div className="flex justify-between text-luna-accent-dark">
                <dt>
                  {t('orderDetails.discount')}
                  {discount.promotionNames.length > 0 && ` (${discount.promotionNames.join(', ')})`}
                </dt>
                <dd>−{formatPrice(discount.discountTotal)}</dd>
              </div>
            )}
            <div className="flex justify-between">
              <dt className="text-luna-charcoal/60">{t('cart.summary.shipping')}</dt>
              <dd>
                {shippingError ? (
                  <span className="text-red-600">{t('checkout.shippingUnavailableShort')}</span>
                ) : shippingCost === null ? (
                  t('checkout.shippingByWilaya')
                ) : (
                  formatPrice(shippingCost)
                )}
              </dd>
            </div>
            <div className="flex justify-between border-t border-black/10 pt-3 text-base font-medium text-luna-black">
              <dt>{t('checkout.totalToPay')}</dt>
              <dd>{formatPrice(estimatedTotal)}</dd>
            </div>
          </dl>

          <div className="mt-4">
            <label className="mb-1 block text-sm font-medium text-luna-black">{t('checkout.couponCode')}</label>
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
            {isSubmitting && <Loader2 className="me-2 h-4 w-4 animate-spin" />}
            {isSubmitting ? t('checkout.sending') : t('checkout.confirmOrder')}
          </button>
          <p className="mt-3 text-center text-xs text-luna-charcoal/60">{t('checkout.callToConfirm')}</p>
        </aside>
      </form>
    </div>
  );
}
