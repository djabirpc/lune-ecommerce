import { apiClient } from './client';
import type {
  ChangeOrderStatusRequest,
  CreateOrderRequest,
  OrderDetailDto,
  OrderStatus,
  OrderSummaryDto,
  PagedResult,
} from './types';

export const ordersApi = {
  create: (request: CreateOrderRequest) => apiClient.post<OrderDetailDto>('/api/orders', request),

  track: (orderNumber: string, phone: string) =>
    apiClient.get<OrderDetailDto>(
      `/api/orders/track?orderNumber=${encodeURIComponent(orderNumber)}&phone=${encodeURIComponent(phone)}`,
    ),

  getPaged: (params: { status?: OrderStatus; page?: number; pageSize?: number } = {}) => {
    const query = new URLSearchParams();
    if (params.status) query.set('status', params.status);
    if (params.page) query.set('page', String(params.page));
    if (params.pageSize) query.set('pageSize', String(params.pageSize));
    const queryString = query.toString();
    return apiClient.get<PagedResult<OrderSummaryDto>>(`/api/orders${queryString ? `?${queryString}` : ''}`);
  },

  getById: (id: string) => apiClient.get<OrderDetailDto>(`/api/orders/${id}`),

  changeStatus: (id: string, request: ChangeOrderStatusRequest) =>
    apiClient.post<OrderDetailDto>(`/api/orders/${id}/status`, request),
};
