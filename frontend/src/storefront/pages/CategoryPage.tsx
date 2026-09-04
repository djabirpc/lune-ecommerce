import { useParams } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';

import { catalogApi } from '../../lib/api/catalog';
import { ProductCard } from '../../lib/components/ProductCard';
import { PagePlaceholder } from '../../lib/components/PagePlaceholder';

export function CategoryPage() {
  const { slug } = useParams<{ slug: string }>();

  const categoryQuery = useQuery({
    queryKey: ['category', slug],
    queryFn: () => catalogApi.getCategoryBySlug(slug!),
    enabled: !!slug,
  });

  const productsQuery = useQuery({
    queryKey: ['products', { category: slug }],
    queryFn: () => catalogApi.getProducts({ category: slug, pageSize: 40 }),
    enabled: !!slug,
  });

  if (categoryQuery.isLoading || productsQuery.isLoading) {
    return <div className="px-4 py-16 text-center text-sm text-luna-charcoal/60">Chargement...</div>;
  }

  if (categoryQuery.isError || !categoryQuery.data) {
    return <PagePlaceholder title="Catégorie introuvable" />;
  }

  const products = productsQuery.data?.items ?? [];

  return (
    <div className="mx-auto max-w-6xl px-4 py-10 sm:px-6 lg:px-8">
      <h1 className="mb-1 font-display text-2xl italic text-luna-black">{categoryQuery.data.name}</h1>
      {categoryQuery.data.description && (
        <p className="mb-6 max-w-lg text-sm text-luna-charcoal/70">{categoryQuery.data.description}</p>
      )}

      {products.length === 0 ? (
        <p className="py-12 text-center text-sm text-luna-charcoal/60">Aucun produit dans cette catégorie pour le moment.</p>
      ) : (
        <div className="mt-4 grid grid-cols-2 gap-x-4 gap-y-8 sm:grid-cols-3 lg:grid-cols-4">
          {products.map((product) => (
            <ProductCard key={product.id} product={product} />
          ))}
        </div>
      )}
    </div>
  );
}
