import {
  Dialog, DialogTitle, DialogContent, DialogActions,
  Button, Typography, Box, Chip, TextField, CircularProgress,
  RadioGroup, FormControlLabel, Radio,
} from '@mui/material';
import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { ticketsApi } from '@/api/tickets';
import { KanbanBoardDto } from '@/types';
import { useNotification } from '@/hooks/useNotification';

interface TransitionDialogProps {
  open: boolean;
  onClose: () => void;
  ticketId: string;
  projectId: string;
  currentStatusName: string;
  etag: string;
  onSuccess?: () => void;
}

const CATEGORY_COLOR: Record<string, string> = {
  todo: '#97A0AF',
  'in progress': '#0052CC',
  done: '#36B37E',
  blocked: '#FF5630',
};

export function TransitionDialog({
  open, onClose, ticketId, projectId, currentStatusName, etag, onSuccess,
}: TransitionDialogProps) {
  const qc = useQueryClient();
  const { success, error } = useNotification();
  const [selectedStatusId, setSelectedStatusId] = useState<number | null>(null);
  const [comment, setComment] = useState('');

  const { data: kanban, isLoading: loadingStatuses } = useQuery<KanbanBoardDto>({
    queryKey: ['kanban-statuses', projectId],
    queryFn: () => ticketsApi.getKanbanBoard(projectId) as Promise<KanbanBoardDto>,
    enabled: open && !!projectId,
    staleTime: 5 * 60_000,
  });

  const availableStatuses = (kanban?.columns ?? [])
    .filter((c) => c.statusName !== currentStatusName)
    .sort((a, b) => a.displayOrder - b.displayOrder);

  const transitionMutation = useMutation({
    mutationFn: () => ticketsApi.transition(
      ticketId,
      { toStatusId: selectedStatusId!, comment: comment || undefined },
      etag,
    ),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['ticket', ticketId] });
      qc.invalidateQueries({ queryKey: ['activity', ticketId] });
      success('Status updated');
      onSuccess?.();
      handleClose();
    },
    onError: (err: Error) => error(err.message),
  });

  const handleClose = () => {
    setSelectedStatusId(null);
    setComment('');
    onClose();
  };

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="sm" fullWidth>
      <DialogTitle>
        Transition Status
        <Typography variant="body2" color="text.secondary">
          Current: <strong>{currentStatusName}</strong>
        </Typography>
      </DialogTitle>
      <DialogContent>
        {loadingStatuses ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', py: 3 }}>
            <CircularProgress size={24} />
          </Box>
        ) : availableStatuses.length === 0 ? (
          <Typography color="text.secondary">No available transitions.</Typography>
        ) : (
          <Box>
            <Typography variant="subtitle2" sx={{ mb: 1.5 }}>Select new status:</Typography>
            <RadioGroup value={selectedStatusId ?? ''} onChange={(e) => setSelectedStatusId(Number(e.target.value))}>
              {availableStatuses.map((s) => {
                const color = s.statusColor ?? CATEGORY_COLOR[(s.statusCategory ?? '').toLowerCase()] ?? '#97A0AF';
                return (
                  <FormControlLabel
                    key={s.statusId}
                    value={s.statusId}
                    control={<Radio size="small" />}
                    label={
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                        <Box sx={{ width: 10, height: 10, borderRadius: '50%', bgcolor: color, flexShrink: 0 }} />
                        <Typography variant="body2">{s.statusName}</Typography>
                        <Chip label={s.statusCategory} size="small" sx={{ height: 18, fontSize: 10 }} />
                      </Box>
                    }
                    sx={{
                      mb: 0.5, mx: 0, px: 1.5, py: 0.75, borderRadius: 1,
                      border: '1px solid',
                      borderColor: selectedStatusId === s.statusId ? 'primary.main' : 'divider',
                      bgcolor: selectedStatusId === s.statusId ? 'action.selected' : 'transparent',
                    }}
                  />
                );
              })}
            </RadioGroup>
            <TextField
              fullWidth
              multiline
              rows={2}
              label="Transition comment (optional)"
              value={comment}
              onChange={(e) => setComment(e.target.value)}
              sx={{ mt: 2 }}
              size="small"
            />
          </Box>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={handleClose}>Cancel</Button>
        <Button
          variant="contained"
          disabled={!selectedStatusId || transitionMutation.isPending}
          onClick={() => transitionMutation.mutate()}
        >
          {transitionMutation.isPending ? <CircularProgress size={18} color="inherit" /> : 'Transition'}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
