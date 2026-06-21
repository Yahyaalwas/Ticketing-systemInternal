import React, { useState, useRef, useEffect } from 'react';
import {
  Box,
  Typography,
  Chip,
  Alert,
  AlertTitle,
  CircularProgress,
  Breadcrumbs,
  Link as MuiLink,
  Card,
  CardContent,
  Tabs,
  Tab,
  TextField,
  IconButton,
  Tooltip,
  Button,
  Skeleton,
  Stack,
} from '@mui/material';
import { Grid } from '@mui/material';
const Grid2 = Grid;
import {
  Home as HomeIcon,
  Edit as EditIcon,
  Check as CheckIcon,
  Close as CloseIcon,
  Warning as WarningIcon,
} from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useParams, useNavigate, Link as RouterLink } from 'react-router-dom';

import { ticketsApi } from '@/api/tickets';
import { referenceApi } from '@/api/reference';
import { useNotification } from '@/hooks/useNotification';
import { AiSummaryCard } from '@/components/shared/AiSummaryCard';
import { CommentsPanel } from '@/components/shared/CommentsPanel';
import { AttachmentPanel } from '@/components/shared/AttachmentPanel';
import { ActivityFeed } from '@/components/shared/ActivityFeed';
import { MarkdownRenderer } from '@/components/shared/MarkdownRenderer';
import { TicketSidebar } from './TicketSidebar';

// ─── helpers ────────────────────────────────────────────────────────────────

function statusCategoryColor(category: string, fallback?: string): string {
  if (fallback) return fallback;
  if (category === 'done') return '#2e7d32';
  if (category === 'in-progress' || category === 'inprogress') return '#0288d1';
  return '#757575';
}

function priorityColor(color?: string): string {
  return color ?? '#9e9e9e';
}

type TabValue = 'comments' | 'attachments' | 'activity';

// ─── Inline Title Edit ────────────────────────────────────────────────────

interface InlineTitleEditProps {
  value: string;
  onSave: (title: string) => void;
  loading: boolean;
}

function InlineTitleEdit({ value, onSave, loading }: InlineTitleEditProps) {
  const [editing, setEditing] = useState(false);
  const [draft, setDraft] = useState(value);
  const inputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (editing) inputRef.current?.focus();
  }, [editing]);

  const handleSave = () => {
    const trimmed = draft.trim();
    if (trimmed && trimmed !== value) {
      onSave(trimmed);
    }
    setEditing(false);
  };

  const handleCancel = () => {
    setDraft(value);
    setEditing(false);
  };

  if (editing) {
    return (
      <Box sx={{ display: 'flex', alignItems: 'flex-start', gap: 0.5, flex: 1 }}>
        <TextField
          inputRef={inputRef}
          value={draft}
          onChange={(e) => setDraft(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === 'Enter' && (e.ctrlKey || e.metaKey)) handleSave();
            if (e.key === 'Escape') handleCancel();
          }}
          onBlur={handleSave}
          fullWidth
          variant="outlined"
          size="small"
          multiline
          maxRows={4}
          sx={{ '& textarea': { fontSize: '1.25rem', fontWeight: 600, lineHeight: 1.4 } }}
        />
        <Tooltip title="Save (Ctrl+Enter)">
          <IconButton size="small" color="primary" onMouseDown={(e) => { e.preventDefault(); handleSave(); }} disabled={loading}>
            {loading ? <CircularProgress size={16} /> : <CheckIcon fontSize="small" />}
          </IconButton>
        </Tooltip>
        <Tooltip title="Cancel">
          <IconButton size="small" onMouseDown={(e) => { e.preventDefault(); handleCancel(); }}>
            <CloseIcon fontSize="small" />
          </IconButton>
        </Tooltip>
      </Box>
    );
  }

  return (
    <Box sx={{ display: 'flex', alignItems: 'flex-start', gap: 0.5, flex: 1 }}>
      <Typography variant="h5" sx={{ fontWeight: 700, flex: 1, lineHeight: 1.4 }}>
        {value}
      </Typography>
      <Tooltip title="Edit title">
        <IconButton size="small" onClick={() => { setDraft(value); setEditing(true); }}>
          <EditIcon fontSize="small" />
        </IconButton>
      </Tooltip>
    </Box>
  );
}

// ─── Description Card ────────────────────────────────────────────────────────

interface DescriptionCardProps {
  html?: string;
  text?: string;
  onSave: (description: string) => void;
  loading: boolean;
}

function DescriptionCard({ html, text, onSave, loading }: DescriptionCardProps) {
  const [editing, setEditing] = useState(false);
  const [draft, setDraft] = useState(text ?? '');

  const handleSave = () => {
    onSave(draft);
    setEditing(false);
  };

  const handleCancel = () => {
    setDraft(text ?? '');
    setEditing(false);
  };

  return (
    <Card variant="outlined">
      <CardContent>
        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 1 }}>
          <Typography variant="subtitle2" color="text.secondary">
            Description
          </Typography>
          {!editing && (
            <Tooltip title="Edit description">
              <IconButton size="small" onClick={() => { setDraft(text ?? ''); setEditing(true); }}>
                <EditIcon fontSize="small" />
              </IconButton>
            </Tooltip>
          )}
        </Box>

        {editing ? (
          <>
            <TextField
              value={draft}
              onChange={(e) => setDraft(e.target.value)}
              multiline
              minRows={6}
              fullWidth
              placeholder="Add a description… (Markdown supported)"
              variant="outlined"
              size="small"
              autoFocus
              onKeyDown={(e) => {
                if (e.key === 'Escape') handleCancel();
              }}
            />
            <Stack direction="row" spacing={1} sx={{ mt: 1 }}>
              <Button
                size="small"
                variant="contained"
                onClick={handleSave}
                disabled={loading}
                startIcon={loading ? <CircularProgress size={14} /> : <CheckIcon />}
              >
                Save
              </Button>
              <Button size="small" onClick={handleCancel} disabled={loading}>
                Cancel
              </Button>
            </Stack>
          </>
        ) : (
          <Box
            sx={{
              minHeight: 60,
              color: html || text ? 'text.primary' : 'text.disabled',
              fontSize: '0.875rem',
            }}
          >
            {html ? (
              <MarkdownRenderer html={html} />
            ) : text ? (
              <Typography variant="body2" sx={{ whiteSpace: 'pre-wrap' }}>
                {text}
              </Typography>
            ) : (
              <Typography variant="body2" color="text.disabled" sx={{ fontStyle: 'italic' }}>
                No description provided. Click the edit button to add one.
              </Typography>
            )}
          </Box>
        )}
      </CardContent>
    </Card>
  );
}

// ─── Main Page ───────────────────────────────────────────────────────────────

export function TicketDetailPage() {
  const { ticketId } = useParams<{ ticketId: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const notify = useNotification();

  const [activeTab, setActiveTab] = useState<TabValue>('comments');

  // ── Data fetching ──────────────────────────────────────────────────────────

  const { data, refetch, isLoading, error } = useQuery({
    queryKey: ['ticket', ticketId],
    queryFn: () => ticketsApi.get(ticketId!),
    staleTime: 30_000,
    enabled: !!ticketId,
  });

  const ticket = data?.data;
  const etag = data?.etag ?? '';

  const { data: issueTypes = [] } = useQuery({
    queryKey: ['issueTypes', ticket?.projectId],
    queryFn: () => referenceApi.getIssueTypes(ticket!.projectId),
    enabled: !!ticket?.projectId,
    staleTime: 5 * 60_000,
  });

  const { data: priorities = [] } = useQuery({
    queryKey: ['priorities'],
    queryFn: () => referenceApi.getPriorities(),
    staleTime: 5 * 60_000,
  });

  // ── Mutations ──────────────────────────────────────────────────────────────

  const updateTitleMutation = useMutation({
    mutationFn: (title: string) => {
      const issueType = issueTypes.find((it) => it.name === ticket!.issueTypeName);
      if (!issueType) throw new Error('Issue type not found');
      return ticketsApi.update(
        ticket!.id,
        {
          title,
          issueTypeId: issueType.id,
          description: ticket!.description,
          priorityId: priorities.find((p) => p.name === ticket!.priorityName)?.id,
          assigneeUserId: ticket!.assigneeUserId,
          dueDate: ticket!.dueDate,
          storyPoints: ticket!.storyPoints,
          estimatedHours: ticket!.estimatedHours,
        },
        etag,
      );
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['ticket', ticketId] });
      notify.success('Title updated');
    },
    onError: () => notify.error('Failed to update title'),
  });

  const updateDescriptionMutation = useMutation({
    mutationFn: (description: string) => {
      const issueType = issueTypes.find((it) => it.name === ticket!.issueTypeName);
      if (!issueType) throw new Error('Issue type not found');
      return ticketsApi.update(
        ticket!.id,
        {
          title: ticket!.title,
          issueTypeId: issueType.id,
          description,
          priorityId: priorities.find((p) => p.name === ticket!.priorityName)?.id,
          assigneeUserId: ticket!.assigneeUserId,
          dueDate: ticket!.dueDate,
          storyPoints: ticket!.storyPoints,
          estimatedHours: ticket!.estimatedHours,
        },
        etag,
      );
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['ticket', ticketId] });
      notify.success('Description updated');
    },
    onError: () => notify.error('Failed to update description'),
  });

  // ── States ─────────────────────────────────────────────────────────────────

  if (isLoading) {
    return (
      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          minHeight: '60vh',
        }}
      >
        <CircularProgress size={48} />
      </Box>
    );
  }

  if (error || !ticket) {
    return (
      <Box sx={{ p: 3 }}>
        <Alert
          severity="error"
          action={
            <Button color="inherit" size="small" onClick={() => navigate(-1)}>
              Go Back
            </Button>
          }
        >
          <AlertTitle>Ticket Not Found</AlertTitle>
          {error instanceof Error ? error.message : 'The requested ticket could not be loaded.'}
        </Alert>
      </Box>
    );
  }

  const slaBreached =
    ticket.slaBreachAt != null && new Date(ticket.slaBreachAt) < new Date();

  const statusColor = statusCategoryColor(ticket.statusCategory, ticket.statusColor);

  return (
    <Box sx={{ p: { xs: 2, md: 3 }, maxWidth: 1400, mx: 'auto' }}>

      {/* Deleted banner */}
      {ticket.isDeleted && (
        <Alert severity="warning" icon={<WarningIcon />} sx={{ mb: 2 }}>
          This ticket has been deleted and is in read-only mode.
        </Alert>
      )}

      {/* Breadcrumbs */}
      <Breadcrumbs sx={{ mb: 2 }} aria-label="breadcrumb">
        <MuiLink
          component={RouterLink}
          to="/"
          underline="hover"
          color="inherit"
          sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}
        >
          <HomeIcon sx={{ fontSize: 16 }} />
          Home
        </MuiLink>
        <MuiLink
          component={RouterLink}
          to={`/projects/${ticket.projectId}`}
          underline="hover"
          color="inherit"
        >
          {ticket.projectKey}
        </MuiLink>
        <Typography color="text.primary" sx={{ fontWeight: 500 }}>
          {ticket.ticketKey}
        </Typography>
      </Breadcrumbs>

      <Grid2 container spacing={3}>
        {/* ── Left Column ─────────────────────────────────────────── */}
        <Grid2 size={{ xs: 12, lg: 8 }}>
          <Stack spacing={2.5}>

            {/* Header Card */}
            <Card variant="outlined">
              <CardContent>
                {/* Meta chips row */}
                <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.75, mb: 1.5 }}>
                  <Chip
                    label={ticket.ticketKey}
                    size="small"
                    variant="outlined"
                    color="primary"
                    sx={{ fontWeight: 700, fontFamily: 'monospace' }}
                  />
                  <Chip
                    label={ticket.issueTypeName}
                    size="small"
                    variant="outlined"
                    sx={{ color: 'text.secondary' }}
                  />
                  <Chip
                    label={ticket.statusName}
                    size="small"
                    sx={{
                      bgcolor: statusColor,
                      color: 'white',
                      fontWeight: 600,
                    }}
                  />
                  {ticket.priorityName && (
                    <Chip
                      label={ticket.priorityName}
                      size="small"
                      sx={{
                        bgcolor: priorityColor(ticket.priorityColor),
                        color: 'white',
                        fontWeight: 500,
                      }}
                    />
                  )}
                  {slaBreached && (
                    <Chip
                      label="SLA Breached"
                      size="small"
                      color="error"
                      icon={<WarningIcon sx={{ fontSize: '14px !important' }} />}
                    />
                  )}
                  {ticket.resolutionName && (
                    <Chip
                      label={ticket.resolutionName}
                      size="small"
                      variant="outlined"
                      color="success"
                    />
                  )}
                </Box>

                {/* Title */}
                <InlineTitleEdit
                  value={ticket.title}
                  onSave={(title) => updateTitleMutation.mutate(title)}
                  loading={updateTitleMutation.isPending}
                />

                {/* Meta info row */}
                <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 2, mt: 1.5 }}>
                  <Typography variant="caption" color="text.disabled">
                    Created {new Date(ticket.createdAt).toLocaleDateString()}
                  </Typography>
                  <Typography variant="caption" color="text.disabled">
                    Updated {new Date(ticket.updatedAt).toLocaleDateString()}
                  </Typography>
                  {ticket.commentCount > 0 && (
                    <Typography variant="caption" color="text.disabled">
                      {ticket.commentCount} comment{ticket.commentCount !== 1 ? 's' : ''}
                    </Typography>
                  )}
                  {ticket.attachmentCount > 0 && (
                    <Typography variant="caption" color="text.disabled">
                      {ticket.attachmentCount} attachment{ticket.attachmentCount !== 1 ? 's' : ''}
                    </Typography>
                  )}
                </Box>
              </CardContent>
            </Card>

            {/* Description Card */}
            <DescriptionCard
              html={ticket.descriptionHtml}
              text={ticket.description}
              onSave={(desc) => updateDescriptionMutation.mutate(desc)}
              loading={updateDescriptionMutation.isPending}
            />

            {/* AI Summary */}
            <AiSummaryCard ticketId={ticket.id} />

            {/* Tabs */}
            <Card variant="outlined">
              <Box sx={{ borderBottom: 1, borderColor: 'divider' }}>
                <Tabs
                  value={activeTab}
                  onChange={(_, val: TabValue) => setActiveTab(val)}
                  sx={{ px: 2 }}
                >
                  <Tab
                    label={`Comments${ticket.commentCount > 0 ? ` (${ticket.commentCount})` : ''}`}
                    value="comments"
                  />
                  <Tab
                    label={`Attachments${ticket.attachmentCount > 0 ? ` (${ticket.attachmentCount})` : ''}`}
                    value="attachments"
                  />
                  <Tab label="Activity" value="activity" />
                </Tabs>
              </Box>
              <CardContent sx={{ pt: 2 }}>
                {activeTab === 'comments' && <CommentsPanel ticketId={ticket.id} />}
                {activeTab === 'attachments' && <AttachmentPanel ticketId={ticket.id} />}
                {activeTab === 'activity' && <ActivityFeed ticketId={ticket.id} />}
              </CardContent>
            </Card>
          </Stack>
        </Grid2>

        {/* ── Right Column ─────────────────────────────────────────── */}
        <Grid2 size={{ xs: 12, lg: 4 }}>
          <Card variant="outlined" sx={{ position: { lg: 'sticky' }, top: { lg: 80 } }}>
            <CardContent>
              <TicketSidebar
                ticket={ticket}
                etag={etag}
                onTicketUpdate={() => void refetch()}
              />
            </CardContent>
          </Card>
        </Grid2>
      </Grid2>
    </Box>
  );
}

export default TicketDetailPage;
