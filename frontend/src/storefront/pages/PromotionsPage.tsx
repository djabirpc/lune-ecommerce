import { useQuery } from '@tanstack/react-query';

import { promotionsApi } from '../../lib/api/promotions';
import { formatPrice } from '../../lib/format/price';
import { PROMOTION_TYPE_LABELS } from '../../lib/format/promotionLabels';

export function PromotionsPage() {
  const { data: promotions, isLoading } = useQuery({
    queryKey: ['storefront-active-promotions'],
    queryFn: () => promotionsApi.getActive(),
  });

  return (
    <div className="mx-auto max-w-2xl px-4 py-10">
      <h1 className="mb-6 text-xl font-semibold text-luna-black">Promotions</h1>

      {isLoading && <p className="text-sm text-luna-charcoal/60">Chargement...</p>}

      {promotions && promotions.length === 0 && (
        <p className="text-sm text-luna-charcoal/70">Aucune promotion en cours pour le moment.</p>
      )}

      <div className="flex flex-col gap-3">
        {promotions?.map((promo) => (
          <div key={promo.id} className="rounded-lg border border-black/10 p-4">
            <div className="flex items-center justify-between gap-2">
              <h2 className="font-medium text-luna-black">{promo.name}</h2>
              <span className="shrink-0 rounded-full bg-luna-cream px-3 py-1 text-xs font-medium text-luna-black">
                {promo.percentageValue
                  ? `-${promo.percentageValue}%`
                  : promo.fixedAmountValue
                    ? `-${formatPrice(promo.fixedAmountValue)}`
                    : PROMOTION_TYPE_LABELS[promo.type]}
              </span>
            </div>
            {promo.description && <p className="mt-1 text-sm text-luna-charcoal/70">{promo.description}</p>}
            <p className="mt-2 text-xs text-luna-charcoal/50">
              Jusqu'au {new Date(promo.endsAtUtc).toLocaleDateString('fr-FR')}
            </p>
          </div>
        ))}
      </div>
    </div>
  );
}
