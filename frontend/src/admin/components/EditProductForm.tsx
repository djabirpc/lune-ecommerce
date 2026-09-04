import { useEffect } from 'react';
import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { z } from 'zod';

import { catalogApi } from '../../lib/api/catalog';
import { ApiError } from '../../lib/api/client';
import { slugSchema } from '../../lib/format/slug';

const productSchema = z.object({
  categoryId: z.string().min(1, 'La catégorie est requise.'),
  name: z.string().min(1, 'Le nom est requis.').max(200),
  slug: slugSchema,
  description: z.string().max(4000).optional(),
  price: z.coerce.number().positive('Le prix doit être supérieur à zéro.'),
  isActive: z.boolean(),
  facebookPixelId: z.string().max(50).optional(),
  tikTokPixelId: z.string().max(50).optional(),
});

type ProductFormInput = z.input<typeof productSchema>;
type ProductFormValues = z.output<typeof productSchema>;

export function EditProductForm({ slug, onDone }: { slug: string; onDone: () => void }) {
  const queryClient = useQueryClient();

  const { data: categories } = useQuery({
    queryKey: ['admin-categories'],
    queryFn: () => catalogApi.getCategories({ includeInactive: true }),
  });

  const { data: product, isLoading } = useQuery({
    queryKey: ['admin-product-detail', slug],
    queryFn: () => catalogApi.getProductBySlug(slug),
  });

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<ProductFormInput, unknown, ProductFormValues>({
    resolver: zodResolver(productSchema),
  });

  useEffect(() => {
    if (product) {
      reset({
        categoryId: product.categoryId,
        name: product.name,
        slug: product.slug,
        description: product.description ?? '',
        price: product.price,
        isActive: product.isActive,
        facebookPixelId: product.facebookPixelId ?? '',
        tikTokPixelId: product.tikTokPixelId ?? '',
      });
    }
  }, [product, reset]);

  const updateProduct = useMutation({
    mutationFn: (values: ProductFormValues) =>
      catalogApi.updateProduct(product!.id, {
        ...values,
        description: values.description || null,
        facebookPixelId: values.facebookPixelId || null,
        tikTokPixelId: values.tikTokPixelId || null,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin-products'] });
      queryClient.invalidateQueries({ queryKey: ['admin-product-detail', slug] });
      onDone();
    },
  });

  if (isLoading || !product) {
    return <p className="border-t border-black/10 bg-luna-cream/30 p-3 text-xs text-luna-charcoal/60">Chargement...</p>;
  }

  return (
    <div className="border-t border-black/10 bg-luna-cream/30 p-3">
      <form onSubmit={handleSubmit((values) => updateProduct.mutate(values))} className="flex flex-col gap-3">
        <div className="grid gap-3 sm:grid-cols-2">
          <div>
            <label className="mb-1 block text-xs font-medium">Nom</label>
            <input {...register('name')} className="w-full rounded border border-black/20 px-2 py-1 text-sm" />
            {errors.name && <p className="text-xs text-red-600">{errors.name.message}</p>}
          </div>
          <div>
            <label className="mb-1 block text-xs font-medium">Slug</label>
            <input {...register('slug')} className="w-full rounded border border-black/20 px-2 py-1 text-sm" />
            {errors.slug && <p className="text-xs text-red-600">{errors.slug.message}</p>}
          </div>
          <div>
            <label className="mb-1 block text-xs font-medium">Catégorie</label>
            <select {...register('categoryId')} className="w-full rounded border border-black/20 px-2 py-1 text-sm">
              {categories?.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name}
                </option>
              ))}
            </select>
            {errors.categoryId && <p className="text-xs text-red-600">{errors.categoryId.message}</p>}
          </div>
          <div>
            <label className="mb-1 block text-xs font-medium">Prix (DA)</label>
            <input
              type="number"
              step="0.01"
              {...register('price')}
              className="w-full rounded border border-black/20 px-2 py-1 text-sm"
            />
            {errors.price && <p className="text-xs text-red-600">{errors.price.message}</p>}
          </div>
        </div>

        <div>
          <label className="mb-1 block text-xs font-medium">Description</label>
          <textarea {...register('description')} rows={2} className="w-full rounded border border-black/20 px-2 py-1 text-sm" />
        </div>

        <label className="flex w-fit items-center gap-2 text-xs">
          <input type="checkbox" {...register('isActive')} />
          Actif (visible sur la boutique)
        </label>

        <div>
          <p className="mb-2 text-xs font-medium">Pixels marketing (optionnel)</p>
          <p className="mb-2 text-xs text-luna-charcoal/60">
            Laissez vide pour n&apos;utiliser que le pixel du site. Si renseigné, ce pixel reçoit les événements en plus
            du pixel du site.
          </p>
          <div className="grid gap-3 sm:grid-cols-2">
            <div>
              <label className="mb-1 block text-xs text-luna-charcoal/60">Facebook Pixel ID</label>
              <input {...register('facebookPixelId')} className="w-full rounded border border-black/20 px-2 py-1 text-sm" />
            </div>
            <div>
              <label className="mb-1 block text-xs text-luna-charcoal/60">TikTok Pixel ID</label>
              <input {...register('tikTokPixelId')} className="w-full rounded border border-black/20 px-2 py-1 text-sm" />
            </div>
          </div>
        </div>

        <div className="flex items-center gap-3">
          <button
            type="submit"
            disabled={updateProduct.isPending}
            className="w-fit rounded-full bg-luna-black px-5 py-2 text-sm text-white disabled:opacity-40"
          >
            {updateProduct.isPending ? 'Enregistrement...' : 'Enregistrer'}
          </button>
          <button type="button" onClick={onDone} className="text-xs underline">
            Annuler
          </button>
        </div>

        {updateProduct.isError && (
          <p className="text-sm text-red-600">
            {updateProduct.error instanceof ApiError ? updateProduct.error.message : 'Une erreur est survenue.'}
          </p>
        )}
      </form>
    </div>
  );
}
