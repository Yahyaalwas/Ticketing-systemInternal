import {
  Box, TextField, Button, Typography, CircularProgress, Alert,
  Accordion, AccordionSummary, AccordionDetails, Chip, List,
  ListItem, ListItemText, Divider,
} from '@mui/material';
import { ExpandMore as ExpandMoreIcon, Task as TaskIcon } from '@mui/icons-material';
import { useState } from 'react';
import { useMutation } from '@tanstack/react-query';
import { parseMeetingNotes, type MeetingParseResultDto } from '@/api/ai';
import { PriorityChip } from '@/components/common/PriorityChip';

export function MeetingParser() {
  const [notes, setNotes] = useState('');
  const [title, setTitle] = useState('');
  const [result, setResult] = useState<MeetingParseResultDto | null>(null);

  const mutation = useMutation({
    mutationFn: () => parseMeetingNotes(notes, title || undefined),
    onSuccess: setResult,
  });

  return (
    <Box sx={{ p: 2, display: 'flex', flexDirection: 'column', gap: 2, height: '100%', overflow: 'auto' }}>
      <Box>
        <Typography variant="subtitle2" sx={{ fontWeight: 600, mb: 0.5 }}>Meeting Notes Parser</Typography>
        <Typography variant="caption" color="text.secondary">
          Paste meeting notes to extract tasks, decisions, and risks
        </Typography>
      </Box>

      <TextField size="small" label="Meeting Title (optional)" value={title} onChange={(e) => setTitle(e.target.value)} fullWidth />

      <TextField
        multiline rows={8}
        label="Paste meeting notes here"
        value={notes}
        onChange={(e) => setNotes(e.target.value)}
        fullWidth
        placeholder="Paste email, meeting transcript, or notes..."
      />

      <Button
        variant="contained" fullWidth
        disabled={!notes.trim() || mutation.isPending}
        onClick={() => mutation.mutate()}
      >
        {mutation.isPending ? <CircularProgress size={20} color="inherit" /> : 'Extract Tasks & Insights'}
      </Button>

      {mutation.isError && <Alert severity="error">{(mutation.error as Error).message}</Alert>}

      {result && (
        <Box>
          <Typography variant="subtitle2" sx={{ fontWeight: 600, mb: 1 }}>{result.meetingTitle}</Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>{result.narrativeSummary}</Typography>

          <Accordion defaultExpanded>
            <AccordionSummary expandIcon={<ExpandMoreIcon />}>
              <Typography variant="body2" sx={{ fontWeight: 600 }}>
                Tasks ({result.tasks.length}) <Chip label={result.tasks.length} size="small" color="primary" sx={{ ml: 1 }} />
              </Typography>
            </AccordionSummary>
            <AccordionDetails sx={{ p: 0 }}>
              <List disablePadding>
                {result.tasks.map((task, i) => (
                  <Box key={i}>
                    <ListItem alignItems="flex-start" sx={{ py: 1 }}>
                      <TaskIcon sx={{ mt: 0.5, mr: 1.5, color: 'primary.main', fontSize: 18 }} />
                      <ListItemText
                        primary={
                          <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5, flexWrap: 'wrap' }}>
                            <Typography variant="body2" sx={{ fontWeight: 500 }}>{task.title}</Typography>
                            <PriorityChip priority={task.priority} />
                          </Box>
                        }
                        secondary={
                          <>
                            {task.description && <Typography variant="caption" sx={{ display: "block" }}>{task.description}</Typography>}
                            {task.owner && <Typography variant="caption" color="text.secondary">Owner: {task.owner}</Typography>}
                            {task.dueDate && <Typography variant="caption" color="text.secondary"> · Due: {task.dueDate}</Typography>}
                          </>
                        }
                      />
                    </ListItem>
                    {i < result.tasks.length - 1 && <Divider component="li" />}
                  </Box>
                ))}
              </List>
            </AccordionDetails>
          </Accordion>

          {result.decisions.length > 0 && (
            <Accordion>
              <AccordionSummary expandIcon={<ExpandMoreIcon />}>
                <Typography variant="body2" sx={{ fontWeight: 600 }}>Decisions ({result.decisions.length})</Typography>
              </AccordionSummary>
              <AccordionDetails>
                <List disablePadding>
                  {result.decisions.map((d, i) => (
                    <ListItem key={i} sx={{ py: 0.5 }}>
                      <ListItemText
                        primary={<Typography variant="body2">{d.decision}</Typography>}
                        secondary={d.context}
                      />
                    </ListItem>
                  ))}
                </List>
              </AccordionDetails>
            </Accordion>
          )}

          {result.risks.length > 0 && (
            <Accordion>
              <AccordionSummary expandIcon={<ExpandMoreIcon />}>
                <Typography variant="body2" sx={{ fontWeight: 600 }} color="error">Risks ({result.risks.length})</Typography>
              </AccordionSummary>
              <AccordionDetails>
                {result.risks.map((r, i) => (
                  <Typography key={i} variant="body2" sx={{ mb: 0.5 }}>• {r}</Typography>
                ))}
              </AccordionDetails>
            </Accordion>
          )}
        </Box>
      )}
    </Box>
  );
}
