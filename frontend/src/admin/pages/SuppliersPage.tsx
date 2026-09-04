import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { suppliersApi } from '../../lib/api/suppliers';
import { ApiError } from '../../lib/api/client';
import type { SaveSupplierRequest, SupplierDto } from '../../lib/api/types';

function emptyForm(): SaveSupplierRequest {
  return { name: '', phone: null, email: null, address: null, notes: null, isActive: true };
}

function toFormState(s: SupplierDto): SaveSupplierRequest {
  return { name: s.name, phone: s.phone, email: s.email, address: s.address, notes: s.notes, isActive: s.isActive };
}

export function SuppliersPage() {
  const queryClient = useQueryClient();
  const [form, setForm] = useState<SaveSupplierRequest>(emptyForm());
  const [editingId, setEditingId] = useState<string | null>(null);
  const [formError, setFormError] = useState<string | null>(null);

  const { data, isLoading } = useQuery({
    queryKey: ['admin-suppliers'],
    queryFn: () => suppliersApi.getPaged({ includeInactive: true, pageSize: 100 }),
  });

  const save = useMutation({
    mutationFn: () => (editingId ? suppliersApi.update(editingId, form) : suppliersApi.create(form)),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin-suppliers'] });
      setForm(emptyForm());
      setEditingId(null);
      setFormError(null);
    },
    onError: (err) => setFormError(err instanceof ApiError ? err.message : 'Une erreur est survenue.'),
  });

  const toggleActive = useMutation({
    mutationFn: (s: SupplierDto) => suppliersApi.update(s.id, { ...toFormState(s), isActive: !s.isActive }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['admin-suppliers'] }),
    onError: (err) => setFormError(err instanceof ApiError ? err.message : 'Une erreur est survenue.'),
  });

  function startEdit(s: SupplierDto) {
    setForm(toFormState(s));
    setEditingId(s.id);
    setFormError(null);
  }

  function cancelEdit() {
    setForm(emptyForm());
    setEditingId(null);
    setFormError(null);
  }

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    save.mutate();
  }

  return (
    <div>
      <h1 className="mb-4 text-xl font-semibold">Fournisseurs</h1>

      <div className="grid gap-6 lg:grid-cols-2">
        <div className="rounded-lg border border-black/10 bg-white p-4">
          <h2 className="mb-3 text-sm font-semibold uppercase text-luna-charcoal/60">
            {editingId ? 'Modifier le fournisseur' : 'Nouveau fournisseur'}
          </h2>

          <form onSubmit={handleSubmit} className="flex flex-col gap-3">
            <div>
              <label className="mb-1 block text-xs font-medium">Nom</label>
              <input
                value={form.name}
                onChange={(e) => setForm({ ...form, name: e.target.value })}
                required
                className="w-full rounded border border-black/20 px-2 py-1 text-sm"
              />
            </div>

            <div className="grid gap-3 sm:grid-cols-2">
              <div>
                <label className="mb-1 block text-xs font-medium">Téléphone</label>
                <input
                  value={form.phone ?? ''}
                  onChange={(e) => setForm({ ...form, phone: e.target.value || null })}
                  className="w-full rounded border border-black/20 px-2 py-1 text-sm"
                />
              </div>
              <div>
                <label className="mb-1 block text-xs font-medium">Email</label>
                <input
                  type="email"
                  value={form.email ?? ''}
                  onChange={(e) => setForm({ ...form, email: e.target.value || null })}
                  className="w-full rounded border border-black/20 px-2 py-1 text-sm"
                />
              </div>
            </div>

            <div>
              <label className="mb-1 block text-xs font-medium">Adresse</label>
              <input
                value={form.address ?? ''}
                onChange={(e) => setForm({ ...form, address: e.target.value || null })}
                className="w-full rounded border border-black/20 px-2 py-1 text-sm"
              />
            </div>

            <div>
              <label className="mb-1 block text-xs font-medium">Notes</label>
              <textarea
                value={form.notes ?? ''}
                onChange={(e) => setForm({ ...form, notes: e.target.value || null })}
                rows={2}
                className="w-full rounded border border-black/20 px-2 py-1 text-sm"
              />
            </div>

            {editingId && (
              <label className="flex w-fit items-center gap-2 text-xs">
                <input
                  type="checkbox"
                  checked={form.isActive}
                  onChange={(e) => setForm({ ...form, isActive: e.target.checked })}
                />
                Actif
              </label>
            )}

            <div className="flex gap-2">
              <button
                type="submit"
                disabled={save.isPending || !form.name}
                className="mt-2 w-fit rounded-full bg-luna-black px-5 py-2 text-sm text-white disabled:opacity-40"
              >
                {save.isPending ? 'Enregistrement...' : editingId ? 'Enregistrer les modifications' : 'Créer le fournisseur'}
              </button>
              {editingId && (
                <button type="button" onClick={cancelEdit} className="mt-2 w-fit rounded-full border border-black/20 px-5 py-2 text-sm">
                  Annuler
                </button>
              )}
            </div>

            {formError && <p className="text-sm text-red-600">{formError}</p>}
          </form>
        </div>

        <div>
          <h2 className="mb-3 text-sm font-semibold uppercase text-luna-charcoal/60">Fournisseurs existants</h2>
          {isLoading && <p className="text-sm text-luna-charcoal/60">Chargement...</p>}
          <div className="flex flex-col divide-y divide-black/5 rounded-lg border border-black/10 bg-white text-sm">
            {data?.items.map((s) => (
              <div key={s.id} className="flex items-center justify-between gap-2 px-4 py-2">
                <div>
                  <p className="font-medium">
                    {s.name}
                    {!s.isActive && <span className="ml-1 rounded-full bg-red-100 px-2 py-0.5 text-xs text-red-700">Inactif</span>}
                  </p>
                  <p className="text-xs text-luna-charcoal/60">{[s.phone, s.email].filter(Boolean).join(' · ')}</p>
                </div>
                <div className="flex shrink-0 gap-2 text-xs">
                  <button type="button" onClick={() => startEdit(s)} className="underline">
                    Modifier
                  </button>
                  <button type="button" onClick={() => toggleActive.mutate(s)} className="underline">
                    {s.isActive ? 'Désactiver' : 'Activer'}
                  </button>
                </div>
              </div>
            ))}
            {data?.items.length === 0 && <p className="px-4 py-8 text-center text-luna-charcoal/60">Aucun fournisseur.</p>}
          </div>
        </div>
      </div>
    </div>
  );
}
