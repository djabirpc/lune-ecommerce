import { useState } from 'react';
import { useMutation } from '@tanstack/react-query';

import { inventoryApi } from '../../lib/api/inventory';
import { ApiError } from '../../lib/api/client';
import type { SupplierDto } from '../../lib/api/types';

// Shared restock/adjust mini-form — used by both ProductVariantsPanel's per-product panel and the
// global InventoryPage table, so the two never drift out of sync on validation/error handling.
export function RestockAdjustControls({
  variantId,
  suppliers,
  onChanged,
}: {
  variantId: string;
  suppliers: SupplierDto[];
  onChanged: () => void;
}) {
  const [restockQty, setRestockQty] = useState(1);
  const [restockSupplierId, setRestockSupplierId] = useState('');
  const [restockUnitCost, setRestockUnitCost] = useState('');
  const [adjustDelta, setAdjustDelta] = useState(0);
  const [adjustReason, setAdjustReason] = useState('');
  const [error, setError] = useState<string | null>(null);

  const restock = useMutation({
    mutationFn: () =>
      inventoryApi.restock({
        productVariantId: variantId,
        quantity: restockQty,
        reason: 'Réassort admin',
        supplierId: restockSupplierId || null,
        unitCost: restockUnitCost ? Number(restockUnitCost) : null,
      }),
    onSuccess: () => {
      setError(null);
      setRestockUnitCost('');
      onChanged();
    },
    onError: (err) => setError(err instanceof ApiError ? err.message : 'Une erreur est survenue.'),
  });

  const adjust = useMutation({
    mutationFn: () => inventoryApi.adjust({ productVariantId: variantId, quantityDelta: adjustDelta, reason: adjustReason }),
    onSuccess: () => {
      setError(null);
      setAdjustDelta(0);
      setAdjustReason('');
      onChanged();
    },
    onError: (err) => setError(err instanceof ApiError ? err.message : 'Une erreur est survenue.'),
  });

  return (
    <div className="flex flex-col gap-1.5">
      <div className="flex flex-wrap items-center gap-1">
        <input
          type="number"
          min={1}
          value={restockQty}
          onChange={(e) => setRestockQty(Number(e.target.value))}
          className="w-14 rounded border border-black/20 px-1 py-0.5 text-xs"
          title="Quantité"
        />
        <select
          value={restockSupplierId}
          onChange={(e) => setRestockSupplierId(e.target.value)}
          className="w-24 rounded border border-black/20 px-1 py-0.5 text-xs"
        >
          <option value="">Fournisseur...</option>
          {suppliers.map((s) => (
            <option key={s.id} value={s.id}>
              {s.name}
            </option>
          ))}
        </select>
        <input
          type="number"
          min={0}
          step="0.01"
          value={restockUnitCost}
          onChange={(e) => setRestockUnitCost(e.target.value)}
          placeholder="Prix achat"
          className="w-20 rounded border border-black/20 px-1 py-0.5 text-xs"
        />
        <button
          type="button"
          disabled={restock.isPending}
          onClick={() => restock.mutate()}
          className="rounded border border-black/20 px-2 py-0.5 text-xs disabled:opacity-40"
        >
          Réassort
        </button>
      </div>
      <div className="flex flex-wrap items-center gap-1">
        <input
          type="number"
          value={adjustDelta}
          onChange={(e) => setAdjustDelta(Number(e.target.value))}
          className="w-16 rounded border border-black/20 px-1 py-0.5 text-xs"
          placeholder="±qté"
        />
        <input
          type="text"
          value={adjustReason}
          onChange={(e) => setAdjustReason(e.target.value)}
          className="w-28 rounded border border-black/20 px-1 py-0.5 text-xs"
          placeholder="Raison"
        />
        <button
          type="button"
          disabled={adjust.isPending || adjustDelta === 0 || !adjustReason}
          onClick={() => adjust.mutate()}
          className="rounded border border-black/20 px-2 py-0.5 text-xs disabled:opacity-40"
        >
          Ajuster
        </button>
      </div>
      {error && <p className="text-xs text-red-600">{error}</p>}
    </div>
  );
}
