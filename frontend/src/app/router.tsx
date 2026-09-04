import { createBrowserRouter } from 'react-router-dom';

import { StorefrontLayout } from '../storefront/layout/StorefrontLayout';
import { HomePage } from '../storefront/pages/HomePage';
import { CategoriesPage } from '../storefront/pages/CategoriesPage';
import { CategoryPage } from '../storefront/pages/CategoryPage';
import { ProductPage } from '../storefront/pages/ProductPage';
import { PromotionsPage } from '../storefront/pages/PromotionsPage';
import { CartPage } from '../storefront/pages/CartPage';
import { CheckoutPage } from '../storefront/pages/CheckoutPage';
import { OrderConfirmationPage } from '../storefront/pages/OrderConfirmationPage';
import { TrackOrderPage } from '../storefront/pages/TrackOrderPage';
import { OrdersPage as StorefrontOrdersPage } from '../storefront/pages/OrdersPage';
import { AccountPage } from '../storefront/pages/AccountPage';
import { FavoritesPage } from '../storefront/pages/FavoritesPage';

import { RequireAdminAuth } from '../lib/auth/RequireAdminAuth';
import { AdminLayout } from '../admin/layout/AdminLayout';
import { LoginPage as AdminLoginPage } from '../admin/pages/LoginPage';
import { DashboardPage } from '../admin/pages/DashboardPage';
import { OrdersPage as AdminOrdersPage } from '../admin/pages/OrdersPage';
import { OrderDetailPage } from '../admin/pages/OrderDetailPage';
import { OrderConfirmationCenterPage } from '../admin/pages/OrderConfirmationCenterPage';
import { ProductsPage } from '../admin/pages/ProductsPage';
import { InventoryPage } from '../admin/pages/InventoryPage';
import { SuppliersPage } from '../admin/pages/SuppliersPage';
import { PromotionsPage as AdminPromotionsPage } from '../admin/pages/PromotionsPage';
import { CustomersPage } from '../admin/pages/CustomersPage';
import { ShippingPage } from '../admin/pages/ShippingPage';
import { MarketingPage } from '../admin/pages/MarketingPage';
import { UsersPage } from '../admin/pages/UsersPage';
import { SettingsPage } from '../admin/pages/SettingsPage';

export const router = createBrowserRouter([
  {
    path: '/',
    element: <StorefrontLayout />,
    children: [
      { index: true, element: <HomePage /> },
      { path: 'categories', element: <CategoriesPage /> },
      { path: 'category/:slug', element: <CategoryPage /> },
      { path: 'product/:slug', element: <ProductPage /> },
      { path: 'promotions', element: <PromotionsPage /> },
      { path: 'cart', element: <CartPage /> },
      { path: 'checkout', element: <CheckoutPage /> },
      { path: 'order-confirmation/:orderNumber', element: <OrderConfirmationPage /> },
      { path: 'track-order', element: <TrackOrderPage /> },
      { path: 'orders', element: <StorefrontOrdersPage /> },
      { path: 'account', element: <AccountPage /> },
      { path: 'favoris', element: <FavoritesPage /> },
    ],
  },
  {
    path: '/admin/login',
    element: <AdminLoginPage />,
  },
  {
    path: '/admin',
    element: <RequireAdminAuth />,
    children: [
      {
        element: <AdminLayout />,
        children: [
          { index: true, element: <DashboardPage /> },
          { path: 'dashboard', element: <DashboardPage /> },
          { path: 'orders', element: <AdminOrdersPage /> },
          { path: 'orders/confirmation', element: <OrderConfirmationCenterPage /> },
          { path: 'orders/:id', element: <OrderDetailPage /> },
          { path: 'products', element: <ProductsPage /> },
          { path: 'inventory', element: <InventoryPage /> },
          { path: 'suppliers', element: <SuppliersPage /> },
          { path: 'promotions', element: <AdminPromotionsPage /> },
          { path: 'customers', element: <CustomersPage /> },
          { path: 'shipping', element: <ShippingPage /> },
          { path: 'marketing', element: <MarketingPage /> },
          { path: 'users', element: <UsersPage /> },
          { path: 'settings', element: <SettingsPage /> },
        ],
      },
    ],
  },
]);
