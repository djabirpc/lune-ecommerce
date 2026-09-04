import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { Heart } from 'lucide-react';

import { formatPrice } from '../format/price';
import { colorToHex } from '../format/colorSwatch';
import { promotionsApi } from '../api/promotions';
import { estimatePrice } from '../promotions/estimate';
import { useFavorites } from '../favorites/FavoritesContext';
import type { ProductListItemDto } from '../api/types';

const NEW_WINDOW_DAYS = 14;

export function ProductCard({ product }: { product: ProductListItemDto }) {
  const { isFavorite, toggleFavorite } = useFavorites();
  const { data: activePromotions } = useQuery({
    queryKey: ['active-promotions'],
    queryFn: () => promotionsApi.getActive(),
    staleTime: 60_000,
  });

  const estimate = activePromotions ? estimatePrice(product, activePromotions) : null;
  const isNew = Date.now() - new Date(product.createdAtUtc).getTime() < NEW_WINDOW_DAYS * 24 * 60 * 60 * 1000;
  const fav = isFavorite(product.id);

  return (
    <div className="group relative">
      <Link to={`/product/${product.slug}`} className="block overflow-hidden rounded-sm bg-luna-cream-dark">
        <div className="relative aspect-[3/4]">
          {product.primaryImageUrl ? (
            <img
              src={product.primaryImageUrl}
              alt={product.name}
              loading="lazy"
              className="h-full w-full object-cover transition-transform duration-700 group-hover:scale-[1.04]"
            />
          ) : (
            <div className="flex h-full w-full items-center justify-center text-xs text-luna-charcoal/40">Pas d'image</div>
          )}
          <div className="absolute top-2 left-2 flex flex-col items-start gap-1">
            {estimate && (
              <span className="rounded-full bg-luna-accent px-2 py-0.5 text-[10px] font-medium text-white">-{estimate.percent}%</span>
            )}
            {isNew && (
              <span className="rounded-full bg-white/90 px-2 py-0.5 text-[10px] tracking-wider text-luna-black uppercase">Nouveau</span>
            )}
          </div>
          {!product.isInStock && (
            <div className="absolute inset-0 flex items-center justify-center bg-white/70 text-xs tracking-[0.2em] text-luna-black uppercase">
              Épuisé
            </div>
          )}
        </div>
      </Link>

      <button
        onClick={() => toggleFavorite(product.id)}
        aria-label="Ajouter aux favoris"
        className="absolute top-2 right-2 rounded-full bg-white/90 p-2"
      >
        <Heart className={`h-4 w-4 ${fav ? 'fill-luna-accent text-luna-accent' : 'text-luna-black'}`} />
      </button>

      <div className="mt-3 space-y-1">
        <Link to={`/product/${product.slug}`} className="line-clamp-1 text-sm text-luna-black">
          {product.name}
        </Link>
        <div className="flex items-baseline gap-2">
          <span className="text-sm font-medium text-luna-black">{formatPrice(estimate ? estimate.discountedPrice : product.price)}</span>
          {estimate && <span className="text-xs text-luna-charcoal/50 line-through">{formatPrice(estimate.compareAtPrice)}</span>}
        </div>
        {product.colors.length > 0 && (
          <div className="flex gap-1.5 pt-0.5">
            {product.colors.slice(0, 4).map((color) => (
              <span
                key={color}
                title={color}
                className="h-3 w-3 rounded-full border border-black/10"
                style={{ backgroundColor: colorToHex(color) }}
              />
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
