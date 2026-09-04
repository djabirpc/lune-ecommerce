import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';

import { catalogApi } from '../../lib/api/catalog';

export function CategoriesPage() {
  const { data: categories, isLoading, isError } = useQuery({
    queryKey: ['categories'],
    queryFn: () => catalogApi.getCategories(),
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

  return (
    <div className="mx-auto max-w-6xl px-4 py-10 sm:px-6 lg:px-8">
      <h1 className="mb-6 font-display text-2xl italic text-luna-black">Catégories</h1>
      <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4">
        {categories.map((category, index) => (
          <Link
            key={category.id}
            to={`/category/${category.slug}`}
            className="flex aspect-[4/3] flex-col items-center justify-center gap-1 rounded-2xl text-center transition hover:opacity-90"
            style={{ backgroundColor: index % 2 === 0 ? 'var(--color-luna-rose)' : 'var(--color-luna-cream)' }}
          >
            <span className="font-display text-lg italic text-luna-black">{category.name}</span>
          </Link>
        ))}
      </div>
    </div>
  );
}
