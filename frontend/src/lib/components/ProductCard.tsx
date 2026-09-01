import { Link } from 'react-router-dom';

import { formatPrice } from '../format/price';
import type { ProductListItemDto } from '../api/types';

export function ProductCard({ product }: { product: ProductListItemDto }) {
  return (
    <Link to={`/product/${product.slug}`} className="group flex flex-col gap-2">
      <div className="aspect-[3/4] overflow-hidden rounded-lg bg-luna-cream">
        {product.primaryImageUrl ? (
          <img
            src={product.primaryImageUrl}
            alt={product.name}
            className="h-full w-full object-cover transition group-hover:scale-105"
          />
        ) : (
          <div className="flex h-full w-full items-center justify-center text-xs text-luna-charcoal/40">
            Pas d'image
          </div>
        )}
      </div>
      <div className="text-sm">
        <p className="font-medium text-luna-black">{product.name}</p>
        <p className="text-luna-charcoal/70">{formatPrice(product.price)}</p>
      </div>
    </Link>
  );
}
