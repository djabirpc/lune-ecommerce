import { useEffect, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { ArrowLeft, Package, Plus, Trash2 } from 'lucide-react';

import { catalogApi } from '../../lib/api/catalog';
import { inventoryApi } from '../../lib/api/inventory';
import { ordersApi } from '../../lib/api/orders';
import { ApiError } from '../../lib/api/client';
import { formatPrice } from '../../lib/format/price';
import { colorToHex } from '../../lib/format/colorSwatch';
import { slugify } from '../../lib/format/slug';
import { ProductImagesPanel } from '../components/ProductImagesPanel';
import { ProductOrderHistoryPanel } from '../components/ProductOrderHistoryPanel';
import type { CreateProductVariantRequest, ProductDetailDto } from '../../lib/api/types';

const COMMON_SIZES = ['XS', 'S', 'M', 'L', 'XL', 'XXL'];

type View = { mode: 'list' } | { mode: 'create' } | { mode: 'detail'; slug: string } | { mode: 'edit'; slug: string };

function cellKey(color: string, size: string) {
  return `${color}|${size}`;
}

function generateSku(productSlug: string, color: string, size: string) {
  return `${productSlug}-${color}-${size}`.toUpperCase().replace(/[^A-Z0-9]+/g, '-').replace(/^-+|-+$/g, '').slice(0, 64);
}

// --- List view -------------------------------------------------------------------------------

function ProductListView({ onCreate, onView }: { onCreate: () => void; onView: (slug: string) => void }) {
  const [search, setSearch] = useState('');
  const [categoryId, setCategoryId] = useState('');

  const { data: categories } = useQuery({
    queryKey: ['admin-categories'],
    queryFn: () => catalogApi.getCategories({ includeInactive: true }),
  });

  const { data, isLoading, isError } = useQuery({
    queryKey: ['admin-products-catalog-test'],
    queryFn: () => catalogApi.getProducts({ pageSize: 100, includeInactive: true }),
  });

  const filtered = (data?.items ?? [])
    .filter((p) => !search || p.name.toLowerCase().includes(search.toLowerCase()))
    .filter((p) => !categoryId || p.categoryId === categoryId);

  // Grouped by category, in a simple list (one row per product) within each group — not a grid of
  // cards — per direct request.
  const groups: { categoryName: string; products: typeof filtered }[] = [];
  const groupIndexByCategory = new Map<string, number>();
  for (const p of filtered) {
    let index = groupIndexByCategory.get(p.categoryId);
    if (index === undefined) {
      index = groups.length;
      groupIndexByCategory.set(p.categoryId, index);
      groups.push({ categoryName: p.categoryName, products: [] });
    }
    groups[index].products.push(p);
  }

  return (
    <div>
      <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
        <h2 className="text-lg font-semibold">Articles ({filtered.length})</h2>
        <div className="flex flex-wrap gap-2">
          <input
            placeholder="Rechercher..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="w-48 rounded border border-black/20 px-3 py-1.5 text-sm"
          />
          <select value={categoryId} onChange={(e) => setCategoryId(e.target.value)} className="rounded border border-black/20 px-3 py-1.5 text-sm">
            <option value="">Toutes les catégories</option>
            {(categories ?? []).map((c) => (
              <option key={c.id} value={c.id}>
                {c.name}
              </option>
            ))}
          </select>
          <button type="button" onClick={onCreate} className="flex items-center gap-1.5 rounded-full bg-luna-black px-4 py-1.5 text-sm text-white">
            <Plus className="h-4 w-4" /> Nouvel article
          </button>
        </div>
      </div>

      {isLoading && <p className="text-sm text-luna-charcoal/60">Chargement...</p>}
      {isError && <p className="text-sm text-red-600">Impossible de charger les articles.</p>}

      {data && groups.length === 0 && <p className="text-sm text-luna-charcoal/50">Aucun article.</p>}

      {groups.map((group) => (
        <div key={group.categoryName} className="mb-6">
          <h3 className="mb-2 text-sm font-semibold uppercase text-luna-charcoal/60">{group.categoryName}</h3>
          <div className="divide-y divide-black/5 rounded-lg border border-black/10 bg-white">
            {group.products.map((p) => (
              <button
                key={p.id}
                type="button"
                onClick={() => onView(p.slug)}
                className="flex w-full items-center gap-3 px-4 py-3 text-left hover:bg-luna-cream/40"
              >
                <div className="h-14 w-14 shrink-0 overflow-hidden rounded bg-luna-cream">
                  {p.primaryImageUrl && <img src={p.primaryImageUrl} alt="" className="h-full w-full object-cover" />}
                </div>
                <div className="min-w-0 flex-1">
                  <p className="truncate text-sm font-medium text-luna-black">{p.name}</p>
                  <p className="text-xs text-luna-charcoal/50">
                    {formatPrice(p.price)} · {p.colors.length} couleur{p.colors.length > 1 ? 's' : ''}
                  </p>
                </div>
                {!p.isInStock && <span className="shrink-0 rounded-full bg-red-100 px-2.5 py-1 text-xs text-red-700">Rupture</span>}
                {!p.isActive && <span className="shrink-0 rounded-full bg-luna-cream px-2.5 py-1 text-xs text-luna-charcoal/50">Inactif</span>}
              </button>
            ))}
          </div>
        </div>
      ))}
    </div>
  );
}

// --- Color / size / stock matrix builder ------------------------------------------------------

function ColorSizeStockEditor({
  colors,
  colorHexes,
  sizes,
  stock,
  skus,
  onAddColor,
  onRemoveColor,
  onAddSize,
  onRemoveSize,
  onStockChange,
  onSkuChange,
}: {
  colors: string[];
  colorHexes: Record<string, string>;
  sizes: string[];
  stock: Record<string, number>;
  skus: Record<string, string>;
  onAddColor: (color: string, hex: string) => void;
  onRemoveColor: (color: string) => void;
  onAddSize: (size: string) => void;
  onRemoveSize: (size: string) => void;
  onStockChange: (color: string, size: string, value: number) => void;
  onSkuChange: (color: string, size: string, value: string) => void;
}) {
  const [newColor, setNewColor] = useState('');
  const [newColorHex, setNewColorHex] = useState('#c9c2b8');
  const [hexTouched, setHexTouched] = useState(false);
  const [newSize, setNewSize] = useState('');

  return (
    <div className="flex flex-col gap-4">
      <div>
        <label className="mb-1 block text-xs font-medium">Couleurs</label>
        <div className="mb-2 flex flex-wrap gap-2">
          {colors.map((c) => (
            <span key={c} className="flex items-center gap-1.5 rounded-full border border-black/15 bg-white py-1 pl-2 pr-1 text-sm">
              <span className="h-3.5 w-3.5 rounded-full border border-black/10" style={{ backgroundColor: colorHexes[c] ?? colorToHex(c) }} />
              {c}
              <button type="button" aria-label={`Retirer ${c}`} onClick={() => onRemoveColor(c)} className="rounded-full p-1 hover:bg-luna-cream">
                <Trash2 className="h-3 w-3" />
              </button>
            </span>
          ))}
        </div>
        <div className="flex flex-wrap items-end gap-2 rounded-md border border-dashed border-black/15 p-2">
          <div>
            <label className="mb-1 block text-[11px] font-medium text-luna-charcoal/60">Teinte</label>
            <input
              type="color"
              title="Choisir la couleur exacte"
              value={newColorHex}
              onChange={(e) => {
                setNewColorHex(e.target.value);
                setHexTouched(true);
              }}
              className="h-10 w-16 cursor-pointer rounded border border-black/20 bg-white p-0.5"
            />
          </div>
          <div>
            <label className="mb-1 block text-[11px] font-medium text-luna-charcoal/60">Nom</label>
            <input
              placeholder="ex. Noir"
              value={newColor}
              onChange={(e) => {
                const value = e.target.value;
                setNewColor(value);
                // Suggest a matching swatch for known French names until the admin picks their own.
                if (!hexTouched) setNewColorHex(colorToHex(value));
              }}
              className="h-10 w-40 rounded border border-black/20 px-2 text-sm"
            />
          </div>
          <button
            type="button"
            onClick={() => {
              if (newColor.trim()) {
                onAddColor(newColor.trim(), newColorHex);
                setNewColor('');
                setNewColorHex('#c9c2b8');
                setHexTouched(false);
              }
            }}
            className="h-10 rounded border border-black/20 px-3 text-xs"
          >
            + Ajouter
          </button>
        </div>
        <p className="mt-1 text-[11px] text-luna-charcoal/50">
          Cliquez sur la pastille « Teinte » pour choisir la couleur exacte, puis tapez son nom.
        </p>
      </div>

      <div>
        <label className="mb-1 block text-xs font-medium">Tailles</label>
        <div className="mb-2 flex flex-wrap gap-2">
          {COMMON_SIZES.map((s) => (
            <button
              key={s}
              type="button"
              onClick={() => (sizes.includes(s) ? onRemoveSize(s) : onAddSize(s))}
              className={`rounded-full border px-3 py-1 text-sm ${
                sizes.includes(s) ? 'border-luna-black bg-luna-black text-white' : 'border-black/15 bg-white'
              }`}
            >
              {s}
            </button>
          ))}
          {sizes.filter((s) => !COMMON_SIZES.includes(s)).map((s) => (
            <span key={s} className="flex items-center gap-1 rounded-full border border-luna-black bg-luna-black py-1 pl-3 pr-1 text-sm text-white">
              {s}
              <button type="button" aria-label={`Retirer ${s}`} onClick={() => onRemoveSize(s)} className="rounded-full p-1 hover:bg-white/20">
                <Trash2 className="h-3 w-3" />
              </button>
            </span>
          ))}
        </div>
        <div className="flex gap-2">
          <input
            placeholder="Taille personnalisée (ex. 42)"
            value={newSize}
            onChange={(e) => setNewSize(e.target.value)}
            className="w-48 rounded border border-black/20 px-2 py-1 text-sm"
          />
          <button
            type="button"
            onClick={() => {
              if (newSize.trim()) {
                onAddSize(newSize.trim());
                setNewSize('');
              }
            }}
            className="rounded border border-black/20 px-3 py-1 text-xs"
          >
            + Ajouter
          </button>
        </div>
      </div>

      {colors.length > 0 && sizes.length > 0 && (
        <div>
          <label className="mb-1 block text-xs font-medium">Stock &amp; SKU par variante</label>
          <div className="overflow-x-auto">
            <table className="text-sm">
              <thead>
                <tr>
                  <th className="p-2 text-left text-xs text-luna-charcoal/60">Couleur</th>
                  {sizes.map((s) => (
                    <th key={s} className="p-2 text-xs text-luna-charcoal/60">
                      {s}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {colors.map((c) => (
                  <tr key={c}>
                    <td className="p-2 font-medium">
                      <span className="flex items-center gap-1.5">
                        <span className="h-3 w-3 shrink-0 rounded-full border border-black/10" style={{ backgroundColor: colorHexes[c] ?? colorToHex(c) }} />
                        {c}
                      </span>
                    </td>
                    {sizes.map((s) => (
                      <td key={s} className="p-1">
                        <div className="flex flex-col gap-1">
                          <input
                            type="number"
                            min={0}
                            title="Stock"
                            value={stock[cellKey(c, s)] ?? 0}
                            onChange={(e) => onStockChange(c, s, Math.max(0, Number(e.target.value) || 0))}
                            className="w-24 rounded border border-black/20 px-2 py-1 text-sm"
                          />
                          <input
                            type="text"
                            title="SKU"
                            value={skus[cellKey(c, s)] ?? ''}
                            onChange={(e) => onSkuChange(c, s, e.target.value)}
                            placeholder="SKU"
                            className="w-24 rounded border border-black/20 px-2 py-1 font-mono text-[11px]"
                          />
                        </div>
                      </td>
                    ))}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
}

// --- Detail / statistics view -----------------------------------------------------------------

function VariantStatBar({ label, stat, max }: { label: string; stat: { unitsOrdered: number; amount: number }; max: number }) {
  const pct = max > 0 ? Math.max(4, Math.round((stat.unitsOrdered / max) * 100)) : 4;
  return (
    <div className="flex items-center gap-2 text-sm">
      <span className="w-20 shrink-0 truncate">{label}</span>
      <div className="h-2.5 flex-1 overflow-hidden rounded-full bg-luna-cream">
        <div className="h-full rounded-full bg-luna-accent" style={{ width: `${pct}%` }} />
      </div>
      <span className="w-10 shrink-0 text-right text-xs tabular-nums">{stat.unitsOrdered}</span>
    </div>
  );
}

function ProductDetailView({ slug, onEdit, onBack }: { slug: string; onEdit: () => void; onBack: () => void }) {
  const { data: product, isLoading, isError } = useQuery({
    queryKey: ['admin-product-detail', slug],
    queryFn: () => catalogApi.getProductBySlug(slug),
  });

  const { data: stats } = useQuery({
    queryKey: ['admin-product-stats', product?.id],
    queryFn: () => ordersApi.getProductStats(product!.id),
    enabled: !!product,
  });

  if (isLoading) return <p className="text-sm text-luna-charcoal/60">Chargement...</p>;
  if (isError || !product) return <p className="text-sm text-red-600">Impossible de charger cet article.</p>;

  const primaryImage = product.images.find((i) => i.isPrimary) ?? product.images[0];
  const colors = [...new Set(product.variants.map((v) => v.color))];
  const sizes = [...new Set(product.variants.map((v) => v.size))];
  const totalStock = product.variants.reduce((sum, v) => sum + v.availableQuantity, 0);
  const maxColorUnits = Math.max(1, ...(stats?.byColor.map((c) => c.unitsOrdered) ?? [0]));
  const maxSizeUnits = Math.max(1, ...(stats?.bySize.map((s) => s.unitsOrdered) ?? [0]));

  return (
    <div className="flex flex-col gap-5">
      <button type="button" onClick={onBack} className="flex w-fit items-center gap-1.5 text-sm text-luna-charcoal/60 hover:underline">
        <ArrowLeft className="h-4 w-4" /> Retour à la liste
      </button>

      <div className="flex flex-wrap items-start gap-4 rounded-lg border border-black/10 bg-white p-5">
        <div className="h-28 w-28 shrink-0 overflow-hidden rounded bg-luna-cream">
          {primaryImage ? (
            <img src={primaryImage.url} alt="" className="h-full w-full object-cover" />
          ) : (
            <div className="flex h-full w-full items-center justify-center text-luna-charcoal/30">
              <Package className="h-8 w-8" />
            </div>
          )}
        </div>
        <div className="min-w-0 flex-1">
          <h2 className="text-xl font-semibold">{product.name}</h2>
          <p className="mt-1 text-sm text-luna-charcoal/60">
            {product.categoryName} · {formatPrice(product.price)} · {totalStock} en stock
            {!product.isActive && ' · Inactif'}
          </p>
          {product.description && <p className="mt-2 text-sm text-luna-charcoal/70">{product.description}</p>}
          <div className="mt-3 flex flex-wrap gap-2">
            {colors.map((c) => (
              <span key={c} className="flex items-center gap-1.5 rounded-full border border-black/10 bg-luna-cream/60 px-2.5 py-1 text-xs">
                <span className="h-3 w-3 rounded-full border border-black/10" style={{ backgroundColor: colorToHex(c) }} />
                {c}
              </span>
            ))}
            {sizes.map((s) => (
              <span key={s} className="rounded-full border border-black/10 bg-luna-cream/60 px-2.5 py-1 text-xs">
                {s}
              </span>
            ))}
          </div>
        </div>
        <button type="button" onClick={onEdit} className="shrink-0 rounded-full border border-luna-black px-4 py-2 text-sm">
          Modifier
        </button>
      </div>

      <div className="rounded-lg border border-black/10 bg-white p-5">
        <h3 className="mb-3 text-sm font-semibold uppercase text-luna-charcoal/60">Statistiques</h3>
        {stats && stats.totalOrders === 0 ? (
          <p className="text-sm text-luna-charcoal/50">Cet article n'a encore été commandé dans aucune commande.</p>
        ) : (
          <>
            <div className="mb-4 grid grid-cols-3 gap-3 text-center">
              <div>
                <p className="text-2xl font-semibold tabular-nums">{stats?.totalOrders ?? '…'}</p>
                <p className="text-xs text-luna-charcoal/60">Commandes</p>
              </div>
              <div>
                <p className="text-2xl font-semibold tabular-nums">{stats?.totalUnitsOrdered ?? '…'}</p>
                <p className="text-xs text-luna-charcoal/60">Unités commandées</p>
              </div>
              <div>
                <p className="text-2xl font-semibold tabular-nums">{stats ? formatPrice(stats.totalAmount) : '…'}</p>
                <p className="text-xs text-luna-charcoal/60">Montant total</p>
              </div>
            </div>

            {stats && stats.byColor.length > 0 && (
              <div className="mb-4">
                <p className="mb-2 text-xs font-medium text-luna-charcoal/60">Par couleur</p>
                <div className="flex flex-col gap-1.5">
                  {stats.byColor.map((c) => (
                    <VariantStatBar key={c.value} label={c.value} stat={c} max={maxColorUnits} />
                  ))}
                </div>
              </div>
            )}

            {stats && stats.bySize.length > 0 && (
              <div>
                <p className="mb-2 text-xs font-medium text-luna-charcoal/60">Par taille</p>
                <div className="flex flex-col gap-1.5">
                  {stats.bySize.map((s) => (
                    <VariantStatBar key={s.value} label={s.value} stat={s} max={maxSizeUnits} />
                  ))}
                </div>
              </div>
            )}
          </>
        )}
      </div>

      <div className="rounded-lg border border-black/10 bg-white">
        <h3 className="border-b border-black/10 px-5 py-3 text-sm font-semibold uppercase text-luna-charcoal/60">Historique des commandes</h3>
        <ProductOrderHistoryPanel productId={product.id} />
      </div>
    </div>
  );
}

// --- Editor view (create + edit) ------------------------------------------------------------

function ProductEditorView({ slug, onDone }: { slug: string | null; onDone: () => void }) {
  const queryClient = useQueryClient();
  const isEdit = slug !== null;

  const { data: categories } = useQuery({
    queryKey: ['admin-categories'],
    queryFn: () => catalogApi.getCategories({ includeInactive: true }),
  });

  const { data: existingProduct, isLoading } = useQuery({
    queryKey: ['admin-product-detail', slug],
    queryFn: () => catalogApi.getProductBySlug(slug!),
    enabled: isEdit,
  });

  const [name, setName] = useState('');
  const [productSlug, setProductSlug] = useState('');
  const [slugTouched, setSlugTouched] = useState(false);
  const [categoryId, setCategoryId] = useState('');
  const [price, setPrice] = useState('');
  const [description, setDescription] = useState('');
  const [colors, setColors] = useState<string[]>([]);
  const [colorHexes, setColorHexes] = useState<Record<string, string>>({});
  const [sizes, setSizes] = useState<string[]>([]);
  const [stock, setStock] = useState<Record<string, number>>({});
  const [skuOverrides, setSkuOverrides] = useState<Record<string, string>>({});
  const [facebookPixelId, setFacebookPixelId] = useState('');
  const [tikTokPixelId, setTikTokPixelId] = useState('');
  const [imageFiles, setImageFiles] = useState<File[]>([]);
  const [error, setError] = useState<string | null>(null);

  // Pre-fill from the existing product's real variants once loaded.
  useEffect(() => {
    if (existingProduct) {
      setName(existingProduct.name);
      setProductSlug(existingProduct.slug);
      setCategoryId(existingProduct.categoryId);
      setPrice(String(existingProduct.price));
      setDescription(existingProduct.description ?? '');
      setFacebookPixelId(existingProduct.facebookPixelId ?? '');
      setTikTokPixelId(existingProduct.tikTokPixelId ?? '');
      setColors([...new Set(existingProduct.variants.map((v) => v.color))]);
      setSizes([...new Set(existingProduct.variants.map((v) => v.size))]);
      setStock(
        Object.fromEntries(existingProduct.variants.map((v) => [cellKey(v.color, v.size), v.availableQuantity])),
      );
      // Existing variants keep their real SKU rather than a freshly generated one.
      setSkuOverrides(
        Object.fromEntries(existingProduct.variants.map((v) => [cellKey(v.color, v.size), v.sku])),
      );
    }
  }, [existingProduct]);

  // Every color×size cell shows a real SKU value by default (auto-generated, editable) — this
  // merges that default with whatever the admin has actually typed/kept.
  const effectiveSkus = Object.fromEntries(
    colors.flatMap((c) => sizes.map((s) => [cellKey(c, s), skuOverrides[cellKey(c, s)] ?? generateSku(productSlug, c, s)])),
  );

  useEffect(() => {
    if (!isEdit && !slugTouched) {
      setProductSlug(slugify(name));
    }
  }, [name, slugTouched, isEdit]);

  function addColor(color: string, hex: string) {
    setColors((c) => (c.includes(color) ? c : [...c, color]));
    setColorHexes((h) => ({ ...h, [color]: hex }));
  }
  function removeColor(color: string) {
    setColors((c) => c.filter((x) => x !== color));
  }
  function addSize(size: string) {
    setSizes((s) => (s.includes(size) ? s : [...s, size]));
  }
  function removeSize(size: string) {
    setSizes((s) => s.filter((x) => x !== size));
  }
  function setStockValue(color: string, size: string, value: number) {
    setStock((s) => ({ ...s, [cellKey(color, size)]: value }));
  }
  function setSkuValue(color: string, size: string, value: string) {
    setSkuOverrides((s) => ({ ...s, [cellKey(color, size)]: value }));
  }

  const create = useMutation({
    mutationFn: async () => {
      const variants: CreateProductVariantRequest[] = colors.flatMap((c) =>
        sizes.map((s) => ({
          color: c,
          size: s,
          sku: effectiveSkus[cellKey(c, s)],
          priceOverride: null,
          costPrice: null,
          initialQuantity: stock[cellKey(c, s)] ?? 0,
        })),
      );
      const product = await catalogApi.createProduct({
        categoryId,
        name,
        slug: productSlug,
        description: description || null,
        price: Number(price),
        variants,
        facebookPixelId: facebookPixelId || null,
        tikTokPixelId: tikTokPixelId || null,
      });
      for (const file of imageFiles) {
        await catalogApi.uploadImage(product.id, file);
      }
      return product;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin-products-catalog-test'] });
      onDone();
    },
    onError: (err) => setError(err instanceof ApiError ? err.message : 'Une erreur est survenue.'),
  });

  const save = useMutation({
    mutationFn: async () => {
      const product = existingProduct as ProductDetailDto;
      await catalogApi.updateProduct(product.id, {
        categoryId,
        name,
        slug: productSlug,
        description: description || null,
        price: Number(price),
        isActive: product.isActive,
        facebookPixelId: facebookPixelId || null,
        tikTokPixelId: tikTokPixelId || null,
      });

      for (const c of colors) {
        for (const s of sizes) {
          const key = cellKey(c, s);
          const desired = stock[key] ?? 0;
          const existingVariant = product.variants.find((v) => v.color === c && v.size === s);
          if (existingVariant) {
            const delta = desired - existingVariant.availableQuantity;
            if (delta !== 0) {
              await inventoryApi.adjust({ productVariantId: existingVariant.id, quantityDelta: delta, reason: 'Ajustement depuis le catalogue (test)' });
            }
          } else {
            await catalogApi.addVariant(product.id, {
              color: c,
              size: s,
              sku: effectiveSkus[key],
              priceOverride: null,
              costPrice: null,
              initialQuantity: desired,
            });
          }
        }
      }
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin-products-catalog-test'] });
      queryClient.invalidateQueries({ queryKey: ['admin-product-detail', slug] });
      onDone();
    },
    onError: (err) => setError(err instanceof ApiError ? err.message : 'Une erreur est survenue.'),
  });

  const pending = create.isPending || save.isPending;
  const canSubmit = name.trim().length > 1 && productSlug.trim().length > 0 && categoryId && Number(price) > 0;

  if (isEdit && isLoading) {
    return <p className="text-sm text-luna-charcoal/60">Chargement...</p>;
  }

  return (
    <div className="flex flex-col gap-5 rounded-lg border border-black/10 bg-white p-5">
      <h2 className="text-lg font-semibold">{isEdit ? name || 'Modifier l’article' : 'Nouvel article'}</h2>

      <div className="grid gap-3 sm:grid-cols-2">
        <div>
          <label className="mb-1 block text-xs font-medium">Nom</label>
          <input value={name} onChange={(e) => setName(e.target.value)} className="w-full rounded border border-black/20 px-2 py-1.5 text-sm" />
        </div>
        <div>
          <label className="mb-1 block text-xs font-medium">Slug</label>
          <input
            value={productSlug}
            onChange={(e) => {
              setSlugTouched(true);
              setProductSlug(e.target.value);
            }}
            className="w-full rounded border border-black/20 px-2 py-1.5 text-sm"
          />
        </div>
        <div>
          <label className="mb-1 block text-xs font-medium">Catégorie</label>
          <select value={categoryId} onChange={(e) => setCategoryId(e.target.value)} className="w-full rounded border border-black/20 px-2 py-1.5 text-sm">
            <option value="">Sélectionner...</option>
            {(categories ?? []).map((c) => (
              <option key={c.id} value={c.id}>
                {c.name}
              </option>
            ))}
          </select>
        </div>
        <div>
          <label className="mb-1 block text-xs font-medium">Prix (DA)</label>
          <input
            type="number"
            step="0.01"
            value={price}
            onChange={(e) => setPrice(e.target.value)}
            className="w-full rounded border border-black/20 px-2 py-1.5 text-sm"
          />
        </div>
      </div>

      <div>
        <label className="mb-1 block text-xs font-medium">Description</label>
        <textarea value={description} onChange={(e) => setDescription(e.target.value)} rows={3} className="w-full rounded border border-black/20 px-2 py-1.5 text-sm" />
      </div>

      <ColorSizeStockEditor
        colors={colors}
        colorHexes={colorHexes}
        sizes={sizes}
        stock={stock}
        skus={effectiveSkus}
        onAddColor={addColor}
        onRemoveColor={removeColor}
        onAddSize={addSize}
        onRemoveSize={removeSize}
        onStockChange={setStockValue}
        onSkuChange={setSkuValue}
      />
      {isEdit && (
        <p className="text-[11px] text-luna-charcoal/50">
          Retirer une couleur/taille ici ne supprime pas ses variantes existantes côté serveur (aucune suppression de
          variante n'existe dans l'API) — seules les nouvelles combinaisons et les changements de stock sont
          enregistrés.
        </p>
      )}

      <div>
        <p className="mb-2 text-xs font-medium">Pixels marketing (optionnel)</p>
        <p className="mb-2 text-[11px] text-luna-charcoal/50">
          Laissez vide pour n'utiliser que le pixel du site. Si renseigné, ce pixel reçoit les événements en plus du
          pixel du site.
        </p>
        <div className="grid gap-3 sm:grid-cols-2">
          <div>
            <label className="mb-1 block text-xs text-luna-charcoal/60">Facebook Pixel ID</label>
            <input
              value={facebookPixelId}
              onChange={(e) => setFacebookPixelId(e.target.value)}
              className="w-full rounded border border-black/20 px-2 py-1.5 text-sm"
            />
          </div>
          <div>
            <label className="mb-1 block text-xs text-luna-charcoal/60">TikTok Pixel ID</label>
            <input
              value={tikTokPixelId}
              onChange={(e) => setTikTokPixelId(e.target.value)}
              className="w-full rounded border border-black/20 px-2 py-1.5 text-sm"
            />
          </div>
        </div>
      </div>

      <div>
        <p className="mb-2 text-xs font-medium">Photos</p>
        {isEdit ? (
          <ProductImagesPanel slug={productSlug} />
        ) : (
          <>
            <input
              type="file"
              accept="image/jpeg,image/png,image/webp"
              multiple
              onChange={(e) => setImageFiles(Array.from(e.target.files ?? []))}
              className="block w-full text-sm file:mr-3 file:rounded-full file:border-0 file:bg-luna-black file:px-4 file:py-1.5 file:text-sm file:text-white"
            />
            {imageFiles.length > 0 && (
              <p className="mt-1 text-xs text-luna-charcoal/60">
                {imageFiles.length} image{imageFiles.length > 1 ? 's' : ''} — la première deviendra l'image principale.
              </p>
            )}
          </>
        )}
      </div>

      <div className="flex items-center gap-3">
        <button
          type="button"
          disabled={!canSubmit || pending}
          onClick={() => (isEdit ? save.mutate() : create.mutate())}
          className="rounded-full bg-luna-black px-5 py-2 text-sm text-white disabled:opacity-40"
        >
          {pending ? 'Enregistrement...' : 'Enregistrer'}
        </button>
        <button type="button" onClick={onDone} className="text-xs underline">
          Annuler
        </button>
      </div>

      {error && <p className="text-sm text-red-600">{error}</p>}
    </div>
  );
}

// --- Page --------------------------------------------------------------------------------------

// Isolated test/preview area for a redesigned admin catalog workflow, inspired by the Lovable
// prototype (github.com/djabirpc/belle-mode): a real list view, a separate "new article" view, a
// separate "edit article" view, and a color/size/stock-matrix builder — instead of the current
// /admin/products' always-visible create form + inline expand panels. Wired to the SAME real
// backend endpoints as /admin/products (nothing new on the backend) so it's a genuine trial, not a
// mockup — but kept on its own route, untouched /admin/products stays the real page unless this is
// explicitly adopted.
export function ProductsCatalogTestPage() {
  // The view lives in the URL (?view=...&slug=...), not local state, so the browser's back/forward
  // buttons move through list → detail → edit the same way they would on any other admin page.
  const [searchParams, setSearchParams] = useSearchParams();
  const viewMode = searchParams.get('view');
  const slugParam = searchParams.get('slug');
  const view: View =
    viewMode === 'create'
      ? { mode: 'create' }
      : viewMode === 'detail' && slugParam
        ? { mode: 'detail', slug: slugParam }
        : viewMode === 'edit' && slugParam
          ? { mode: 'edit', slug: slugParam }
          : { mode: 'list' };

  const goToList = () => setSearchParams({});
  const goToCreate = () => setSearchParams({ view: 'create' });
  const goToDetail = (slug: string) => setSearchParams({ view: 'detail', slug });
  const goToEdit = (slug: string) => setSearchParams({ view: 'edit', slug });

  return (
    <div>
      <div className="mb-4 rounded-lg border border-luna-accent/30 bg-luna-rose/30 px-4 py-3 text-sm text-luna-accent-dark">
        Page de test — nouvelle organisation du catalogue (inspirée de Lovable). Utilise les vraies données et les
        vraies API de production ; /admin/products n'est pas modifié.
      </div>

      {view.mode === 'list' && <ProductListView onCreate={goToCreate} onView={goToDetail} />}
      {view.mode === 'detail' && (
        <ProductDetailView slug={view.slug} onEdit={() => goToEdit(view.slug)} onBack={goToList} />
      )}
      {view.mode === 'create' && <ProductEditorView slug={null} onDone={goToList} />}
      {view.mode === 'edit' && <ProductEditorView slug={view.slug} onDone={() => goToDetail(view.slug)} />}
    </div>
  );
}
