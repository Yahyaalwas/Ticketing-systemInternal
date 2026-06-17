import { apiClient } from '@/lib/axios';
import { NotificationDto } from '@/types';

export const notificationsApi = {
  list: () => apiClient.get<NotificationDto[]>('/notifications').then(r => r.data),
  markRead: (id: string) => apiClient.post(`/notifications/${id}/read`).then(r => r.data),
  markAllRead: () => apiClient.post('/notifications/read-all').then(r => r.data),
};
