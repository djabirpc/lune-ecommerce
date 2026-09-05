/**
 * Remembers the last-used checkout details on this device so a returning guest doesn't have to
 * retype them (no accounts exist, per CLAUDE.md section 11 — this is a local convenience only,
 * never sent anywhere until the customer submits a real order).
 */
export interface SavedCustomerInfo {
  firstName: string;
  lastName: string;
  phone: string;
  wilaya: string;
  commune: string;
  address: string;
}

const STORAGE_KEY = 'luna-customer-info';

export function getSavedCustomerInfo(): SavedCustomerInfo | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    return raw ? (JSON.parse(raw) as SavedCustomerInfo) : null;
  } catch {
    return null;
  }
}

export function saveCustomerInfo(info: SavedCustomerInfo): void {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(info));
}
