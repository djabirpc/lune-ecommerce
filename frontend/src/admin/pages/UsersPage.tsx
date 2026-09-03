import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { usersApi } from '../../lib/api/users';
import { ApiError } from '../../lib/api/client';
import { useAdminAuth } from '../../lib/auth/AdminAuthContext';
import { ALL_ROLES, ROLE_LABELS } from '../../lib/format/roleLabels';
import type { UserDto } from '../../lib/api/types';

interface FormState {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  isActive: boolean;
  roles: string[];
}

function emptyForm(): FormState {
  return { email: '', password: '', firstName: '', lastName: '', isActive: true, roles: [] };
}

function toFormState(u: UserDto): FormState {
  return { email: u.email, password: '', firstName: u.firstName, lastName: u.lastName, isActive: u.isActive, roles: u.roles };
}

export function UsersPage() {
  const queryClient = useQueryClient();
  const { user: currentUser } = useAdminAuth();
  const [form, setForm] = useState<FormState>(emptyForm());
  const [editingId, setEditingId] = useState<string | null>(null);
  const [formError, setFormError] = useState<string | null>(null);

  const { data, isLoading } = useQuery({
    queryKey: ['admin-users'],
    queryFn: () => usersApi.getPaged({ pageSize: 100 }),
  });

  const save = useMutation({
    mutationFn: () =>
      editingId
        ? usersApi.update(editingId, {
            firstName: form.firstName,
            lastName: form.lastName,
            isActive: form.isActive,
            roles: form.roles,
          })
        : usersApi.create({
            email: form.email,
            password: form.password,
            firstName: form.firstName,
            lastName: form.lastName,
            roles: form.roles,
          }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin-users'] });
      setForm(emptyForm());
      setEditingId(null);
      setFormError(null);
    },
    onError: (err) => setFormError(err instanceof ApiError ? err.message : 'Une erreur est survenue.'),
  });

  const toggleActive = useMutation({
    mutationFn: (u: UserDto) => usersApi.update(u.id, { firstName: u.firstName, lastName: u.lastName, isActive: !u.isActive, roles: u.roles }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['admin-users'] }),
    onError: (err) => setFormError(err instanceof ApiError ? err.message : 'Une erreur est survenue.'),
  });

  function startEdit(u: UserDto) {
    setForm(toFormState(u));
    setEditingId(u.id);
    setFormError(null);
  }

  function cancelEdit() {
    setForm(emptyForm());
    setEditingId(null);
    setFormError(null);
  }

  function toggleRole(role: string) {
    setForm((f) => ({
      ...f,
      roles: f.roles.includes(role) ? f.roles.filter((r) => r !== role) : [...f.roles, role],
    }));
  }

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    save.mutate();
  }

  return (
    <div>
      <h1 className="mb-4 text-xl font-semibold">Utilisateurs</h1>

      <div className="grid gap-6 lg:grid-cols-2">
        <div className="rounded-lg border border-black/10 bg-white p-4">
          <h2 className="mb-3 text-sm font-semibold uppercase text-luna-charcoal/60">
            {editingId ? 'Modifier le compte' : 'Nouveau compte'}
          </h2>

          <form onSubmit={handleSubmit} className="flex flex-col gap-3">
            <div className="grid gap-3 sm:grid-cols-2">
              <div>
                <label className="mb-1 block text-xs font-medium">Prénom</label>
                <input
                  value={form.firstName}
                  onChange={(e) => setForm({ ...form, firstName: e.target.value })}
                  required
                  className="w-full rounded border border-black/20 px-2 py-1 text-sm"
                />
              </div>
              <div>
                <label className="mb-1 block text-xs font-medium">Nom</label>
                <input
                  value={form.lastName}
                  onChange={(e) => setForm({ ...form, lastName: e.target.value })}
                  required
                  className="w-full rounded border border-black/20 px-2 py-1 text-sm"
                />
              </div>
            </div>

            {!editingId && (
              <>
                <div>
                  <label className="mb-1 block text-xs font-medium">Email</label>
                  <input
                    type="email"
                    value={form.email}
                    onChange={(e) => setForm({ ...form, email: e.target.value })}
                    required
                    className="w-full rounded border border-black/20 px-2 py-1 text-sm"
                  />
                </div>
                <div>
                  <label className="mb-1 block text-xs font-medium">Mot de passe initial</label>
                  <input
                    type="password"
                    value={form.password}
                    onChange={(e) => setForm({ ...form, password: e.target.value })}
                    required
                    minLength={8}
                    className="w-full rounded border border-black/20 px-2 py-1 text-sm"
                  />
                </div>
              </>
            )}

            <div>
              <label className="mb-1 block text-xs font-medium">Rôles</label>
              <div className="rounded border border-black/20 p-2">
                {ALL_ROLES.map((role) => (
                  <label key={role} className="flex items-center gap-2 py-0.5 text-xs">
                    <input type="checkbox" checked={form.roles.includes(role)} onChange={() => toggleRole(role)} />
                    {ROLE_LABELS[role]}
                  </label>
                ))}
              </div>
            </div>

            {editingId && (
              <label className="flex w-fit items-center gap-2 text-xs">
                <input
                  type="checkbox"
                  checked={form.isActive}
                  disabled={editingId === currentUser?.id}
                  onChange={(e) => setForm({ ...form, isActive: e.target.checked })}
                />
                Actif{editingId === currentUser?.id && ' (vous ne pouvez pas désactiver votre propre compte)'}
              </label>
            )}

            <div className="flex gap-2">
              <button
                type="submit"
                disabled={save.isPending || form.roles.length === 0}
                className="mt-2 w-fit rounded-full bg-luna-black px-5 py-2 text-sm text-white disabled:opacity-40"
              >
                {save.isPending ? 'Enregistrement...' : editingId ? 'Enregistrer les modifications' : 'Créer le compte'}
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
          <h2 className="mb-3 text-sm font-semibold uppercase text-luna-charcoal/60">Comptes existants</h2>
          {isLoading && <p className="text-sm text-luna-charcoal/60">Chargement...</p>}
          <div className="flex flex-col divide-y divide-black/5 rounded-lg border border-black/10 bg-white text-sm">
            {data?.items.map((u) => (
              <div key={u.id} className="flex items-center justify-between gap-2 px-4 py-2">
                <div>
                  <p className="font-medium">
                    {u.firstName} {u.lastName}
                    {!u.isActive && <span className="ml-1 rounded-full bg-red-100 px-2 py-0.5 text-xs text-red-700">Inactif</span>}
                  </p>
                  <p className="text-xs text-luna-charcoal/60">{u.email}</p>
                  <p className="text-xs text-luna-charcoal/60">{u.roles.map((r) => ROLE_LABELS[r] ?? r).join(', ')}</p>
                </div>
                <div className="flex shrink-0 gap-2 text-xs">
                  <button type="button" onClick={() => startEdit(u)} className="underline">
                    Modifier
                  </button>
                  <button
                    type="button"
                    disabled={u.id === currentUser?.id}
                    onClick={() => toggleActive.mutate(u)}
                    className="underline disabled:opacity-40"
                  >
                    {u.isActive ? 'Désactiver' : 'Activer'}
                  </button>
                </div>
              </div>
            ))}
            {data?.items.length === 0 && <p className="px-4 py-8 text-center text-luna-charcoal/60">Aucun utilisateur.</p>}
          </div>
        </div>
      </div>
    </div>
  );
}
