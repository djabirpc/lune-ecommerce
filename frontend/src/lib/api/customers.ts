import { apiClient } from './client';
import type { CustomerSummaryDto, PagedResult } from './types';

export const customersApi = {
  getPaged: (params: { search?: string; page?: number; pageSize?: number } = {}) => {
    const query = new URLSearchParams();
    if (params.search) query.set('search', params.search);
    if (params.page) query.set('page', String(params.page));
    if (params.pageSize) query.set('pageSize', String(params.pageSize));
    const queryString = query.toString();
    return apiClient.get<PagedResult<CustomerSummaryDto>>(`/api/customers${queryString ? `?${queryString}` : ''}`);
  },
};
