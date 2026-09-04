import { apiClient } from './client';
import type { AdjustInventoryRequest, InventoryDto, InventoryTransactionDto, RestockRequest } from './types';

export const inventoryApi = {
  getByVariantId: (variantId: string) => apiClient.get<InventoryDto>(`/api/inventory/${variantId}`),

  getTransactions: (variantId: string) =>
    apiClient.get<InventoryTransactionDto[]>(`/api/inventory/${variantId}/transactions`),

  restock: (request: RestockRequest) => apiClient.post<InventoryDto>('/api/inventory/restock', request),

  adjust: (request: AdjustInventoryRequest) => apiClient.post<InventoryDto>('/api/inventory/adjust', request),
};
