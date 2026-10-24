import { apiClient } from './client';
import type { StoreSettingsDto, UpdateStoreSettingsRequest } from './types';

export const storeSettingsApi = {
  // Public (no auth) — called by the storefront layout to render the announcement bar, footer
  // social links, and contact phone.
  get: () => apiClient.get<StoreSettingsDto>('/api/store-settings'),
  update: (request: UpdateStoreSettingsRequest) => apiClient.put<StoreSettingsDto>('/api/store-settings', request),
};
