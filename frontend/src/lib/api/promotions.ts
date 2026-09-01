import { apiClient } from './client';
import type { PagedResult, PromotionDetailDto, PromotionDto, SavePromotionRequest } from './types';

export const promotionsApi = {
  getActive: () => apiClient.get<PromotionDto[]>('/api/promotions/active'),

  getPaged: (params: { includeInactive?: boolean; page?: number; pageSize?: number } = {}) => {
    const query = new URLSearchParams();
    if (params.includeInactive) query.set('includeInactive', 'true');
    if (params.page) query.set('page', String(params.page));
    if (params.pageSize) query.set('pageSize', String(params.pageSize));
    const queryString = query.toString();
    return apiClient.get<PagedResult<PromotionDto>>(`/api/promotions${queryString ? `?${queryString}` : ''}`);
  },

  getById: (id: string) => apiClient.get<PromotionDetailDto>(`/api/promotions/${id}`),

  create: (request: SavePromotionRequest) => apiClient.post<PromotionDetailDto>('/api/promotions', request),

  update: (id: string, request: SavePromotionRequest) => apiClient.put<PromotionDetailDto>(`/api/promotions/${id}`, request),
};
