import { apiClient } from '@/lib/axios';
import { ProjectDetail, ProjectSummary, CreateProjectRequest, UpdateProjectRequest } from '@/types';

interface ProjectListResult {
  items: ProjectSummary[]; totalCount: number; pageNumber: number;
  totalPages: number; hasPreviousPage: boolean; hasNextPage: boolean;
}

interface ListProjectsParams {
  pageNumber?: number; pageSize?: number; search?: string;
  isArchived?: boolean; departmentId?: number; sortBy?: string; sortDescending?: boolean;
}

export const projectsApi = {
  list: (params?: ListProjectsParams) =>
    apiClient.get<ProjectListResult>('/projects', { params }).then(r => r.data),

  get: (projectId: string) =>
    apiClient.get<ProjectDetail>(`/projects/${projectId}`).then(r => ({
      data: r.data,
      etag: r.headers['etag'] as string | undefined,
    })),

  create: (data: CreateProjectRequest) =>
    apiClient.post<{ projectId: string; projectKey: string; name: string }>('/projects', data).then(r => r.data),

  update: (projectId: string, data: UpdateProjectRequest, etag: string) =>
    apiClient.put(`/projects/${projectId}`, data, { headers: { 'If-Match': etag } }).then(r => r.data),

  archive: (projectId: string) =>
    apiClient.post(`/projects/${projectId}/archive`).then(r => r.data),

  delete: (projectId: string) =>
    apiClient.delete(`/projects/${projectId}`).then(r => r.data),

  addMember: (projectId: string, userId: string, roleId: number) =>
    apiClient.post<{ memberId: string }>(`/projects/${projectId}/members`, { userId, roleId }).then(r => r.data),

  removeMember: (projectId: string, userId: string) =>
    apiClient.delete(`/projects/${projectId}/members/${userId}`).then(r => r.data),

  changeMemberRole: (projectId: string, userId: string, roleId: number) =>
    apiClient.patch(`/projects/${projectId}/members/${userId}/role`, { roleId }).then(r => r.data),

  assignLead: (projectId: string, newLeadUserId: string) =>
    apiClient.put(`/projects/${projectId}/lead`, { newLeadUserId }).then(r => r.data),
};
