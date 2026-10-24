import { useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { ChevronRight, Package } from 'lucide-react';

import { inventoryApi } from '../../lib/api/inventory';
import { catalogApi } from '../../lib/api/catalog';
import type { InventoryProductOverviewDto, InventoryStockStatus } from '../../lib/api/types';

const PAGE_SIZE = 20;
// Matches DashboardService's/InventoryService's own LowStockThreshold — see backend comment.
const LOW_STOCK_THRESHOLD = 5;

function ProductStockBadge({ product }: { product: InventoryProductOverviewDto }) {
  const totalAvailable = product.variants.reduce((sum, v) => sum + v.availableQuantity, 0);
  const hasOut = product.variants.some((v) => v.availableQuantity <= 0);
  const hasLow = product.variants.some((v) => v.availableQuantity > 0 && v.availableQuantity <= LOW_STOCK_THRESHOLD);
  const tone = hasOut ? 'bg-red-100 text-red-700' : hasLow ? 'bg-amber-100 text-amber-700' : 'bg-luna-cream text-luna-charcoal/70';

  return <span className={`shrink-0 rounded-full px-2.5 py-1 text-xs font-medium ${tone}`}>{totalAvailable} en stock</span>;
}

// Global, filterable stock view across the whole catalog, organized by category then product —
// complements ProductsPage's per-product restock/adjust panel with a cross-product "what needs
// attention" view. Each product row links to InventoryProductDetailPage (2026-09-21 follow-up — a
// real page instead of an inline-expanding row), with a photo thumbnail for quicker visual scanning.
export function InventoryPage() {
  const [search, setSearch] = useState('');
  const [categoryId, setCategoryId] = useState('');
  const [stockStatus, setStockStatus] = useState<InventoryStockStatus>('All');
  const [page, setPage] = useState(1);

  const { data: categories } = useQuery({
    queryKey: ['admin-categories-for-inventory'],
    queryFn: () => catalogApi.getCategories({ includeInactive: true }),
  });

  const { data, isLoading, isError } = useQuery({
    queryKey: ['admin-inventory-overview', { search, categoryId, stockStatus, page }],
    queryFn: () =>
      inventoryApi.getOverview({
        search: search || undefined,
        categoryId: categoryId || undefined,
        stockStatus,
        page,
        pageSize: PAGE_SIZE,
      }),
  });

  const totalPages = data ? Math.max(1, Math.ceil(data.totalCount / PAGE_SIZE)) : 1;

  // Products are already ranked "lowest stock first" by the backend — grouping here only clusters
  // same-category products together for display, it doesn't re-sort them.
  const groups = useMemo(() => {
    const map = new Map<string, { categoryName: string; products: InventoryProductOverviewDto[] }>();
    for (const product of data?.items ?? []) {
      const existing = map.get(product.categoryId);
      if (existing) {
        existing.products.push(product);
      } else {
        map.set(product.categoryId, { categoryName: product.categoryName, products: [product] });
      }
    }
    return [...map.values()];
  }, [data]);

  return (
    <div>
      <h1 className="mb-4 text-xl font-semibold">Stock</h1>

      <div className="mb-4 grid gap-3 rounded-lg border border-black/10 bg-white p-4 sm:grid-cols-3">
        <label className="flex flex-col gap-1 text-xs text-luna-charcoal/60">
          Recherche (produit, SKU)
          <input
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
              setPage(1);
            }}
            placeholder="ex. Robe ou SKU-..."
            className="rounded border border-black/20 px-3 py-1.5 text-sm"
          />
        </label>
        <label className="flex flex-col gap-1 text-xs text-luna-charcoal/60">
          Catégorie
          <select
            value={categoryId}
            onChange={(e) => {
              setCategoryId(e.target.value);
              setPage(1);
            }}
            className="rounded border border-black/20 px-3 py-1.5 text-sm"
          >
            <option value="">Toutes les catégories</option>
            {(categories ?? []).map((c) => (
              <option key={c.id} value={c.id}>
                {c.name}
              </option>
            ))}
          </select>
        </label>
        <label className="flex flex-col gap-1 text-xs text-luna-charcoal/60">
          Statut du stock
          <select
            value={stockStatus}
            onChange={(e) => {
              setStockStatus(e.target.value as InventoryStockStatus);
              setPage(1);
            }}
            className="rounded border border-black/20 px-3 py-1.5 text-sm"
          >
            <option value="All">Tous</option>
            <option value="Low">Stock faible</option>
            <option value="Out">Rupture</option>
          </select>
        </label>
      </div>

      {isLoading && <p className="text-sm text-luna-charcoal/60">Chargement...</p>}
      {isError && <p className="text-sm text-red-600">Impossible de charger le stock.</p>}

      {data && groups.length === 0 && <p className="text-sm text-luna-charcoal/50">Aucun produit pour ces filtres.</p>}

      {groups.map((group) => (
        <div key={group.categoryName} className="mb-6">
          <h2 className="mb-2 text-sm font-semibold uppercase text-luna-charcoal/60">{group.categoryName}</h2>
          <div className="divide-y divide-black/5 rounded-lg border border-black/10 bg-white">
            {group.products.map((product) => (
              <Link
                key={product.productId}
                to={`/admin/inventory/${product.productId}`}
                className="flex w-full items-center gap-3 px-4 py-3 text-left hover:bg-luna-cream/40"
              >
                <div className="h-12 w-12 shrink-0 overflow-hidden rounded bg-luna-cream">
                  {product.imageUrl ? (
                    <img src={product.imageUrl} alt="" className="h-full w-full object-cover" />
                  ) : (
                    <div className="flex h-full w-full items-center justify-center text-luna-charcoal/30">
                      <Package className="h-5 w-5" />
                    </div>
                  )}
                </div>
                <div className="min-w-0 flex-1">
                  <p className="truncate text-sm font-medium text-luna-black">{product.productName}</p>
                  <p className="text-xs text-luna-charcoal/50">
                    {product.variants.length} variante{product.variants.length > 1 ? 's' : ''}
                  </p>
                </div>
                <ProductStockBadge product={product} />
                <ChevronRight className="h-4 w-4 shrink-0 text-luna-charcoal/40" />
              </Link>
            ))}
          </div>
        </div>
      ))}

      {data && groups.length > 0 && (
        <div className="mt-4 flex items-center justify-between text-sm">
          <span className="text-luna-charcoal/60">
            Page {data.page} / {totalPages} ({data.totalCount} produits)
          </span>
          <div className="flex gap-2">
            <button
              type="button"
              disabled={page <= 1}
              onClick={() => setPage((p) => p - 1)}
              className="rounded border border-black/20 px-3 py-1 disabled:opacity-40"
            >
              Précédent
            </button>
            <button
              type="button"
              disabled={page >= totalPages}
              onClick={() => setPage((p) => p + 1)}
              className="rounded border border-black/20 px-3 py-1 disabled:opacity-40"
            >
              Suivant
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
