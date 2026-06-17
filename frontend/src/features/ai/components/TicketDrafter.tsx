import {
  Box, TextField, Button, Typography, CircularProgress, Alert,
  Card, CardContent, Chip, Grid,
} from '@mui/material';
import { AutoAwesome as AiIcon, Edit as EditIcon } from '@mui/icons-material';
import { useState } from 'react';
import { useMutation } from '@tanstack/react-query';
import { draftTicket, type TicketDraftDto } from '@/api/ai';
import { PriorityChip } from '@/components/common/PriorityChip';

export function TicketDrafter() {
  const [rawText, setRawText] = useState('');
  const [draft, setDraft] = useState<TicketDraftDto | null>(null);

  const mutation = useMutation({
    mutationFn: () => draftTicket(rawText),
    onSuccess: setDraft,
  });

  return (
    <Box sx={{ p: 2, display: 'flex', flexDirection: 'column', gap: 2, height: '100%', overflow: 'auto' }}>
      <Box>
        <Typography variant="subtitle2" fontWeight={600} mb={0.5}>AI Ticket Drafter</Typography>
        <Typography variant="caption" color="text.secondary">
          Paste an email, Slack message, or description to auto-generate a ticket
        </Typography>
      </Box>

      <TextField
        multiline rows={8}
        label="Paste raw text (email, meeting notes, Slack message...)"
        value={rawText}
        onChange={(e) => setRawText(e.target.value)}
        fullWidth
      />

      <Button
        variant="contained" fullWidth
        disabled={!rawText.trim() || mutation.isPending}
        onClick={() => mutation.mutate()}
        startIcon={<AiIcon />}
      >
        {mutation.isPending ? <CircularProgress size={20} color="inherit" /> : 'Generate Ticket Draft'}
      </Button>

      {mutation.isError && <Alert severity="error">{(mutation.error as Error).message}</Alert>}

      {draft && (
        <Card variant="outlined">
          <CardContent sx={{ display: 'flex', flexDirection: 'column', gap: 1.5 }}>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 0.5 }}>
              <AiIcon color="primary" fontSize="small" />
              <Typography variant="subtitle2" fontWeight={700}>AI Draft</Typography>
              <Chip label={`via ${draft.providerName}`} size="small" variant="outlined" />
            </Box>

            <Box>
              <Typography variant="caption" color="text.secondary">TITLE</Typography>
              <Typography variant="body2" fontWeight={500}>{draft.suggestedTitle}</Typography>
            </Box>

            <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
              <Chip label={draft.suggestedIssueType} size="small" color="primary" variant="outlined" />
              <PriorityChip priority={draft.suggestedPriority} />
              {draft.suggestedLabels.map((l) => (
                <Chip key={l} label={l} size="small" variant="outlined" />
              ))}
            </Box>

            {draft.suggestedAssigneeName && (
              <Box>
                <Typography variant="caption" color="text.secondary">SUGGESTED ASSIGNEE</Typography>
                <Typography variant="body2">{draft.suggestedAssigneeName}</Typography>
              </Box>
            )}

            {draft.suggestedDueDate && (
              <Box>
                <Typography variant="caption" color="text.secondary">SUGGESTED DUE DATE</Typography>
                <Typography variant="body2">{draft.suggestedDueDate}</Typography>
              </Box>
            )}

            <Box>
              <Typography variant="caption" color="text.secondary">DESCRIPTION PREVIEW</Typography>
              <Typography variant="body2" sx={{ whiteSpace: 'pre-wrap', maxHeight: 120, overflow: 'auto', fontSize: 12 }}>
                {draft.suggestedDescription.slice(0, 400)}{draft.suggestedDescription.length > 400 ? '...' : ''}
              </Typography>
            </Box>

            <Button variant="contained" startIcon={<EditIcon />} size="small">
              Edit & Create Ticket
            </Button>
          </CardContent>
        </Card>
      )}
    </Box>
  );
}
