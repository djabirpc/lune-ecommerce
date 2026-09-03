import { useState } from 'react';
import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { z } from 'zod';

import { catalogApi } from '../../lib/api/catalog';
import { ApiError } from '../../lib/api/client';
import { slugSchema } from '../../lib/format/slug';
import type { CategoryDto } from '../../lib/api/types';

const categorySchema = z.object({
  name: z.string().min(1, 'Le nom est requis.').max(150),
  slug: slugSchema,
  description: z.string().max(1000).optional(),
  displayOrder: z.coerce.number().int().min(0),
  isActive: z.boolean(),
});

type CategoryFormInput = z.input<typeof categorySchema>;
type CategoryFormValues = z.output<typeof categorySchema>;

const emptyValues: CategoryFormInput = { name: '', slug: '', description: '', displayOrder: 0, isActive: true };

export function CategoryQuickManager() {
  const queryClient = useQueryClient();
  const [editing, setEditing] = useState<CategoryDto | null>(null);

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
    defaultValues: emptyValues,
  });

  function startEditing(category: CategoryDto) {
    setEditing(category);
    reset({
      name: category.name,
      slug: category.slug,
      description: category.description ?? '',
      displayOrder: category.displayOrder,
      isActive: category.isActive,
    });
  }

  function cancelEditing() {
    setEditing(null);
    reset(emptyValues);
  }

  const createCategory = useMutation({
    mutationFn: (values: CategoryFormValues) =>
      catalogApi.createCategory({
        name: values.name,
        slug: values.slug,
        description: values.description || null,
        displayOrder: values.displayOrder,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin-categories'] });
      reset(emptyValues);
    },
  });

  const updateCategory = useMutation({
    mutationFn: (values: CategoryFormValues) =>
      catalogApi.updateCategory(editing!.id, { ...values, description: values.description || null }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin-categories'] });
      cancelEditing();
    },
  });

  const mutation = editing ? updateCategory : createCategory;

  return (
    <div className="rounded-lg border border-black/10 bg-white p-4">
      <h2 className="mb-3 text-sm font-semibold uppercase text-luna-charcoal/60">Catégories</h2>

      <div className="mb-4 flex flex-wrap gap-2">
        {categories?.map((c) => (
          <button
            key={c.id}
            type="button"
            onClick={() => startEditing(c)}
            className={`rounded-full px-3 py-1 text-xs ${
              editing?.id === c.id
                ? 'border border-luna-black bg-luna-black text-white'
                : c.isActive
                  ? 'bg-luna-cream'
                  : 'bg-luna-cream/40 text-luna-charcoal/50 line-through'
            }`}
          >
            {c.name}
          </button>
        ))}
        {categories?.length === 0 && <span className="text-xs text-luna-charcoal/50">Aucune catégorie.</span>}
      </div>

      {editing && <p className="mb-2 text-xs text-luna-charcoal/60">Modification de « {editing.name} »</p>}

      <form
        onSubmit={handleSubmit((values) => mutation.mutate(values))}
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
        {editing && (
          <label className="mb-1.5 flex items-center gap-1.5 text-xs">
            <input type="checkbox" {...register('isActive')} />
            Active
          </label>
        )}
        <button
          type="submit"
          disabled={mutation.isPending}
          className="rounded-full bg-luna-black px-4 py-1.5 text-sm text-white disabled:opacity-40"
        >
          {editing ? (mutation.isPending ? 'Enregistrement...' : 'Enregistrer') : '+ Créer'}
        </button>
        {editing && (
          <button type="button" onClick={cancelEditing} className="text-xs underline">
            Annuler
          </button>
        )}
      </form>
      {(errors.name || errors.slug || errors.displayOrder) && (
        <p className="mt-1 text-xs text-red-600">
          {errors.name?.message || errors.slug?.message || errors.displayOrder?.message}
        </p>
      )}
      {mutation.isError && (
        <p className="mt-1 text-xs text-red-600">
          {mutation.error instanceof ApiError ? mutation.error.message : 'Une erreur est survenue.'}
        </p>
      )}
    </div>
  );
}
