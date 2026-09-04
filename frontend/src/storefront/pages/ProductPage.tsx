import { useEffect, useMemo, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { Heart, Minus, Plus, Truck, ShieldCheck, RefreshCw } from 'lucide-react';

import { catalogApi } from '../../lib/api/catalog';
import { promotionsApi } from '../../lib/api/promotions';
import { estimatePrice } from '../../lib/promotions/estimate';
import { colorToHex } from '../../lib/format/colorSwatch';
import { useCart } from '../../lib/cart/CartContext';
import { useFavorites } from '../../lib/favorites/FavoritesContext';
import { formatPrice } from '../../lib/format/price';
import { initProductPixels, trackEvent } from '../../lib/marketing/pixels';
import { ProductCard } from '../../lib/components/ProductCard';
import { PagePlaceholder } from '../../lib/components/PagePlaceholder';

export function ProductPage() {
  const { slug } = useParams<{ slug: string }>();
  const navigate = useNavigate();
  const { addItem } = useCart();
  const { isFavorite, toggleFavorite } = useFavorites();

  const { data: product, isLoading, isError } = useQuery({
    queryKey: ['product', slug],
    queryFn: () => catalogApi.getProductBySlug(slug!),
    enabled: !!slug,
  });

  const { data: activePromotions } = useQuery({
    queryKey: ['active-promotions'],
    queryFn: () => promotionsApi.getActive(),
  });

  const { data: relatedProducts } = useQuery({
    queryKey: ['products', { category: product?.categorySlug }],
    queryFn: () => catalogApi.getProducts({ category: product!.categorySlug, pageSize: 8 }),
    enabled: !!product,
  });

  const [selectedColor, setSelectedColor] = useState<string | null>(null);
  const [selectedSize, setSelectedSize] = useState<string | null>(null);
  const [quantity, setQuantity] = useState(1);
  const [justAdded, setJustAdded] = useState(false);
  const [selectedImageId, setSelectedImageId] = useState<string | null>(null);

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

  useEffect(() => {
    if (product) {
      initProductPixels(product.facebookPixelId, product.tikTokPixelId);
      trackEvent('VIEW_CONTENT', { content_name: product.name, content_ids: [product.slug], value: product.price, currency: 'DZD' });
    }
  }, [product]);

  useEffect(() => {
    setSelectedImageId(null);
  }, [product?.id]);

  if (isLoading) {
    return <div className="px-4 py-16 text-center text-sm text-luna-charcoal/60">Chargement...</div>;
  }

  if (isError || !product) {
    return <PagePlaceholder title="Produit introuvable" />;
  }

  const primaryImage = product.images.find((i) => i.isPrimary) ?? product.images[0];
  const displayedImage = product.images.find((i) => i.id === selectedImageId) ?? primaryImage;
  const estimate = activePromotions ? estimatePrice({ id: product.id, categoryId: product.categoryId, price: product.price }, activePromotions) : null;
  const unitPrice = selectedVariant?.price ?? (estimate ? estimate.discountedPrice : product.price);
  const fav = isFavorite(product.id);
  const related = (relatedProducts?.items ?? []).filter((p) => p.id !== product.id).slice(0, 4);

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
    trackEvent('ADD_TO_CART', {
      content_name: product!.name,
      content_ids: [selectedVariant.sku],
      value: selectedVariant.price * quantity,
      currency: 'DZD',
    });
    setJustAdded(true);
  }

  const canAddToCart = Boolean(selectedVariant && selectedVariant.availableQuantity > 0);

  return (
    <div className="mx-auto max-w-6xl px-4 pb-28 sm:px-6 sm:pb-12 lg:px-8">
      <nav className="py-4 text-xs text-luna-charcoal/60">
        <Link to="/categories" className="hover:text-luna-black">
          Boutique
        </Link>
        <span className="mx-1">/</span>
        <span className="text-luna-black">{product.name}</span>
      </nav>

      <div className="grid gap-8 sm:grid-cols-2">
        <div>
          <div className="relative aspect-[3/4] overflow-hidden rounded-sm bg-luna-cream-dark">
            {displayedImage ? (
              <img src={displayedImage.url} alt={displayedImage.altText ?? product.name} className="h-full w-full object-cover" />
            ) : (
              <div className="flex h-full w-full items-center justify-center text-xs text-luna-charcoal/40">Pas d'image</div>
            )}
            {estimate && (
              <span className="absolute top-3 left-3 rounded-full bg-luna-accent px-2.5 py-1 text-xs text-white">-{estimate.percent}%</span>
            )}
            <button
              onClick={() => toggleFavorite(product.id)}
              aria-label="Ajouter aux favoris"
              className="absolute top-3 right-3 rounded-full bg-white/90 p-2.5"
            >
              <Heart className={`h-4 w-4 ${fav ? 'fill-luna-accent text-luna-accent' : 'text-luna-black'}`} />
            </button>
          </div>

          {product.images.length > 1 && (
            <div className="no-scrollbar mt-3 flex gap-2 overflow-x-auto">
              {product.images.map((image) => (
                <button
                  key={image.id}
                  type="button"
                  onClick={() => setSelectedImageId(image.id)}
                  aria-label={image.altText ?? product.name}
                  className={`h-20 w-16 shrink-0 overflow-hidden rounded-sm border ${
                    displayedImage?.id === image.id ? 'border-luna-black' : 'border-transparent'
                  }`}
                >
                  <img src={image.url} alt="" className="h-full w-full object-cover" />
                </button>
              ))}
            </div>
          )}
        </div>

        <div>
          <h1 className="font-display text-3xl text-luna-black sm:text-4xl">{product.name}</h1>

          <div className="mt-4 flex items-baseline gap-3">
            <span className="text-2xl font-medium text-luna-black">{formatPrice(unitPrice)}</span>
            {estimate && !selectedVariant?.price && (
              <span className="text-sm text-luna-charcoal/50 line-through">{formatPrice(estimate.compareAtPrice)}</span>
            )}
          </div>

          {product.description && <p className="mt-4 text-sm leading-relaxed text-luna-charcoal/70">{product.description}</p>}

          <div className="mt-6">
            <p className="eyebrow mb-2">Couleur{selectedColor ? ` : ${selectedColor}` : ''}</p>
            <div className="flex flex-wrap gap-2">
              {colors.map((color) => (
                <button
                  key={color}
                  type="button"
                  onClick={() => handleColorSelect(color)}
                  title={color}
                  className={`h-9 w-9 rounded-full border-2 ${selectedColor === color ? 'border-luna-black' : 'border-black/15'}`}
                  style={{ backgroundColor: colorToHex(color) }}
                />
              ))}
            </div>
          </div>

          {selectedColor && (
            <div className="mt-6">
              <p className="eyebrow mb-2">Taille</p>
              <div className="flex flex-wrap gap-2">
                {sizesForColor.map((size) => (
                  <button
                    key={size}
                    type="button"
                    onClick={() => handleSizeSelect(size)}
                    className={`h-11 min-w-14 rounded-sm border text-sm transition-colors ${
                      selectedSize === size ? 'border-luna-black bg-luna-black text-white' : 'border-black/15 hover:bg-luna-cream-dark'
                    }`}
                  >
                    {size}
                  </button>
                ))}
              </div>
              {selectedVariant && selectedVariant.availableQuantity > 0 && selectedVariant.availableQuantity <= 3 && (
                <p className="mt-2 text-xs text-luna-accent">Plus que {selectedVariant.availableQuantity} en stock — commandez vite</p>
              )}
              {selectedVariant && selectedVariant.availableQuantity === 0 && (
                <p className="mt-2 text-xs text-luna-charcoal/60">Taille épuisée</p>
              )}
            </div>
          )}

          {selectedVariant && selectedVariant.availableQuantity > 0 && (
            <div className="mt-6 flex items-center gap-3">
              <div className="flex items-center rounded-sm border border-black/15">
                <button className="p-3" aria-label="Diminuer" onClick={() => setQuantity((q) => Math.max(1, q - 1))}>
                  <Minus className="h-3.5 w-3.5" />
                </button>
                <span className="w-8 text-center text-sm">{quantity}</span>
                <button
                  className="p-3"
                  aria-label="Augmenter"
                  onClick={() => setQuantity((q) => Math.min(selectedVariant.availableQuantity, q + 1))}
                >
                  <Plus className="h-3.5 w-3.5" />
                </button>
              </div>
              <button
                type="button"
                onClick={handleAddToCart}
                disabled={!canAddToCart}
                className="h-12 flex-1 rounded-sm border border-luna-black text-sm text-luna-black transition hover:bg-luna-black hover:text-white disabled:opacity-40"
              >
                Ajouter au panier
              </button>
            </div>
          )}

          {justAdded && (
            <button
              type="button"
              onClick={() => navigate('/checkout')}
              className="mt-3 hidden h-12 w-full rounded-sm bg-luna-black text-sm font-medium text-white sm:inline-flex sm:items-center sm:justify-center"
            >
              Acheter maintenant — Paiement à la livraison
            </button>
          )}

          <div className="mt-6 grid gap-3 rounded-sm bg-luna-cream-dark p-4 text-xs text-luna-black">
            <p className="flex items-center gap-2">
              <Truck className="h-4 w-4 shrink-0" /> Livraison dans les 58 wilayas, 2 à 5 jours
            </p>
            <p className="flex items-center gap-2">
              <ShieldCheck className="h-4 w-4 shrink-0" /> Paiement à la livraison (COD), sans avance
            </p>
            <p className="flex items-center gap-2">
              <RefreshCw className="h-4 w-4 shrink-0" /> Échange sous 7 jours
            </p>
          </div>

          <details className="group mt-6 border-t border-black/10 py-3">
            <summary className="flex cursor-pointer list-none items-center justify-between text-sm text-luna-black">
              Livraison &amp; retours
              <span className="text-luna-charcoal/50 transition group-open:rotate-45">+</span>
            </summary>
            <p className="mt-2 text-sm text-luna-charcoal/70">
              Livraison à domicile ou en point de retrait via nos partenaires. Vous payez à la réception. Échange possible sous 7
              jours si l'article n'a pas été porté.
            </p>
          </details>
        </div>
      </div>

      {related.length > 0 && (
        <section className="mt-16">
          <h2 className="mb-5 font-display text-2xl text-luna-black">Vous aimerez aussi</h2>
          <div className="grid grid-cols-2 gap-x-4 gap-y-8 lg:grid-cols-4">
            {related.map((p) => (
              <ProductCard key={p.id} product={p} />
            ))}
          </div>
        </section>
      )}

      {/* Sticky mobile CTA bar — keeps the primary action reachable without scrolling back up */}
      <div className="fixed inset-x-0 bottom-0 z-30 border-t border-black/10 bg-white/95 p-3 backdrop-blur sm:hidden">
        {justAdded ? (
          <button
            type="button"
            onClick={() => navigate('/checkout')}
            className="w-full rounded-sm bg-luna-accent px-6 py-3.5 text-sm font-medium text-white"
          >
            Ajouté — Acheter maintenant
          </button>
        ) : (
          <button
            type="button"
            onClick={handleAddToCart}
            disabled={!canAddToCart}
            className="w-full rounded-sm bg-luna-black px-6 py-3.5 text-sm font-medium text-white disabled:opacity-40"
          >
            {selectedVariant ? `Ajouter — ${formatPrice(unitPrice * quantity)}` : 'Choisissez une couleur et une taille'}
          </button>
        )}
      </div>
    </div>
  );
}
