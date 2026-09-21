import { apiClient } from './client';
import type { Ecotrack48hPreviewDashboardDto, Ecotrack48hPreviewOrdersPageDto, Ecotrack48hPreviewTrackingDto } from './types';

// Shared "soft" filters (period/amount/free-text search) — apply to both the orders table and the
// dashboard (map/stat cards/top-wilayas). status/wilayaId are "hard" filters, only ever sent with the
// orders-table call (see Ecotrack48hPreviewFilter's doc comment on the backend for why).
export interface EcotrackPreviewSoftFilter {
  startDate?: string; // yyyy-mm-dd
  endDate?: string; // yyyy-mm-dd
  minAmount?: number;
  maxAmount?: number;
  search?: string;
}

function appendSoftFilter(query: URLSearchParams, filter: EcotrackPreviewSoftFilter) {
  if (filter.startDate) query.set('startDate', filter.startDate);
  if (filter.endDate) query.set('endDate', filter.endDate);
  if (filter.minAmount !== undefined) query.set('minAmount', String(filter.minAmount));
  if (filter.maxAmount !== undefined) query.set('maxAmount', String(filter.maxAmount));
  if (filter.search) query.set('search', filter.search);
}

// Read-only preview of the merchant's real 48h Express (Ecotrack) account — see
// Ecotrack48hPreviewController on the backend. Not part of the real order/shipping API surface.
export const ecotrackPreviewApi = {
  getOrders: (
    params: { page: number; pageSize?: number; status?: string; wilayaId?: number } & EcotrackPreviewSoftFilter,
  ) => {
    const query = new URLSearchParams();
    query.set('page', String(params.page));
    query.set('pageSize', String(params.pageSize ?? 20));
    if (params.status) query.set('status', params.status);
    if (params.wilayaId) query.set('wilayaId', String(params.wilayaId));
    appendSoftFilter(query, params);
    return apiClient.get<Ecotrack48hPreviewOrdersPageDto>(`/api/ecotrack-preview/orders?${query.toString()}`);
  },

  getTracking: (tracking: string) =>
    apiClient.get<Ecotrack48hPreviewTrackingDto>(`/api/ecotrack-preview/orders/${encodeURIComponent(tracking)}`),

  // Real PDF label from Ecotrack's documented GET /get/order/label endpoint — still a GET, no
  // different from any other preview call (see Ecotrack48hPreviewController).
  getLabelPdf: (tracking: string) =>
    apiClient.getBlob(`/api/ecotrack-preview/orders/${encodeURIComponent(tracking)}/label`),

  getDashboard: (filter: EcotrackPreviewSoftFilter = {}) => {
    const query = new URLSearchParams();
    appendSoftFilter(query, filter);
    const queryString = query.toString();
    return apiClient.get<Ecotrack48hPreviewDashboardDto>(`/api/ecotrack-preview/dashboard${queryString ? `?${queryString}` : ''}`);
  },
};
