import { apiClient } from './client';
import type {
  AdjustInventoryRequest,
  InventoryDto,
  InventoryProductOverviewDto,
  InventoryStockStatus,
  InventoryTransactionDto,
  PagedResult,
  RestockRequest,
} from './types';

export const inventoryApi = {
  getOverview: (params: { search?: string; categoryId?: string; stockStatus?: InventoryStockStatus; page?: number; pageSize?: number } = {}) => {
    const query = new URLSearchParams();
    if (params.search) query.set('search', params.search);
    if (params.categoryId) query.set('categoryId', params.categoryId);
    if (params.stockStatus) query.set('stockStatus', params.stockStatus);
    if (params.page) query.set('page', String(params.page));
    if (params.pageSize) query.set('pageSize', String(params.pageSize));
    const queryString = query.toString();
    return apiClient.get<PagedResult<InventoryProductOverviewDto>>(`/api/inventory${queryString ? `?${queryString}` : ''}`);
  },

  getByProduct: (productId: string) => apiClient.get<InventoryProductOverviewDto>(`/api/inventory/by-product/${productId}`),

  getByVariantId: (variantId: string) => apiClient.get<InventoryDto>(`/api/inventory/${variantId}`),

  getTransactions: (variantId: string) =>
    apiClient.get<InventoryTransactionDto[]>(`/api/inventory/${variantId}/transactions`),

  restock: (request: RestockRequest) => apiClient.post<InventoryDto>('/api/inventory/restock', request),

  adjust: (request: AdjustInventoryRequest) => apiClient.post<InventoryDto>('/api/inventory/adjust', request),
};
