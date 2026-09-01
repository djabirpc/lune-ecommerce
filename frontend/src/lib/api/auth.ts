import { apiClient } from './client';
import type { AuthResponse, CurrentUserResponse, LoginRequest } from './types';

export const authApi = {
  login: (request: LoginRequest) => apiClient.post<AuthResponse>('/api/auth/login', request),

  logout: (refreshToken: string) => apiClient.post<void>('/api/auth/logout', { refreshToken }),

  me: () => apiClient.get<CurrentUserResponse>('/api/auth/me'),
};
