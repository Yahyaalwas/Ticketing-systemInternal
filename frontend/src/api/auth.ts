import { apiClient } from '@/lib/axios';
import { LoginRequest, LoginResponse, CurrentUser } from '@/types';

export const authApi = {
  login: (data: LoginRequest) =>
    apiClient.post<LoginResponse>('/auth/login', data).then(r => r.data),

  logout: () =>
    apiClient.post('/auth/logout').then(r => r.data),

  me: () =>
    apiClient.get<CurrentUser>('/auth/me').then(r => r.data),
};
