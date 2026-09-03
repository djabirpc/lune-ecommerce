import { apiClient } from './client';
import type {
  CreateShipmentRequest,
  DeliveryType,
  ShipmentDto,
  ShippingCarrierAvailabilityDto,
  ShippingQuoteDto,
  ShippingRateDto,
  UpdateShippingRateRequest,
} from './types';

export const shippingApi = {
  createShipment: (orderId: string, request: CreateShipmentRequest) =>
    apiClient.post<ShipmentDto>(`/api/orders/${orderId}/shipment`, request),

  sync: (shipmentId: string) => apiClient.post<ShipmentDto>(`/api/shipments/${shipmentId}/sync`),

  getLabel: (shipmentId: string) => apiClient.getText(`/api/shipments/${shipmentId}/label`),

  getCarriers: () => apiClient.get<ShippingCarrierAvailabilityDto[]>('/api/shipping/carriers'),
};

export const shippingRatesApi = {
  getAll: () => apiClient.get<ShippingRateDto[]>('/api/shipping-rates'),

  update: (wilaya: string, request: UpdateShippingRateRequest) =>
    apiClient.put<ShippingRateDto>(`/api/shipping-rates/${encodeURIComponent(wilaya)}`, request),

  getQuote: (wilaya: string, deliveryType: DeliveryType) =>
    apiClient.get<ShippingQuoteDto>(
      `/api/shipping-rates/quote?wilaya=${encodeURIComponent(wilaya)}&deliveryType=${deliveryType}`,
    ),
};
