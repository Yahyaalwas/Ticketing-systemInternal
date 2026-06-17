import {
  Box, Button, Typography, CircularProgress, Alert,
  Card, CardContent, Grid, Chip, List, ListItem, ListItemText,
  Select, MenuItem, FormControl, InputLabel, LinearProgress,
} from '@mui/material';
import { Assessment as ReportIcon, Refresh as RefreshIcon } from '@mui/icons-material';
import { useState } from 'react';
import { useMutation } from '@tanstack/react-query';
import { getExecutiveReport, type ExecutiveReportDto } from '@/api/ai';
import { formatDateTime } from '@/utils/date';

export function ExecutiveReportGenerator() {
  const [period, setPeriod] = useState<'Weekly' | 'Monthly'>('Weekly');
  const [report, setReport] = useState<ExecutiveReportDto | null>(null);

  const mutation = useMutation({
    mutationFn: (force = false) => getExecutiveReport(period, undefined, force),
    onSuccess: setReport,
  });

  const slaColor = (pct: number) => pct >= 95 ? 'success' : pct >= 80 ? 'warning' : 'error';

  return (
    <Box sx={{ p: 2, display: 'flex', flexDirection: 'column', gap: 2, height: '100%', overflow: 'auto' }}>
      <Box>
        <Typography variant="subtitle2" fontWeight={600} mb={0.5}>Executive AI Report</Typography>
        <Typography variant="caption" color="text.secondary">
          AI-generated narrative and metrics for stakeholders
        </Typography>
      </Box>

      <Box sx={{ display: 'flex', gap: 1 }}>
        <FormControl size="small" sx={{ minWidth: 140 }}>
          <InputLabel>Period</InputLabel>
          <Select value={period} label="Period" onChange={(e) => setPeriod(e.target.value as 'Weekly' | 'Monthly')}>
            <MenuItem value="Weekly">Last 7 Days</MenuItem>
            <MenuItem value="Monthly">Last 30 Days</MenuItem>
          </Select>
        </FormControl>
        <Button variant="contained" onClick={() => mutation.mutate(false)} disabled={mutation.isPending} startIcon={<ReportIcon />}>
          {mutation.isPending ? <CircularProgress size={18} color="inherit" /> : 'Generate Report'}
        </Button>
        {report && (
          <Button variant="outlined" onClick={() => mutation.mutate(true)} disabled={mutation.isPending} startIcon={<RefreshIcon />}>
            Refresh
          </Button>
        )}
      </Box>

      {mutation.isError && <Alert severity="error">{(mutation.error as Error).message}</Alert>}

      {report && (
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <Typography variant="subtitle1" fontWeight={700}>{report.periodLabel}</Typography>
            {report.wasFromCache && <Chip label="Cached" size="small" variant="outlined" />}
            <Typography variant="caption" color="text.secondary">Generated {formatDateTime(report.generatedAt)}</Typography>
          </Box>

          {/* KPI Grid */}
          <Grid container spacing={1.5}>
            {[
              { label: 'Total', value: report.metrics.totalTickets, color: '#0052CC' },
              { label: 'Open', value: report.metrics.openTickets, color: '#FF8B00' },
              { label: 'Resolved', value: report.metrics.resolvedTickets, color: '#36B37E' },
              { label: 'Overdue', value: report.metrics.overdueTickets, color: '#FF5630' },
            ].map((m) => (
              <Grid key={m.label} size={{ xs: 6 }}>
                <Card variant="outlined" sx={{ textAlign: 'center', py: 1 }}>
                  <Typography variant="h5" fontWeight={700} color={m.color}>{m.value}</Typography>
                  <Typography variant="caption" color="text.secondary">{m.label}</Typography>
                </Card>
              </Grid>
            ))}
          </Grid>

          {/* SLA */}
          <Card variant="outlined">
            <CardContent sx={{ py: 1.5 }}>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 0.5 }}>
                <Typography variant="caption" fontWeight={600}>SLA Compliance</Typography>
                <Chip label={`${report.metrics.slaCompliancePercent}%`} size="small"
                  color={slaColor(report.metrics.slaCompliancePercent)} />
              </Box>
              <LinearProgress
                variant="determinate"
                value={report.metrics.slaCompliancePercent}
                color={slaColor(report.metrics.slaCompliancePercent)}
                sx={{ height: 6, borderRadius: 3 }}
              />
            </CardContent>
          </Card>

          {/* Narrative */}
          <Card variant="outlined">
            <CardContent>
              <Typography variant="caption" fontWeight={600} color="text.secondary" display="block" mb={0.5}>EXECUTIVE SUMMARY</Typography>
              <Typography variant="body2">{report.narrativeSummary}</Typography>
            </CardContent>
          </Card>

          <Card variant="outlined">
            <CardContent>
              <Typography variant="caption" fontWeight={600} color="text.secondary" display="block" mb={0.5}>SLA HEALTH</Typography>
              <Typography variant="body2">{report.slaHealthReport}</Typography>
            </CardContent>
          </Card>

          {/* Risks */}
          {report.deliveryRisks.length > 0 && (
            <Box>
              <Typography variant="caption" fontWeight={600} color="error" display="block" mb={0.5}>DELIVERY RISKS</Typography>
              {report.deliveryRisks.map((r, i) => (
                <Typography key={i} variant="body2" sx={{ mb: 0.25 }}>⚠ {r}</Typography>
              ))}
            </Box>
          )}

          {report.topRecurringCategories.length > 0 && (
            <Box>
              <Typography variant="caption" fontWeight={600} color="text.secondary" display="block" mb={0.5}>TOP ISSUE CATEGORIES</Typography>
              {report.topRecurringCategories.map((c, i) => (
                <Typography key={i} variant="body2" sx={{ mb: 0.25 }}>• {c}</Typography>
              ))}
            </Box>
          )}
        </Box>
      )}
    </Box>
  );
}
