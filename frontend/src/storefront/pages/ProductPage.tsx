import { useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';

import { catalogApi } from '../../lib/api/catalog';
import { useCart } from '../../lib/cart/CartContext';
import { formatPrice } from '../../lib/format/price';
import { PagePlaceholder } from '../../lib/components/PagePlaceholder';

export function ProductPage() {
  const { slug } = useParams<{ slug: string }>();
  const navigate = useNavigate();
  const { addItem } = useCart();

  const { data: product, isLoading, isError } = useQuery({
    queryKey: ['product', slug],
    queryFn: () => catalogApi.getProductBySlug(slug!),
    enabled: !!slug,
  });

  const [selectedColor, setSelectedColor] = useState<string | null>(null);
  const [selectedSize, setSelectedSize] = useState<string | null>(null);
  const [quantity, setQuantity] = useState(1);
  const [justAdded, setJustAdded] = useState(false);

  const colors = useMemo(
    () => [...new Set((product?.variants ?? []).filter((v) => v.isActive).map((v) => v.color))],
    [product],
  );
  const sizesForColor = useMemo(
    () =>
      [...new Set((product?.variants ?? []).filter((v) => v.isActive && v.color === selectedColor).map((v) => v.size))],
    [product, selectedColor],
  );
  const selectedVariant = useMemo(
    () => product?.variants.find((v) => v.color === selectedColor && v.size === selectedSize) ?? null,
    [product, selectedColor, selectedSize],
  );

  if (isLoading) {
    return <div className="px-4 py-16 text-center text-sm text-luna-charcoal/60">Chargement...</div>;
  }

  if (isError || !product) {
    return <PagePlaceholder title="Produit introuvable" />;
  }

  const primaryImage = product.images.find((i) => i.isPrimary) ?? product.images[0];

  function handleColorSelect(color: string) {
    setSelectedColor(color);
    setSelectedSize(null);
    setQuantity(1);
    setJustAdded(false);
  }

  function handleSizeSelect(size: string) {
    setSelectedSize(size);
    setQuantity(1);
    setJustAdded(false);
  }

  function handleAddToCart() {
    if (!selectedVariant || selectedVariant.availableQuantity < 1) return;

    addItem(
      {
        variantId: selectedVariant.id,
        productSlug: product!.slug,
        productName: product!.name,
        color: selectedVariant.color,
        size: selectedVariant.size,
        sku: selectedVariant.sku,
        unitPrice: selectedVariant.price,
        imageUrl: primaryImage?.url ?? null,
        availableQuantity: selectedVariant.availableQuantity,
      },
      quantity,
    );
    setJustAdded(true);
  }

  return (
    <div className="grid gap-6 px-4 py-8 sm:grid-cols-2 sm:gap-10 sm:px-8">
      <div className="aspect-[3/4] overflow-hidden rounded-lg bg-luna-cream">
        {primaryImage ? (
          <img src={primaryImage.url} alt={primaryImage.altText ?? product.name} className="h-full w-full object-cover" />
        ) : (
          <div className="flex h-full w-full items-center justify-center text-xs text-luna-charcoal/40">Pas d'image</div>
        )}
      </div>

      <div>
        <h1 className="text-xl font-semibold text-luna-black">{product.name}</h1>
        <p className="mt-1 text-lg">{formatPrice(selectedVariant?.price ?? product.price)}</p>
        {product.description && <p className="mt-4 text-sm text-luna-charcoal/70">{product.description}</p>}

        <div className="mt-6">
          <p className="mb-2 text-sm font-medium">Couleur</p>
          <div className="flex flex-wrap gap-2">
            {colors.map((color) => (
              <button
                key={color}
                type="button"
                onClick={() => handleColorSelect(color)}
                className={`rounded-full border px-4 py-2 text-sm ${
                  selectedColor === color ? 'border-luna-black bg-luna-black text-white' : 'border-black/20'
                }`}
              >
                {color}
              </button>
            ))}
          </div>
        </div>

        {selectedColor && (
          <div className="mt-4">
            <p className="mb-2 text-sm font-medium">Taille</p>
            <div className="flex flex-wrap gap-2">
              {sizesForColor.map((size) => (
                <button
                  key={size}
                  type="button"
                  onClick={() => handleSizeSelect(size)}
                  className={`rounded-full border px-4 py-2 text-sm ${
                    selectedSize === size ? 'border-luna-black bg-luna-black text-white' : 'border-black/20'
                  }`}
                >
                  {size}
                </button>
              ))}
            </div>
          </div>
        )}

        {selectedVariant && (
          <div className="mt-4 text-sm">
            {selectedVariant.availableQuantity > 0 ? (
              <p className="text-green-700">En stock ({selectedVariant.availableQuantity} disponibles)</p>
            ) : (
              <p className="text-red-600">Rupture de stock</p>
            )}
          </div>
        )}

        {selectedVariant && selectedVariant.availableQuantity > 0 && (
          <div className="mt-4 flex items-center gap-3">
            <label htmlFor="quantity" className="text-sm font-medium">
              Quantité
            </label>
            <select
              id="quantity"
              value={quantity}
              onChange={(e) => setQuantity(Number(e.target.value))}
              className="rounded border border-black/20 px-2 py-1 text-sm"
            >
              {Array.from({ length: Math.min(10, selectedVariant.availableQuantity) }, (_, i) => i + 1).map((n) => (
                <option key={n} value={n}>
                  {n}
                </option>
              ))}
            </select>
          </div>
        )}

        <div className="mt-6 flex flex-col gap-2">
          <button
            type="button"
            onClick={handleAddToCart}
            disabled={!selectedVariant || selectedVariant.availableQuantity < 1}
            className="rounded-full bg-luna-black px-6 py-3 text-sm text-white disabled:opacity-40"
          >
            {selectedVariant ? 'Ajouter au panier' : 'Choisissez une couleur et une taille'}
          </button>

          {justAdded && (
            <button
              type="button"
              onClick={() => navigate('/checkout')}
              className="rounded-full border border-luna-black px-6 py-3 text-sm"
            >
              Ajouté — Acheter maintenant
            </button>
          )}
        </div>
      </div>
    </div>
  );
}
