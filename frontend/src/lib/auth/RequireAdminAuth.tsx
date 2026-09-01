import { Navigate, Outlet, useLocation } from 'react-router-dom';

import { useAdminAuth } from './AdminAuthContext';

export function RequireAdminAuth() {
  const { user, isLoading } = useAdminAuth();
  const location = useLocation();

  if (isLoading) {
    return <div className="flex min-h-screen items-center justify-center text-sm text-luna-charcoal/60">Chargement...</div>;
  }

  if (!user) {
    return <Navigate to="/admin/login" state={{ from: location }} replace />;
  }

  return <Outlet />;
}
