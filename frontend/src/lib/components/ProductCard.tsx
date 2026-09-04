import { Link } from 'react-router-dom';

import { formatPrice } from '../format/price';
import type { ProductListItemDto } from '../api/types';

export function ProductCard({ product }: { product: ProductListItemDto }) {
  return (
    <Link to={`/product/${product.slug}`} className="group flex flex-col gap-2.5">
      <div className="relative aspect-[3/4] overflow-hidden rounded-xl bg-luna-cream">
        {product.primaryImageUrl ? (
          <img
            src={product.primaryImageUrl}
            alt={product.name}
            loading="lazy"
            className="h-full w-full object-cover transition duration-500 ease-out group-hover:scale-[1.04]"
          />
        ) : (
          <div className="flex h-full w-full items-center justify-center text-xs text-luna-charcoal/40">
            Pas d'image
          </div>
        )}
      </div>
      <div className="px-0.5 text-sm">
        <p className="truncate font-medium text-luna-black">{product.name}</p>
        <p className="text-luna-charcoal/60">{formatPrice(product.price)}</p>
      </div>
    </Link>
  );
}
