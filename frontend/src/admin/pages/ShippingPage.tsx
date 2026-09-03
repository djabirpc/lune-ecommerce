import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { shippingApi, shippingRatesApi } from '../../lib/api/shipping';
import { ApiError } from '../../lib/api/client';
import { SHIPPING_CARRIER_LABELS } from '../../lib/format/shippingLabels';
import type { ShippingRateDto } from '../../lib/api/types';

function ShippingRateRow({ rate, onChanged }: { rate: ShippingRateDto; onChanged: () => void }) {
  const [homePrice, setHomePrice] = useState(rate.homeDeliveryPrice);
  const [stopDeskPrice, setStopDeskPrice] = useState(rate.stopDeskPrice);
  const [isActive, setIsActive] = useState(rate.isActive);
  const [error, setError] = useState<string | null>(null);

  const update = useMutation({
    mutationFn: () =>
      shippingRatesApi.update(rate.wilaya, {
        homeDeliveryPrice: homePrice,
        stopDeskPrice: stopDeskPrice,
        isActive,
      }),
    onSuccess: () => {
      setError(null);
      onChanged();
    },
    onError: (err) => setError(err instanceof ApiError ? err.message : 'Une erreur est survenue.'),
  });

  const isDirty = homePrice !== rate.homeDeliveryPrice || stopDeskPrice !== rate.stopDeskPrice || isActive !== rate.isActive;

  return (
    <tr className="border-b border-black/5 last:border-0">
      <td className="px-3 py-2 font-medium">{rate.wilaya}</td>
      <td className="px-3 py-2">
        <input
          type="number"
          min={0}
          value={homePrice}
          onChange={(e) => setHomePrice(Number(e.target.value))}
          className="w-24 rounded border border-black/20 px-1 py-0.5 text-xs"
        />
      </td>
      <td className="px-3 py-2">
        <input
          type="number"
          min={0}
          value={stopDeskPrice}
          onChange={(e) => setStopDeskPrice(Number(e.target.value))}
          className="w-24 rounded border border-black/20 px-1 py-0.5 text-xs"
        />
      </td>
      <td className="px-3 py-2 text-center">
        <input type="checkbox" checked={isActive} onChange={(e) => setIsActive(e.target.checked)} />
      </td>
      <td className="px-3 py-2">
        <button
          type="button"
          disabled={update.isPending || !isDirty}
          onClick={() => update.mutate()}
          className="rounded border border-black/20 px-2 py-0.5 text-xs disabled:opacity-40"
        >
          Enregistrer
        </button>
        {error && <p className="mt-1 text-xs text-red-600">{error}</p>}
      </td>
    </tr>
  );
}

export function ShippingPage() {
  const { data: carriers, isLoading: carriersLoading } = useQuery({
    queryKey: ['shipping-carriers'],
    queryFn: () => shippingApi.getCarriers(),
  });

  const queryClient = useQueryClient();
  const { data: rates, isLoading: ratesLoading } = useQuery({
    queryKey: ['admin-shipping-rates'],
    queryFn: () => shippingRatesApi.getAll(),
  });

  return (
    <div>
      <h1 className="mb-4 text-xl font-semibold">Livraison</h1>
      <p className="mb-4 text-sm text-luna-charcoal/70">
        Transporteurs disponibles pour la création d'expéditions. Yalidine et ZR Express nécessitent la
        documentation API officielle du transporteur avant de pouvoir être implémentés — voir PROJECT_CONTEXT.md.
      </p>

      {carriersLoading && <p className="text-sm text-luna-charcoal/60">Chargement...</p>}

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

      <h2 className="mb-2 mt-8 text-lg font-semibold">Tarifs de livraison par wilaya</h2>
      <p className="mb-4 text-sm text-luna-charcoal/70">
        Ces tarifs (pas ceux d'un transporteur) déterminent le montant de livraison facturé au client au moment
        de la commande. Ils sont pré-remplis avec une valeur par défaut — ajustez-les selon vos coûts réels.
      </p>

      {ratesLoading && <p className="text-sm text-luna-charcoal/60">Chargement...</p>}

      {rates && (
        <div className="overflow-x-auto rounded-lg border border-black/10 bg-white">
          <table className="w-full text-left text-sm">
            <thead className="border-b border-black/10 bg-luna-cream/40 text-xs uppercase text-luna-charcoal/60">
              <tr>
                <th className="px-3 py-2">Wilaya</th>
                <th className="px-3 py-2">Domicile</th>
                <th className="px-3 py-2">Stop Desk</th>
                <th className="px-3 py-2 text-center">Active</th>
                <th className="px-3 py-2" />
              </tr>
            </thead>
            <tbody>
              {rates.map((rate) => (
                <ShippingRateRow
                  key={rate.wilaya}
                  rate={rate}
                  onChanged={() => queryClient.invalidateQueries({ queryKey: ['admin-shipping-rates'] })}
                />
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
