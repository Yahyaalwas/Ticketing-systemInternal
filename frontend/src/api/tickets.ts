import { apiClient } from '@/lib/axios';
import {
  TicketDetail, TicketSummary, CreateTicketRequest, UpdateTicketRequest,
  CreateTicketResponse, AttachmentDto, CommentListResult,
  TicketActivityResult, TransitionRequest,
} from '@/types';

interface TicketListResult {
  items: TicketSummary[]; totalCount: number; pageNumber: number;
  totalPages: number; hasPreviousPage: boolean; hasNextPage: boolean;
}

interface ListTicketsParams {
  projectId: string; pageNumber?: number; pageSize?: number; search?: string;
  statusId?: number; priorityId?: number; issueTypeId?: number;
  assigneeUserId?: string; labelId?: number; isOverdue?: boolean;
  sortBy?: string; sortDescending?: boolean;
}

export const ticketsApi = {
  list: (params: ListTicketsParams) =>
    apiClient.get<TicketListResult>('/tickets', { params }).then(r => r.data),

  get: (ticketId: string) =>
    apiClient.get<TicketDetail>(`/tickets/${ticketId}`).then(r => ({
      data: r.data,
      etag: r.headers['etag'] as string | undefined,
    })),

  create: (data: CreateTicketRequest) =>
    apiClient.post<CreateTicketResponse>('/tickets', data).then(r => r.data),

  update: (ticketId: string, data: UpdateTicketRequest, etag: string) =>
    apiClient.put(`/tickets/${ticketId}`, data, { headers: { 'If-Match': etag } }).then(r => r.data),

  delete: (ticketId: string) =>
    apiClient.delete(`/tickets/${ticketId}`).then(r => r.data),

  transition: (ticketId: string, data: TransitionRequest, etag: string) =>
    apiClient.post(`/tickets/${ticketId}/transitions`, data, { headers: { 'If-Match': etag } }).then(r => r.data),

  assign: (ticketId: string, assigneeUserId: string | null, etag: string) =>
    apiClient.patch(`/tickets/${ticketId}/assign`, { assigneeUserId }, { headers: { 'If-Match': etag } }).then(r => r.data),

  changePriority: (ticketId: string, priorityId: number | null, etag: string) =>
    apiClient.patch(`/tickets/${ticketId}/priority`, { priorityId }, { headers: { 'If-Match': etag } }).then(r => r.data),

  changeDueDate: (ticketId: string, dueDate: string | null, etag: string) =>
    apiClient.patch(`/tickets/${ticketId}/due-date`, { dueDate }, { headers: { 'If-Match': etag } }).then(r => r.data),

  addLabel: (ticketId: string, labelId: number) =>
    apiClient.post(`/tickets/${ticketId}/labels/${labelId}`).then(r => r.data),

  removeLabel: (ticketId: string, labelId: number) =>
    apiClient.delete(`/tickets/${ticketId}/labels/${labelId}`).then(r => r.data),

  addWatcher: (ticketId: string, userId: string) =>
    apiClient.post(`/tickets/${ticketId}/watchers/${userId}`).then(r => r.data),

  removeWatcher: (ticketId: string, userId: string) =>
    apiClient.delete(`/tickets/${ticketId}/watchers/${userId}`).then(r => r.data),

  // Comments
  getComments: (ticketId: string, pageNumber = 1, pageSize = 50) =>
    apiClient.get<CommentListResult>(`/tickets/${ticketId}/comments`, { params: { pageNumber, pageSize } }).then(r => r.data),

  addComment: (ticketId: string, body: string, parentCommentId?: string) =>
    apiClient.post<{ commentId: string; bodyHtml: string }>(`/tickets/${ticketId}/comments`, { body, parentCommentId }).then(r => r.data),

  editComment: (ticketId: string, commentId: string, body: string) =>
    apiClient.put(`/tickets/${ticketId}/comments/${commentId}`, { body }).then(r => r.data),

  deleteComment: (ticketId: string, commentId: string) =>
    apiClient.delete(`/tickets/${ticketId}/comments/${commentId}`).then(r => r.data),

  // Attachments
  getAttachments: (ticketId: string) =>
    apiClient.get<AttachmentDto[]>(`/tickets/${ticketId}/attachments`).then(r => r.data),

  uploadAttachment: (ticketId: string, file: File) => {
    const form = new FormData();
    form.append('file', file);
    return apiClient.post<{ attachmentId: string; fileName: string; publicUrl: string }>(
      `/tickets/${ticketId}/attachments`, form,
      { headers: { 'Content-Type': 'multipart/form-data' } }
    ).then(r => r.data);
  },

  deleteAttachment: (ticketId: string, attachmentId: string) =>
    apiClient.delete(`/tickets/${ticketId}/attachments/${attachmentId}`).then(r => r.data),

  getActivity: (ticketId: string, pageNumber = 1, pageSize = 50) =>
    apiClient.get<TicketActivityResult>(`/tickets/${ticketId}/activity`, { params: { page: pageNumber, pageSize } }).then(r => r.data),

  // Kanban
  getKanbanBoard: (projectId: string, params?: {
    assigneeUserId?: string; priorityId?: number; labelId?: number;
    epicTicketId?: string; issueTypeId?: number; search?: string;
  }) =>
    apiClient.get(`/kanban/${projectId}`, { params }).then(r => r.data),
};
