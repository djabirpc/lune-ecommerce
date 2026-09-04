import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';

import { catalogApi } from '../../lib/api/catalog';
import { promotionsApi } from '../../lib/api/promotions';
import { ProductCard } from '../../lib/components/ProductCard';

const TRUST_ITEMS = [
  { icon: '🚚', label: 'Livraison partout en Algérie' },
  { icon: '💵', label: 'Paiement à la livraison' },
  { icon: '↺', label: 'Échange facile' },
  { icon: '✓', label: 'Satisfait ou remboursé' },
];

const INSTAGRAM_URL = import.meta.env.VITE_INSTAGRAM_URL as string | undefined;
const TIKTOK_URL = import.meta.env.VITE_TIKTOK_URL as string | undefined;

export function HomePage() {
  const { data: categories } = useQuery({
    queryKey: ['home-categories'],
    queryFn: () => catalogApi.getCategories(),
  });

  const { data: newArrivals } = useQuery({
    queryKey: ['home-new-arrivals'],
    queryFn: () => catalogApi.getProducts({ sortByNewest: true, pageSize: 8 }),
  });

  const { data: promotions } = useQuery({
    queryKey: ['home-promotions'],
    queryFn: () => promotionsApi.getActive(),
  });

  const featuredPromotion = promotions?.[0];
  const followSectionVisible = Boolean(INSTAGRAM_URL || TIKTOK_URL) && (newArrivals?.items.length ?? 0) > 0;

  return (
    <div>
      {/* Hero */}
      <section className="relative overflow-hidden bg-luna-cream">
        <div className="pointer-events-none absolute -right-16 -top-16 h-64 w-64 rounded-full bg-luna-accent/20 blur-3xl" />
        <div className="pointer-events-none absolute -bottom-24 -left-16 h-64 w-64 rounded-full bg-luna-accent/10 blur-3xl" />
        <div className="relative mx-auto flex max-w-6xl flex-col items-center gap-6 px-4 py-20 text-center sm:py-28">
          <span className="rounded-full border border-luna-black/10 bg-white px-4 py-1.5 text-xs font-medium tracking-wide text-luna-charcoal/70">
            Nouvelle collection disponible
          </span>
          <h1 className="max-w-lg font-display text-4xl italic leading-tight text-luna-black sm:text-5xl">
            La mode qui vous ressemble.
          </h1>
          <p className="max-w-sm text-sm text-luna-charcoal/70">
            Des pièces intemporelles, livrées partout en Algérie, payables à la livraison.
          </p>
          <div className="flex flex-wrap justify-center gap-3 pt-2">
            <Link
              to="/categories"
              className="rounded-full bg-luna-black px-7 py-3.5 text-sm font-medium text-white transition hover:bg-luna-charcoal"
            >
              Découvrir la collection
            </Link>
            <Link
              to="/promotions"
              className="rounded-full border border-luna-black px-7 py-3.5 text-sm font-medium text-luna-black transition hover:bg-luna-black hover:text-white"
            >
              Voir les promotions
            </Link>
          </div>
        </div>
      </section>

      {/* Trust strip */}
      <section className="border-y border-black/5 bg-white">
        <div className="mx-auto grid max-w-6xl grid-cols-2 gap-6 px-4 py-8 sm:grid-cols-4 sm:px-6 lg:px-8">
          {TRUST_ITEMS.map((item) => (
            <div key={item.label} className="flex flex-col items-center gap-2 text-center">
              <span className="text-xl" aria-hidden="true">{item.icon}</span>
              <span className="text-xs text-luna-charcoal/70 sm:text-[13px]">{item.label}</span>
            </div>
          ))}
        </div>
      </section>

      {/* Promotion banner */}
      {featuredPromotion && (
        <section className="bg-luna-black">
          <Link
            to="/promotions"
            className="mx-auto flex max-w-6xl flex-col items-center gap-1.5 px-4 py-6 text-center text-white sm:flex-row sm:justify-between sm:px-6 lg:px-8"
          >
            <span className="font-display text-lg italic">{featuredPromotion.name}</span>
            <span className="inline-flex items-center gap-2 text-sm text-luna-accent underline underline-offset-4">
              Découvrir l'offre →
            </span>
          </Link>
        </section>
      )}

      {/* Categories */}
      {categories && categories.length > 0 && (
        <section className="mx-auto max-w-6xl px-4 py-14 sm:px-6 lg:px-8">
          <div className="mb-6 flex items-end justify-between">
            <h2 className="font-display text-2xl italic text-luna-black">Nos catégories</h2>
            <Link to="/categories" className="text-xs font-medium text-luna-charcoal/60 hover:text-luna-accent">
              Tout voir →
            </Link>
          </div>
          <div className="no-scrollbar flex gap-4 overflow-x-auto pb-2 sm:grid sm:grid-cols-3 sm:overflow-visible lg:grid-cols-4">
            {categories.slice(0, 8).map((category, index) => (
              <Link
                key={category.id}
                to={`/category/${category.slug}`}
                className="group flex aspect-[4/3] w-40 shrink-0 flex-col items-center justify-center gap-2 rounded-2xl bg-luna-rose text-center transition hover:opacity-90 sm:w-auto"
                style={{ backgroundColor: index % 2 === 0 ? 'var(--color-luna-rose)' : 'var(--color-luna-cream)' }}
              >
                <span className="font-display text-lg italic text-luna-black">{category.name}</span>
              </Link>
            ))}
          </div>
        </section>
      )}

      {/* New arrivals */}
      {newArrivals && newArrivals.items.length > 0 && (
        <section className="mx-auto max-w-6xl px-4 py-14 sm:px-6 lg:px-8">
          <div className="mb-6 flex items-end justify-between">
            <h2 className="font-display text-2xl italic text-luna-black">Nouveautés</h2>
            <Link to="/categories" className="text-xs font-medium text-luna-charcoal/60 hover:text-luna-accent">
              Tout voir →
            </Link>
          </div>
          <div className="grid grid-cols-2 gap-x-4 gap-y-8 sm:grid-cols-3 lg:grid-cols-4">
            {newArrivals.items.map((product) => (
              <ProductCard key={product.id} product={product} />
            ))}
          </div>
        </section>
      )}

      {/* Follow us */}
      {followSectionVisible && (
        <section className="border-t border-black/5 bg-luna-cream">
          <div className="mx-auto max-w-6xl px-4 py-14 text-center sm:px-6 lg:px-8">
            <h2 className="font-display text-2xl italic text-luna-black">Suivez-nous</h2>
            <p className="mx-auto mt-2 max-w-sm text-sm text-luna-charcoal/70">
              Retrouvez nos dernières pièces et inspirations sur les réseaux sociaux.
            </p>
            <div className="mt-6 grid grid-cols-4 gap-2 sm:gap-3">
              {newArrivals!.items.slice(0, 4).map((product) => (
                <div key={product.id} className="aspect-square overflow-hidden rounded-lg bg-white">
                  {product.primaryImageUrl && (
                    <img src={product.primaryImageUrl} alt="" className="h-full w-full object-cover" />
                  )}
                </div>
              ))}
            </div>
            <div className="mt-6 flex justify-center gap-4 text-sm font-medium">
              {INSTAGRAM_URL && (
                <a href={INSTAGRAM_URL} target="_blank" rel="noreferrer" className="text-luna-black underline underline-offset-4 hover:text-luna-accent">
                  Instagram
                </a>
              )}
              {TIKTOK_URL && (
                <a href={TIKTOK_URL} target="_blank" rel="noreferrer" className="text-luna-black underline underline-offset-4 hover:text-luna-accent">
                  TikTok
                </a>
              )}
            </div>
          </div>
        </section>
      )}
    </div>
  );
}
