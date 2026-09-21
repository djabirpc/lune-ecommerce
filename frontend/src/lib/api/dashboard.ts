import { apiClient } from './client';
import type { DashboardOverviewDto } from './types';

export const dashboardApi = {
  getOverview: (from: string, to: string) =>
    apiClient.get<DashboardOverviewDto>(`/api/dashboard/overview?from=${from}&to=${to}`),
};
