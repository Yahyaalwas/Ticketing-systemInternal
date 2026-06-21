import React, { useState, useCallback, useEffect } from 'react';
import {
  Box,
  Typography,
  Button,
  Chip,
  Avatar,
  AvatarGroup,
  Tooltip,
  TextField,
  Autocomplete,
  IconButton,
  Divider,
  Accordion,
  AccordionSummary,
  AccordionDetails,
  Skeleton,
  CircularProgress,
  Stack,
} from '@mui/material';
import {
  ExpandMore as ExpandMoreIcon,
  PersonAdd as PersonAddIcon,
  Visibility as VisibilityIcon,
  VisibilityOff as VisibilityOffIcon,
  Link as LinkIcon,
  Delete as DeleteIcon,
  Close as CloseIcon,
  Circle as CircleIcon,
} from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';

import { ticketsApi } from '@/api/tickets';
import { referenceApi } from '@/api/reference';
import { getSimilarTickets, getRiskAnalysis } from '@/api/ai';
import { useAuthStore } from '@/stores/authStore';
import { useNotification } from '@/hooks/useNotification';
import { TransitionDialog } from '@/components/shared/TransitionDialog';
import type { TicketDetail, User } from '@/types';

interface TicketSidebarProps {
  ticket: TicketDetail;
  etag: string;
  onTicketUpdate: () => void;
}

// ─── helpers ────────────────────────────────────────────────────────────────

function stringAvatar(name: string) {
  const parts = name.split(' ');
  return parts.length >= 2 ? `${parts[0][0]}${parts[1][0]}` : name.slice(0, 2);
}

const SEVERITY_COLORS: Record<string, string> = {
  low: 'success',
  medium: 'warning',
  high: 'error',
  critical: 'error',
};

function riskChipColor(severity: string): 'success' | 'warning' | 'error' | 'default' {
  return (SEVERITY_COLORS[severity] ?? 'default') as 'success' | 'warning' | 'error' | 'default';
}

function overallRiskBg(risk: string): string {
  if (risk === 'low') return '#2e7d32';
  if (risk === 'medium') return '#ed6c02';
  if (risk === 'high') return '#d32f2f';
  if (risk === 'critical') return '#7f0000';
  return '#757575';
}

// ─── Confirm Delete Dialog ───────────────────────────────────────────────────

interface ConfirmDeleteDialogProps {
  open: boolean;
  onClose: () => void;
  onConfirm: () => void;
  loading: boolean;
}

function ConfirmDeleteDialog({ open, onClose, onConfirm, loading }: ConfirmDeleteDialogProps) {
  if (!open) return null;
  return (
    <Box
      sx={{
        position: 'fixed',
        inset: 0,
        zIndex: 1300,
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        bgcolor: 'rgba(0,0,0,0.5)',
      }}
      onClick={onClose}
    >
      <Box
        sx={{
          bgcolor: 'background.paper',
          borderRadius: 2,
          p: 3,
          minWidth: 360,
          boxShadow: 24,
        }}
        onClick={(e) => e.stopPropagation()}
      >
        <Typography variant="h6" gutterBottom>
          Delete Ticket
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
          Are you sure you want to delete this ticket? This action cannot be undone.
        </Typography>
        <Stack direction="row" spacing={1} sx={{ justifyContent: 'flex-end' }}>
          <Button onClick={onClose} disabled={loading}>
            Cancel
          </Button>
          <Button
            variant="contained"
            color="error"
            onClick={onConfirm}
            disabled={loading}
            startIcon={loading ? <CircularProgress size={14} /> : <DeleteIcon />}
          >
            Delete
          </Button>
        </Stack>
      </Box>
    </Box>
  );
}

// ─── Main Component ──────────────────────────────────────────────────────────

export function TicketSidebar({ ticket, etag, onTicketUpdate }: TicketSidebarProps) {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const notify = useNotification();
  const { user } = useAuthStore();

  // local UI state
  const [transitionOpen, setTransitionOpen] = useState(false);
  const [assigneeSearch, setAssigneeSearch] = useState('');
  const [assigneeOptions, setAssigneeOptions] = useState<User[]>([]);
  const [assigneeLoading, setAssigneeLoading] = useState(false);
  const [storyPointsEdit, setStoryPointsEdit] = useState(false);
  const [estimatedHoursEdit, setEstimatedHoursEdit] = useState(false);
  const [storyPointsVal, setStoryPointsVal] = useState(String(ticket.storyPoints ?? ''));
  const [estimatedHoursVal, setEstimatedHoursVal] = useState(String(ticket.estimatedHours ?? ''));
  const [similarExpanded, setSimilarExpanded] = useState(false);
  const [riskExpanded, setRiskExpanded] = useState(false);
  const [deleteOpen, setDeleteOpen] = useState(false);

  const isWatching = ticket.watchers.some((w) => w.userId === user?.userId);

  // ── Reference data ────────────────────────────────────────────────────────

  const { data: priorities = [] } = useQuery({
    queryKey: ['priorities'],
    queryFn: () => referenceApi.getPriorities(),
    staleTime: 5 * 60_000,
  });

  const { data: issueTypes = [] } = useQuery({
    queryKey: ['issueTypes', ticket.projectId],
    queryFn: () => referenceApi.getIssueTypes(ticket.projectId),
    staleTime: 5 * 60_000,
  });

  // ── AI data (lazy) ────────────────────────────────────────────────────────

  const { data: similarData, isFetching: similarLoading } = useQuery({
    queryKey: ['similarTickets', ticket.id],
    queryFn: () => getSimilarTickets(ticket.id),
    enabled: similarExpanded,
    staleTime: 5 * 60_000,
  });

  const { data: riskData, isFetching: riskLoading } = useQuery({
    queryKey: ['riskAnalysis', ticket.id],
    queryFn: () => getRiskAnalysis(undefined, ticket.id),
    enabled: riskExpanded,
    staleTime: 5 * 60_000,
  });

  // ── Debounced user search ─────────────────────────────────────────────────

  useEffect(() => {
    if (!assigneeSearch) {
      setAssigneeOptions([]);
      return;
    }
    const timer = setTimeout(async () => {
      setAssigneeLoading(true);
      try {
        const results = await referenceApi.searchUsers(assigneeSearch, 10);
        setAssigneeOptions(results);
      } catch {
        // ignore
      } finally {
        setAssigneeLoading(false);
      }
    }, 300);
    return () => clearTimeout(timer);
  }, [assigneeSearch]);

  // ── Mutations ─────────────────────────────────────────────────────────────

  const invalidate = useCallback(() => {
    void queryClient.invalidateQueries({ queryKey: ['ticket', ticket.id] });
    onTicketUpdate();
  }, [queryClient, ticket.id, onTicketUpdate]);

  const priorityMutation = useMutation({
    mutationFn: (priorityId: number | null) =>
      ticketsApi.changePriority(ticket.id, priorityId, etag),
    onSuccess: invalidate,
    onError: () => notify.error('Failed to update priority'),
  });

  const assigneeMutation = useMutation({
    mutationFn: (userId: string | null) => ticketsApi.assign(ticket.id, userId, etag),
    onSuccess: invalidate,
    onError: () => notify.error('Failed to update assignee'),
  });

  const dueDateMutation = useMutation({
    mutationFn: (date: string | null) => ticketsApi.changeDueDate(ticket.id, date, etag),
    onSuccess: invalidate,
    onError: () => notify.error('Failed to update due date'),
  });

  const removeLabelMutation = useMutation({
    mutationFn: (labelId: number) => ticketsApi.removeLabel(ticket.id, labelId),
    onSuccess: invalidate,
    onError: () => notify.error('Failed to remove label'),
  });

  const addWatcherMutation = useMutation({
    mutationFn: (userId: string) => ticketsApi.addWatcher(ticket.id, userId),
    onSuccess: invalidate,
    onError: () => notify.error('Failed to add watcher'),
  });

  const removeWatcherMutation = useMutation({
    mutationFn: (userId: string) => ticketsApi.removeWatcher(ticket.id, userId),
    onSuccess: invalidate,
    onError: () => notify.error('Failed to remove watcher'),
  });

  const deleteMutation = useMutation({
    mutationFn: () => ticketsApi.delete(ticket.id),
    onSuccess: () => {
      notify.success('Ticket deleted');
      navigate(-1);
    },
    onError: () => notify.error('Failed to delete ticket'),
  });

  const updateEstimateMutation = useMutation({
    mutationFn: (data: { storyPoints?: number; estimatedHours?: number }) => {
      const issueType = issueTypes.find((it) => it.name === ticket.issueTypeName);
      if (!issueType) throw new Error('Issue type not found');
      return ticketsApi.update(
        ticket.id,
        {
          title: ticket.title,
          issueTypeId: issueType.id,
          description: ticket.description,
          priorityId: priorities.find((p) => p.name === ticket.priorityName)?.id,
          assigneeUserId: ticket.assigneeUserId,
          dueDate: ticket.dueDate,
          storyPoints: data.storyPoints ?? ticket.storyPoints,
          estimatedHours: data.estimatedHours ?? ticket.estimatedHours,
        },
        etag,
      );
    },
    onSuccess: invalidate,
    onError: () => notify.error('Failed to update estimates'),
  });

  // ── Handlers ──────────────────────────────────────────────────────────────

  const handleWatchToggle = () => {
    if (!user) return;
    if (isWatching) {
      removeWatcherMutation.mutate(user.userId);
    } else {
      addWatcherMutation.mutate(user.userId);
    }
  };

  const handleStoryPointsSave = () => {
    const val = storyPointsVal === '' ? undefined : Number(storyPointsVal);
    updateEstimateMutation.mutate({ storyPoints: val });
    setStoryPointsEdit(false);
  };

  const handleEstimatedHoursSave = () => {
    const val = estimatedHoursVal === '' ? undefined : Number(estimatedHoursVal);
    updateEstimateMutation.mutate({ estimatedHours: val });
    setEstimatedHoursEdit(false);
  };

  const slaBreached =
    ticket.slaBreachAt != null && new Date(ticket.slaBreachAt) < new Date();

  const currentPriority = priorities.find((p) => p.name === ticket.priorityName) ?? null;

  // ── Assignee autocomplete value ───────────────────────────────────────────
  const assigneeValue: User | null = ticket.assigneeUserId
    ? {
        id: ticket.assigneeUserId,
        displayName: ticket.assigneeName ?? '',
        email: '',
        avatarUrl: ticket.assigneeAvatarUrl,
        isActive: true,
      }
    : null;

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 0 }}>

      {/* ── Quick Actions ─────────────────────────────────────────── */}
      <Box sx={{ pb: 1.5 }}>
        <Typography variant="overline" color="text.secondary" sx={{ display: 'block', mb: 0.5 }}>
          Quick Actions
        </Typography>
        <Stack direction="row" spacing={0.5}>
          <Tooltip title="Assign to Me">
            <IconButton
              size="small"
              onClick={() => user && assigneeMutation.mutate(user.userId)}
              disabled={assigneeMutation.isPending}
            >
              <PersonAddIcon fontSize="small" />
            </IconButton>
          </Tooltip>
          <Tooltip title={isWatching ? 'Unwatch' : 'Watch'}>
            <IconButton
              size="small"
              onClick={handleWatchToggle}
              disabled={addWatcherMutation.isPending || removeWatcherMutation.isPending}
              color={isWatching ? 'primary' : 'default'}
            >
              {isWatching ? (
                <VisibilityOffIcon fontSize="small" />
              ) : (
                <VisibilityIcon fontSize="small" />
              )}
            </IconButton>
          </Tooltip>
          <Tooltip title="Copy Link">
            <IconButton
              size="small"
              onClick={() => {
                void navigator.clipboard.writeText(window.location.href);
                notify.success('Link copied to clipboard');
              }}
            >
              <LinkIcon fontSize="small" />
            </IconButton>
          </Tooltip>
          <Tooltip title="Delete Ticket">
            <IconButton
              size="small"
              color="error"
              onClick={() => setDeleteOpen(true)}
            >
              <DeleteIcon fontSize="small" />
            </IconButton>
          </Tooltip>
        </Stack>
      </Box>

      <Divider />

      {/* ── Status ───────────────────────────────────────────────── */}
      <Box sx={{ py: 1.5 }}>
        <Typography variant="overline" color="text.secondary" sx={{ display: 'block', mb: 0.75 }}>
          Status
        </Typography>
        <Button
          variant="outlined"
          size="small"
          onClick={() => setTransitionOpen(true)}
          startIcon={
            <CircleIcon
              sx={{ fontSize: '10px !important', color: ticket.statusColor ?? 'grey.500' }}
            />
          }
          sx={{ textTransform: 'none', borderColor: ticket.statusColor ?? undefined }}
        >
          {ticket.statusName}
        </Button>
      </Box>

      <Divider />

      {/* ── Priority ─────────────────────────────────────────────── */}
      <Box sx={{ py: 1.5 }}>
        <Typography variant="overline" color="text.secondary" sx={{ display: 'block', mb: 0.75 }}>
          Priority
        </Typography>
        <TextField
          select
          size="small"
          fullWidth
          value={currentPriority?.id ?? ''}
          onChange={(e) => {
            const val = e.target.value;
            priorityMutation.mutate(val === '' ? null : Number(val));
          }}
          disabled={priorityMutation.isPending}
          slotProps={{ select: { native: true } }}
          sx={{ '& select': { display: 'flex', alignItems: 'center' } }}
        >
          <option value="">No Priority</option>
          {priorities.map((p) => (
            <option key={p.id} value={p.id}>
              {p.name}
            </option>
          ))}
        </TextField>
        {ticket.priorityColor && (
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5, mt: 0.5 }}>
            <CircleIcon sx={{ fontSize: 10, color: ticket.priorityColor }} />
            <Typography variant="caption" sx={{ color: ticket.priorityColor }}>
              {ticket.priorityName}
            </Typography>
          </Box>
        )}
      </Box>

      <Divider />

      {/* ── Assignee ─────────────────────────────────────────────── */}
      <Box sx={{ py: 1.5 }}>
        <Typography variant="overline" color="text.secondary" sx={{ display: 'block', mb: 0.75 }}>
          Assignee
        </Typography>
        <Autocomplete<User | null, false, false, false>
          size="small"
          options={[null, ...assigneeOptions]}
          value={assigneeValue}
          inputValue={assigneeSearch}
          onInputChange={(_, val) => setAssigneeSearch(val)}
          onChange={(_, newVal) => {
            assigneeMutation.mutate(newVal?.id ?? null);
          }}
          loading={assigneeLoading}
          getOptionLabel={(option) => option?.displayName ?? 'Unassigned'}
          isOptionEqualToValue={(option, value) => option?.id === value?.id}
          filterOptions={(x) => x}
          renderOption={(props, option) => (
            <Box component="li" {...props} sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
              {option ? (
                <Avatar
                  src={option.avatarUrl}
                  sx={{ width: 24, height: 24, fontSize: 11 }}
                >
                  {stringAvatar(option.displayName)}
                </Avatar>
              ) : (
                <Avatar sx={{ width: 24, height: 24, fontSize: 11, bgcolor: 'grey.300' }}>–</Avatar>
              )}
              <Typography variant="body2">{option?.displayName ?? 'Unassigned'}</Typography>
            </Box>
          )}
          renderInput={(params) => (
            <TextField
              {...params}
              placeholder="Search users…"
              slotProps={{
                input: {
                  // spread removed - MUI v9 Autocomplete params no longer has InputProps
                  startAdornment: assigneeValue ? (
                    <Avatar
                      src={assigneeValue.avatarUrl}
                      sx={{ width: 20, height: 20, fontSize: 10, mr: 0.5 }}
                    >
                      {stringAvatar(assigneeValue.displayName)}
                    </Avatar>
                  ) : undefined,
                  endAdornment: (
                    <>
                      {assigneeLoading ? <CircularProgress size={14} /> : null}
                      {(params as unknown as { InputProps: { endAdornment: React.ReactNode } }).InputProps?.endAdornment}
                    </>
                  ),
                },
              }}
            />
          )}
        />
      </Box>

      <Divider />

      {/* ── Reporter ─────────────────────────────────────────────── */}
      <Box sx={{ py: 1.5 }}>
        <Typography variant="overline" color="text.secondary" sx={{ display: 'block', mb: 0.75 }}>
          Reporter
        </Typography>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <Avatar sx={{ width: 28, height: 28, fontSize: 12, bgcolor: 'primary.main' }}>
            {stringAvatar(ticket.reporterName)}
          </Avatar>
          <Typography variant="body2">{ticket.reporterName}</Typography>
        </Box>
      </Box>

      <Divider />

      {/* ── Due Date ─────────────────────────────────────────────── */}
      <Box sx={{ py: 1.5 }}>
        <Typography variant="overline" color="text.secondary" sx={{ display: 'block', mb: 0.75 }}>
          Due Date
        </Typography>
        <TextField
          type="date"
          size="small"
          fullWidth
          value={ticket.dueDate ? ticket.dueDate.slice(0, 10) : ''}
          onChange={(e) => {
            dueDateMutation.mutate(e.target.value || null);
          }}
          disabled={dueDateMutation.isPending}
          slotProps={{ inputLabel: { shrink: true } }}
        />
        {slaBreached && (
          <Chip
            label="SLA Breached"
            color="error"
            size="small"
            sx={{ mt: 0.75 }}
          />
        )}
      </Box>

      <Divider />

      {/* ── Labels ───────────────────────────────────────────────── */}
      <Box sx={{ py: 1.5 }}>
        <Typography variant="overline" color="text.secondary" sx={{ display: 'block', mb: 0.75 }}>
          Labels
        </Typography>
        {ticket.labels.length === 0 ? (
          <Typography variant="caption" color="text.disabled">
            No labels
          </Typography>
        ) : (
          <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
            {ticket.labels.map((label) => (
              <Chip
                key={label.id}
                label={label.name}
                size="small"
                onDelete={() => removeLabelMutation.mutate(label.id)}
                deleteIcon={<CloseIcon sx={{ fontSize: '14px !important' }} />}
                sx={{
                  bgcolor: label.color ?? undefined,
                  color: label.color ? 'white' : undefined,
                  '& .MuiChip-deleteIcon': { color: label.color ? 'rgba(255,255,255,0.7)' : undefined },
                }}
              />
            ))}
          </Box>
        )}
        <Typography variant="caption" color="text.disabled" sx={{ mt: 0.5, display: 'block' }}>
          Use the ticket editor to add labels.
        </Typography>
      </Box>

      <Divider />

      {/* ── Watchers ─────────────────────────────────────────────── */}
      <Box sx={{ py: 1.5 }}>
        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 0.75 }}>
          <Typography variant="overline" color="text.secondary">
            Watchers ({ticket.watchers.length})
          </Typography>
          <Tooltip title={isWatching ? 'Unwatch' : 'Watch'}>
            <IconButton
              size="small"
              onClick={handleWatchToggle}
              color={isWatching ? 'primary' : 'default'}
              disabled={addWatcherMutation.isPending || removeWatcherMutation.isPending}
            >
              {isWatching ? (
                <VisibilityOffIcon fontSize="small" />
              ) : (
                <VisibilityIcon fontSize="small" />
              )}
            </IconButton>
          </Tooltip>
        </Box>
        {ticket.watchers.length > 0 ? (
          <AvatarGroup max={5} sx={{ justifyContent: 'flex-start' }}>
            {ticket.watchers.map((w) => (
              <Tooltip key={w.userId} title={w.displayName}>
                <Avatar sx={{ width: 28, height: 28, fontSize: 11, cursor: 'default' }}>
                  {stringAvatar(w.displayName)}
                </Avatar>
              </Tooltip>
            ))}
          </AvatarGroup>
        ) : (
          <Typography variant="caption" color="text.disabled">
            No watchers
          </Typography>
        )}
      </Box>

      <Divider />

      {/* ── Story Points / Estimates ──────────────────────────────── */}
      <Box sx={{ py: 1.5 }}>
        <Typography variant="overline" color="text.secondary" sx={{ display: 'block', mb: 0.75 }}>
          Estimates
        </Typography>
        <Stack spacing={1}>
          {/* Story Points */}
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <Typography variant="caption" color="text.secondary" sx={{ minWidth: 90 }}>
              Story Points
            </Typography>
            {storyPointsEdit ? (
              <TextField
                size="small"
                type="number"
                value={storyPointsVal}
                onChange={(e) => setStoryPointsVal(e.target.value)}
                onBlur={handleStoryPointsSave}
                onKeyDown={(e) => {
                  if (e.key === 'Enter') handleStoryPointsSave();
                  if (e.key === 'Escape') setStoryPointsEdit(false);
                }}
                autoFocus
                sx={{ width: 80 }}
                slotProps={{ htmlInput: { min: 0 } }}
              />
            ) : (
              <Typography
                variant="body2"
                onClick={() => {
                  setStoryPointsVal(String(ticket.storyPoints ?? ''));
                  setStoryPointsEdit(true);
                }}
                sx={{
                  cursor: 'pointer',
                  borderBottom: '1px dashed',
                  borderColor: 'divider',
                  minWidth: 40,
                  color: ticket.storyPoints != null ? 'text.primary' : 'text.disabled',
                }}
              >
                {ticket.storyPoints ?? '—'}
              </Typography>
            )}
          </Box>

          {/* Estimated Hours */}
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <Typography variant="caption" color="text.secondary" sx={{ minWidth: 90 }}>
              Est. Hours
            </Typography>
            {estimatedHoursEdit ? (
              <TextField
                size="small"
                type="number"
                value={estimatedHoursVal}
                onChange={(e) => setEstimatedHoursVal(e.target.value)}
                onBlur={handleEstimatedHoursSave}
                onKeyDown={(e) => {
                  if (e.key === 'Enter') handleEstimatedHoursSave();
                  if (e.key === 'Escape') setEstimatedHoursEdit(false);
                }}
                autoFocus
                sx={{ width: 80 }}
                slotProps={{ htmlInput: { min: 0, step: 0.5 } }}
              />
            ) : (
              <Typography
                variant="body2"
                onClick={() => {
                  setEstimatedHoursVal(String(ticket.estimatedHours ?? ''));
                  setEstimatedHoursEdit(true);
                }}
                sx={{
                  cursor: 'pointer',
                  borderBottom: '1px dashed',
                  borderColor: 'divider',
                  minWidth: 40,
                  color: ticket.estimatedHours != null ? 'text.primary' : 'text.disabled',
                }}
              >
                {ticket.estimatedHours != null ? `${ticket.estimatedHours}h` : '—'}
              </Typography>
            )}
          </Box>

          {/* Actual Hours (read-only) */}
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <Typography variant="caption" color="text.secondary" sx={{ minWidth: 90 }}>
              Actual Hours
            </Typography>
            <Typography
              variant="body2"
              color={ticket.actualHours != null ? 'text.primary' : 'text.disabled'}
            >
              {ticket.actualHours != null ? `${ticket.actualHours}h` : '—'}
            </Typography>
          </Box>
        </Stack>
      </Box>

      <Divider />

      {/* ── Similar Tickets (AI) ──────────────────────────────────── */}
      <Accordion
        expanded={similarExpanded}
        onChange={(_, expanded) => setSimilarExpanded(expanded)}
        disableGutters
        elevation={0}
        sx={{ '&:before': { display: 'none' } }}
      >
        <AccordionSummary expandIcon={<ExpandMoreIcon />} sx={{ px: 0, minHeight: 40 }}>
          <Typography variant="overline" color="text.secondary">
            Similar Tickets (AI)
          </Typography>
        </AccordionSummary>
        <AccordionDetails sx={{ px: 0, pt: 0 }}>
          {similarLoading ? (
            <Stack spacing={1}>
              {[1, 2, 3].map((i) => (
                <Skeleton key={i} variant="rounded" height={48} />
              ))}
            </Stack>
          ) : similarData?.related?.length ? (
            <Stack spacing={0.75}>
              {similarData.related.map((t) => (
                <Box
                  key={t.id}
                  component="a"
                  href={`/tickets/${t.id}`}
                  sx={{
                    display: 'block',
                    p: 1,
                    borderRadius: 1,
                    border: '1px solid',
                    borderColor: 'divider',
                    textDecoration: 'none',
                    color: 'inherit',
                    '&:hover': { bgcolor: 'action.hover' },
                  }}
                >
                  <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 1 }}>
                    <Typography variant="caption" color="primary.main" sx={{ fontWeight: 600 }}>
                      {t.ticketKey}
                    </Typography>
                    <Tooltip title={t.resolution ?? ""}>
                      <Chip
                        label={`${t.similarityPercent}%`}
                        size="small"
                        color="primary"
                        variant="outlined"
                        sx={{ height: 18, fontSize: 10 }}
                      />
                    </Tooltip>
                  </Box>
                  <Typography variant="body2" noWrap sx={{ fontSize: 12 }}>
                    {t.title}
                  </Typography>
                  <Typography variant="caption" color="text.disabled">
                    {t.statusName}
                  </Typography>
                </Box>
              ))}
            </Stack>
          ) : (
            <Typography variant="body2" color="text.disabled">
              No similar tickets found.
            </Typography>
          )}
        </AccordionDetails>
      </Accordion>

      <Divider />

      {/* ── Risk Analysis (AI) ────────────────────────────────────── */}
      <Accordion
        expanded={riskExpanded}
        onChange={(_, expanded) => setRiskExpanded(expanded)}
        disableGutters
        elevation={0}
        sx={{ '&:before': { display: 'none' } }}
      >
        <AccordionSummary expandIcon={<ExpandMoreIcon />} sx={{ px: 0, minHeight: 40 }}>
          <Typography variant="overline" color="text.secondary">
            Risk Analysis (AI)
          </Typography>
        </AccordionSummary>
        <AccordionDetails sx={{ px: 0, pt: 0 }}>
          {riskLoading ? (
            <Stack spacing={1}>
              {[1, 2].map((i) => (
                <Skeleton key={i} variant="rounded" height={60} />
              ))}
            </Stack>
          ) : riskData ? (
            <Stack spacing={1}>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <Typography variant="caption" color="text.secondary">
                  Overall Risk:
                </Typography>
                <Chip
                  label={riskData.riskLevel}
                  size="small"
                  sx={{
                    bgcolor: overallRiskBg(riskData.riskLevel.toLowerCase()),
                    color: 'white',
                    fontWeight: 600,
                    textTransform: 'capitalize',
                  }}
                />
              </Box>
              {riskData.risks.map((item, idx) => (
                <Box
                  key={idx}
                  sx={{
                    p: 1,
                    borderRadius: 1,
                    border: '1px solid',
                    borderColor: 'divider',
                    bgcolor: 'background.default',
                  }}
                >
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5, mb: 0.25 }}>
                    <Chip
                      label={item.severity}
                      size="small"
                      color={riskChipColor(item.severity)}
                      sx={{ height: 18, fontSize: 10, textTransform: 'capitalize' }}
                    />
                    <Typography variant="caption" sx={{ fontWeight: 600 }}>
                      {item.title}
                    </Typography>
                  </Box>
                  <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>
                    {item.description}
                  </Typography>
                  {item.riskType && (
                    <Typography variant="caption" color="text.disabled" sx={{ display: 'block', mt: 0.25 }}>
                      Type: {item.riskType}
                    </Typography>
                  )}
                </Box>
              ))}
            </Stack>
          ) : (
            <Typography variant="body2" color="text.disabled">
              No risk data available.
            </Typography>
          )}
        </AccordionDetails>
      </Accordion>

      {/* ── Dialogs ───────────────────────────────────────────────── */}
      {transitionOpen && (
        <TransitionDialog
          open={transitionOpen}
          ticketId={ticket.id}
          projectId={ticket.projectId}
          currentStatusName={ticket.statusName}
          etag={etag}
          onClose={() => setTransitionOpen(false)}
          onSuccess={() => {
            setTransitionOpen(false);
            onTicketUpdate();
          }}
        />
      )}

      <ConfirmDeleteDialog
        open={deleteOpen}
        onClose={() => setDeleteOpen(false)}
        onConfirm={() => deleteMutation.mutate()}
        loading={deleteMutation.isPending}
      />
    </Box>
  );
}

export default TicketSidebar;
