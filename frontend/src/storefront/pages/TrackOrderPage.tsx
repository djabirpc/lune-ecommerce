import { useState } from 'react';
import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import { z } from 'zod';

import { ordersApi } from '../../lib/api/orders';
import { ApiError } from '../../lib/api/client';
import { OrderDetailsCard } from '../../lib/components/OrderDetailsCard';
import type { OrderDetailDto } from '../../lib/api/types';

const trackSchema = z.object({
  orderNumber: z.string().min(1, 'Le numéro de commande est requis.'),
  phone: z.string().regex(/^0[0-9]{9}$/, 'Numéro de téléphone algérien invalide.'),
});

type TrackFormValues = z.infer<typeof trackSchema>;

const inputClass = 'w-full rounded-lg border border-black/15 px-3.5 py-2.5 text-sm outline-none transition focus:border-luna-black';

export function TrackOrderPage() {
  const [order, setOrder] = useState<OrderDetailDto | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<TrackFormValues>({ resolver: zodResolver(trackSchema) });

  async function onSubmit(values: TrackFormValues) {
    setError(null);
    setOrder(null);
    setIsSubmitting(true);
    try {
      const result = await ordersApi.track(values.orderNumber.trim(), values.phone.trim());
      setOrder(result);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Commande introuvable.');
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div className="mx-auto max-w-lg px-4 py-10">
      <h1 className="mb-1 font-display text-2xl italic text-luna-black">Suivre ma commande</h1>
      <p className="mb-6 text-sm text-luna-charcoal/70">Entrez votre numéro de commande et de téléphone pour voir son statut.</p>

      <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-4">
        <div>
          <label className="mb-1 block text-sm font-medium">Numéro de commande</label>
          <input {...register('orderNumber')} placeholder="LUNA-260101-1234" className={inputClass} />
          {errors.orderNumber && <p className="mt-1 text-xs text-red-600">{errors.orderNumber.message}</p>}
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium">Téléphone</label>
          <input {...register('phone')} placeholder="0551234567" className={inputClass} inputMode="numeric" />
          {errors.phone && <p className="mt-1 text-xs text-red-600">{errors.phone.message}</p>}
        </div>

        <button
          type="submit"
          disabled={isSubmitting}
          className="rounded-full bg-luna-black px-6 py-3.5 text-sm font-medium text-white transition hover:bg-luna-charcoal disabled:opacity-40"
        >
          {isSubmitting ? 'Recherche...' : 'Rechercher'}
        </button>
      </form>

      {error && <p className="mt-4 text-center text-sm text-red-600">{error}</p>}

      {order && (
        <div className="mt-6">
          <OrderDetailsCard order={order} />
        </div>
      )}
    </div>
  );
}
