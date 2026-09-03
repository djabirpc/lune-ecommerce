import { apiClient } from './client';
import type { CreateUserRequest, PagedResult, ResetPasswordRequest, UpdateUserRequest, UserDto } from './types';

export const usersApi = {
  getPaged: (params: { page?: number; pageSize?: number } = {}) => {
    const query = new URLSearchParams();
    if (params.page) query.set('page', String(params.page));
    if (params.pageSize) query.set('pageSize', String(params.pageSize));
    const queryString = query.toString();
    return apiClient.get<PagedResult<UserDto>>(`/api/users${queryString ? `?${queryString}` : ''}`);
  },

  create: (request: CreateUserRequest) => apiClient.post<UserDto>('/api/users', request),

  update: (id: string, request: UpdateUserRequest) => apiClient.put<UserDto>(`/api/users/${id}`, request),

  resetPassword: (id: string, request: ResetPasswordRequest) =>
    apiClient.post<void>(`/api/users/${id}/reset-password`, request),
};
