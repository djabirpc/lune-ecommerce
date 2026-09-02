import { useQuery } from '@tanstack/react-query';

import { shippingApi } from '../../lib/api/shipping';
import { SHIPPING_CARRIER_LABELS } from '../../lib/format/shippingLabels';

export function ShippingPage() {
  const { data: carriers, isLoading } = useQuery({
    queryKey: ['shipping-carriers'],
    queryFn: () => shippingApi.getCarriers(),
  });

  return (
    <div>
      <h1 className="mb-4 text-xl font-semibold">Livraison</h1>
      <p className="mb-4 text-sm text-luna-charcoal/70">
        Transporteurs disponibles pour la création d'expéditions. Yalidine et ZR Express nécessitent la
        documentation API officielle du transporteur avant de pouvoir être implémentés — voir PROJECT_CONTEXT.md.
      </p>

      {isLoading && <p className="text-sm text-luna-charcoal/60">Chargement...</p>}

      <div className="flex flex-col divide-y divide-black/5 rounded-lg border border-black/10 bg-white text-sm sm:max-w-md">
        {carriers?.map((c) => (
          <div key={c.carrier} className="flex items-center justify-between px-4 py-3">
            <span className="font-medium">{SHIPPING_CARRIER_LABELS[c.carrier]}</span>
            {c.isConfigured ? (
              <span className="rounded-full bg-green-100 px-3 py-1 text-xs text-green-700">Disponible</span>
            ) : (
              <span
                className="rounded-full bg-luna-cream px-3 py-1 text-xs text-luna-charcoal/60"
                title={c.unavailableReason ?? undefined}
              >
                Non disponible
              </span>
            )}
          </div>
        ))}
      </div>
    </div>
  );
}
