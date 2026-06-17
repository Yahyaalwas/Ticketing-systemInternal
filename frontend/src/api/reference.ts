import { apiClient } from '@/lib/axios';
import { Priority, IssueType, User, Role } from '@/types';

export const referenceApi = {
  getPriorities: () =>
    apiClient.get<Priority[]>('/api/reference/priorities').then(r => r.data),

  getIssueTypes: (projectId: string) =>
    apiClient.get<IssueType[]>(`/api/reference/issue-types`, { params: { projectId } }).then(r => r.data),

  searchUsers: (search: string, limit = 20) =>
    apiClient.get<User[]>('/api/reference/users', { params: { search, limit } }).then(r => r.data),

  getRoles: (scope?: 'Project' | 'Global') =>
    apiClient.get<Role[]>('/api/reference/roles', { params: { scope } }).then(r => r.data),
};
