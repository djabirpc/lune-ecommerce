import { apiClient } from './client';
import type {
  CategoryDto,
  CreateCategoryRequest,
  CreateProductRequest,
  CreateProductVariantRequest,
  PagedResult,
  ProductDetailDto,
  ProductImageDto,
  ProductListItemDto,
  ProductVariantDto,
  UpdateCategoryRequest,
  UpdateProductRequest,
} from './types';

export const catalogApi = {
  getCategories: (params: { includeInactive?: boolean } = {}) => {
    const query = params.includeInactive ? '?includeInactive=true' : '';
    return apiClient.get<CategoryDto[]>(`/api/categories${query}`);
  },

  getCategoryBySlug: (slug: string) => apiClient.get<CategoryDto>(`/api/categories/${slug}`),

  createCategory: (request: CreateCategoryRequest) => apiClient.post<CategoryDto>('/api/categories', request),

  updateCategory: (id: string, request: UpdateCategoryRequest) =>
    apiClient.put<CategoryDto>(`/api/categories/${id}`, request),

  getProducts: (
    params: { category?: string; page?: number; pageSize?: number; includeInactive?: boolean; sortByNewest?: boolean } = {},
  ) => {
    const query = new URLSearchParams();
    if (params.category) query.set('category', params.category);
    if (params.page) query.set('page', String(params.page));
    if (params.pageSize) query.set('pageSize', String(params.pageSize));
    if (params.includeInactive) query.set('includeInactive', 'true');
    if (params.sortByNewest) query.set('sortByNewest', 'true');
    const queryString = query.toString();
    return apiClient.get<PagedResult<ProductListItemDto>>(`/api/products${queryString ? `?${queryString}` : ''}`);
  },

  getProductBySlug: (slug: string) => apiClient.get<ProductDetailDto>(`/api/products/${slug}`),

  createProduct: (request: CreateProductRequest) => apiClient.post<ProductDetailDto>('/api/products', request),

  updateProduct: (id: string, request: UpdateProductRequest) =>
    apiClient.put<ProductDetailDto>(`/api/products/${id}`, request),

  addVariant: (productId: string, request: CreateProductVariantRequest) =>
    apiClient.post<ProductVariantDto>(`/api/products/${productId}/variants`, request),

  uploadImage: (productId: string, file: File, options: { altText?: string; isPrimary?: boolean } = {}) => {
    const formData = new FormData();
    formData.append('File', file);
    formData.append('IsPrimary', options.isPrimary ? 'true' : 'false');
    if (options.altText) formData.append('AltText', options.altText);
    return apiClient.postForm<ProductImageDto>(`/api/products/${productId}/images`, formData);
  },

  deleteImage: (productId: string, imageId: string) =>
    apiClient.delete<void>(`/api/products/${productId}/images/${imageId}`),

  setPrimaryImage: (productId: string, imageId: string) =>
    apiClient.put<ProductImageDto>(`/api/products/${productId}/images/${imageId}/primary`),
};
