import { Box, Typography, Card, CardContent, Grid, Skeleton } from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import {
  BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, Legend,
  LineChart, Line, ResponsiveContainer, PieChart, Pie, Cell,
} from 'recharts';
import { dashboardApi } from '@/api/dashboard';

const COLORS = ['#0052CC', '#36B37E', '#FF8B00', '#FF5630', '#6554C0', '#00B8D9'];

export default function ReportsPage() {
  const { data, isLoading } = useQuery({ queryKey: ['dashboard'], queryFn: dashboardApi.get });

  const byStatus = data?.ticketsByStatus?.map((s) => ({ name: s.statusName, value: s.count })) ?? [];
  const byPriority = data?.ticketsByPriority?.map((p) => ({ name: p.priorityName, value: p.count })) ?? [];

  return (
    <Box>
      <Typography variant="h5" sx={{ fontWeight: 700, mb: 3 }}>Reports</Typography>
      <Grid container spacing={3}>
        <Grid size={{ xs: 12, md: 6 }}>
          <Card>
            <CardContent>
              <Typography variant="subtitle1" sx={{ fontWeight: 600, mb: 2 }}>Tickets by Status</Typography>
              {isLoading ? <Skeleton variant="rectangular" height={250} /> : (
                <ResponsiveContainer width="100%" height={250}>
                  <PieChart>
                    <Pie data={byStatus} dataKey="value" nameKey="name" cx="50%" cy="50%" outerRadius={100} label>
                      {byStatus.map((_, i) => <Cell key={i} fill={COLORS[i % COLORS.length]} />)}
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
              <Typography variant="subtitle1" sx={{ fontWeight: 600, mb: 2 }}>Tickets by Priority</Typography>
              {isLoading ? <Skeleton variant="rectangular" height={250} /> : (
                <ResponsiveContainer width="100%" height={250}>
                  <BarChart data={byPriority}>
                    <CartesianGrid strokeDasharray="3 3" />
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
    </Box>
  );
}
