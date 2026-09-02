import type { NormalizedShippingStatus, ShippingCarrier } from '../api/types';

export const SHIPPING_CARRIER_LABELS: Record<ShippingCarrier, string> = {
  Fake: 'Transporteur de test (Fake)',
  Yalidine: 'Yalidine',
  ZRExpress: 'ZR Express',
};

export const NORMALIZED_SHIPPING_STATUS_LABELS: Record<NormalizedShippingStatus, string> = {
  Created: 'Créée',
  PickedUp: 'Colis récupéré',
  InTransit: 'En transit',
  AtDestination: 'Arrivé à destination',
  OutForDelivery: 'En cours de livraison',
  Delivered: 'Livrée',
  Failed: 'Échec',
  Refused: 'Refusée',
  Returned: 'Retournée',
  Cancelled: 'Annulée',
  Unknown: 'Inconnu',
};
