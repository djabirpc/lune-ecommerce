import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { catalogApi } from '../../lib/api/catalog';
import { ApiError } from '../../lib/api/client';

export function ProductImagesPanel({ slug }: { slug: string }) {
  const queryClient = useQueryClient();
  const [altText, setAltText] = useState('');
  const [error, setError] = useState<string | null>(null);

  const {
    data: product,
    isLoading,
    isError,
  } = useQuery({
    queryKey: ['admin-product-detail', slug],
    queryFn: () => catalogApi.getProductBySlug(slug),
  });

  function invalidate() {
    queryClient.invalidateQueries({ queryKey: ['admin-product-detail', slug] });
    queryClient.invalidateQueries({ queryKey: ['admin-products'] });
  }

  const upload = useMutation({
    mutationFn: (file: File) => catalogApi.uploadImage(product!.id, file, { altText: altText || undefined }),
    onSuccess: () => {
      setError(null);
      setAltText('');
      invalidate();
    },
    onError: (err) => setError(err instanceof ApiError ? err.message : 'Une erreur est survenue.'),
  });

  const deleteImage = useMutation({
    mutationFn: (imageId: string) => catalogApi.deleteImage(product!.id, imageId),
    onSuccess: () => {
      setError(null);
      invalidate();
    },
    onError: (err) => setError(err instanceof ApiError ? err.message : 'Une erreur est survenue.'),
  });

  const setPrimary = useMutation({
    mutationFn: (imageId: string) => catalogApi.setPrimaryImage(product!.id, imageId),
    onSuccess: () => {
      setError(null);
      invalidate();
    },
    onError: (err) => setError(err instanceof ApiError ? err.message : 'Une erreur est survenue.'),
  });

  if (isLoading) return <p className="p-3 text-xs text-luna-charcoal/60">Chargement des images...</p>;
  if (isError || !product) return <p className="p-3 text-xs text-red-600">Impossible de charger les images.</p>;

  return (
    <div className="border-t border-black/10 bg-luna-cream/30 p-3">
      <div className="mb-3 flex flex-wrap gap-3">
        {product.images.length === 0 && <p className="text-xs text-luna-charcoal/50">Aucune image pour ce produit.</p>}
        {product.images.map((image) => (
          <div key={image.id} className="w-28 rounded border border-black/10 bg-white p-1.5">
            <div className="relative aspect-square overflow-hidden rounded bg-luna-cream">
              <img src={image.url} alt={image.altText ?? product.name} className="h-full w-full object-cover" />
              {image.isPrimary && (
                <span className="absolute left-1 top-1 rounded-full bg-luna-black px-1.5 py-0.5 text-[10px] text-white">
                  Principale
                </span>
              )}
            </div>
            <div className="mt-1.5 flex flex-col gap-1">
              {!image.isPrimary && (
                <button
                  type="button"
                  disabled={setPrimary.isPending}
                  onClick={() => setPrimary.mutate(image.id)}
                  className="text-[10px] underline disabled:opacity-40"
                >
                  Définir principale
                </button>
              )}
              <button
                type="button"
                disabled={deleteImage.isPending}
                onClick={() => deleteImage.mutate(image.id)}
                className="text-[10px] text-red-600 underline disabled:opacity-40"
              >
                Supprimer
              </button>
            </div>
          </div>
        ))}
      </div>

      <div className="flex flex-wrap items-end gap-2">
        <div>
          <label className="mb-1 block text-xs font-medium">Texte alternatif (facultatif)</label>
          <input
            value={altText}
            onChange={(e) => setAltText(e.target.value)}
            className="w-48 rounded border border-black/20 px-2 py-1 text-sm"
          />
        </div>
        <label className="rounded-full bg-luna-black px-4 py-1.5 text-sm text-white">
          {upload.isPending ? 'Envoi...' : '+ Ajouter une image'}
          <input
            type="file"
            accept="image/jpeg,image/png,image/webp"
            className="hidden"
            disabled={upload.isPending}
            onChange={(e) => {
              const file = e.target.files?.[0];
              if (file) upload.mutate(file);
              e.target.value = '';
            }}
          />
        </label>
      </div>
      <p className="mt-1 text-[11px] text-luna-charcoal/50">JPEG, PNG ou WebP, 5 Mo maximum. La première image ajoutée devient automatiquement l'image principale.</p>

      {error && <p className="mt-2 text-xs text-red-600">{error}</p>}
    </div>
  );
}
