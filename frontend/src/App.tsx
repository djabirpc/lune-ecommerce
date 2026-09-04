import { QueryClientProvider } from '@tanstack/react-query';
import { RouterProvider } from 'react-router-dom';

import { queryClient } from './lib/api/queryClient';
import { CartProvider } from './lib/cart/CartContext';
import { FavoritesProvider } from './lib/favorites/FavoritesContext';
import { AdminAuthProvider } from './lib/auth/AdminAuthContext';
import { router } from './app/router';

export function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <AdminAuthProvider>
        <CartProvider>
          <FavoritesProvider>
            <RouterProvider router={router} />
          </FavoritesProvider>
        </CartProvider>
      </AdminAuthProvider>
    </QueryClientProvider>
  );
}
