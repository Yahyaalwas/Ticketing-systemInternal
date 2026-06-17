import {
  Box, TextField, Button, Typography, Chip, Table, TableBody,
  TableCell, TableHead, TableRow, CircularProgress, Alert, Paper,
} from '@mui/material';
import { Search as SearchIcon } from '@mui/icons-material';
import { useState } from 'react';
import { useMutation } from '@tanstack/react-query';
import { naturalLanguageSearch, type NaturalLanguageSearchResult } from '@/api/ai';
import { StatusChip } from '@/components/common/StatusChip';
import { PriorityChip } from '@/components/common/PriorityChip';
import { formatDate } from '@/utils/date';

const EXAMPLE_QUERIES = [
  'Show my overdue high priority tickets',
  'Bugs assigned to Ahmad',
  'Unassigned critical incidents',
  'Finance tickets waiting for approval',
];

export function NaturalLanguageSearch() {
  const [query, setQuery] = useState('');
  const [result, setResult] = useState<NaturalLanguageSearchResult | null>(null);

  const mutation = useMutation({
    mutationFn: (q: string) => naturalLanguageSearch(q),
    onSuccess: setResult,
  });

  const handleSearch = (q = query.trim()) => {
    if (!q) return;
    setQuery(q);
    mutation.mutate(q);
  };

  return (
    <Box sx={{ p: 2, display: 'flex', flexDirection: 'column', gap: 2, height: '100%', overflow: 'auto' }}>
      <Box>
        <Typography variant="subtitle2" fontWeight={600} mb={1}>Natural Language Search</Typography>
        <Typography variant="caption" color="text.secondary">
          Describe what you're looking for in plain English
        </Typography>
      </Box>

      <Box sx={{ display: 'flex', gap: 1 }}>
        <TextField
          fullWidth size="small"
          placeholder='e.g. "Show overdue high priority bugs assigned to Ali"'
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          onKeyDown={(e) => e.key === 'Enter' && handleSearch()}
        />
        <Button variant="contained" onClick={() => handleSearch()} disabled={mutation.isPending}>
          {mutation.isPending ? <CircularProgress size={18} color="inherit" /> : <SearchIcon />}
        </Button>
      </Box>

      <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
        {EXAMPLE_QUERIES.map((q) => (
          <Chip key={q} label={q} size="small" variant="outlined" onClick={() => handleSearch(q)}
            sx={{ cursor: 'pointer', fontSize: 11 }} />
        ))}
      </Box>

      {mutation.isError && (
        <Alert severity="error">{(mutation.error as Error).message}</Alert>
      )}

      {result && (
        <Box>
          <Paper elevation={0} sx={{ p: 1.5, mb: 2, bgcolor: 'primary.light', borderRadius: 1.5 }}>
            <Typography variant="caption" color="primary.contrastText" fontWeight={600}>
              AI Interpretation:
            </Typography>
            <Typography variant="body2" color="primary.contrastText">{result.interpretation}</Typography>
          </Paper>

          <Typography variant="caption" color="text.secondary" mb={1} display="block">
            {result.tickets.length} result(s)
          </Typography>

          {result.tickets.length > 0 && (
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell sx={{ fontSize: 11 }}>Key</TableCell>
                  <TableCell sx={{ fontSize: 11 }}>Title</TableCell>
                  <TableCell sx={{ fontSize: 11 }}>Status</TableCell>
                  <TableCell sx={{ fontSize: 11 }}>Priority</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {result.tickets.slice(0, 20).map((t) => (
                  <TableRow key={t.id} hover>
                    <TableCell>
                      <Typography variant="caption" fontFamily="monospace" color={t.isOverdue ? 'error' : 'primary.main'}>
                        {t.ticketKey}
                      </Typography>
                    </TableCell>
                    <TableCell>
                      <Typography variant="caption" noWrap sx={{ maxWidth: 160, display: 'block' }}>{t.title}</Typography>
                    </TableCell>
                    <TableCell><StatusChip label={t.statusName} /></TableCell>
                    <TableCell><PriorityChip priority={t.priorityName} /></TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </Box>
      )}
    </Box>
  );
}
