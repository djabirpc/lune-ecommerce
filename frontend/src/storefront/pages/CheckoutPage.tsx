import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import { z } from 'zod';

import { ordersApi } from '../../lib/api/orders';
import { ApiError } from '../../lib/api/client';
import { useCart } from '../../lib/cart/CartContext';
import { formatPrice } from '../../lib/format/price';
import { DELIVERY_TYPE_LABELS } from '../../lib/format/orderLabels';

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

export function CheckoutPage() {
  const { items, subtotal, clear } = useCart();
  const navigate = useNavigate();
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [couponCode, setCouponCode] = useState('');

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<CheckoutFormValues>({
    resolver: zodResolver(checkoutSchema),
    defaultValues: { deliveryType: 'HomeDelivery' },
  });

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
      });
      clear();
      navigate(`/order-confirmation/${order.orderNumber}`, { state: { order } });
    } catch (error) {
      setSubmitError(error instanceof ApiError ? error.message : 'Une erreur est survenue. Veuillez réessayer.');
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div className="grid gap-8 px-4 py-8 sm:grid-cols-2 sm:px-8">
      <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-4">
        <h1 className="text-xl font-semibold">Livraison</h1>

        <div className="grid grid-cols-2 gap-3">
          <div>
            <label className="mb-1 block text-sm font-medium">Prénom</label>
            <input {...register('firstName')} className="w-full rounded border border-black/20 px-3 py-2 text-sm" />
            {errors.firstName && <p className="mt-1 text-xs text-red-600">{errors.firstName.message}</p>}
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium">Nom</label>
            <input {...register('lastName')} className="w-full rounded border border-black/20 px-3 py-2 text-sm" />
            {errors.lastName && <p className="mt-1 text-xs text-red-600">{errors.lastName.message}</p>}
          </div>
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium">Téléphone</label>
          <input {...register('phone')} placeholder="0551234567" className="w-full rounded border border-black/20 px-3 py-2 text-sm" />
          {errors.phone && <p className="mt-1 text-xs text-red-600">{errors.phone.message}</p>}
        </div>

        <div className="grid grid-cols-2 gap-3">
          <div>
            <label className="mb-1 block text-sm font-medium">Wilaya</label>
            <input {...register('wilaya')} className="w-full rounded border border-black/20 px-3 py-2 text-sm" />
            {errors.wilaya && <p className="mt-1 text-xs text-red-600">{errors.wilaya.message}</p>}
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium">Commune</label>
            <input {...register('commune')} className="w-full rounded border border-black/20 px-3 py-2 text-sm" />
            {errors.commune && <p className="mt-1 text-xs text-red-600">{errors.commune.message}</p>}
          </div>
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium">Adresse</label>
          <input {...register('address')} className="w-full rounded border border-black/20 px-3 py-2 text-sm" />
          {errors.address && <p className="mt-1 text-xs text-red-600">{errors.address.message}</p>}
        </div>

        <div>
          <label className="mb-2 block text-sm font-medium">Type de livraison</label>
          <div className="flex flex-col gap-2">
            {(Object.keys(DELIVERY_TYPE_LABELS) as (keyof typeof DELIVERY_TYPE_LABELS)[]).map((type) => (
              <label key={type} className="flex items-center gap-2 text-sm">
                <input type="radio" value={type} {...register('deliveryType')} />
                {DELIVERY_TYPE_LABELS[type]}
              </label>
            ))}
          </div>
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium">Notes (facultatif)</label>
          <textarea {...register('notes')} rows={2} className="w-full rounded border border-black/20 px-3 py-2 text-sm" />
        </div>

        {submitError && <p className="text-sm text-red-600">{submitError}</p>}

        <button
          type="submit"
          disabled={isSubmitting}
          className="mt-2 rounded-full bg-luna-black px-6 py-3 text-sm text-white disabled:opacity-40"
        >
          {isSubmitting ? 'Envoi...' : 'Confirmer la commande — Paiement à la livraison'}
        </button>
      </form>

      <div>
        <h2 className="mb-4 text-lg font-semibold">Résumé</h2>
        <div className="flex flex-col divide-y divide-black/5">
          {items.map((item) => (
            <div key={item.variantId} className="flex items-center justify-between py-2 text-sm">
              <span>
                {item.productName} ({item.color}/{item.size}) × {item.quantity}
              </span>
              <span>{formatPrice(item.unitPrice * item.quantity)}</span>
            </div>
          ))}
        </div>
        <div className="mt-4 flex items-center justify-between border-t border-black/10 pt-4 text-sm font-medium">
          <span>Sous-total</span>
          <span>{formatPrice(subtotal)}</span>
        </div>
        <div className="mt-1 flex items-center justify-between text-sm text-luna-charcoal/60">
          <span>Livraison</span>
          <span>Calculée à la préparation</span>
        </div>

        <div className="mt-4">
          <label className="mb-1 block text-sm font-medium">Code promo (facultatif)</label>
          <input
            value={couponCode}
            onChange={(e) => setCouponCode(e.target.value.toUpperCase())}
            placeholder="CODE2024"
            className="w-full rounded border border-black/20 px-3 py-2 text-sm"
          />
          <p className="mt-1 text-xs text-luna-charcoal/50">La réduction sera appliquée et affichée sur la confirmation.</p>
        </div>
      </div>
    </div>
  );
}
