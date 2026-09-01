import { z } from 'zod';

// Mirrors Ecommerce.Application.Catalog.Validators.SlugValidationRule on the backend.
export const slugSchema = z
  .string()
  .min(1, 'Le slug est requis.')
  .max(220)
  .regex(/^[a-z0-9]+(-[a-z0-9]+)*$/, 'Le slug doit être en minuscules, alphanumérique, avec des tirets (ex: robe-longue).');
