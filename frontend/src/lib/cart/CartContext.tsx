import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';

export interface CartItem {
  variantId: string;
  productSlug: string;
  productName: string;
  color: string;
  size: string;
  sku: string;
  unitPrice: number;
  quantity: number;
  imageUrl: string | null;
  availableQuantity: number;
}

interface CartContextValue {
  items: CartItem[];
  addItem: (item: Omit<CartItem, 'quantity'>, quantity: number) => void;
  removeItem: (variantId: string) => void;
  setQuantity: (variantId: string, quantity: number) => void;
  clear: () => void;
  subtotal: number;
  itemCount: number;
}

const CartContext = createContext<CartContextValue | null>(null);
const STORAGE_KEY = 'luna-cart';

function loadCart(): CartItem[] {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    return raw ? (JSON.parse(raw) as CartItem[]) : [];
  } catch {
    return [];
  }
}

export function CartProvider({ children }: { children: ReactNode }) {
  const [items, setItems] = useState<CartItem[]>(loadCart);

  useEffect(() => {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(items));
  }, [items]);

  const addItem: CartContextValue['addItem'] = (item, quantity) => {
    setItems((current) => {
      const existing = current.find((i) => i.variantId === item.variantId);
      const cap = item.availableQuantity;

      if (existing) {
        return current.map((i) =>
          i.variantId === item.variantId
            ? { ...i, quantity: Math.min(i.quantity + quantity, cap) }
            : i,
        );
      }

      return [...current, { ...item, quantity: Math.min(quantity, cap) }];
    });
  };

  const removeItem: CartContextValue['removeItem'] = (variantId) => {
    setItems((current) => current.filter((i) => i.variantId !== variantId));
  };

  const setQuantity: CartContextValue['setQuantity'] = (variantId, quantity) => {
    setItems((current) =>
      current
        .map((i) => (i.variantId === variantId ? { ...i, quantity: Math.min(Math.max(quantity, 1), i.availableQuantity) } : i))
        .filter((i) => i.quantity > 0),
    );
  };

  const clear = () => setItems([]);

  const subtotal = useMemo(() => items.reduce((sum, i) => sum + i.unitPrice * i.quantity, 0), [items]);
  const itemCount = useMemo(() => items.reduce((sum, i) => sum + i.quantity, 0), [items]);

  return (
    <CartContext.Provider value={{ items, addItem, removeItem, setQuantity, clear, subtotal, itemCount }}>
      {children}
    </CartContext.Provider>
  );
}

export function useCart(): CartContextValue {
  const context = useContext(CartContext);
  if (!context) {
    throw new Error('useCart must be used within a CartProvider');
  }
  return context;
}
