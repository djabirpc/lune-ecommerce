import { apiClient } from './client';
import type { MarketingSettingsDto, MarketingSourceSummaryDto, PurchaseTriggerDto, UpdateMarketingSettingsRequest } from './types';

export const marketingApi = {
  getSources: (days = 30) => apiClient.get<MarketingSourceSummaryDto[]>(`/api/marketing/sources?days=${days}`),
  getSettings: () => apiClient.get<MarketingSettingsDto>('/api/marketing/settings'),
  updateSettings: (request: UpdateMarketingSettingsRequest) =>
    apiClient.put<MarketingSettingsDto>('/api/marketing/settings', request),
  // Public (no auth) — called by the storefront checkout page.
  getPurchaseTrigger: () => apiClient.get<PurchaseTriggerDto>('/api/marketing/purchase-trigger'),
};
