import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { homeBannersApi } from '../../lib/api/homeBanners';
import { ApiError } from '../../lib/api/client';
import type { HomeBannerDto } from '../../lib/api/types';

function BannerRow({ banner }: { banner: HomeBannerDto }) {
  const queryClient = useQueryClient();
  const [linkUrl, setLinkUrl] = useState(banner.linkUrl ?? '');
  const [displayOrder, setDisplayOrder] = useState(String(banner.displayOrder));
  const [error, setError] = useState<string | null>(null);

  function invalidate() {
    queryClient.invalidateQueries({ queryKey: ['admin-home-banners'] });
  }

  const save = useMutation({
    mutationFn: () =>
      homeBannersApi.update(banner.id, {
        linkUrl: linkUrl.trim() || null,
        displayOrder: Number(displayOrder) || 0,
        isActive: banner.isActive,
      }),
    onSuccess: () => {
      setError(null);
      invalidate();
    },
    onError: (err) => setError(err instanceof ApiError ? err.message : 'Une erreur est survenue.'),
  });

  const toggleActive = useMutation({
    mutationFn: () =>
      homeBannersApi.update(banner.id, {
        linkUrl: banner.linkUrl,
        displayOrder: banner.displayOrder,
        isActive: !banner.isActive,
      }),
    onSuccess: invalidate,
    onError: (err) => setError(err instanceof ApiError ? err.message : 'Une erreur est survenue.'),
  });

  const remove = useMutation({
    mutationFn: () => homeBannersApi.remove(banner.id),
    onSuccess: invalidate,
    onError: (err) => setError(err instanceof ApiError ? err.message : 'Une erreur est survenue.'),
  });

  return (
    <div className="flex flex-col gap-3 rounded-lg border border-black/10 bg-white p-4 sm:flex-row sm:items-center">
      <div className="aspect-[16/7] w-full shrink-0 overflow-hidden rounded bg-luna-cream-dark sm:w-56">
        <img src={banner.imageUrl} alt="" className="h-full w-full object-cover" />
      </div>

      <div className="flex flex-1 flex-col gap-2">
        <div className="grid gap-2 sm:grid-cols-2">
          <div>
            <label className="mb-1 block text-xs font-medium">Lien (optionnel)</label>
            <input
              value={linkUrl}
              onChange={(e) => setLinkUrl(e.target.value)}
              placeholder="/promotions, /category/robes..."
              className="w-full rounded border border-black/20 px-2 py-1.5 text-sm"
            />
          </div>
          <div>
            <label className="mb-1 block text-xs font-medium">Ordre d'affichage</label>
            <input
              type="number"
              min={0}
              value={displayOrder}
              onChange={(e) => setDisplayOrder(e.target.value)}
              className="w-full rounded border border-black/20 px-2 py-1.5 text-sm"
            />
          </div>
        </div>

        {error && <p className="text-xs text-red-600">{error}</p>}

        <div className="flex flex-wrap items-center gap-3">
          <button
            type="button"
            onClick={() => save.mutate()}
            disabled={save.isPending}
            className="rounded-full bg-luna-black px-4 py-1.5 text-xs text-white disabled:opacity-40"
          >
            {save.isPending ? 'Enregistrement...' : 'Enregistrer'}
          </button>
          <button
            type="button"
            onClick={() => toggleActive.mutate()}
            disabled={toggleActive.isPending}
            className="text-xs underline disabled:opacity-40"
          >
            {banner.isActive ? 'Désactiver' : 'Activer'}
          </button>
          <button
            type="button"
            onClick={() => remove.mutate()}
            disabled={remove.isPending}
            className="text-xs text-red-600 underline disabled:opacity-40"
          >
            Supprimer
          </button>
          {!banner.isActive && (
            <span className="rounded-full bg-red-100 px-2 py-0.5 text-xs text-red-700">Inactive</span>
          )}
        </div>
      </div>
    </div>
  );
}

export function HomeBannersPage() {
  const queryClient = useQueryClient();
  const [linkUrl, setLinkUrl] = useState('');
  const [uploadError, setUploadError] = useState<string | null>(null);

  const { data: banners, isLoading } = useQuery({
    queryKey: ['admin-home-banners'],
    queryFn: () => homeBannersApi.getAll(),
  });

  const upload = useMutation({
    mutationFn: (file: File) => homeBannersApi.add(file, linkUrl.trim() || undefined),
    onSuccess: () => {
      setUploadError(null);
      setLinkUrl('');
      queryClient.invalidateQueries({ queryKey: ['admin-home-banners'] });
    },
    onError: (err) => setUploadError(err instanceof ApiError ? err.message : 'Une erreur est survenue.'),
  });

  return (
    <div>
      <h1 className="mb-1 text-xl font-semibold">Page d'accueil</h1>
      <p className="mb-4 text-sm text-luna-charcoal/60">
        Les images ci-dessous défilent en fondu enchaîné toutes les 7 secondes dans le hero de la page
        d'accueil. Sans bannière active, le site affiche automatiquement la photo du produit le plus récent.
      </p>

      <div className="mb-6 rounded-lg border border-black/10 bg-white p-4">
        <h2 className="mb-3 text-sm font-semibold uppercase text-luna-charcoal/60">Ajouter une image</h2>
        <div className="flex flex-wrap items-end gap-2">
          <div>
            <label className="mb-1 block text-xs font-medium">Lien (optionnel)</label>
            <input
              value={linkUrl}
              onChange={(e) => setLinkUrl(e.target.value)}
              placeholder="/promotions, /category/robes..."
              className="w-56 rounded border border-black/20 px-2 py-1.5 text-sm"
            />
          </div>
          <label className="rounded-full bg-luna-black px-4 py-2 text-sm text-white">
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
        <p className="mt-1 text-[11px] text-luna-charcoal/50">JPEG, PNG ou WebP, 5 Mo maximum.</p>
        {uploadError && <p className="mt-2 text-xs text-red-600">{uploadError}</p>}
      </div>

      {isLoading && <p className="text-sm text-luna-charcoal/60">Chargement...</p>}

      <div className="flex flex-col gap-3">
        {banners?.map((banner) => (
          <BannerRow key={banner.id} banner={banner} />
        ))}
        {banners?.length === 0 && (
          <p className="rounded-lg border border-black/10 bg-white px-4 py-8 text-center text-sm text-luna-charcoal/60">
            Aucune bannière. La page d'accueil affiche la photo du produit le plus récent par défaut.
          </p>
        )}
      </div>
    </div>
  );
}
