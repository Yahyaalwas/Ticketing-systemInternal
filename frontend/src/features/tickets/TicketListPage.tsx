import {
  Box, Typography, Button, Table, TableBody, TableCell, TableHead,
  TableRow, Skeleton, TextField, InputAdornment, Chip, MenuItem, Select,
  FormControl, InputLabel, Tooltip,
} from '@mui/material';
import { Add as AddIcon, Search as SearchIcon } from '@mui/icons-material';
import { useQuery } from '@tanstack/react-query';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { useState, useEffect } from 'react';
import { ticketsApi } from '@/api/tickets';
import { StatusChip } from '@/components/common/StatusChip';
import { PriorityChip } from '@/components/common/PriorityChip';
import { UserAvatar } from '@/components/common/UserAvatar';
import { PaginationBar } from '@/components/common/PaginationBar';
import { EmptyState } from '@/components/common/EmptyState';
import { formatDate } from '@/utils/date';

export default function TicketListPage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const [search, setSearch] = useState(searchParams.get('search') ?? '');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const [statusFilter, setStatusFilter] = useState('');

  useEffect(() => {
    const s = searchParams.get('search');
    if (s) setSearch(s);
  }, [searchParams]);

  const projectId = searchParams.get('projectId') ?? '';

  const { data, isLoading } = useQuery({
    queryKey: ['tickets', projectId, page, pageSize, search, statusFilter],
    queryFn: () => ticketsApi.list({
      projectId,
      pageNumber: page,
      pageSize,
      search: search || undefined,
    }),
    enabled: !!projectId,
  });

  const tickets = data?.items ?? [];

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h5" sx={{ fontWeight: 700 }}>Tickets</Typography>
        <Button variant="contained" startIcon={<AddIcon />} onClick={() => navigate('/tickets/new')}>
          New Ticket
        </Button>
      </Box>

      <Box sx={{ display: 'flex', gap: 2, mb: 3, flexWrap: 'wrap' }}>
        <TextField
          size="small"
          placeholder="Search tickets..."
          value={search}
          onChange={(e) => { setSearch(e.target.value); setPage(1); }}
          sx={{ width: 280 }}
          slotProps={{
            input: { 'aria-label': 'Search tickets', startAdornment: <InputAdornment position="start"><SearchIcon fontSize="small" /></InputAdornment> },
          }}
        />
        <FormControl size="small" sx={{ minWidth: 150 }}>
          <InputLabel>Status</InputLabel>
          <Select value={statusFilter} label="Status" onChange={(e) => { setStatusFilter(e.target.value); setPage(1); }}>
            <MenuItem value="">All</MenuItem>
            <MenuItem value="Open">Open</MenuItem>
            <MenuItem value="In Progress">In Progress</MenuItem>
            <MenuItem value="Done">Done</MenuItem>
            <MenuItem value="Closed">Closed</MenuItem>
          </Select>
        </FormControl>
      </Box>

      <Box sx={{ bgcolor: 'background.paper', borderRadius: 1, border: '1px solid', borderColor: 'divider', overflow: 'hidden' }}>
        <Table aria-label="Tickets table">
          <TableHead>
            <TableRow>
              <TableCell width={100}>Key</TableCell>
              <TableCell>Title</TableCell>
              <TableCell width={140}>Status</TableCell>
              <TableCell width={120}>Priority</TableCell>
              <TableCell width={120}>Assignee</TableCell>
              <TableCell width={110}>Due Date</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {isLoading
              ? Array.from({ length: 8 }).map((_, i) => (
                  <TableRow key={i}>
                    {Array.from({ length: 6 }).map((_, j) => <TableCell key={j}><Skeleton /></TableCell>)}
                  </TableRow>
                ))
              : tickets.length === 0
              ? (
                <TableRow>
                  <TableCell colSpan={6}>
                    <EmptyState title="No tickets found" message="Try adjusting your filters." />
                  </TableCell>
                </TableRow>
              )
              : tickets.map((t) => (
                  <TableRow
                    key={t.id}
                    hover
                    sx={{ cursor: 'pointer' }}
                    onClick={() => navigate(`/tickets/${t.id}`)}
                  >
                    <TableCell>
                      <Typography variant="caption" color="primary.main" sx={{ fontFamily: 'monospace', fontWeight: 600 }}>
                        {t.ticketKey}
                      </Typography>
                    </TableCell>
                    <TableCell>
                      <Tooltip title={t.title}>
                        <Typography variant="body2" noWrap sx={{ maxWidth: 400 }}>{t.title}</Typography>
                      </Tooltip>
                    </TableCell>
                    <TableCell><StatusChip label={t.statusName} category={t.statusCategory} /></TableCell>
                    <TableCell><PriorityChip priority={t.priorityName} /></TableCell>
                    <TableCell>
                      {t.assigneeName ? (
                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                          <UserAvatar name={t.assigneeName} avatarUrl={t.assigneeAvatarUrl} size={24} />
                          <Typography variant="caption" noWrap>{t.assigneeName}</Typography>
                        </Box>
                      ) : (
                        <Typography variant="caption" color="text.disabled">Unassigned</Typography>
                      )}
                    </TableCell>
                    <TableCell>
                      <Typography variant="caption" color={t.dueDate && new Date(t.dueDate) < new Date() ? 'error' : 'text.secondary'}>
                        {formatDate(t.dueDate)}
                      </Typography>
                    </TableCell>
                  </TableRow>
                ))}
          </TableBody>
        </Table>
      </Box>

      {data && (
        <PaginationBar
          page={page}
          totalPages={data.totalPages}
          totalCount={data.totalCount}
          pageSize={pageSize}
          onPageChange={setPage}
          onPageSizeChange={(s) => { setPageSize(s); setPage(1); }}
        />
      )}
    </Box>
  );
}
