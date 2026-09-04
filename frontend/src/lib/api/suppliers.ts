import { apiClient } from './client';
import type { PagedResult, SaveSupplierRequest, SupplierDto } from './types';

export const suppliersApi = {
  getPaged: (params: { includeInactive?: boolean; page?: number; pageSize?: number } = {}) => {
    const query = new URLSearchParams();
    if (params.includeInactive) query.set('includeInactive', 'true');
    if (params.page) query.set('page', String(params.page));
    if (params.pageSize) query.set('pageSize', String(params.pageSize));
    const queryString = query.toString();
    return apiClient.get<PagedResult<SupplierDto>>(`/api/suppliers${queryString ? `?${queryString}` : ''}`);
  },

  create: (request: SaveSupplierRequest) => apiClient.post<SupplierDto>('/api/suppliers', request),

  update: (id: string, request: SaveSupplierRequest) => apiClient.put<SupplierDto>(`/api/suppliers/${id}`, request),
};
