export const ROLE_LABELS: Record<string, string> = {
  SUPER_ADMIN: 'Super administrateur',
  ADMIN: 'Administrateur',
  ORDER_MANAGER: 'Gestionnaire de commandes',
  CONFIRMATION_AGENT: 'Agent de confirmation',
  STOCK_MANAGER: 'Gestionnaire de stock',
  MARKETING_MANAGER: 'Gestionnaire marketing',
  VIEWER: 'Lecteur',
};

export const ALL_ROLES = Object.keys(ROLE_LABELS);
