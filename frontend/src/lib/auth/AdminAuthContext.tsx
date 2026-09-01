import { createContext, useContext, useEffect, useState, type ReactNode } from 'react';

import { authApi } from '../api/auth';
import type { CurrentUserResponse } from '../api/types';
import { AUTH_EXPIRED_EVENT, clearAuth, loadAuth, saveAuth } from './tokenStorage';

interface AdminAuthContextValue {
  user: CurrentUserResponse | null;
  isLoading: boolean;
  login: (email: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
}

const AdminAuthContext = createContext<AdminAuthContextValue | null>(null);

export function AdminAuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<CurrentUserResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;

    async function restoreSession() {
      if (!loadAuth()) {
        setIsLoading(false);
        return;
      }

      try {
        const currentUser = await authApi.me();
        if (!cancelled) setUser(currentUser);
      } catch {
        clearAuth();
      } finally {
        if (!cancelled) setIsLoading(false);
      }
    }

    void restoreSession();

    const onAuthExpired = () => setUser(null);
    window.addEventListener(AUTH_EXPIRED_EVENT, onAuthExpired);

    return () => {
      cancelled = true;
      window.removeEventListener(AUTH_EXPIRED_EVENT, onAuthExpired);
    };
  }, []);

  async function login(email: string, password: string) {
    const response = await authApi.login({ email, password });
    saveAuth(response);
    setUser(response.user);
  }

  async function logout() {
    const auth = loadAuth();
    if (auth) {
      await authApi.logout(auth.refreshToken).catch(() => undefined);
    }
    clearAuth();
    setUser(null);
  }

  return (
    <AdminAuthContext.Provider value={{ user, isLoading, login, logout }}>{children}</AdminAuthContext.Provider>
  );
}

export function useAdminAuth(): AdminAuthContextValue {
  const context = useContext(AdminAuthContext);
  if (!context) {
    throw new Error('useAdminAuth must be used within an AdminAuthProvider');
  }
  return context;
}
