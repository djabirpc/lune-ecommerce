import { apiClient } from './client';
import type { AdjustInventoryRequest, InventoryDto, RestockRequest } from './types';

export const inventoryApi = {
  getByVariantId: (variantId: string) => apiClient.get<InventoryDto>(`/api/inventory/${variantId}`),

  restock: (request: RestockRequest) => apiClient.post<InventoryDto>('/api/inventory/restock', request),

  adjust: (request: AdjustInventoryRequest) => apiClient.post<InventoryDto>('/api/inventory/adjust', request),
};
