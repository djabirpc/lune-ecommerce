import { useMemo, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { useTranslation } from 'react-i18next';

import { ordersApi } from '../../lib/api/orders';
import { ApiError } from '../../lib/api/client';
import { OrderDetailsCard } from '../../lib/components/OrderDetailsCard';
import { findPhoneForOrder, rememberOrder } from '../../lib/orders/localOrderHistory';
import type { OrderDetailDto } from '../../lib/api/types';

function createTrackSchema(t: (key: string) => string) {
  return z.object({
    orderNumber: z.string().min(1, t('trackOrder.validation.orderNumberRequired')),
    phone: z.string().regex(/^0[0-9]{9}$/, t('trackOrder.validation.phoneInvalid')),
  });
}

type TrackFormValues = z.infer<ReturnType<typeof createTrackSchema>>;

const inputClass = 'h-12 w-full rounded-sm border border-black/15 px-3.5 text-sm outline-none transition focus:border-luna-black';

export function TrackOrderPage() {
  const { t } = useTranslation();
  const [searchParams] = useSearchParams();
  const prefilledOrderNumber = searchParams.get('orderNumber') ?? '';
  const [order, setOrder] = useState<OrderDetailDto | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const trackSchema = useMemo(() => createTrackSchema(t), [t]);

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<TrackFormValues>({
    resolver: zodResolver(trackSchema),
    defaultValues: {
      orderNumber: prefilledOrderNumber,
      phone: prefilledOrderNumber ? (findPhoneForOrder(prefilledOrderNumber) ?? '') : '',
    },
  });

  async function onSubmit(values: TrackFormValues) {
    setError(null);
    setOrder(null);
    setIsSubmitting(true);
    try {
      const result = await ordersApi.track(values.orderNumber.trim(), values.phone.trim());
      setOrder(result);
      rememberOrder(values.orderNumber.trim(), values.phone.trim());
    } catch (err) {
      setError(err instanceof ApiError ? err.message : t('trackOrder.notFound'));
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div className="mx-auto max-w-lg px-4 py-10">
      <p className="eyebrow">{t('trackOrder.eyebrow')}</p>
      <h1 className="mt-1 font-display text-4xl text-luna-black">{t('layout.trackOrder')}</h1>
      <p className="mt-2 mb-6 text-sm text-luna-charcoal/70">{t('trackOrder.subtitle')}</p>

      <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-4">
        <div>
          <label className="mb-1 block text-sm font-medium text-luna-black">{t('trackOrder.orderNumber')}</label>
          <input {...register('orderNumber')} placeholder="LUNA-260101-1234" className={inputClass} />
          {errors.orderNumber && <p className="mt-1 text-xs text-red-600">{errors.orderNumber.message}</p>}
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium text-luna-black">{t('checkout.phone')}</label>
          <input {...register('phone')} placeholder="0551234567" className={inputClass} inputMode="numeric" />
          {errors.phone && <p className="mt-1 text-xs text-red-600">{errors.phone.message}</p>}
        </div>

        <button
          type="submit"
          disabled={isSubmitting}
          className="h-12 rounded-sm bg-luna-black text-sm font-medium text-white transition hover:bg-luna-charcoal disabled:opacity-40"
        >
          {isSubmitting ? t('trackOrder.searching') : t('trackOrder.search')}
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
