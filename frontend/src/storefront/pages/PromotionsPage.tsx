import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { Truck } from 'lucide-react';

import { catalogApi } from '../../lib/api/catalog';
import { promotionsApi } from '../../lib/api/promotions';
import { estimatePrice, findFlashSale } from '../../lib/promotions/estimate';
import { ProductCard } from '../../lib/components/ProductCard';
import { Countdown } from '../../lib/components/Countdown';

export function PromotionsPage() {
  const { data: promotions, isLoading } = useQuery({
    queryKey: ['active-promotions'],
    queryFn: () => promotionsApi.getActive(),
  });

  const { data: products } = useQuery({
    queryKey: ['products', { pageSize: 100 }],
    queryFn: () => catalogApi.getProducts({ pageSize: 100 }),
  });

  const { data: categories } = useQuery({
    queryKey: ['categories'],
    queryFn: () => catalogApi.getCategories(),
  });

  const allProducts = products?.items ?? [];
  const flash = promotions ? findFlashSale(promotions) : undefined;
  const flashProducts = flash
    ? allProducts.filter((p) => flash.productIds.includes(p.id) || flash.categoryIds.includes(p.categoryId))
    : [];

  const categoryPromos = (promotions ?? []).filter((p) => p.type === 'CategoryDiscount');
  const freeShipping = (promotions ?? []).find((p) => p.type === 'FreeShipping');

  const onSale = allProducts
    .map((p) => ({ product: p, estimate: promotions ? estimatePrice(p, promotions) : null }))
    .filter((entry): entry is { product: (typeof allProducts)[number]; estimate: NonNullable<typeof entry.estimate> } => Boolean(entry.estimate))
    .sort((a, b) => b.estimate.percent - a.estimate.percent);

  return (
    <div className="mx-auto max-w-6xl px-4 py-8 sm:px-6 lg:px-8">
      <p className="eyebrow">Offres en cours</p>
      <h1 className="mt-1 font-display text-4xl text-luna-black">Promotions</h1>

      {isLoading && <p className="mt-6 text-sm text-luna-charcoal/60">Chargement...</p>}

      {freeShipping && (
        <div className="mt-6 max-w-sm rounded-sm bg-luna-cream-dark p-5">
          <p className="eyebrow flex items-center gap-2">
            <Truck className="h-3.5 w-3.5" /> Livraison
          </p>
          <p className="mt-1 font-display text-xl text-luna-black">{freeShipping.name}</p>
          {freeShipping.description && <p className="mt-1 text-sm text-luna-charcoal/70">{freeShipping.description}</p>}
        </div>
      )}

      {flash && flashProducts.length > 0 && (
        <section className="mt-12 rounded-sm bg-luna-black p-5 text-white md:p-8">
          <div className="flex flex-wrap items-center justify-between gap-4">
            <div>
              <p className="eyebrow opacity-70">Vente flash</p>
              <h2 className="mt-1 font-display text-3xl">{flash.name}</h2>
              {flash.description && <p className="mt-1 text-sm opacity-80">{flash.description}</p>}
            </div>
            <div>
              <p className="mb-1 text-[10px] tracking-[0.2em] text-white/70 uppercase">Se termine dans</p>
              <Countdown endsAt={flash.endsAtUtc} dark />
            </div>
          </div>
          <div className="mt-6 grid grid-cols-2 gap-x-4 gap-y-8 rounded-sm bg-luna-cream p-4 lg:grid-cols-4">
            {flashProducts.map((p) => (
              <ProductCard key={p.id} product={p} />
            ))}
          </div>
        </section>
      )}

      {categoryPromos.length > 0 && (
        <section className="mt-12">
          <h2 className="mb-5 font-display text-2xl text-luna-black">Offres par catégorie</h2>
          <div className="grid gap-4 md:grid-cols-2">
            {categoryPromos.map((promo) => {
              const category = categories?.find((c) => promo.categoryIds.includes(c.id));
              const image = allProducts.find((p) => p.categoryId === category?.id && p.primaryImageUrl)?.primaryImageUrl;
              if (!category) return null;
              return (
                <div key={promo.id} className="relative overflow-hidden rounded-sm">
                  {image ? (
                    <img src={image} alt={promo.name} loading="lazy" className="h-56 w-full object-cover md:h-72" />
                  ) : (
                    <div className="h-56 w-full bg-luna-cream-dark md:h-72" />
                  )}
                  <div className="absolute inset-0 flex flex-col justify-end bg-gradient-to-t from-black/70 to-transparent p-5 text-white">
                    {promo.percentageValue && <p className="eyebrow opacity-80">-{promo.percentageValue}%</p>}
                    <p className="font-display text-2xl">{promo.name}</p>
                    {promo.description && <p className="text-sm opacity-85">{promo.description}</p>}
                    <Link
                      to={`/category/${category.slug}`}
                      className="mt-3 inline-flex w-fit items-center gap-2 rounded-sm bg-white px-4 py-2 text-sm text-luna-black"
                    >
                      Découvrir {category.name}
                    </Link>
                  </div>
                </div>
              );
            })}
          </div>
        </section>
      )}

      {onSale.length > 0 && (
        <section className="mt-12">
          <h2 className="mb-5 font-display text-2xl text-luna-black">Tous les articles en promo</h2>
          <div className="grid grid-cols-2 gap-x-4 gap-y-8 lg:grid-cols-4">
            {onSale.map(({ product }) => (
              <ProductCard key={product.id} product={product} />
            ))}
          </div>
        </section>
      )}

      {!isLoading && (promotions?.length ?? 0) === 0 && (
        <p className="mt-8 text-sm text-luna-charcoal/70">Aucune promotion en cours pour le moment.</p>
      )}
    </div>
  );
}
