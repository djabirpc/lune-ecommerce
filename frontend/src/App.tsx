import { QueryClientProvider } from '@tanstack/react-query';
import { RouterProvider } from 'react-router-dom';

import { queryClient } from './lib/api/queryClient';
import { CartProvider } from './lib/cart/CartContext';
import { AdminAuthProvider } from './lib/auth/AdminAuthContext';
import { router } from './app/router';

export function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <AdminAuthProvider>
        <CartProvider>
          <RouterProvider router={router} />
        </CartProvider>
      </AdminAuthProvider>
    </QueryClientProvider>
  );
}
