import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Trash2 } from 'lucide-react';

import { carrierShippingRatesApi, shippingApi, shippingRatesApi } from '../../lib/api/shipping';
import { ApiError } from '../../lib/api/client';
import { SHIPPING_CARRIER_LABELS } from '../../lib/format/shippingLabels';
import { ALGERIAN_WILAYAS } from '../../lib/data/wilayas';
import type { CarrierShippingRateDto, SaveCarrierShippingRateRequest, ShippingCarrier, ShippingRateDto } from '../../lib/api/types';

const CARRIER_OPTIONS: ShippingCarrier[] = ['Fake', 'Yalidine', 'ZRExpress', 'Ecotrack48h'];

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

function CarrierRateRow({ rate, onChanged }: { rate: CarrierShippingRateDto; onChanged: () => void }) {
  const [homePrice, setHomePrice] = useState(rate.homeDeliveryPrice?.toString() ?? '');
  const [stopDeskPrice, setStopDeskPrice] = useState(rate.stopDeskPrice?.toString() ?? '');
  const [notes, setNotes] = useState(rate.notes ?? '');
  const [error, setError] = useState<string | null>(null);

  const save = useMutation({
    mutationFn: () =>
      carrierShippingRatesApi.update(rate.id, {
        carrier: rate.carrier,
        wilaya: rate.wilaya,
        homeDeliveryPrice: homePrice === '' ? null : Number(homePrice),
        stopDeskPrice: stopDeskPrice === '' ? null : Number(stopDeskPrice),
        notes: notes || null,
      }),
    onSuccess: () => {
      setError(null);
      onChanged();
    },
    onError: (err) => setError(err instanceof ApiError ? err.message : 'Une erreur est survenue.'),
  });

  const remove = useMutation({
    mutationFn: () => carrierShippingRatesApi.delete(rate.id),
    onSuccess: onChanged,
    onError: (err) => setError(err instanceof ApiError ? err.message : 'Une erreur est survenue.'),
  });

  const isDirty =
    homePrice !== (rate.homeDeliveryPrice?.toString() ?? '') ||
    stopDeskPrice !== (rate.stopDeskPrice?.toString() ?? '') ||
    notes !== (rate.notes ?? '');

  return (
    <tr className="border-b border-black/5 last:border-0">
      <td className="px-3 py-2 font-medium">{SHIPPING_CARRIER_LABELS[rate.carrier]}</td>
      <td className="px-3 py-2">{rate.wilaya}</td>
      <td className="px-3 py-2">
        <input
          type="number"
          min={0}
          value={homePrice}
          onChange={(e) => setHomePrice(e.target.value)}
          className="w-24 rounded border border-black/20 px-1 py-0.5 text-xs"
        />
      </td>
      <td className="px-3 py-2">
        <input
          type="number"
          min={0}
          value={stopDeskPrice}
          onChange={(e) => setStopDeskPrice(e.target.value)}
          className="w-24 rounded border border-black/20 px-1 py-0.5 text-xs"
        />
      </td>
      <td className="px-3 py-2">
        <input
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
          placeholder="Note"
          className="w-32 rounded border border-black/20 px-1 py-0.5 text-xs"
        />
      </td>
      <td className="px-3 py-2">
        <div className="flex items-center gap-2">
          <button
            type="button"
            disabled={save.isPending || !isDirty}
            onClick={() => save.mutate()}
            className="rounded border border-black/20 px-2 py-0.5 text-xs disabled:opacity-40"
          >
            Enregistrer
          </button>
          <button
            type="button"
            disabled={remove.isPending}
            onClick={() => remove.mutate()}
            aria-label="Supprimer ce tarif"
            className="rounded border border-red-200 p-1 text-red-600 hover:bg-red-50"
          >
            <Trash2 className="h-3.5 w-3.5" />
          </button>
        </div>
        {error && <p className="mt-1 text-xs text-red-600">{error}</p>}
      </td>
    </tr>
  );
}

function AddCarrierRateForm({ onAdded }: { onAdded: () => void }) {
  const [carrier, setCarrier] = useState<ShippingCarrier>('Ecotrack48h');
  const [wilaya, setWilaya] = useState(ALGERIAN_WILAYAS[0]);
  const [homePrice, setHomePrice] = useState('');
  const [stopDeskPrice, setStopDeskPrice] = useState('');
  const [notes, setNotes] = useState('');
  const [error, setError] = useState<string | null>(null);

  const create = useMutation({
    mutationFn: () => {
      const request: SaveCarrierShippingRateRequest = {
        carrier,
        wilaya,
        homeDeliveryPrice: homePrice === '' ? null : Number(homePrice),
        stopDeskPrice: stopDeskPrice === '' ? null : Number(stopDeskPrice),
        notes: notes || null,
      };
      return carrierShippingRatesApi.create(request);
    },
    onSuccess: () => {
      setHomePrice('');
      setStopDeskPrice('');
      setNotes('');
      setError(null);
      onAdded();
    },
    onError: (err) => setError(err instanceof ApiError ? err.message : 'Une erreur est survenue.'),
  });

  return (
    <div className="mb-4 grid gap-3 rounded-lg border border-black/10 bg-white p-4 sm:grid-cols-2 lg:grid-cols-5">
      <label className="flex flex-col gap-1 text-xs text-luna-charcoal/60">
        Transporteur
        <select
          value={carrier}
          onChange={(e) => setCarrier(e.target.value as ShippingCarrier)}
          className="rounded border border-black/20 px-2 py-1.5 text-sm"
        >
          {CARRIER_OPTIONS.map((c) => (
            <option key={c} value={c}>
              {SHIPPING_CARRIER_LABELS[c]}
            </option>
          ))}
        </select>
      </label>
      <label className="flex flex-col gap-1 text-xs text-luna-charcoal/60">
        Wilaya
        <select value={wilaya} onChange={(e) => setWilaya(e.target.value)} className="rounded border border-black/20 px-2 py-1.5 text-sm">
          {ALGERIAN_WILAYAS.map((w) => (
            <option key={w} value={w}>
              {w}
            </option>
          ))}
        </select>
      </label>
      <label className="flex flex-col gap-1 text-xs text-luna-charcoal/60">
        Domicile (DA)
        <input
          type="number"
          min={0}
          value={homePrice}
          onChange={(e) => setHomePrice(e.target.value)}
          placeholder="ex. 700"
          className="rounded border border-black/20 px-2 py-1.5 text-sm"
        />
      </label>
      <label className="flex flex-col gap-1 text-xs text-luna-charcoal/60">
        Stop Desk (DA)
        <input
          type="number"
          min={0}
          value={stopDeskPrice}
          onChange={(e) => setStopDeskPrice(e.target.value)}
          placeholder="ex. 450"
          className="rounded border border-black/20 px-2 py-1.5 text-sm"
        />
      </label>
      <div className="flex flex-col gap-1">
        <label className="flex flex-col gap-1 text-xs text-luna-charcoal/60">
          Note (optionnel)
          <input
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            placeholder="ex. tarif au 20/09"
            className="rounded border border-black/20 px-2 py-1.5 text-sm"
          />
        </label>
        <button
          type="button"
          disabled={create.isPending}
          onClick={() => create.mutate()}
          className="mt-1 rounded-full bg-luna-black px-4 py-1.5 text-xs text-white disabled:opacity-40"
        >
          + Ajouter
        </button>
      </div>
      {error && <p className="text-xs text-red-600 sm:col-span-2 lg:col-span-5">{error}</p>}
    </div>
  );
}

function CarrierRatesSection() {
  const queryClient = useQueryClient();
  const { data: rates, isLoading } = useQuery({
    queryKey: ['admin-carrier-shipping-rates'],
    queryFn: () => carrierShippingRatesApi.getAll(),
  });

  function refresh() {
    queryClient.invalidateQueries({ queryKey: ['admin-carrier-shipping-rates'] });
  }

  return (
    <div>
      <h2 className="mb-2 mt-8 text-lg font-semibold">Tarifs par transporteur (référence)</h2>
      <p className="mb-4 text-sm text-luna-charcoal/70">
        Notes personnelles — ce que chaque transporteur facture réellement, wilaya par wilaya. Aucun transporteur
        n'expose d'API de tarifs (vérifié dans leur documentation), donc ces valeurs sont saisies à la main. Elles
        n'affectent jamais le montant de livraison facturé au client (voir le tableau ci-dessus pour ça).
      </p>

      <AddCarrierRateForm onAdded={refresh} />

      {isLoading && <p className="text-sm text-luna-charcoal/60">Chargement...</p>}

      {rates && rates.length === 0 && (
        <p className="text-sm text-luna-charcoal/50">Aucun tarif enregistré pour le moment — ajoute-en un ci-dessus.</p>
      )}

      {rates && rates.length > 0 && (
        <div className="overflow-x-auto rounded-lg border border-black/10 bg-white">
          <table className="w-full text-left text-sm">
            <thead className="border-b border-black/10 bg-luna-cream/40 text-xs uppercase text-luna-charcoal/60">
              <tr>
                <th className="px-3 py-2">Transporteur</th>
                <th className="px-3 py-2">Wilaya</th>
                <th className="px-3 py-2">Domicile</th>
                <th className="px-3 py-2">Stop Desk</th>
                <th className="px-3 py-2">Note</th>
                <th className="px-3 py-2" />
              </tr>
            </thead>
            <tbody>
              {rates.map((rate) => (
                <CarrierRateRow key={rate.id} rate={rate} onChanged={refresh} />
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
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
        48h Express est fonctionnel mais nécessite vos identifiants de compte (Ecotrack48h__BaseUrl /
        Ecotrack48h__ApiToken) pour s'activer.
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

      <CarrierRatesSection />
    </div>
  );
}
