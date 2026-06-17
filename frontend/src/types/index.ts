// Auth
export interface LoginRequest { userPrincipalName: string; password: string; }
export interface LoginResponse {
  token: string; userId: string; displayName: string;
  email: string; avatarUrl?: string; roles: string[];
}
export interface CurrentUser {
  userId: string; userPrincipalName: string; displayName: string;
  email: string; avatarUrl?: string; roles: string[]; departmentId?: number;
}

// Pagination
export interface PaginatedResult<T> {
  items: T[]; totalCount: number; pageNumber: number;
  totalPages: number; hasPreviousPage: boolean; hasNextPage: boolean;
}

// Projects
export interface ProjectSummary {
  id: string; projectKey: string; name: string; description?: string;
  avatarUrl?: string; leadUserId: string; leadDisplayName: string;
  departmentId: number; departmentName?: string; isArchived: boolean;
  createdAt: string; memberCount: number;
}
export interface ProjectDetail extends ProjectSummary {
  activeWorkflowId?: string; activeWorkflowName?: string; archivedAt?: string;
  updatedAt: string; rowVersion: string;
  issueTypes: string[]; members: ProjectMember[]; activeTicketCount: number;
}
export interface ProjectMember {
  memberId: string; userId: string; displayName: string;
  avatarUrl?: string; roleName: string; grantedAt: string;
}
export interface CreateProjectRequest {
  projectKey: string; name: string; description?: string;
  leadUserId: string; departmentId: number; workflowId?: string;
}
export interface UpdateProjectRequest {
  name: string; description?: string; leadUserId: string;
  avatarUrl?: string; workflowId?: string;
}

// Tickets
export interface TicketSummary {
  id: string; ticketKey: string; ticketNumber: number; title: string;
  issueTypeName: string; issueTypeIconUrl?: string;
  statusName: string; statusCategory: string; statusColor?: string;
  priorityName?: string; priorityColor?: string;
  assigneeUserId?: string; assigneeName?: string; assigneeAvatarUrl?: string;
  dueDate?: string; slaBreachAt?: string; isSlaBreached: boolean;
  createdAt: string; updatedAt: string; rowVersion: string;
}
export interface TicketDetail {
  id: string; projectId: string; projectKey: string;
  ticketNumber: number; ticketKey: string;
  title: string; description?: string; descriptionHtml?: string;
  issueTypeName: string; statusName: string; statusCategory: string; statusColor?: string;
  priorityName?: string; priorityColor?: string;
  assigneeUserId?: string; assigneeName?: string; assigneeAvatarUrl?: string;
  reporterUserId: string; reporterName: string;
  parentTicketId?: string; parentTicketKey?: string;
  epicTicketId?: string; epicTicketKey?: string;
  dueDate?: string; storyPoints?: number; estimatedHours?: number; actualHours?: number;
  resolutionName?: string; resolvedAt?: string; slaBreachAt?: string;
  isDeleted: boolean; createdAt: string; createdByName: string; updatedAt: string;
  rowVersion: string;
  labels: LabelDto[]; links: TicketLinkDto[]; watchers: TicketWatcherDto[];
  commentCount: number; attachmentCount: number;
}
export interface CreateTicketRequest {
  projectId: string; title: string; description?: string;
  issueTypeId: number; priorityId?: number; assigneeUserId?: string;
  parentTicketId?: string; epicTicketId?: string;
  dueDate?: string; storyPoints?: number; labelIds?: number[];
}
export interface UpdateTicketRequest {
  title: string; description?: string; issueTypeId: number;
  priorityId?: number; assigneeUserId?: string;
  dueDate?: string; storyPoints?: number; estimatedHours?: number;
}
export interface LabelDto { id: number; name: string; color?: string; }
export interface TicketLinkDto {
  linkId: string; linkedTicketId: string; linkedTicketKey: string;
  linkedTicketTitle: string; linkDescription: string;
}
export interface TicketWatcherDto { userId: string; displayName: string; }
export interface CreateTicketResponse { ticketId: string; ticketKey: string; ticketNumber: number; }

// Comments
export interface CommentDto {
  id: string; parentCommentId?: string;
  authorUserId: string; authorName: string; authorAvatarUrl?: string;
  body: string; bodyHtml?: string; isEdited: boolean;
  createdAt: string; updatedAt: string; mentionedUserIds: string[];
}
export interface CommentListResult {
  items: CommentDto[]; totalCount: number; pageNumber: number; totalPages: number;
}

// Attachments
export interface AttachmentDto {
  id: string; fileName: string; contentType: string;
  fileSizeBytes: number; checksum?: string; publicUrl: string;
  uploaderUserId: string; uploaderName: string; uploadedAt: string;
}

// Kanban
export interface KanbanBoardDto {
  projectId: string; projectKey: string; projectName: string;
  columns: KanbanColumnDto[];
}
export interface KanbanColumnDto {
  statusId: number; statusName: string; statusCategory: string;
  statusColor?: string; displayOrder: number; wipLimit?: number;
  tickets: KanbanTicketDto[];
}
export interface KanbanTicketDto {
  id: string; ticketNumber: number; ticketKey: string; title: string;
  issueTypeName: string; issueTypeIconUrl?: string;
  priorityName?: string; priorityColor?: string;
  assigneeUserId?: string; assigneeName?: string; assigneeAvatarUrl?: string;
  dueDate?: string; storyPoints?: number; slaBreachAt?: string; isSlaBreached: boolean;
  labelNames: string[]; commentCount: number; attachmentCount: number; rowVersion: string;
}

// Dashboard
export interface DashboardDto {
  myOpenTickets: DashboardTicketDto[];
  assignedToMe: DashboardTicketDto[];
  reportedByMe: DashboardTicketDto[];
  overdueTickets: DashboardTicketDto[];
  recentlyUpdated: DashboardTicketDto[];
  ticketsByPriority: PriorityBreakdownDto[];
  ticketsByStatus: StatusBreakdownDto[];
  ticketsByProject: ProjectBreakdownDto[];
}
export interface DashboardTicketDto {
  ticketId: string; ticketKey: string; title: string; projectName: string;
  statusName: string; priorityName?: string; priorityColor?: string;
  assigneeUserId?: string; assigneeName?: string;
  dueDate?: string; slaBreachAt?: string; isSlaBreached: boolean; updatedAt: string;
}
export interface PriorityBreakdownDto { priorityId?: number; priorityName: string; count: number; }
export interface StatusBreakdownDto { statusId: number; statusName: string; category: string; count: number; }
export interface ProjectBreakdownDto { projectId: string; projectName: string; count: number; }

// Activity
export interface TicketActivityResult {
  items: ActivityItemDto[]; totalCount: number; pageNumber: number; totalPages: number;
}
export interface ActivityItemDto {
  id: number; activityType: string; entityType: string; fieldName?: string;
  oldValue?: string; newValue?: string;
  actorUserId: string; actorName: string; actorAvatarUrl?: string; occurredAt: string;
}

// Workflows
export interface WorkflowStatus {
  id: number; workflowId: string; name: string; category: string;
  color?: string; isInitial: boolean; isFinal: boolean; displayOrder: number;
}
export interface TransitionRequest { toStatusId: number; comment?: string; resolutionId?: number; }

// Reference
export interface IssueType { id: number; name: string; iconUrl?: string; projectId: string; isSubTask: boolean; isEpic: boolean; }
export interface Priority { id: number; name: string; color?: string; slaTargetHours?: number; displayOrder: number; }
export interface Label { id: number; name: string; color?: string; projectId?: string; }
export interface Role { id: number; name: string; description?: string; scope: string; }
export interface Department { id: number; name: string; description?: string; }
export interface User { id: string; displayName: string; email: string; avatarUrl?: string; isActive: boolean; departmentId?: number; }

// Notifications
export interface NotificationDto {
  id: string; type: string; title: string; message?: string;
  isRead: boolean; createdAt: string; ticketId?: string; projectId?: string;
}
