import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';

import { catalogApi } from '../../lib/api/catalog';
import { ProductCard } from '../../lib/components/ProductCard';

export function CategoriesPage() {
  const { data: categories, isLoading, isError } = useQuery({
    queryKey: ['categories'],
    queryFn: () => catalogApi.getCategories(),
  });

  const { data: products } = useQuery({
    queryKey: ['products', { pageSize: 100 }],
    queryFn: () => catalogApi.getProducts({ pageSize: 100 }),
  });

  if (isLoading) {
    return <div className="px-4 py-16 text-center text-sm text-luna-charcoal/60">Chargement...</div>;
  }

  if (isError) {
    return <div className="px-4 py-16 text-center text-sm text-red-600">Impossible de charger les catégories.</div>;
  }

  if (!categories || categories.length === 0) {
    return <div className="px-4 py-16 text-center text-sm text-luna-charcoal/60">Aucune catégorie pour le moment.</div>;
  }

  const imageForCategory = (slug: string) => products?.items.find((p) => p.categorySlug === slug && p.primaryImageUrl)?.primaryImageUrl;
  const countForCategory = (slug: string) => products?.items.filter((p) => p.categorySlug === slug).length ?? 0;

  return (
    <div className="mx-auto max-w-6xl px-4 py-8 sm:px-6 lg:px-8">
      <p className="eyebrow">La boutique</p>
      <h1 className="mt-1 font-display text-4xl text-luna-black">Toute la collection</h1>

      <div className="mt-6 grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-5">
        {categories.map((category) => {
          const image = imageForCategory(category.slug);
          return (
            <Link key={category.id} to={`/category/${category.slug}`} className="group">
              <div className="aspect-[3/4] overflow-hidden rounded-sm bg-luna-cream-dark">
                {image ? (
                  <img
                    src={image}
                    alt={category.name}
                    loading="lazy"
                    className="h-full w-full object-cover transition-transform duration-700 group-hover:scale-105"
                  />
                ) : (
                  <div className="flex h-full w-full items-center justify-center font-display text-lg text-luna-black">{category.name}</div>
                )}
              </div>
              <p className="mt-2 text-sm text-luna-black">{category.name}</p>
              <p className="text-xs text-luna-charcoal/60">{countForCategory(category.slug)} pièces</p>
            </Link>
          );
        })}
      </div>

      {products && products.items.length > 0 && (
        <>
          <h2 className="mt-12 mb-5 font-display text-3xl text-luna-black">Tous les articles</h2>
          <div className="grid grid-cols-2 gap-x-4 gap-y-8 lg:grid-cols-4">
            {products.items.map((p) => (
              <ProductCard key={p.id} product={p} />
            ))}
          </div>
        </>
      )}
    </div>
  );
}
