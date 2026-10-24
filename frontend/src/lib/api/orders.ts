import { apiClient } from './client';
import type {
  AddOrderItemRequest,
  ChangeOrderStatusRequest,
  CreateAdminOrderRequest,
  CreateOrderRequest,
  OrderDetailDto,
  OrderStatus,
  OrderSummaryDto,
  PagedResult,
  ProductOrderHistoryItemDto,
  ProductStatsDto,
  RecordCallAttemptRequest,
  ReturnReasonSummaryDto,
  UpdateOrderNegotiationRequest,
  UpdateOrderNotesRequest,
} from './types';

export const ordersApi = {
  create: (request: CreateOrderRequest) => apiClient.post<OrderDetailDto>('/api/orders', request),

  createAdmin: (request: CreateAdminOrderRequest) => apiClient.post<OrderDetailDto>('/api/orders/admin', request),

  track: (orderNumber: string, phone: string) =>
    apiClient.get<OrderDetailDto>(
      `/api/orders/track?orderNumber=${encodeURIComponent(orderNumber)}&phone=${encodeURIComponent(phone)}`,
    ),

  getPaged: (params: { status?: OrderStatus; page?: number; pageSize?: number; phone?: string } = {}) => {
    const query = new URLSearchParams();
    if (params.status) query.set('status', params.status);
    if (params.page) query.set('page', String(params.page));
    if (params.pageSize) query.set('pageSize', String(params.pageSize));
    if (params.phone) query.set('phone', params.phone);
    const queryString = query.toString();
    return apiClient.get<PagedResult<OrderSummaryDto>>(`/api/orders${queryString ? `?${queryString}` : ''}`);
  },

  getById: (id: string) => apiClient.get<OrderDetailDto>(`/api/orders/${id}`),

  changeStatus: (id: string, request: ChangeOrderStatusRequest) =>
    apiClient.post<OrderDetailDto>(`/api/orders/${id}/status`, request),

  recordCallAttempt: (id: string, request: RecordCallAttemptRequest) =>
    apiClient.post<OrderDetailDto>(`/api/orders/${id}/call-attempts`, request),

  getReturnReasonSummary: () => apiClient.get<ReturnReasonSummaryDto[]>('/api/orders/return-reasons'),

  getByProduct: (productId: string, params: { page?: number; pageSize?: number } = {}) => {
    const query = new URLSearchParams();
    if (params.page) query.set('page', String(params.page));
    if (params.pageSize) query.set('pageSize', String(params.pageSize));
    const queryString = query.toString();
    return apiClient.get<PagedResult<ProductOrderHistoryItemDto>>(`/api/orders/by-product/${productId}${queryString ? `?${queryString}` : ''}`);
  },

  getProductStats: (productId: string) => apiClient.get<ProductStatsDto>(`/api/orders/by-product/${productId}/stats`),

  addItem: (id: string, request: AddOrderItemRequest) => apiClient.post<OrderDetailDto>(`/api/orders/${id}/items`, request),

  removeItem: (id: string, itemId: string) => apiClient.delete<OrderDetailDto>(`/api/orders/${id}/items/${itemId}`),

  updateNegotiation: (id: string, request: UpdateOrderNegotiationRequest) =>
    apiClient.put<OrderDetailDto>(`/api/orders/${id}/negotiation`, request),

  updateNotes: (id: string, request: UpdateOrderNotesRequest) =>
    apiClient.put<OrderDetailDto>(`/api/orders/${id}/notes`, request),
};
