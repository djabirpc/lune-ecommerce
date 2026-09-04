import { useMemo, useState } from 'react';
import { useParams } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { SlidersHorizontal, X } from 'lucide-react';

import { catalogApi } from '../../lib/api/catalog';
import { promotionsApi } from '../../lib/api/promotions';
import { estimatePrice } from '../../lib/promotions/estimate';
import { colorToHex } from '../../lib/format/colorSwatch';
import { formatPrice } from '../../lib/format/price';
import { ProductCard } from '../../lib/components/ProductCard';
import { PagePlaceholder } from '../../lib/components/PagePlaceholder';

const SORTS = [
  { id: 'newest', label: 'Nouveautés' },
  { id: 'price-asc', label: 'Prix croissant' },
  { id: 'price-desc', label: 'Prix décroissant' },
] as const;

export function CategoryPage() {
  const { slug } = useParams<{ slug: string }>();
  const [sizes, setSizes] = useState<string[]>([]);
  const [colors, setColors] = useState<string[]>([]);
  const [maxPrice, setMaxPrice] = useState<number | null>(null);
  const [onSaleOnly, setOnSaleOnly] = useState(false);
  const [sort, setSort] = useState<(typeof SORTS)[number]['id']>('newest');
  const [filtersOpen, setFiltersOpen] = useState(false);

  const categoryQuery = useQuery({
    queryKey: ['category', slug],
    queryFn: () => catalogApi.getCategoryBySlug(slug!),
    enabled: !!slug,
  });

  const productsQuery = useQuery({
    queryKey: ['products', { category: slug }],
    queryFn: () => catalogApi.getProducts({ category: slug, pageSize: 60 }),
    enabled: !!slug,
  });

  const { data: activePromotions } = useQuery({
    queryKey: ['active-promotions'],
    queryFn: () => promotionsApi.getActive(),
  });

  const inCategory = productsQuery.data?.items ?? [];

  const availableSizes = useMemo(() => [...new Set(inCategory.flatMap((p) => p.sizes))].sort(), [inCategory]);
  const availableColors = useMemo(() => [...new Set(inCategory.flatMap((p) => p.colors))], [inCategory]);
  const priceSteps = useMemo(() => {
    const max = Math.max(...inCategory.map((p) => p.price), 0);
    return [4000, 6000, 8000, 12000].filter((s) => s < max);
  }, [inCategory]);

  const products = useMemo(() => {
    let list = inCategory.filter((p) => {
      const estimate = activePromotions ? estimatePrice(p, activePromotions) : null;
      if (onSaleOnly && !estimate) return false;
      if (sizes.length && !sizes.some((s) => p.sizes.includes(s))) return false;
      if (colors.length && !colors.some((c) => p.colors.includes(c))) return false;
      if (maxPrice && p.price > maxPrice) return false;
      return true;
    });
    list = [...list];
    if (sort === 'price-asc') list.sort((a, b) => a.price - b.price);
    else if (sort === 'price-desc') list.sort((a, b) => b.price - a.price);
    else list.sort((a, b) => b.createdAtUtc.localeCompare(a.createdAtUtc));
    return list;
  }, [inCategory, sizes, colors, maxPrice, onSaleOnly, sort, activePromotions]);

  const activeCount = sizes.length + colors.length + (maxPrice ? 1 : 0) + (onSaleOnly ? 1 : 0);

  function toggle<T>(list: T[], value: T, set: (v: T[]) => void) {
    set(list.includes(value) ? list.filter((v) => v !== value) : [...list, value]);
  }

  if (categoryQuery.isLoading || productsQuery.isLoading) {
    return <div className="px-4 py-16 text-center text-sm text-luna-charcoal/60">Chargement...</div>;
  }

  if (categoryQuery.isError || !categoryQuery.data) {
    return <PagePlaceholder title="Catégorie introuvable" />;
  }

  return (
    <div className="mx-auto max-w-6xl px-4 py-8 sm:px-6 lg:px-8">
      <p className="eyebrow mt-2">{products.length} pièces</p>
      <h1 className="mt-1 font-display text-4xl text-luna-black">{categoryQuery.data.name}</h1>
      {categoryQuery.data.description && (
        <p className="mt-2 max-w-lg text-sm text-luna-charcoal/70">{categoryQuery.data.description}</p>
      )}

      <div className="no-scrollbar mt-6 flex items-center gap-2 overflow-x-auto pb-1">
        <button
          onClick={() => setFiltersOpen((v) => !v)}
          className="inline-flex shrink-0 items-center gap-2 rounded-full border border-black/15 px-4 py-2 text-xs text-luna-black"
        >
          <SlidersHorizontal className="h-3.5 w-3.5" />
          Filtres{activeCount > 0 ? ` (${activeCount})` : ''}
        </button>
        {SORTS.map((s) => (
          <button
            key={s.id}
            onClick={() => setSort(s.id)}
            className={`shrink-0 rounded-full border px-4 py-2 text-xs transition-colors ${
              sort === s.id ? 'border-luna-black bg-luna-black text-white' : 'border-black/15 text-luna-black hover:bg-luna-cream-dark'
            }`}
          >
            {s.label}
          </button>
        ))}
      </div>

      {filtersOpen && (
        <div className="mt-4 space-y-5 rounded-sm border border-black/10 bg-white p-4">
          {availableSizes.length > 0 && (
            <div>
              <p className="eyebrow mb-2">Tailles</p>
              <div className="flex flex-wrap gap-2">
                {availableSizes.map((s) => (
                  <button
                    key={s}
                    onClick={() => toggle(sizes, s, setSizes)}
                    className={`h-9 min-w-11 rounded-sm border text-xs ${
                      sizes.includes(s) ? 'border-luna-black bg-luna-black text-white' : 'border-black/15 text-luna-black'
                    }`}
                  >
                    {s}
                  </button>
                ))}
              </div>
            </div>
          )}

          {availableColors.length > 0 && (
            <div>
              <p className="eyebrow mb-2">Couleurs</p>
              <div className="flex flex-wrap gap-2">
                {availableColors.map((c) => (
                  <button
                    key={c}
                    onClick={() => toggle(colors, c, setColors)}
                    title={c}
                    className={`h-8 w-8 rounded-full border-2 ${colors.includes(c) ? 'border-luna-black' : 'border-black/15'}`}
                    style={{ backgroundColor: colorToHex(c) }}
                  />
                ))}
              </div>
            </div>
          )}

          <div>
            <p className="eyebrow mb-2">Budget</p>
            <div className="flex flex-wrap gap-2">
              {priceSteps.map((p) => (
                <button
                  key={p}
                  onClick={() => setMaxPrice(maxPrice === p ? null : p)}
                  className={`rounded-full border px-3 py-1.5 text-xs ${
                    maxPrice === p ? 'border-luna-black bg-luna-black text-white' : 'border-black/15 text-luna-black'
                  }`}
                >
                  Moins de {formatPrice(p)}
                </button>
              ))}
              <button
                onClick={() => setOnSaleOnly((v) => !v)}
                className={`rounded-full border px-3 py-1.5 text-xs ${
                  onSaleOnly ? 'border-luna-accent bg-luna-accent text-white' : 'border-black/15 text-luna-black'
                }`}
              >
                En promo
              </button>
            </div>
          </div>

          {activeCount > 0 && (
            <button
              onClick={() => {
                setSizes([]);
                setColors([]);
                setMaxPrice(null);
                setOnSaleOnly(false);
              }}
              className="inline-flex items-center gap-1 text-xs text-luna-charcoal/70 underline"
            >
              <X className="h-3 w-3" /> Réinitialiser les filtres
            </button>
          )}
        </div>
      )}

      {products.length === 0 ? (
        <p className="py-20 text-center text-sm text-luna-charcoal/60">Aucun article ne correspond à ces filtres.</p>
      ) : (
        <div className="mt-8 grid grid-cols-2 gap-x-4 gap-y-8 lg:grid-cols-4">
          {products.map((p) => (
            <ProductCard key={p.id} product={p} />
          ))}
        </div>
      )}
    </div>
  );
}
