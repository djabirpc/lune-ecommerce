import { apiClient } from './client';
import type { CategoryDto, PagedResult, ProductDetailDto, ProductListItemDto } from './types';

export const catalogApi = {
  getCategories: () => apiClient.get<CategoryDto[]>('/api/categories'),

  getCategoryBySlug: (slug: string) => apiClient.get<CategoryDto>(`/api/categories/${slug}`),

  getProducts: (params: { category?: string; page?: number; pageSize?: number } = {}) => {
    const query = new URLSearchParams();
    if (params.category) query.set('category', params.category);
    if (params.page) query.set('page', String(params.page));
    if (params.pageSize) query.set('pageSize', String(params.pageSize));
    const queryString = query.toString();
    return apiClient.get<PagedResult<ProductListItemDto>>(`/api/products${queryString ? `?${queryString}` : ''}`);
  },

  getProductBySlug: (slug: string) => apiClient.get<ProductDetailDto>(`/api/products/${slug}`),
};
