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
      <h1 className="mb-6 font-display text-2xl italic text-luna-black">Promotions</h1>

      {isLoading && <p className="text-sm text-luna-charcoal/60">Chargement...</p>}

      {promotions && promotions.length === 0 && (
        <p className="text-sm text-luna-charcoal/70">Aucune promotion en cours pour le moment.</p>
      )}

      <div className="flex flex-col gap-3">
        {promotions?.map((promo) => (
          <div key={promo.id} className="rounded-2xl border border-black/10 p-5">
            <div className="flex items-center justify-between gap-2">
              <h2 className="font-display text-lg italic text-luna-black">{promo.name}</h2>
              <span className="shrink-0 rounded-full bg-luna-accent px-3 py-1 text-xs font-semibold text-white">
                {promo.percentageValue
                  ? `-${promo.percentageValue}%`
                  : promo.fixedAmountValue
                    ? `-${formatPrice(promo.fixedAmountValue)}`
                    : PROMOTION_TYPE_LABELS[promo.type]}
              </span>
            </div>
            {promo.description && <p className="mt-1.5 text-sm text-luna-charcoal/70">{promo.description}</p>}
            <p className="mt-3 text-xs text-luna-charcoal/50">
              Jusqu'au {new Date(promo.endsAtUtc).toLocaleDateString('fr-FR')}
            </p>
          </div>
        ))}
      </div>
    </div>
  );
}
