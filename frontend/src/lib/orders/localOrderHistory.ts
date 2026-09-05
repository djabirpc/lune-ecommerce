/**
 * Remembers which orders were placed from this browser, so a guest (no accounts exist, per
 * CLAUDE.md section 11) can see "my orders" without retyping their order number/phone every time.
 * Only {orderNumber, phone} pairs are stored — every actual lookup still goes through the real,
 * phone-verified GET /api/orders/track endpoint, so this is a local convenience shortcut, not a
 * bypass of the anti-enumeration protection.
 */
export interface LocalOrderEntry {
  orderNumber: string;
  phone: string;
  placedAtUtc: string;
}

const STORAGE_KEY = 'luna-order-history';

export function getOrderHistory(): LocalOrderEntry[] {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    return raw ? (JSON.parse(raw) as LocalOrderEntry[]) : [];
  } catch {
    return [];
  }
}

export function rememberOrder(orderNumber: string, phone: string): void {
  const current = getOrderHistory().filter((entry) => entry.orderNumber !== orderNumber);
  current.unshift({ orderNumber, phone, placedAtUtc: new Date().toISOString() });
  localStorage.setItem(STORAGE_KEY, JSON.stringify(current.slice(0, 50)));
}

export function findPhoneForOrder(orderNumber: string): string | undefined {
  return getOrderHistory().find((entry) => entry.orderNumber === orderNumber)?.phone;
}
