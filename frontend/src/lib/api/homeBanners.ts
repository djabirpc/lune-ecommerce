import { apiClient } from './client';
import type { HomeBannerDto, UpdateHomeBannerRequest } from './types';

export const homeBannersApi = {
  getActive: () => apiClient.get<HomeBannerDto[]>('/api/home-banners/active'),

  getAll: () => apiClient.get<HomeBannerDto[]>('/api/home-banners'),

  add: (file: File, linkUrl?: string) => {
    const formData = new FormData();
    formData.append('File', file);
    if (linkUrl) formData.append('LinkUrl', linkUrl);
    return apiClient.postForm<HomeBannerDto>('/api/home-banners', formData);
  },

  update: (id: string, request: UpdateHomeBannerRequest) =>
    apiClient.put<HomeBannerDto>(`/api/home-banners/${id}`, request),

  remove: (id: string) => apiClient.delete<void>(`/api/home-banners/${id}`),
};
