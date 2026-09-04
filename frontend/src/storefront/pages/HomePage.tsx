import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { ArrowRight, Truck, ShieldCheck, RotateCcw } from 'lucide-react';

import { catalogApi } from '../../lib/api/catalog';
import { promotionsApi } from '../../lib/api/promotions';
import { findFlashSale } from '../../lib/promotions/estimate';
import { ProductCard } from '../../lib/components/ProductCard';
import { Countdown } from '../../lib/components/Countdown';

export function HomePage() {
  const { data: categories } = useQuery({
    queryKey: ['home-categories'],
    queryFn: () => catalogApi.getCategories(),
  });

  const { data: newArrivals } = useQuery({
    queryKey: ['home-new-arrivals'],
    queryFn: () => catalogApi.getProducts({ sortByNewest: true, pageSize: 8 }),
  });

  const { data: activePromotions } = useQuery({
    queryKey: ['active-promotions'],
    queryFn: () => promotionsApi.getActive(),
  });

  const arrivals = newArrivals?.items ?? [];
  const heroImage = arrivals.find((p) => p.primaryImageUrl)?.primaryImageUrl;

  const flash = activePromotions ? findFlashSale(activePromotions) : undefined;
  const flashProducts = flash ? arrivals.filter((p) => flash.productIds.includes(p.id) || flash.categoryIds.includes(p.categoryId)) : [];

  const categoryPromo = (activePromotions ?? []).find((p) => p.type === 'CategoryDiscount');
  const categoryPromoTarget = categories?.find((c) => categoryPromo?.categoryIds.includes(c.id));
  const categoryPromoImage = arrivals.find((p) => p.categoryId === categoryPromoTarget?.id && p.primaryImageUrl)?.primaryImageUrl;

  return (
    <div>
      {/* Hero */}
      <section className="relative">
        <div className="relative h-[72vh] min-h-[460px] w-full overflow-hidden bg-luna-black">
          {heroImage && <img src={heroImage} alt="Collection Luna" className="h-full w-full object-cover opacity-80" />}
          <div className="absolute inset-0 bg-gradient-to-t from-black/75 via-black/25 to-transparent" />
          <div className="absolute inset-x-0 bottom-0 mx-auto max-w-6xl px-5 pb-10">
            <p className="eyebrow text-white/80">Nouvelle collection</p>
            <h1 className="mt-2 max-w-xl font-display text-4xl leading-tight text-white sm:text-6xl">
              La mode qui vous ressemble
            </h1>
            <p className="mt-3 max-w-sm text-sm text-white/85">
              Des pièces choisies une à une, livrées chez vous partout en Algérie.
            </p>
            <div className="mt-6 flex flex-wrap gap-3">
              <Link to="/categories" className="rounded-sm bg-white px-6 py-3 text-sm text-luna-black">
                Découvrir la collection
              </Link>
              <Link to="/promotions" className="rounded-sm border border-white px-6 py-3 text-sm text-white">
                Voir les promos
              </Link>
            </div>
          </div>
        </div>
      </section>

      {/* Reassurance */}
      <section className="border-b border-black/10 bg-white">
        <div className="mx-auto grid max-w-6xl grid-cols-3 gap-2 px-4 py-5 text-center sm:px-6 lg:px-8">
          <Reassure icon={<Truck className="h-4 w-4" />} text="Livraison 58 wilayas" />
          <Reassure icon={<ShieldCheck className="h-4 w-4" />} text="Paiement à la livraison" />
          <Reassure icon={<RotateCcw className="h-4 w-4" />} text="Échange sous 7 jours" />
        </div>
      </section>

      {/* Categories */}
      {categories && categories.length > 0 && (
        <Section title="Nos catégories" href="/categories" linkLabel="Tout voir">
          <div className="no-scrollbar -mx-4 flex snap-x gap-3 overflow-x-auto px-4 sm:mx-0 sm:grid sm:grid-cols-3 sm:px-0 lg:grid-cols-5">
            {categories.map((c) => (
              <Link key={c.id} to={`/category/${c.slug}`} className="group w-40 shrink-0 snap-start sm:w-auto">
                <div className="aspect-[3/4] overflow-hidden rounded-sm bg-luna-cream-dark">
                  {(() => {
                    const image = arrivals.find((p) => p.categorySlug === c.slug && p.primaryImageUrl)?.primaryImageUrl;
                    return image ? (
                      <img
                        src={image}
                        alt={c.name}
                        loading="lazy"
                        className="h-full w-full object-cover transition-transform duration-700 group-hover:scale-105"
                      />
                    ) : (
                      <div className="flex h-full w-full items-center justify-center font-display text-lg text-luna-black">{c.name}</div>
                    );
                  })()}
                </div>
                <p className="mt-2 text-sm text-luna-black">{c.name}</p>
              </Link>
            ))}
          </div>
        </Section>
      )}

      {/* Flash sale */}
      {flash && flashProducts.length > 0 && (
        <section className="bg-luna-black py-10 text-white">
          <div className="mx-auto max-w-6xl px-4 sm:px-6 lg:px-8">
            <div className="flex flex-wrap items-center justify-between gap-3">
              <div>
                <p className="eyebrow text-white/70">Vente flash</p>
                <h2 className="font-display text-3xl">{flash.name}</h2>
              </div>
              <Countdown endsAt={flash.endsAtUtc} dark />
            </div>
            <div className="mt-6 grid grid-cols-2 gap-4 lg:grid-cols-4">
              {flashProducts.map((p) => (
                <div key={p.id} className="rounded-sm bg-luna-cream p-3 text-luna-black">
                  <ProductCard product={p} />
                </div>
              ))}
            </div>
          </div>
        </section>
      )}

      {/* New arrivals */}
      {arrivals.length > 0 && (
        <Section title="Nouveautés" href="/categories" linkLabel="Tout voir">
          <div className="grid grid-cols-2 gap-x-4 gap-y-8 lg:grid-cols-4">
            {arrivals.map((p) => (
              <ProductCard key={p.id} product={p} />
            ))}
          </div>
        </Section>
      )}

      {/* Category promo banner */}
      {categoryPromo && categoryPromoTarget && (
        <section className="mx-auto max-w-6xl px-4 py-6 sm:px-6 lg:px-8">
          <Link to={`/category/${categoryPromoTarget.slug}`} className="relative block overflow-hidden rounded-sm">
            {categoryPromoImage ? (
              <img src={categoryPromoImage} alt={categoryPromo.name} className="h-64 w-full object-cover sm:h-80" />
            ) : (
              <div className="h-64 w-full bg-luna-cream-dark sm:h-80" />
            )}
            <div className="absolute inset-0 bg-black/35" />
            <div className="absolute inset-0 flex flex-col items-center justify-center px-6 text-center text-white">
              <h2 className="font-display text-4xl sm:text-5xl">{categoryPromo.name}</h2>
              {categoryPromo.description && <p className="mt-2 text-sm text-white/85">{categoryPromo.description}</p>}
              <span className="mt-5 inline-flex items-center gap-2 rounded-sm bg-white px-5 py-2.5 text-sm text-luna-black">
                Découvrir {categoryPromoTarget.name} <ArrowRight className="h-4 w-4" />
              </span>
            </div>
          </Link>
        </section>
      )}
    </div>
  );
}

function Reassure({ icon, text }: { icon: React.ReactNode; text: string }) {
  return (
    <div className="flex flex-col items-center gap-1 text-[11px] text-luna-charcoal/70 sm:flex-row sm:justify-center sm:text-xs">
      <span className="text-luna-accent">{icon}</span>
      {text}
    </div>
  );
}

function Section({
  title,
  href,
  linkLabel,
  children,
}: {
  title: string;
  href: string;
  linkLabel: string;
  children: React.ReactNode;
}) {
  return (
    <section className="mx-auto max-w-6xl px-4 py-10 sm:px-6 lg:px-8">
      <div className="mb-5 flex items-end justify-between">
        <h2 className="font-display text-3xl text-luna-black">{title}</h2>
        <Link to={href} className="text-xs tracking-wider text-luna-black uppercase underline underline-offset-4">
          {linkLabel}
        </Link>
      </div>
      {children}
    </section>
  );
}
