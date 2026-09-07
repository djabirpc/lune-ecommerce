import { z } from 'zod';

// Mirrors Ecommerce.Application.Catalog.Validators.SlugValidationRule on the backend.
export const slugSchema = z
  .string()
  .min(1, 'Le slug est requis.')
  .max(220)
  .regex(/^[a-z0-9]+(-[a-z0-9]+)*$/, 'Le slug doit être en minuscules, alphanumérique, avec des tirets (ex: robe-longue).');

const COMBINING_DIACRITICS = /[̀-ͯ]/g;

/** Derives a slug matching `slugSchema` from free text (e.g. a product/category name) — strips accents, lowercases, replaces any run of non-alphanumeric characters with a single hyphen. */
export function slugify(text: string): string {
  return text
    .normalize('NFD')
    .replace(COMBINING_DIACRITICS, '')
    .toLowerCase()
    .trim()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '')
    .slice(0, 220);
}
