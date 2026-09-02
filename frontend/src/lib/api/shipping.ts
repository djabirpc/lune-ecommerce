import { apiClient } from './client';
import type { CreateShipmentRequest, ShipmentDto, ShippingCarrierAvailabilityDto } from './types';

export const shippingApi = {
  createShipment: (orderId: string, request: CreateShipmentRequest) =>
    apiClient.post<ShipmentDto>(`/api/orders/${orderId}/shipment`, request),

  sync: (shipmentId: string) => apiClient.post<ShipmentDto>(`/api/shipments/${shipmentId}/sync`),

  getLabel: (shipmentId: string) => apiClient.getText(`/api/shipments/${shipmentId}/label`),

  getCarriers: () => apiClient.get<ShippingCarrierAvailabilityDto[]>('/api/shipping/carriers'),
};
