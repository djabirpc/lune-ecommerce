import { useParams, Link } from 'react-router-dom';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { ArrowLeft, Package } from 'lucide-react';

import { inventoryApi } from '../../lib/api/inventory';
import { suppliersApi } from '../../lib/api/suppliers';
import { formatPrice } from '../../lib/format/price';
import { RestockAdjustControls } from '../components/RestockAdjustControls';

// Matches InventoryService's own LowStockThreshold.
const LOW_STOCK_THRESHOLD = 5;

function marginLabel(price: number, costPrice: number | null): string {
  if (costPrice === null || costPrice <= 0) return '—';
  return `${(((price - costPrice) / price) * 100).toFixed(0)} %`;
}

// Dedicated stock detail page for one product — reached by clicking a product row on
// /admin/inventory (2026-09-21 follow-up: that used to be an inline-expanding row, now a real page).
export function InventoryProductDetailPage() {
  const { productId } = useParams<{ productId: string }>();
  const queryClient = useQueryClient();

  const { data: product, isLoading, isError } = useQuery({
    queryKey: ['admin-inventory-product', productId],
    queryFn: () => inventoryApi.getByProduct(productId!),
    enabled: !!productId,
  });

  const { data: suppliersResult } = useQuery({
    queryKey: ['admin-suppliers', { includeInactive: false }],
    queryFn: () => suppliersApi.getPaged({ pageSize: 100 }),
  });
  const suppliers = suppliersResult?.items ?? [];

  function refresh() {
    queryClient.invalidateQueries({ queryKey: ['admin-inventory-product', productId] });
  }

  return (
    <div>
      <Link to="/admin/inventory" className="mb-4 flex items-center gap-1.5 text-sm text-luna-charcoal/60 hover:underline">
        <ArrowLeft className="h-4 w-4" /> Retour au stock
      </Link>

      {isLoading && <p className="text-sm text-luna-charcoal/60">Chargement...</p>}
      {isError && <p className="text-sm text-red-600">Impossible de charger ce produit.</p>}

      {product && (
        <>
          <div className="mb-4 flex items-center gap-3">
            <div className="h-16 w-16 shrink-0 overflow-hidden rounded bg-luna-cream">
              {product.imageUrl ? (
                <img src={product.imageUrl} alt="" className="h-full w-full object-cover" />
              ) : (
                <div className="flex h-full w-full items-center justify-center text-luna-charcoal/30">
                  <Package className="h-6 w-6" />
                </div>
              )}
            </div>
            <div>
              <h1 className="text-xl font-semibold">{product.productName}</h1>
              <p className="text-xs text-luna-charcoal/50">
                {product.categoryName} · {product.variants.length} variante{product.variants.length > 1 ? 's' : ''}
              </p>
            </div>
          </div>

          <div className="overflow-x-auto rounded-lg border border-black/10 bg-white">
            <table className="w-full text-left text-sm">
              <thead className="border-b border-black/10 text-xs uppercase text-luna-charcoal/60">
                <tr>
                  <th className="px-3 py-2">Couleur / Taille</th>
                  <th className="px-3 py-2">SKU</th>
                  <th className="px-3 py-2">Achat</th>
                  <th className="px-3 py-2">Vente</th>
                  <th className="px-3 py-2">Marge</th>
                  <th className="px-3 py-2">Disponible</th>
                  <th className="px-3 py-2">Réservé</th>
                  <th className="px-3 py-2">Vendu</th>
                  <th className="px-3 py-2">Retourné</th>
                  <th className="px-3 py-2">Endommagé</th>
                  <th className="px-3 py-2">Actions</th>
                </tr>
              </thead>
              <tbody>
                {product.variants.map((v) => (
                  <tr key={v.productVariantId} className="border-b border-black/5 last:border-0">
                    <td className="px-3 py-2">
                      {v.color} / {v.size}
                    </td>
                    <td className="px-3 py-2 font-mono text-xs">{v.sku}</td>
                    <td className="px-3 py-2">{v.costPrice !== null ? formatPrice(v.costPrice) : '—'}</td>
                    <td className="px-3 py-2">{formatPrice(v.price)}</td>
                    <td className="px-3 py-2">{marginLabel(v.price, v.costPrice)}</td>
                    <td
                      className={`px-3 py-2 font-medium ${
                        v.availableQuantity === 0 ? 'text-red-600' : v.availableQuantity <= LOW_STOCK_THRESHOLD ? 'text-amber-600' : ''
                      }`}
                    >
                      {v.availableQuantity}
                    </td>
                    <td className="px-3 py-2">{v.reservedQuantity}</td>
                    <td className="px-3 py-2">{v.soldQuantity}</td>
                    <td className="px-3 py-2">{v.returnedQuantity}</td>
                    <td className="px-3 py-2">{v.damagedQuantity}</td>
                    <td className="px-3 py-2">
                      <RestockAdjustControls variantId={v.productVariantId} suppliers={suppliers} onChanged={refresh} />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </>
      )}
    </div>
  );
}
