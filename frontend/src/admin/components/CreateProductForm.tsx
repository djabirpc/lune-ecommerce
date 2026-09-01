import { zodResolver } from '@hookform/resolvers/zod';
import { useFieldArray, useForm } from 'react-hook-form';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { z } from 'zod';

import { catalogApi } from '../../lib/api/catalog';
import { ApiError } from '../../lib/api/client';
import { slugSchema } from '../../lib/format/slug';

const variantSchema = z.object({
  color: z.string().min(1, 'Requis').max(100),
  size: z.string().min(1, 'Requis').max(50),
  sku: z.string().min(1, 'Requis').max(64),
  initialQuantity: z.coerce.number().int().min(0),
});

const productSchema = z.object({
  categoryId: z.string().min(1, 'La catégorie est requise.'),
  name: z.string().min(1, 'Le nom est requis.').max(200),
  slug: slugSchema,
  description: z.string().max(4000).optional(),
  price: z.coerce.number().positive('Le prix doit être supérieur à zéro.'),
  variants: z.array(variantSchema).min(1, 'Au moins une variante est requise.'),
});

type ProductFormInput = z.input<typeof productSchema>;
type ProductFormValues = z.output<typeof productSchema>;

export function CreateProductForm() {
  const queryClient = useQueryClient();

  const { data: categories } = useQuery({
    queryKey: ['admin-categories'],
    queryFn: () => catalogApi.getCategories({ includeInactive: true }),
  });

  const {
    register,
    handleSubmit,
    control,
    reset,
    formState: { errors },
  } = useForm<ProductFormInput, unknown, ProductFormValues>({
    resolver: zodResolver(productSchema),
    defaultValues: { variants: [{ color: '', size: '', sku: '', initialQuantity: 0 }] },
  });

  const { fields, append, remove } = useFieldArray({ control, name: 'variants' });

  const createProduct = useMutation({
    mutationFn: (values: ProductFormValues) =>
      catalogApi.createProduct({
        ...values,
        description: values.description || null,
        variants: values.variants.map((v) => ({ ...v, priceOverride: null })),
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin-products'] });
      reset({
        categoryId: '',
        name: '',
        slug: '',
        description: '',
        price: undefined,
        variants: [{ color: '', size: '', sku: '', initialQuantity: 0 }],
      });
    },
  });

  return (
    <div className="rounded-lg border border-black/10 bg-white p-4">
      <h2 className="mb-3 text-sm font-semibold uppercase text-luna-charcoal/60">Nouveau produit</h2>

      <form onSubmit={handleSubmit((values) => createProduct.mutate(values))} className="flex flex-col gap-3">
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
              <option value="">Sélectionner...</option>
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

        <div>
          <p className="mb-2 text-xs font-medium">Variantes</p>
          <div className="flex flex-col gap-2">
            {fields.map((field, index) => (
              <div key={field.id} className="flex flex-wrap items-end gap-2">
                <div>
                  <label className="mb-1 block text-xs text-luna-charcoal/60">Couleur</label>
                  <input
                    {...register(`variants.${index}.color`)}
                    className="w-24 rounded border border-black/20 px-2 py-1 text-sm"
                  />
                </div>
                <div>
                  <label className="mb-1 block text-xs text-luna-charcoal/60">Taille</label>
                  <input
                    {...register(`variants.${index}.size`)}
                    className="w-16 rounded border border-black/20 px-2 py-1 text-sm"
                  />
                </div>
                <div>
                  <label className="mb-1 block text-xs text-luna-charcoal/60">SKU</label>
                  <input
                    {...register(`variants.${index}.sku`)}
                    className="w-32 rounded border border-black/20 px-2 py-1 text-sm"
                  />
                </div>
                <div>
                  <label className="mb-1 block text-xs text-luna-charcoal/60">Stock initial</label>
                  <input
                    type="number"
                    {...register(`variants.${index}.initialQuantity`)}
                    className="w-20 rounded border border-black/20 px-2 py-1 text-sm"
                  />
                </div>
                {fields.length > 1 && (
                  <button
                    type="button"
                    onClick={() => remove(index)}
                    className="mb-1 text-xs text-red-600 underline"
                  >
                    Retirer
                  </button>
                )}
              </div>
            ))}
          </div>
          <button
            type="button"
            onClick={() => append({ color: '', size: '', sku: '', initialQuantity: 0 })}
            className="mt-2 text-xs underline"
          >
            + Ajouter une variante
          </button>
          {errors.variants && <p className="mt-1 text-xs text-red-600">{errors.variants.root?.message}</p>}
        </div>

        <button
          type="submit"
          disabled={createProduct.isPending}
          className="mt-2 w-fit rounded-full bg-luna-black px-5 py-2 text-sm text-white disabled:opacity-40"
        >
          {createProduct.isPending ? 'Création...' : 'Créer le produit'}
        </button>

        {createProduct.isError && (
          <p className="text-sm text-red-600">
            {createProduct.error instanceof ApiError ? createProduct.error.message : 'Une erreur est survenue.'}
          </p>
        )}
        {createProduct.isSuccess && <p className="text-sm text-green-700">Produit créé.</p>}
      </form>
    </div>
  );
}
