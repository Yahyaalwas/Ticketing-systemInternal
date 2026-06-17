import {
  Box, Grid, Card, CardContent, Typography, Skeleton,
  Table, TableBody, TableCell, TableHead, TableRow, Chip,
} from '@mui/material';
import {
  BugReport as BugIcon, CheckCircle as DoneIcon,
  Warning as OverdueIcon, Assignment as TotalIcon,
} from '@mui/icons-material';
import { useQuery } from '@tanstack/react-query';
import { PieChart, Pie, Cell, BarChart, Bar, XAxis, YAxis, Tooltip, Legend, ResponsiveContainer } from 'recharts';
import { getDashboard } from '@/api/dashboard';
import { StatusChip } from '@/components/common/StatusChip';
import { PriorityChip } from '@/components/common/PriorityChip';
import { formatDate } from '@/utils/date';

const COLORS = ['#0052CC', '#36B37E', '#FF8B00', '#FF5630', '#6554C0', '#00B8D9'];

interface KpiCardProps {
  title: string;
  value: number | string;
  icon: React.ReactNode;
  color: string;
  loading?: boolean;
}

function KpiCard({ title, value, icon, color, loading }: KpiCardProps) {
  return (
    <Card>
      <CardContent sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
        <Box sx={{ p: 1.5, borderRadius: 2, bgcolor: color + '22', color, display: 'flex' }}>
          {icon}
        </Box>
        <Box>
          <Typography variant="h4" fontWeight={700}>
            {loading ? <Skeleton width={60} /> : value}
          </Typography>
          <Typography variant="body2" color="text.secondary">{title}</Typography>
        </Box>
      </CardContent>
    </Card>
  );
}

export default function DashboardPage() {
  const { data, isLoading } = useQuery({ queryKey: ['dashboard'], queryFn: getDashboard });

  const kpis = [
    { title: 'Total Tickets', value: data?.totalTickets ?? 0, icon: <TotalIcon />, color: '#0052CC' },
    { title: 'Open', value: data?.openTickets ?? 0, icon: <BugIcon />, color: '#FF8B00' },
    { title: 'Resolved', value: data?.resolvedTickets ?? 0, icon: <DoneIcon />, color: '#36B37E' },
    { title: 'Overdue', value: data?.overdueTickets ?? 0, icon: <OverdueIcon />, color: '#FF5630' },
  ];

  const byStatusData = data?.ticketsByStatus?.map((s) => ({ name: s.statusName, value: s.count })) ?? [];
  const byPriorityData = data?.ticketsByPriority?.map((p) => ({ name: p.priorityName, value: p.count })) ?? [];

  return (
    <Box>
      <Typography variant="h5" fontWeight={700} mb={3}>Dashboard</Typography>

      <Grid container spacing={2} mb={3}>
        {kpis.map((kpi) => (
          <Grid key={kpi.title} size={{ xs: 12, sm: 6, md: 3 }}>
            <KpiCard {...kpi} loading={isLoading} />
          </Grid>
        ))}
      </Grid>

      <Grid container spacing={2} mb={3}>
        <Grid size={{ xs: 12, md: 6 }}>
          <Card>
            <CardContent>
              <Typography variant="subtitle1" fontWeight={600} mb={2}>By Status</Typography>
              {isLoading ? <Skeleton variant="rectangular" height={200} /> : (
                <ResponsiveContainer width="100%" height={200}>
                  <PieChart>
                    <Pie data={byStatusData} dataKey="value" nameKey="name" cx="50%" cy="50%" outerRadius={80}>
                      {byStatusData.map((_, i) => <Cell key={i} fill={COLORS[i % COLORS.length]} />)}
                    </Pie>
                    <Tooltip />
                    <Legend />
                  </PieChart>
                </ResponsiveContainer>
              )}
            </CardContent>
          </Card>
        </Grid>
        <Grid size={{ xs: 12, md: 6 }}>
          <Card>
            <CardContent>
              <Typography variant="subtitle1" fontWeight={600} mb={2}>By Priority</Typography>
              {isLoading ? <Skeleton variant="rectangular" height={200} /> : (
                <ResponsiveContainer width="100%" height={200}>
                  <BarChart data={byPriorityData}>
                    <XAxis dataKey="name" tick={{ fontSize: 12 }} />
                    <YAxis tick={{ fontSize: 12 }} />
                    <Tooltip />
                    <Bar dataKey="value" fill="#0052CC" radius={[4, 4, 0, 0]} />
                  </BarChart>
                </ResponsiveContainer>
              )}
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      <Card>
        <CardContent>
          <Typography variant="subtitle1" fontWeight={600} mb={2}>My Recent Tickets</Typography>
          {isLoading ? (
            Array.from({ length: 3 }).map((_, i) => <Skeleton key={i} height={48} sx={{ mb: 0.5 }} />)
          ) : (
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>Key</TableCell>
                  <TableCell>Title</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell>Priority</TableCell>
                  <TableCell>Due</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {(data?.myRecentTickets ?? []).map((t) => (
                  <TableRow key={t.id} hover>
                    <TableCell><Typography variant="caption" fontFamily="monospace">{t.ticketKey}</Typography></TableCell>
                    <TableCell><Typography variant="body2" noWrap sx={{ maxWidth: 280 }}>{t.title}</Typography></TableCell>
                    <TableCell><StatusChip label={t.statusName} category={t.statusCategory} /></TableCell>
                    <TableCell><PriorityChip priority={t.priorityName} /></TableCell>
                    <TableCell><Typography variant="caption">{formatDate(t.dueDate)}</Typography></TableCell>
                  </TableRow>
                ))}
                {(data?.myRecentTickets ?? []).length === 0 && (
                  <TableRow><TableCell colSpan={5} align="center"><Typography variant="body2" color="text.secondary">No tickets assigned to you</Typography></TableCell></TableRow>
                )}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>
    </Box>
  );
}
