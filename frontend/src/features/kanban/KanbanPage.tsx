import {
  Box, Typography, Card, CardContent, Chip, IconButton, TextField,
  InputAdornment, CircularProgress, Tooltip, Badge,
} from '@mui/material';
import {
  Search as SearchIcon, Comment as CommentIcon, Attachment as AttachIcon,
} from '@mui/icons-material';
import { useState, useCallback, memo } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useSearchParams } from 'react-router-dom';
import {
  DndContext, DragEndEvent, DragOverlay, DragStartEvent,
  PointerSensor, useSensor, useSensors, closestCorners,
} from '@dnd-kit/core';
import {
  SortableContext, verticalListSortingStrategy, useSortable,
} from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import { ticketsApi } from '@/api/tickets';
import { KanbanBoardDto } from '@/types';
import { PriorityChip } from '@/components/common/PriorityChip';
import { UserAvatar } from '@/components/common/UserAvatar';
import { formatDate } from '@/utils/date';

// ─── Ticket Card ───────────────────────────────────────────────────────────────

interface TicketCardProps {
  ticket: {
    id: string; ticketKey: string; title: string; priorityName?: string;
    priorityColor?: string; assigneeName?: string; assigneeAvatarUrl?: string;
    dueDate?: string; storyPoints?: number; commentCount: number; attachmentCount: number;
    labelNames: string[];
  };
  overlay?: boolean;
}

const TicketCard = memo(function TicketCard({ ticket, overlay }: TicketCardProps) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({ id: ticket.id });
  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isDragging ? 0.4 : 1,
  };

  const isOverdue = ticket.dueDate && new Date(ticket.dueDate) < new Date();

  const handleKeyDown = (e: React.KeyboardEvent<HTMLElement>) => {
    if (e.key === 'Enter' || e.key === ' ') {
      (e.currentTarget as HTMLElement).focus();
    }
  };

  return (
    <Card
      ref={overlay ? undefined : setNodeRef}
      style={overlay ? undefined : style}
      {...(overlay ? {} : { ...attributes, ...listeners })}
      tabIndex={0}
      onKeyDown={handleKeyDown}
      sx={{
        mb: 1, cursor: 'grab', boxShadow: overlay ? 4 : 1,
        '&:hover': { boxShadow: 3 },
        userSelect: 'none',
      }}
    >
      <CardContent sx={{ p: '10px !important' }}>
        <Typography variant="caption" color="text.secondary" sx={{ fontFamily: 'monospace' }}>
          {ticket.ticketKey}
        </Typography>
        <Typography variant="body2" sx={{ fontWeight: 500, mt: 0.5, mb: 1, lineHeight: 1.4 }}>
          {ticket.title}
        </Typography>
        {ticket.labelNames.length > 0 && (
          <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5, mb: 1 }}>
            {ticket.labelNames.slice(0, 3).map((l) => (
              <Chip key={l} label={l} size="small" sx={{ height: 18, fontSize: 10 }} />
            ))}
          </Box>
        )}
        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
            <PriorityChip priority={ticket.priorityName} />
            {ticket.storyPoints != null && (
              <Chip label={ticket.storyPoints} size="small" variant="outlined" sx={{ height: 18, fontSize: 10 }} />
            )}
          </Box>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
            {ticket.commentCount > 0 && (
              <Tooltip title={`${ticket.commentCount} comments`}>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.25 }}>
                  <CommentIcon sx={{ fontSize: 13, color: 'text.disabled' }} />
                  <Typography variant="caption" color="text.disabled">{ticket.commentCount}</Typography>
                </Box>
              </Tooltip>
            )}
            {ticket.attachmentCount > 0 && (
              <Tooltip title={`${ticket.attachmentCount} attachments`}>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.25 }}>
                  <AttachIcon sx={{ fontSize: 13, color: 'text.disabled' }} />
                  <Typography variant="caption" color="text.disabled">{ticket.attachmentCount}</Typography>
                </Box>
              </Tooltip>
            )}
            {ticket.assigneeName && (
              <UserAvatar name={ticket.assigneeName} avatarUrl={ticket.assigneeAvatarUrl} size={20} />
            )}
          </Box>
        </Box>
        {ticket.dueDate && (
          <Typography variant="caption" color={isOverdue ? 'error' : 'text.secondary'} sx={{ display: 'block', mt: 0.5 }}>
            Due: {formatDate(ticket.dueDate)}
          </Typography>
        )}
      </CardContent>
    </Card>
  );
});

// ─── Column ────────────────────────────────────────────────────────────────────

interface ColumnProps {
  column: {
    statusId: number; statusName: string; statusColor?: string;
    wipLimit?: number; tickets: TicketCardProps['ticket'][];
  };
}

const KanbanColumn = memo(function KanbanColumn({ column }: ColumnProps) {
  const ticketIds = column.tickets.map((t) => t.id);
  const overWip = column.wipLimit != null && column.tickets.length > column.wipLimit;

  return (
    <Box
      role="region"
      aria-label={column.statusName}
      sx={{
        width: 280, flexShrink: 0, display: 'flex', flexDirection: 'column',
        bgcolor: 'action.hover', borderRadius: 2, overflow: 'hidden',
      }}
    >
      <Box
        sx={{
          px: 2, py: 1.5, display: 'flex', alignItems: 'center',
          justifyContent: 'space-between', borderBottom: '2px solid',
          borderColor: column.statusColor ?? 'divider',
        }}
      >
        <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>{column.statusName}</Typography>
        <Chip
          label={column.tickets.length + (column.wipLimit ? `/${column.wipLimit}` : '')}
          size="small"
          color={overWip ? 'error' : 'default'}
        />
      </Box>
      <Box sx={{ flex: 1, overflow: 'auto', p: 1 }}>
        <SortableContext items={ticketIds} strategy={verticalListSortingStrategy}>
          {column.tickets.map((t) => <TicketCard key={t.id} ticket={t} />)}
        </SortableContext>
      </Box>
    </Box>
  );
});

// ─── Page ──────────────────────────────────────────────────────────────────────

export default function KanbanPage() {
  const [searchParams] = useSearchParams();
  const projectId = searchParams.get('projectId') ?? '';
  const [search, setSearch] = useState('');
  const [activeTicket, setActiveTicket] = useState<TicketCardProps['ticket'] | null>(null);
  const qc = useQueryClient();

  const sensors = useSensors(useSensor(PointerSensor, { activationConstraint: { distance: 5 } }));

  const { data, isLoading } = useQuery<KanbanBoardDto>({
    queryKey: ['kanban', projectId, search],
    queryFn: () => ticketsApi.getKanbanBoard(projectId, { search: search || undefined }) as Promise<KanbanBoardDto>,
    enabled: !!projectId,
  });

  const transitionMutation = useMutation({
    mutationFn: ({ ticketId, statusId, etag }: { ticketId: string; statusId: number; etag: string }) =>
      ticketsApi.transition(ticketId, { toStatusId: statusId }, etag),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['kanban'] }),
  });

  const allTickets = data?.columns.flatMap((c) => c.tickets) ?? [];

  const handleDragStart = useCallback((event: DragStartEvent) => {
    const ticket = allTickets.find((t) => t.id === event.active.id);
    setActiveTicket(ticket ?? null);
  }, [allTickets]);

  const handleDragEnd = useCallback((event: DragEndEvent) => {
    setActiveTicket(null);
    const { active, over } = event;
    if (!over || active.id === over.id || !data) return;

    // Find which column the dropped-over item belongs to
    const targetColumn = data.columns.find((col) =>
      col.tickets.some((t) => t.id === over.id)
    );
    const sourceColumn = data.columns.find((col) =>
      col.tickets.some((t) => t.id === active.id)
    );

    if (!targetColumn || !sourceColumn || targetColumn.statusId === sourceColumn.statusId) return;

    const movedTicket = allTickets.find((t) => t.id === String(active.id));
    transitionMutation.mutate({ ticketId: String(active.id), statusId: targetColumn.statusId, etag: movedTicket?.rowVersion ?? '' });
  }, [allTickets, data, transitionMutation]);

  if (!projectId) {
    return (
      <Box sx={{ p: 4, textAlign: 'center' }}>
        <Typography color="text.secondary">Select a project to view its Kanban board.</Typography>
        <Typography variant="caption" color="text.disabled" sx={{ display: 'block', mt: 1 }}>
          Add ?projectId=&lt;uuid&gt; to the URL
        </Typography>
      </Box>
    );
  }

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 2, flexWrap: 'wrap' }}>
        <Typography variant="h5" sx={{ fontWeight: 700 }}>
          {data?.projectName ?? 'Kanban Board'}
          {data?.projectKey && (
            <Typography component="span" variant="caption" color="text.secondary" sx={{ ml: 1, fontFamily: 'monospace' }}>
              {data.projectKey}
            </Typography>
          )}
        </Typography>
        <Box sx={{ flexGrow: 1 }} />
        <TextField
          size="small"
          placeholder="Search tickets..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          sx={{ width: 240 }}
          slotProps={{
            input: { 'aria-label': 'Search tickets', startAdornment: <InputAdornment position="start"><SearchIcon fontSize="small" /></InputAdornment> },
          }}
        />
      </Box>

      {isLoading ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', pt: 8 }}>
          <CircularProgress />
        </Box>
      ) : (
        <DndContext
          sensors={sensors}
          collisionDetection={closestCorners}
          onDragStart={handleDragStart}
          onDragEnd={handleDragEnd}
        >
          <Box sx={{ display: 'flex', gap: 2, overflowX: 'auto', flex: 1, pb: 2, alignItems: 'flex-start' }}>
            {(data?.columns ?? []).map((col) => (
              <KanbanColumn key={col.statusId} column={col} />
            ))}
          </Box>
          <DragOverlay>
            {activeTicket && <TicketCard ticket={activeTicket} overlay />}
          </DragOverlay>
        </DndContext>
      )}
    </Box>
  );
}
