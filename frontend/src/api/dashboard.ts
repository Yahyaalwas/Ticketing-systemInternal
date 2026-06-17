import { apiClient } from '@/lib/axios';
import { DashboardDto } from '@/types';

export const dashboardApi = {
  get: () => apiClient.get<DashboardDto>('/dashboard').then(r => r.data),
};
