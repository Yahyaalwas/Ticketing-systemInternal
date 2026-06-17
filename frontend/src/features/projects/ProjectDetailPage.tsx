import {
  Box, Typography, Chip, Button, Grid, Card, CardContent, CardHeader,
  Avatar, Table, TableBody, TableCell, TableHead, TableRow, Skeleton,
  TextField, InputAdornment, Tooltip, IconButton, Menu, MenuItem,
  Dialog, DialogTitle, DialogContent, DialogActions, Select, FormControl,
  InputLabel, LinearProgress, Divider, Alert,
} from '@mui/material';
import {
  Archive as ArchiveIcon, Delete as DeleteIcon, Edit as EditIcon,
  PersonAdd as AddMemberIcon, Search as SearchIcon, MoreVert as MoreIcon,
  Refresh as RefreshIcon, AutoAwesome as AiIcon, People as PeopleIcon,
  BugReport as BugIcon, CheckCircle as DoneIcon, Warning as OverdueIcon,
  Assignment as TotalIcon, Speed as SlaIcon, Schedule as AvgIcon,
  OpenInNew as OpenIcon,
} from '@mui/icons-material';
import { useState } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  PieChart, Pie, Cell, BarChart, Bar, XAxis, YAxis, Tooltip as RechartTooltip,
  Legend, ResponsiveContainer,
} from 'recharts';
import { projectsApi } from '@/api/projects';
import { ticketsApi } from '@/api/tickets';
import { referenceApi } from '@/api/reference';
import { getExecutiveReport, getSprintIntelligence } from '@/api/ai';
import { useAuthStore } from '@/stores/authStore';
import { useNotification } from '@/hooks/useNotification';
import { UserAvatar } from '@/components/common/UserAvatar';
import { StatusChip } from '@/components/common/StatusChip';
import { PriorityChip } from '@/components/common/PriorityChip';
import { PaginationBar } from '@/components/common/PaginationBar';
import { ConfirmDialog } from '@/components/common/ConfirmDialog';
import { EmptyState } from '@/components/common/EmptyState';
import { formatDate, formatDistanceToNow } from '@/utils/date';

const KPI_COLORS = ['#0052CC', '#FF8B00', '#36B37E', '#FF5630', '#6554C0', '#00B8D9', '#97A0AF'];

function KpiCard({ label, value, icon, color, loading }: {
  label: string; value: string | number; icon: React.ReactNode; color: string; loading?: boolean;
}) {
  return (
    <Card sx={{ height: '100%' }}>
      <CardContent sx={{ display: 'flex', alignItems: 'center', gap: 2, py: '16px !important' }}>
        <Box sx={{ p: 1.25, borderRadius: 2, bgcolor: color + '22', color, display: 'flex', flexShrink: 0 }}>
          {icon}
        </Box>
        <Box>
          {loading ? <Skeleton width={48} height={36} /> : (
            <Typography variant="h4" fontWeight={800} lineHeight={1}>{value}</Typography>
          )}
          <Typography variant="caption" color="text.secondary" fontWeight={500}>{label}</Typography>
        </Box>
      </CardContent>
    </Card>
  );
}

export default function ProjectDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const qc = useQueryClient();
  const { user } = useAuthStore();
  const { success, error } = useNotification();

  const [ticketSearch, setTicketSearch] = useState('');
  const [ticketPage, setTicketPage] = useState(1);
  const [ticketPageSize, setTicketPageSize] = useState(10);
  const [menuAnchor, setMenuAnchor] = useState<null | HTMLElement>(null);
  const [confirmArchive, setConfirmArchive] = useState(false);
  const [confirmDelete, setConfirmDelete] = useState(false);
  const [addMemberOpen, setAddMemberOpen] = useState(false);
  const [memberSearch, setMemberSearch] = useState('');
  const [selectedUserId, setSelectedUserId] = useState('');
  const [selectedRoleId, setSelectedRoleId] = useState<number>(0);
  const [memberMenuAnchor, setMemberMenuAnchor] = useState<{ el: HTMLElement; userId: string } | null>(null);

  // Queries
  const { data: projectResult, isLoading: loadingProject } = useQuery({
    queryKey: ['project', id],
    queryFn: () => projectsApi.get(id!),
    enabled: !!id,
    staleTime: 60_000,
  });
  const project = projectResult?.data;
  const etag = projectResult?.etag ?? '';

  const { data: ticketsResult, isLoading: loadingTickets } = useQuery({
    queryKey: ['project-tickets', id, ticketPage, ticketPageSize, ticketSearch],
    queryFn: () => ticketsApi.list({ projectId: id!, pageNumber: ticketPage, pageSize: ticketPageSize, search: ticketSearch || undefined }),
    enabled: !!id,
    staleTime: 30_000,
  });

  const { data: report, isLoading: loadingReport, refetch: refetchReport } = useQuery({
    queryKey: ['executive-report', id, 'Weekly'],
    queryFn: () => getExecutiveReport('Weekly', id, false),
    enabled: !!id,
    staleTime: 4 * 60 * 60 * 1000,
  });

  const { data: sprintData } = useQuery({
    queryKey: ['sprint-intelligence', id],
    queryFn: () => getSprintIntelligence(id!),
    enabled: !!id,
    staleTime: 30 * 60_000,
  });

  const { data: userResults = [] } = useQuery({
    queryKey: ['user-search', memberSearch],
    queryFn: () => referenceApi.searchUsers(memberSearch, 10),
    enabled: memberSearch.length >= 2,
    staleTime: 30_000,
  });

  const { data: roles = [] } = useQuery({
    queryKey: ['roles', 'Project'],
    queryFn: () => referenceApi.getRoles('Project'),
    staleTime: Infinity,
  });

  // Mutations
  const archiveMutation = useMutation({
    mutationFn: () => projectsApi.archive(id!),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['project', id] }); success('Project archived'); setConfirmArchive(false); },
    onError: (e: Error) => error(e.message),
  });

  const deleteMutation = useMutation({
    mutationFn: () => projectsApi.delete(id!),
    onSuccess: () => { success('Project deleted'); navigate('/projects'); },
    onError: (e: Error) => error(e.message),
  });

  const addMemberMutation = useMutation({
    mutationFn: () => projectsApi.addMember(id!, selectedUserId, selectedRoleId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['project', id] });
      success('Member added');
      setAddMemberOpen(false);
      setSelectedUserId('');
      setMemberSearch('');
    },
    onError: (e: Error) => error(e.message),
  });

  const removeMemberMutation = useMutation({
    mutationFn: (userId: string) => projectsApi.removeMember(id!, userId),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['project', id] }); success('Member removed'); setMemberMenuAnchor(null); },
    onError: (e: Error) => error(e.message),
  });

  const assignLeadMutation = useMutation({
    mutationFn: (userId: string) => projectsApi.assignLead(id!, userId),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['project', id] }); success('Project lead updated'); setMemberMenuAnchor(null); },
    onError: (e: Error) => error(e.message),
  });

  // Permissions
  const isAdmin = user?.roles.includes('System Administrator') ?? false;
  const isLead = user?.userId === project?.leadUserId;
  const canManage = isAdmin || isLead;

  // Chart data
  const statusChartData = report?.metrics ? [
    { name: 'Open', value: report.metrics.openTickets, fill: '#FF8B00' },
    { name: 'Resolved', value: report.metrics.resolvedTickets, fill: '#36B37E' },
    { name: 'Overdue', value: report.metrics.overdueTickets, fill: '#FF5630' },
  ].filter(d => d.value > 0) : [];

  if (loadingProject) {
    return (
      <Box>
        <Skeleton height={40} width="40%" sx={{ mb: 1 }} />
        <Skeleton height={24} width="20%" sx={{ mb: 3 }} />
        <Grid container spacing={2} mb={3}>
          {[1, 2, 3, 4, 5, 6, 7].map(i => (
            <Grid key={i} size={{ xs: 12, sm: 6, md: 3, lg: 'auto' }} sx={{ flex: 1 }}>
              <Skeleton variant="rectangular" height={88} sx={{ borderRadius: 1 }} />
            </Grid>
          ))}
        </Grid>
      </Box>
    );
  }

  if (!project) {
    return <EmptyState title="Project not found" message="This project may have been deleted or you don't have access." actionLabel="Back to Projects" onAction={() => navigate('/projects')} />;
  }

  return (
    <Box>
      {/* Header */}
      <Box sx={{ display: 'flex', alignItems: 'flex-start', justifyContent: 'space-between', mb: 3, gap: 2, flexWrap: 'wrap' }}>
        <Box>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 0.5, flexWrap: 'wrap' }}>
            <Typography variant="h4" fontWeight={800}>{project.name}</Typography>
            <Chip label={project.projectKey} size="small" variant="outlined" sx={{ fontFamily: 'monospace', fontWeight: 700 }} />
            {project.isArchived && <Chip label="Archived" size="small" color="warning" />}
            {!project.isArchived && <Chip label="Active" size="small" color="success" />}
          </Box>
          <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap' }}>
            {project.departmentName && (
              <Typography variant="body2" color="text.secondary">🏢 {project.departmentName}</Typography>
            )}
            <Typography variant="body2" color="text.secondary">
              Lead: <strong>{project.leadDisplayName}</strong>
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Created {formatDistanceToNow(project.createdAt)}
            </Typography>
          </Box>
        </Box>
        <Box sx={{ display: 'flex', gap: 1, alignItems: 'center' }}>
          {canManage && (
            <Button variant="outlined" startIcon={<EditIcon />} size="small"
              onClick={() => navigate(`/projects/${id}/edit`)}>
              Edit
            </Button>
          )}
          <IconButton size="small" onClick={(e) => setMenuAnchor(e.currentTarget)}>
            <MoreIcon />
          </IconButton>
          <Menu anchorEl={menuAnchor} open={Boolean(menuAnchor)} onClose={() => setMenuAnchor(null)}>
            {canManage && !project.isArchived && (
              <MenuItem onClick={() => { setMenuAnchor(null); setConfirmArchive(true); }}>
                <ArchiveIcon fontSize="small" sx={{ mr: 1 }} /> Archive Project
              </MenuItem>
            )}
            {isAdmin && (
              <MenuItem onClick={() => { setMenuAnchor(null); setConfirmDelete(true); }} sx={{ color: 'error.main' }}>
                <DeleteIcon fontSize="small" sx={{ mr: 1 }} /> Delete Project
              </MenuItem>
            )}
          </Menu>
        </Box>
      </Box>

      {/* KPI Strip */}
      <Grid container spacing={2} mb={3}>
        {[
          { label: 'Total Tickets', value: report?.metrics.totalTickets ?? project.activeTicketCount, icon: <TotalIcon />, color: KPI_COLORS[0] },
          { label: 'Open', value: report?.metrics.openTickets ?? '—', icon: <BugIcon />, color: KPI_COLORS[1] },
          { label: 'Resolved', value: report?.metrics.resolvedTickets ?? '—', icon: <DoneIcon />, color: KPI_COLORS[2] },
          { label: 'Overdue', value: report?.metrics.overdueTickets ?? '—', icon: <OverdueIcon />, color: KPI_COLORS[3] },
          { label: 'SLA Breaches', value: report?.metrics.slaBreachCount ?? '—', icon: <SlaIcon />, color: KPI_COLORS[4] },
          { label: 'Avg Resolution', value: report?.metrics.averageResolutionHours ? `${report.metrics.averageResolutionHours}h` : '—', icon: <AvgIcon />, color: KPI_COLORS[5] },
          { label: 'Members', value: project.memberCount, icon: <PeopleIcon />, color: KPI_COLORS[6] },
        ].map(kpi => (
          <Grid key={kpi.label} size={{ xs: 6, sm: 4, md: 3, lg: 'auto' }} sx={{ flex: { lg: 1 } }}>
            <KpiCard {...kpi} loading={loadingReport} />
          </Grid>
        ))}
      </Grid>

      <Grid container spacing={3}>
        {/* Left main content */}
        <Grid size={{ xs: 12, lg: 8 }}>
          {/* AI Executive Summary */}
          <Card sx={{ mb: 3 }}>
            <CardHeader
              avatar={<AiIcon color="primary" />}
              title={<Typography fontWeight={700}>AI Executive Summary</Typography>}
              subheader={report ? `${report.periodLabel} · Generated ${formatDistanceToNow(report.generatedAt)}${report.wasFromCache ? ' (cached)' : ''}` : undefined}
              action={
                <Tooltip title="Refresh report">
                  <IconButton size="small" onClick={() => refetchReport()} disabled={loadingReport}>
                    {loadingReport ? <Skeleton variant="circular" width={24} height={24} /> : <RefreshIcon />}
                  </IconButton>
                </Tooltip>
              }
            />
            <CardContent>
              {loadingReport ? (
                <Box>
                  {[1, 2, 3].map(i => <Skeleton key={i} height={18} width={`${90 - i * 10}%`} sx={{ mb: 0.5 }} />)}
                </Box>
              ) : report ? (
                <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                  <Box>
                    <Typography variant="caption" fontWeight={600} color="text.secondary" display="block" mb={0.5}>SUMMARY</Typography>
                    <Typography variant="body2" sx={{ lineHeight: 1.7 }}>{report.narrativeSummary}</Typography>
                  </Box>
                  <Box>
                    <Typography variant="caption" fontWeight={600} color="text.secondary" display="block" mb={0.5}>SLA HEALTH</Typography>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
                      <LinearProgress
                        variant="determinate"
                        value={report.metrics.slaCompliancePercent}
                        color={report.metrics.slaCompliancePercent >= 90 ? 'success' : report.metrics.slaCompliancePercent >= 70 ? 'warning' : 'error'}
                        sx={{ flex: 1, height: 8, borderRadius: 4 }}
                      />
                      <Chip
                        label={`${report.metrics.slaCompliancePercent}%`}
                        size="small"
                        color={report.metrics.slaCompliancePercent >= 90 ? 'success' : report.metrics.slaCompliancePercent >= 70 ? 'warning' : 'error'}
                      />
                    </Box>
                    <Typography variant="body2" color="text.secondary" mt={0.75}>{report.slaHealthReport}</Typography>
                  </Box>
                  {report.deliveryRisks.length > 0 && (
                    <Box>
                      <Typography variant="caption" fontWeight={600} color="error" display="block" mb={0.5}>DELIVERY RISKS</Typography>
                      {report.deliveryRisks.map((r, i) => (
                        <Typography key={i} variant="body2" sx={{ mb: 0.25 }}>⚠ {r}</Typography>
                      ))}
                    </Box>
                  )}
                </Box>
              ) : (
                <Alert severity="info">Click refresh to generate AI analysis.</Alert>
              )}
            </CardContent>
          </Card>

          {/* Sprint Intelligence */}
          {sprintData && (sprintData.atRiskTickets.length > 0 || sprintData.agingTickets.length > 0) && (
            <Card sx={{ mb: 3 }}>
              <CardHeader title={<Typography fontWeight={700}>⚡ Sprint Intelligence</Typography>} />
              <CardContent>
                {sprintData.slaBreachWarnings.length > 0 && (
                  <Alert severity="warning" sx={{ mb: 2 }}>
                    {sprintData.slaBreachWarnings.join(' · ')}
                  </Alert>
                )}
                {sprintData.atRiskTickets.slice(0, 5).map(t => (
                  <Box key={t.ticketKey} sx={{ display: 'flex', alignItems: 'flex-start', gap: 1, mb: 1 }}>
                    <Chip label={t.severity} size="small" color={t.severity === 'Critical' ? 'error' : t.severity === 'High' ? 'warning' : 'default'} />
                    <Box>
                      <Typography variant="body2" fontWeight={600}>{t.ticketKey}: {t.title}</Typography>
                      <Typography variant="caption" color="text.secondary">{t.riskReason}</Typography>
                    </Box>
                  </Box>
                ))}
                {sprintData.recommendedActions.length > 0 && (
                  <Box sx={{ mt: 2 }}>
                    <Typography variant="caption" fontWeight={600} color="text.secondary" display="block" mb={0.5}>RECOMMENDATIONS</Typography>
                    {sprintData.recommendedActions.map((a, i) => (
                      <Typography key={i} variant="body2" sx={{ mb: 0.25 }}>→ {a}</Typography>
                    ))}
                  </Box>
                )}
              </CardContent>
            </Card>
          )}

          {/* Recent Tickets */}
          <Card sx={{ mb: 3 }}>
            <CardHeader
              title={<Typography fontWeight={700}>Tickets</Typography>}
              action={
                <Box sx={{ display: 'flex', gap: 1 }}>
                  <TextField
                    size="small"
                    placeholder="Search tickets..."
                    value={ticketSearch}
                    onChange={(e) => { setTicketSearch(e.target.value); setTicketPage(1); }}
                    InputProps={{ startAdornment: <InputAdornment position="start"><SearchIcon fontSize="small" /></InputAdornment> }}
                    sx={{ width: 220 }}
                  />
                </Box>
              }
            />
            <Box sx={{ overflow: 'auto' }}>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell sx={{ fontWeight: 700, width: 100 }}>Key</TableCell>
                    <TableCell sx={{ fontWeight: 700 }}>Title</TableCell>
                    <TableCell sx={{ fontWeight: 700, width: 130 }}>Status</TableCell>
                    <TableCell sx={{ fontWeight: 700, width: 100 }}>Priority</TableCell>
                    <TableCell sx={{ fontWeight: 700, width: 140 }}>Assignee</TableCell>
                    <TableCell sx={{ fontWeight: 700, width: 110 }}>Updated</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {loadingTickets
                    ? Array.from({ length: 5 }).map((_, i) => (
                        <TableRow key={i}>
                          {[1, 2, 3, 4, 5, 6].map(j => <TableCell key={j}><Skeleton /></TableCell>)}
                        </TableRow>
                      ))
                    : (ticketsResult?.items ?? []).map(t => (
                        <TableRow key={t.id} hover sx={{ cursor: 'pointer' }} onClick={() => navigate(`/tickets/${t.id}`)}>
                          <TableCell>
                            <Typography variant="caption" fontFamily="monospace" color="primary.main" fontWeight={700}>{t.ticketKey}</Typography>
                          </TableCell>
                          <TableCell>
                            <Tooltip title={t.title}>
                              <Typography variant="body2" noWrap sx={{ maxWidth: 300 }}>{t.title}</Typography>
                            </Tooltip>
                          </TableCell>
                          <TableCell><StatusChip label={t.statusName} category={t.statusCategory} color={t.statusColor} /></TableCell>
                          <TableCell><PriorityChip priority={t.priorityName} /></TableCell>
                          <TableCell>
                            {t.assigneeName ? (
                              <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.75 }}>
                                <UserAvatar name={t.assigneeName} size={20} />
                                <Typography variant="caption" noWrap>{t.assigneeName}</Typography>
                              </Box>
                            ) : <Typography variant="caption" color="text.disabled">Unassigned</Typography>}
                          </TableCell>
                          <TableCell>
                            <Typography variant="caption" color="text.secondary">{formatDistanceToNow(t.updatedAt)}</Typography>
                          </TableCell>
                        </TableRow>
                      ))
                  }
                  {!loadingTickets && (ticketsResult?.items ?? []).length === 0 && (
                    <TableRow>
                      <TableCell colSpan={6} sx={{ textAlign: 'center', py: 4 }}>
                        <Typography color="text.disabled">No tickets found</Typography>
                      </TableCell>
                    </TableRow>
                  )}
                </TableBody>
              </Table>
            </Box>
            {ticketsResult && (
              <Box sx={{ px: 2 }}>
                <PaginationBar
                  page={ticketPage}
                  totalPages={ticketsResult.totalPages}
                  totalCount={ticketsResult.totalCount}
                  pageSize={ticketPageSize}
                  onPageChange={setTicketPage}
                  onPageSizeChange={(s) => { setTicketPageSize(s); setTicketPage(1); }}
                />
              </Box>
            )}
          </Card>
        </Grid>

        {/* Right sidebar */}
        <Grid size={{ xs: 12, lg: 4 }}>
          {/* Charts */}
          {statusChartData.length > 0 && (
            <Card sx={{ mb: 3 }}>
              <CardHeader title={<Typography fontWeight={700}>Ticket Distribution</Typography>} />
              <CardContent>
                <ResponsiveContainer width="100%" height={180}>
                  <PieChart>
                    <Pie data={statusChartData} dataKey="value" nameKey="name" cx="50%" cy="50%" outerRadius={70} label={({ name, percent }) => `${name} ${(percent * 100).toFixed(0)}%`} labelLine={false}>
                      {statusChartData.map((entry, i) => <Cell key={i} fill={entry.fill} />)}
                    </Pie>
                    <RechartTooltip />
                  </PieChart>
                </ResponsiveContainer>
              </CardContent>
            </Card>
          )}

          {/* Project Info */}
          <Card sx={{ mb: 3 }}>
            <CardHeader title={<Typography fontWeight={700}>Project Info</Typography>} />
            <CardContent>
              {project.description && (
                <Box mb={2}>
                  <Typography variant="caption" color="text.secondary" fontWeight={600} display="block" mb={0.5}>DESCRIPTION</Typography>
                  <Typography variant="body2">{project.description}</Typography>
                </Box>
              )}
              {[
                { label: 'DEPARTMENT', value: project.departmentName },
                { label: 'LEAD', value: project.leadDisplayName },
                { label: 'WORKFLOW', value: project.activeWorkflowName },
                { label: 'CREATED', value: formatDate(project.createdAt) },
                { label: 'UPDATED', value: formatDate((project as any).updatedAt) },
              ].map(({ label, value }) => value && (
                <Box key={label} sx={{ display: 'flex', justifyContent: 'space-between', py: 0.75, borderBottom: '1px solid', borderColor: 'divider' }}>
                  <Typography variant="caption" color="text.secondary" fontWeight={600}>{label}</Typography>
                  <Typography variant="caption" fontWeight={500}>{value}</Typography>
                </Box>
              ))}
            </CardContent>
          </Card>

          {/* Team Members */}
          <Card>
            <CardHeader
              title={<Typography fontWeight={700}>Team ({project.memberCount})</Typography>}
              action={canManage && (
                <Tooltip title="Add member">
                  <IconButton size="small" onClick={() => setAddMemberOpen(true)}>
                    <AddMemberIcon fontSize="small" />
                  </IconButton>
                </Tooltip>
              )}
            />
            <CardContent sx={{ pt: 0 }}>
              {(project.members ?? []).map(m => (
                <Box key={m.userId} sx={{ display: 'flex', alignItems: 'center', gap: 1, py: 0.75, borderBottom: '1px solid', borderColor: 'divider' }}>
                  <UserAvatar name={m.displayName} avatarUrl={m.avatarUrl} size={32} />
                  <Box sx={{ flex: 1, minWidth: 0 }}>
                    <Typography variant="body2" fontWeight={600} noWrap>{m.displayName}</Typography>
                    <Typography variant="caption" color="text.secondary">{m.roleName}</Typography>
                  </Box>
                  {canManage && (
                    <IconButton size="small" onClick={(e) => setMemberMenuAnchor({ el: e.currentTarget, userId: m.userId })}>
                      <MoreIcon fontSize="small" />
                    </IconButton>
                  )}
                </Box>
              ))}
              {(project.members ?? []).length === 0 && (
                <Typography variant="body2" color="text.disabled" sx={{ textAlign: 'center', py: 2 }}>No members yet</Typography>
              )}
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* Member context menu */}
      <Menu
        anchorEl={memberMenuAnchor?.el}
        open={Boolean(memberMenuAnchor)}
        onClose={() => setMemberMenuAnchor(null)}
      >
        <MenuItem onClick={() => memberMenuAnchor && assignLeadMutation.mutate(memberMenuAnchor.userId)}>
          Assign as Project Lead
        </MenuItem>
        <MenuItem
          onClick={() => memberMenuAnchor && removeMemberMutation.mutate(memberMenuAnchor.userId)}
          sx={{ color: 'error.main' }}
        >
          Remove Member
        </MenuItem>
      </Menu>

      {/* Add Member Dialog */}
      <Dialog open={addMemberOpen} onClose={() => setAddMemberOpen(false)} maxWidth="xs" fullWidth>
        <DialogTitle>Add Team Member</DialogTitle>
        <DialogContent>
          <TextField
            fullWidth size="small" label="Search users" value={memberSearch}
            onChange={(e) => setMemberSearch(e.target.value)}
            sx={{ mb: 2, mt: 1 }}
            placeholder="Type name or email..."
          />
          {userResults.length > 0 && (
            <Box sx={{ mb: 2, maxHeight: 200, overflow: 'auto', border: '1px solid', borderColor: 'divider', borderRadius: 1 }}>
              {userResults.map(u => (
                <Box
                  key={u.id}
                  sx={{
                    display: 'flex', alignItems: 'center', gap: 1, p: 1, cursor: 'pointer',
                    bgcolor: selectedUserId === u.id ? 'action.selected' : 'transparent',
                    '&:hover': { bgcolor: 'action.hover' },
                  }}
                  onClick={() => setSelectedUserId(u.id)}
                >
                  <UserAvatar name={u.displayName} size={28} />
                  <Box>
                    <Typography variant="body2">{u.displayName}</Typography>
                    <Typography variant="caption" color="text.secondary">{u.email}</Typography>
                  </Box>
                </Box>
              ))}
            </Box>
          )}
          <FormControl fullWidth size="small">
            <InputLabel>Role</InputLabel>
            <Select value={selectedRoleId || ''} label="Role" onChange={(e) => setSelectedRoleId(Number(e.target.value))}>
              {roles.map(r => <MenuItem key={r.id} value={r.id}>{r.name}</MenuItem>)}
            </Select>
          </FormControl>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setAddMemberOpen(false)}>Cancel</Button>
          <Button
            variant="contained"
            disabled={!selectedUserId || !selectedRoleId || addMemberMutation.isPending}
            onClick={() => addMemberMutation.mutate()}
          >
            Add Member
          </Button>
        </DialogActions>
      </Dialog>

      {/* Confirm Archive */}
      <ConfirmDialog
        open={confirmArchive}
        title="Archive Project"
        message={`Archive "${project.name}"? It will be hidden from active projects but all data is preserved.`}
        confirmLabel="Archive"
        loading={archiveMutation.isPending}
        onConfirm={() => archiveMutation.mutate()}
        onCancel={() => setConfirmArchive(false)}
      />

      {/* Confirm Delete */}
      <ConfirmDialog
        open={confirmDelete}
        title="Delete Project"
        message={`Permanently delete "${project.name}"? This action cannot be undone.`}
        confirmLabel="Delete"
        destructive
        loading={deleteMutation.isPending}
        onConfirm={() => deleteMutation.mutate()}
        onCancel={() => setConfirmDelete(false)}
      />
    </Box>
  );
}
