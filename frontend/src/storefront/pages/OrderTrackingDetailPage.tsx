import { Link, useParams } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';

import { ordersApi } from '../../lib/api/orders';
import { findPhoneForOrder } from '../../lib/orders/localOrderHistory';
import { OrderDetailsCard } from '../../lib/components/OrderDetailsCard';
import { PagePlaceholder } from '../../lib/components/PagePlaceholder';

export function OrderTrackingDetailPage() {
  const { t, i18n } = useTranslation();
  const { orderNumber } = useParams<{ orderNumber: string }>();
  const phone = orderNumber ? findPhoneForOrder(orderNumber) : undefined;

  const { data: order, isLoading, isError } = useQuery({
    queryKey: ['order-tracking', orderNumber, phone],
    queryFn: () => ordersApi.track(orderNumber!, phone!),
    enabled: Boolean(orderNumber && phone),
  });

  if (!orderNumber) {
    return <PagePlaceholder title={t('orderTrackingDetail.notFound')} />;
  }

  // No phone remembered on this device (different browser, cleared storage, ...) — fall back to
  // the manual, phone-verified lookup form rather than guessing/exposing the order without proof.
  if (!phone) {
    return (
      <div className="mx-auto flex max-w-md flex-col items-center px-4 py-24 text-center">
        <h1 className="font-display text-3xl text-luna-black">{t('orderTrackingDetail.confirmPhone.title')}</h1>
        <p className="mt-2 text-sm text-luna-charcoal/70">{t('orderTrackingDetail.confirmPhone.body')}</p>
        <Link
          to={`/track-order?orderNumber=${orderNumber}`}
          className="mt-6 rounded-sm bg-luna-black px-7 py-3 text-sm text-white"
        >
          {t('layout.trackOrder')}
        </Link>
      </div>
    );
  }

  if (isLoading) {
    return <div className="px-4 py-24 text-center text-sm text-luna-charcoal/60">{t('common.loading')}</div>;
  }

  if (isError || !order) {
    return <PagePlaceholder title={t('orderTrackingDetail.notFound')} />;
  }

  return (
    <div className="mx-auto max-w-2xl px-4 py-8">
      <Link to="/orders" className="text-xs text-luna-charcoal/60 hover:text-luna-black">
        <span className="inline-block rtl:rotate-180">←</span> {t('orderTrackingDetail.backToOrders')}
      </Link>
      <h1 className="mt-2 font-display text-4xl text-luna-black">{order.orderNumber}</h1>
      <p className="mt-1 text-sm text-luna-charcoal/70">
        {t('orderTrackingDetail.placedOn', {
          date: new Date(order.createdAtUtc).toLocaleDateString(i18n.language === 'ar' ? 'ar-DZ' : 'fr-FR'),
        })}{' '}
        · {t('layout.footer.codTitle')}
      </p>

      <div className="mt-8">
        <OrderDetailsCard order={order} />
      </div>
    </div>
  );
}
