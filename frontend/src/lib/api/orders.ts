import { apiClient } from './client';
import type { CreateOrderRequest, OrderDetailDto } from './types';

export const ordersApi = {
  create: (request: CreateOrderRequest) => apiClient.post<OrderDetailDto>('/api/orders', request),

  track: (orderNumber: string, phone: string) =>
    apiClient.get<OrderDetailDto>(
      `/api/orders/track?orderNumber=${encodeURIComponent(orderNumber)}&phone=${encodeURIComponent(phone)}`,
    ),
};
