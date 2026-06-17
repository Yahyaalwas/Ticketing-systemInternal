import { apiClient } from '@/lib/axios';

// ── Types ───────────────────────────────────────────────────────────────────

export interface TicketAiSummaryDto {
  ticketId: string;
  executiveSummary: string;
  technicalSummary: string;
  simpleExplanation: string;
  keyDecisions: string[];
  blockers: string[];
  risks: string[];
  actionItems: string[];
  completionConfidencePercent: number;
  wasFromCache: boolean;
  generatedAt: string;
}

export interface GeneratedCommentDto {
  draftBody: string;
  tone: string;
  providerName: string;
  generatedAt: string;
}

export interface MeetingParseResultDto {
  meetingTitle: string;
  tasks: ExtractedTask[];
  decisions: ExtractedDecision[];
  risks: string[];
  dependencies: string[];
  narrativeSummary: string;
  parsedAt: string;
}

export interface ExtractedTask {
  title: string;
  description?: string;
  owner?: string;
  dueDate?: string;
  priority: string;
}

export interface ExtractedDecision {
  decision: string;
  context?: string;
  owner?: string;
}

export interface TicketDraftDto {
  suggestedTitle: string;
  suggestedDescription: string;
  suggestedPriority: string;
  suggestedIssueType: string;
  suggestedLabels: string[];
  suggestedAssigneeName?: string;
  suggestedDueDate?: string;
  providerName: string;
  generatedAt: string;
}

export interface NaturalLanguageSearchResult {
  interpretation: string;
  filters: ParsedSearchFilters;
  tickets: TicketSearchHit[];
}

export interface ParsedSearchFilters {
  assigneeName?: string;
  priorityName?: string;
  statusName?: string;
  issueTypeName?: string;
  departmentName?: string;
  labelName?: string;
  overdueOnly?: boolean;
  unassignedOnly?: boolean;
  freeTextSearch?: string;
}

export interface TicketSearchHit {
  id: string;
  ticketKey: string;
  title: string;
  statusName: string;
  priorityName?: string;
  assigneeName?: string;
  dueDate?: string;
  isOverdue: boolean;
}

export interface DuplicateDetectionResult {
  hasPotentialDuplicates: boolean;
  candidates: DuplicateCandidate[];
}

export interface DuplicateCandidate {
  ticketId: string;
  ticketKey: string;
  title: string;
  statusName: string;
  similarityPercent: number;
  similarityReason: string;
}

export interface SimilarTicketsDto {
  ticketId: string;
  related: SimilarTicketItem[];
  similarIncidents: SimilarTicketItem[];
  previousResolutions: string[];
}

export interface SimilarTicketItem {
  id: string;
  ticketKey: string;
  title: string;
  statusName: string;
  similarityPercent: number;
  resolution?: string;
}

export interface ExecutiveReportDto {
  periodLabel: string;
  narrativeSummary: string;
  teamPerformanceOverview: string;
  slaHealthReport: string;
  deliveryRisks: string[];
  resourceBottlenecks: string[];
  topRecurringCategories: string[];
  metrics: ReportMetrics;
  wasFromCache: boolean;
  generatedAt: string;
}

export interface ReportMetrics {
  totalTickets: number;
  openTickets: number;
  resolvedTickets: number;
  overdueTickets: number;
  averageResolutionHours: number;
  slaBreachCount: number;
  slaCompliancePercent: number;
}

export interface SprintIntelligenceDto {
  atRiskTickets: RiskTicket[];
  agingTickets: RiskTicket[];
  workloadImbalance: WorkloadItem[];
  slaBreachWarnings: string[];
  recommendedActions: string[];
  generatedAt: string;
}

export interface RiskTicket {
  id: string;
  ticketKey: string;
  title: string;
  assigneeName?: string;
  riskReason: string;
  severity: string;
}

export interface WorkloadItem {
  assigneeName: string;
  ticketCount: number;
  assessment: string;
}

export interface KnowledgeAnswerDto {
  answer: string;
  citations: CitedTicket[];
  providerName: string;
  answeredAt: string;
}

export interface CitedTicket {
  id: string;
  ticketKey: string;
  title: string;
  statusName: string;
  relevance: string;
}

export interface RiskAnalysisDto {
  risks: TicketRisk[];
  totalRiskScore: number;
  riskLevel: string;
  analyzedAt: string;
}

export interface TicketRisk {
  ticketId: string;
  ticketKey: string;
  title: string;
  riskType: string;
  description: string;
  severity: string;
}

// ── API calls ────────────────────────────────────────────────────────────────

export const summarizeTicket = (ticketId: string, forceRefresh = false) =>
  apiClient.post<TicketAiSummaryDto>(`/api/ai/tickets/${ticketId}/summarize?forceRefresh=${forceRefresh}`).then(r => r.data);

export const generateComment = (ticketId: string, instruction: string, tone = 'Professional') =>
  apiClient.post<GeneratedCommentDto>(`/api/ai/tickets/${ticketId}/generate-comment`, { instruction, tone }).then(r => r.data);

export const parseMeetingNotes = (rawNotes: string, meetingTitle?: string, meetingDate?: string) =>
  apiClient.post<MeetingParseResultDto>('/api/ai/parse-meeting-notes', { rawNotes, meetingTitle, meetingDate }).then(r => r.data);

export const draftTicket = (rawText: string, projectId?: string) =>
  apiClient.post<TicketDraftDto>('/api/ai/draft-ticket', { rawText, projectId }).then(r => r.data);

export const naturalLanguageSearch = (query: string, projectId?: string) =>
  apiClient.post<NaturalLanguageSearchResult>('/api/ai/search', { query, projectId }).then(r => r.data);

export const findDuplicates = (title: string, description: string | undefined, projectId: string) =>
  apiClient.post<DuplicateDetectionResult>('/api/ai/find-duplicates', { title, description, projectId }).then(r => r.data);

export const getSimilarTickets = (ticketId: string) =>
  apiClient.get<SimilarTicketsDto>(`/api/ai/tickets/${ticketId}/similar`).then(r => r.data);

export const getExecutiveReport = (period: 'Weekly' | 'Monthly', projectId?: string, forceRefresh = false) =>
  apiClient.get<ExecutiveReportDto>('/api/ai/executive-report', { params: { period, projectId, forceRefresh } }).then(r => r.data);

export const getSprintIntelligence = (projectId: string) =>
  apiClient.get<SprintIntelligenceDto>(`/api/ai/sprint-intelligence/${projectId}`).then(r => r.data);

export const askKnowledgeQuestion = (question: string, projectId?: string) =>
  apiClient.post<KnowledgeAnswerDto>('/api/ai/knowledge-assistant', { question, projectId }).then(r => r.data);

export const getRiskAnalysis = (projectId?: string, ticketId?: string) =>
  apiClient.get<RiskAnalysisDto>('/api/ai/risk-analysis', { params: { projectId, ticketId } }).then(r => r.data);
