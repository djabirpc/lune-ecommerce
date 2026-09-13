import type { NormalizedShippingStatus, ShippingCarrier } from '../api/types';

// "Fake" is presented as the fictional carrier "Atlas Express" everywhere in the UI — cosmetic
// only, the underlying provider is still the FakeShippingProvider dev/test double (see its
// XML doc comment in the backend). Yalidine/ZR Express remain unnamed real carriers, unconfigured
// until real API docs/credentials exist (CLAUDE.md sections 16/17). Ecotrack48h (48h Express) is a
// genuine, functional integration — real API docs were provided — just unconfigured until real
// account credentials (Ecotrack48h__BaseUrl/Ecotrack48h__ApiToken) are set.
export const SHIPPING_CARRIER_LABELS: Record<ShippingCarrier, string> = {
  Fake: 'Atlas Express',
  Yalidine: 'Yalidine',
  ZRExpress: 'ZR Express',
  Ecotrack48h: '48h Express',
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
