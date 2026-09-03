import { Fragment, useState } from 'react';
import { useQuery } from '@tanstack/react-query';

import { catalogApi } from '../../lib/api/catalog';
import { formatPrice } from '../../lib/format/price';
import { CategoryQuickManager } from '../components/CategoryQuickManager';
import { CreateProductForm } from '../components/CreateProductForm';
import { EditProductForm } from '../components/EditProductForm';
import { ProductImagesPanel } from '../components/ProductImagesPanel';
import { ProductVariantsPanel } from '../components/ProductVariantsPanel';

const PAGE_SIZE = 20;

type PanelMode = 'variants' | 'edit' | 'images';
type ExpandedPanel = { slug: string; mode: PanelMode } | null;

export function ProductsPage() {
  const [page, setPage] = useState(1);
  const [expanded, setExpanded] = useState<ExpandedPanel>(null);

  function togglePanel(slug: string, mode: PanelMode) {
    setExpanded((current) => (current?.slug === slug && current.mode === mode ? null : { slug, mode }));
  }

  const { data, isLoading, isError } = useQuery({
    queryKey: ['admin-products', { page }],
    queryFn: () => catalogApi.getProducts({ page, pageSize: PAGE_SIZE, includeInactive: true }),
  });

  const totalPages = data ? Math.max(1, Math.ceil(data.totalCount / PAGE_SIZE)) : 1;

  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-xl font-semibold">Produits</h1>

      <CategoryQuickManager />

      <CreateProductForm />

      <div>
        <h2 className="mb-3 text-sm font-semibold uppercase text-luna-charcoal/60">Catalogue</h2>

        {isLoading && <p className="text-sm text-luna-charcoal/60">Chargement...</p>}
        {isError && <p className="text-sm text-red-600">Impossible de charger les produits.</p>}

        {data && (
          <>
            <div className="overflow-x-auto rounded-lg border border-black/10 bg-white">
              <table className="w-full text-left text-sm">
                <thead className="border-b border-black/10 text-xs uppercase text-luna-charcoal/60">
                  <tr>
                    <th className="px-4 py-2">Nom</th>
                    <th className="px-4 py-2">Catégorie</th>
                    <th className="px-4 py-2">Prix</th>
                    <th className="px-4 py-2">Statut</th>
                    <th className="px-4 py-2" />
                  </tr>
                </thead>
                <tbody>
                  {data.items.map((product) => (
                    <Fragment key={product.id}>
                      <tr className="border-b border-black/5 last:border-0 hover:bg-luna-cream/50">
                        <td className="px-4 py-2">{product.name}</td>
                        <td className="px-4 py-2">{product.categoryName}</td>
                        <td className="px-4 py-2">{formatPrice(product.price)}</td>
                        <td className="px-4 py-2">
                          <span
                            className={`rounded-full px-2 py-1 text-xs ${
                              product.isActive ? 'bg-green-100 text-green-800' : 'bg-luna-cream text-luna-charcoal/50'
                            }`}
                          >
                            {product.isActive ? 'Actif' : 'Inactif'}
                          </span>
                        </td>
                        <td className="px-4 py-2 text-right">
                          <div className="flex justify-end gap-3">
                            <button
                              type="button"
                              onClick={() => togglePanel(product.slug, 'edit')}
                              className="text-xs underline"
                            >
                              {expanded?.slug === product.slug && expanded.mode === 'edit' ? 'Fermer' : 'Modifier'}
                            </button>
                            <button
                              type="button"
                              onClick={() => togglePanel(product.slug, 'images')}
                              className="text-xs underline"
                            >
                              {expanded?.slug === product.slug && expanded.mode === 'images' ? 'Fermer' : 'Images'}
                            </button>
                            <button
                              type="button"
                              onClick={() => togglePanel(product.slug, 'variants')}
                              className="text-xs underline"
                            >
                              {expanded?.slug === product.slug && expanded.mode === 'variants' ? 'Fermer' : 'Variantes / Stock'}
                            </button>
                          </div>
                        </td>
                      </tr>
                      {expanded?.slug === product.slug && expanded.mode === 'edit' && (
                        <tr>
                          <td colSpan={5} className="p-0">
                            <EditProductForm slug={product.slug} onDone={() => setExpanded(null)} />
                          </td>
                        </tr>
                      )}
                      {expanded?.slug === product.slug && expanded.mode === 'images' && (
                        <tr>
                          <td colSpan={5} className="p-0">
                            <ProductImagesPanel slug={product.slug} />
                          </td>
                        </tr>
                      )}
                      {expanded?.slug === product.slug && expanded.mode === 'variants' && (
                        <tr>
                          <td colSpan={5} className="p-0">
                            <ProductVariantsPanel slug={product.slug} />
                          </td>
                        </tr>
                      )}
                    </Fragment>
                  ))}
                  {data.items.length === 0 && (
                    <tr>
                      <td colSpan={5} className="px-4 py-8 text-center text-luna-charcoal/60">
                        Aucun produit.
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>

            <div className="mt-4 flex items-center justify-between text-sm">
              <span className="text-luna-charcoal/60">
                Page {page} / {totalPages} ({data.totalCount} produits)
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
          </>
        )}
      </div>
    </div>
  );
}
