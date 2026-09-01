import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { z } from 'zod';

import { catalogApi } from '../../lib/api/catalog';
import { ApiError } from '../../lib/api/client';
import { slugSchema } from '../../lib/format/slug';

const categorySchema = z.object({
  name: z.string().min(1, 'Le nom est requis.').max(150),
  slug: slugSchema,
  description: z.string().max(1000).optional(),
  displayOrder: z.coerce.number().int().min(0),
});

type CategoryFormInput = z.input<typeof categorySchema>;
type CategoryFormValues = z.output<typeof categorySchema>;

export function CategoryQuickManager() {
  const queryClient = useQueryClient();

  const { data: categories } = useQuery({
    queryKey: ['admin-categories'],
    queryFn: () => catalogApi.getCategories({ includeInactive: true }),
  });

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<CategoryFormInput, unknown, CategoryFormValues>({
    resolver: zodResolver(categorySchema),
    defaultValues: { displayOrder: 0 },
  });

  const createCategory = useMutation({
    mutationFn: (values: CategoryFormValues) =>
      catalogApi.createCategory({ ...values, description: values.description || null }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin-categories'] });
      reset({ name: '', slug: '', description: '', displayOrder: 0 });
    },
  });

  return (
    <div className="rounded-lg border border-black/10 bg-white p-4">
      <h2 className="mb-3 text-sm font-semibold uppercase text-luna-charcoal/60">Catégories</h2>

      <div className="mb-4 flex flex-wrap gap-2">
        {categories?.map((c) => (
          <span
            key={c.id}
            className={`rounded-full px-3 py-1 text-xs ${c.isActive ? 'bg-luna-cream' : 'bg-luna-cream/40 text-luna-charcoal/50 line-through'}`}
          >
            {c.name}
          </span>
        ))}
        {categories?.length === 0 && <span className="text-xs text-luna-charcoal/50">Aucune catégorie.</span>}
      </div>

      <form
        onSubmit={handleSubmit((values) => createCategory.mutate(values))}
        className="flex flex-wrap items-end gap-2"
      >
        <div>
          <label className="mb-1 block text-xs font-medium">Nom</label>
          <input {...register('name')} className="rounded border border-black/20 px-2 py-1 text-sm" />
        </div>
        <div>
          <label className="mb-1 block text-xs font-medium">Slug</label>
          <input {...register('slug')} className="rounded border border-black/20 px-2 py-1 text-sm" />
        </div>
        <div>
          <label className="mb-1 block text-xs font-medium">Ordre</label>
          <input
            type="number"
            {...register('displayOrder')}
            className="w-16 rounded border border-black/20 px-2 py-1 text-sm"
          />
        </div>
        <button
          type="submit"
          disabled={createCategory.isPending}
          className="rounded-full bg-luna-black px-4 py-1.5 text-sm text-white disabled:opacity-40"
        >
          + Créer
        </button>
      </form>
      {(errors.name || errors.slug || errors.displayOrder) && (
        <p className="mt-1 text-xs text-red-600">
          {errors.name?.message || errors.slug?.message || errors.displayOrder?.message}
        </p>
      )}
      {createCategory.isError && (
        <p className="mt-1 text-xs text-red-600">
          {createCategory.error instanceof ApiError ? createCategory.error.message : 'Une erreur est survenue.'}
        </p>
      )}
    </div>
  );
}
