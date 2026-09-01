import { apiClient } from './client';
import type {
  CategoryDto,
  CreateCategoryRequest,
  CreateProductRequest,
  CreateProductVariantRequest,
  PagedResult,
  ProductDetailDto,
  ProductListItemDto,
  ProductVariantDto,
} from './types';

export const catalogApi = {
  getCategories: (params: { includeInactive?: boolean } = {}) => {
    const query = params.includeInactive ? '?includeInactive=true' : '';
    return apiClient.get<CategoryDto[]>(`/api/categories${query}`);
  },

  getCategoryBySlug: (slug: string) => apiClient.get<CategoryDto>(`/api/categories/${slug}`),

  createCategory: (request: CreateCategoryRequest) => apiClient.post<CategoryDto>('/api/categories', request),

  getProducts: (params: { category?: string; page?: number; pageSize?: number; includeInactive?: boolean } = {}) => {
    const query = new URLSearchParams();
    if (params.category) query.set('category', params.category);
    if (params.page) query.set('page', String(params.page));
    if (params.pageSize) query.set('pageSize', String(params.pageSize));
    if (params.includeInactive) query.set('includeInactive', 'true');
    const queryString = query.toString();
    return apiClient.get<PagedResult<ProductListItemDto>>(`/api/products${queryString ? `?${queryString}` : ''}`);
  },

  getProductBySlug: (slug: string) => apiClient.get<ProductDetailDto>(`/api/products/${slug}`),

  createProduct: (request: CreateProductRequest) => apiClient.post<ProductDetailDto>('/api/products', request),

  addVariant: (productId: string, request: CreateProductVariantRequest) =>
    apiClient.post<ProductVariantDto>(`/api/products/${productId}/variants`, request),
};
