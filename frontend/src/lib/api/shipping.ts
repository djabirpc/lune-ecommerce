import { apiClient } from './client';
import type {
  CarrierShippingRateDto,
  CreateShipmentRequest,
  DeliveryType,
  SaveCarrierShippingRateRequest,
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

  // Real PDF label — only works for carriers with a documented label endpoint (currently Ecotrack48h).
  getLabelPdf: (shipmentId: string) => apiClient.getBlob(`/api/shipments/${shipmentId}/label/pdf`),

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

// Admin-entered reference rates per carrier — never used to compute real order pricing, see
// CarrierShippingRate's doc comment on the backend.
export const carrierShippingRatesApi = {
  getAll: () => apiClient.get<CarrierShippingRateDto[]>('/api/carrier-shipping-rates'),

  create: (request: SaveCarrierShippingRateRequest) =>
    apiClient.post<CarrierShippingRateDto>('/api/carrier-shipping-rates', request),

  update: (id: string, request: SaveCarrierShippingRateRequest) =>
    apiClient.put<CarrierShippingRateDto>(`/api/carrier-shipping-rates/${id}`, request),

  delete: (id: string) => apiClient.delete<void>(`/api/carrier-shipping-rates/${id}`),
};
