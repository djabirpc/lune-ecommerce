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
    <div className="px-4 py-10">
      <h1 className="mb-6 text-xl font-semibold">Catégories</h1>
      <div className="grid grid-cols-2 gap-4 sm:grid-cols-3">
        {categories.map((category) => (
          <Link
            key={category.id}
            to={`/category/${category.slug}`}
            className="flex items-center justify-center rounded-lg bg-luna-cream px-4 py-8 text-center text-sm font-medium hover:bg-luna-cream/70"
          >
            {category.name}
          </Link>
        ))}
      </div>
    </div>
  );
}
