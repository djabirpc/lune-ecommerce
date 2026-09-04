import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { Heart } from 'lucide-react';

import { catalogApi } from '../../lib/api/catalog';
import { useFavorites } from '../../lib/favorites/FavoritesContext';
import { ProductCard } from '../../lib/components/ProductCard';

export function FavoritesPage() {
  const { favorites } = useFavorites();

  const { data: products, isLoading } = useQuery({
    queryKey: ['products', { pageSize: 100 }],
    queryFn: () => catalogApi.getProducts({ pageSize: 100 }),
    enabled: favorites.length > 0,
  });

  const favoriteProducts = (products?.items ?? []).filter((p) => favorites.includes(p.id));

  if (favorites.length === 0) {
    return (
      <div className="mx-auto flex max-w-md flex-col items-center px-4 py-24 text-center">
        <Heart className="h-10 w-10 text-luna-charcoal/40" />
        <h1 className="mt-4 font-display text-3xl text-luna-black">Aucun favori</h1>
        <p className="mt-2 text-sm text-luna-charcoal/70">Ajoutez des articles à vos favoris en cliquant sur le cœur.</p>
        <Link to="/categories" className="mt-6 rounded-full bg-luna-black px-7 py-3 text-sm text-white">
          Voir la collection
        </Link>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-6xl px-4 py-8 sm:px-6 lg:px-8">
      <p className="eyebrow">Mon espace</p>
      <h1 className="mt-1 font-display text-4xl text-luna-black">Mes favoris</h1>

      {isLoading ? (
        <p className="mt-8 text-sm text-luna-charcoal/60">Chargement...</p>
      ) : (
        <div className="mt-8 grid grid-cols-2 gap-x-4 gap-y-8 lg:grid-cols-4">
          {favoriteProducts.map((p) => (
            <ProductCard key={p.id} product={p} />
          ))}
        </div>
      )}
    </div>
  );
}
