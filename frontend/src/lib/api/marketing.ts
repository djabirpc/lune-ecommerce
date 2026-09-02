import { apiClient } from './client';
import type { MarketingSourceSummaryDto } from './types';

export const marketingApi = {
  getSources: (days = 30) => apiClient.get<MarketingSourceSummaryDto[]>(`/api/marketing/sources?days=${days}`),
};
