import {
  Box, Typography, Button, Chip, LinearProgress, Skeleton,
  Tooltip, Alert, Divider, List, ListItem, ListItemText,
  CircularProgress, IconButton,
} from '@mui/material';
import {
  AutoAwesome as AiIcon, Refresh as RefreshIcon,
  CheckCircle as CheckIcon, Warning as WarnIcon, Block as BlockIcon,
  TaskAlt as ActionIcon,
} from '@mui/icons-material';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { summarizeTicket } from '@/api/ai';
import { TicketAiSummaryDto } from '@/api/ai';
import { formatDistanceToNow } from '@/utils/date';

const SEVERITY_COLOR = (pct: number) =>
  pct >= 75 ? 'success' : pct >= 50 ? 'warning' : 'error';

interface AiSummaryCardProps {
  ticketId: string;
}

export function AiSummaryCard({ ticketId }: AiSummaryCardProps) {
  const qc = useQueryClient();

  const { data: summary, isLoading, isFetching } = useQuery({
    queryKey: ['ai-summary', ticketId],
    queryFn: () => summarizeTicket(ticketId, false),
    staleTime: 60 * 60 * 1000,
    retry: false,
    enabled: false, // only load on demand
  });

  const refreshMutation = useMutation({
    mutationFn: () => summarizeTicket(ticketId, true),
    onSuccess: (data) => {
      qc.setQueryData(['ai-summary', ticketId], data);
    },
  });

  const generateMutation = useMutation({
    mutationFn: () => summarizeTicket(ticketId, false),
    onSuccess: (data) => {
      qc.setQueryData(['ai-summary', ticketId], data);
    },
  });

  const current = summary ?? (refreshMutation.data || generateMutation.data);
  const isGenerating = generateMutation.isPending || refreshMutation.isPending;

  return (
    <Box>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: current ? 2 : 0 }}>
        <AiIcon sx={{ color: 'primary.main', fontSize: 20 }} />
        <Typography variant="subtitle1" fontWeight={700}>AI Summary</Typography>
        {current?.wasFromCache && (
          <Chip label="cached" size="small" variant="outlined" sx={{ height: 18, fontSize: 10 }} />
        )}
        {current && (
          <Tooltip title={`Generated ${formatDistanceToNow(current.generatedAt)}`}>
            <Typography variant="caption" color="text.secondary" sx={{ ml: 'auto', cursor: 'default' }}>
              {formatDistanceToNow(current.generatedAt)}
            </Typography>
          </Tooltip>
        )}
        {current && (
          <Tooltip title="Regenerate">
            <span>
              <IconButton size="small" onClick={() => refreshMutation.mutate()} disabled={isGenerating}>
                {isGenerating ? <CircularProgress size={14} /> : <RefreshIcon fontSize="small" />}
              </IconButton>
            </span>
          </Tooltip>
        )}
      </Box>

      {isGenerating && (
        <Box>
          <Typography variant="caption" color="text.secondary" mb={0.5} display="block">Analyzing ticket...</Typography>
          <LinearProgress sx={{ mb: 1 }} />
          {[1, 2, 3].map(i => <Skeleton key={i} height={16} sx={{ mb: 0.5 }} width={`${80 - i * 10}%`} />)}
        </Box>
      )}

      {!current && !isGenerating && (
        <Box sx={{ textAlign: 'center', py: 2 }}>
          <AiIcon sx={{ fontSize: 40, color: 'action.disabled', mb: 1 }} />
          <Typography variant="body2" color="text.secondary" mb={2}>
            Get an AI-powered analysis of this ticket including executive summary, blockers, risks, and action items.
          </Typography>
          <Button
            variant="outlined"
            startIcon={<AiIcon />}
            onClick={() => generateMutation.mutate()}
            disabled={isGenerating}
          >
            Generate AI Summary
          </Button>
        </Box>
      )}

      {current && !isGenerating && (
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
          {/* Confidence score */}
          <Box>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 0.5 }}>
              <Typography variant="caption" fontWeight={600} color="text.secondary">COMPLETION CONFIDENCE</Typography>
              <Chip
                label={`${current.completionConfidencePercent}%`}
                size="small"
                color={SEVERITY_COLOR(current.completionConfidencePercent)}
              />
            </Box>
            <LinearProgress
              variant="determinate"
              value={current.completionConfidencePercent}
              color={SEVERITY_COLOR(current.completionConfidencePercent)}
              sx={{ height: 6, borderRadius: 3 }}
            />
          </Box>

          {/* Executive summary */}
          <Box>
            <Typography variant="caption" fontWeight={600} color="text.secondary" display="block" mb={0.5}>
              EXECUTIVE SUMMARY
            </Typography>
            <Typography variant="body2" sx={{ lineHeight: 1.7 }}>{current.executiveSummary}</Typography>
          </Box>

          {/* Simple explanation */}
          <Box sx={{ bgcolor: 'action.hover', borderRadius: 1.5, p: 1.5 }}>
            <Typography variant="caption" fontWeight={600} color="text.secondary" display="block" mb={0.5}>
              SIMPLE EXPLANATION
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ lineHeight: 1.6 }}>
              {current.simpleExplanation}
            </Typography>
          </Box>

          <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap' }}>
            {/* Blockers */}
            {current.blockers.length > 0 && (
              <Box sx={{ flex: 1, minWidth: 180 }}>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5, mb: 0.75 }}>
                  <BlockIcon sx={{ fontSize: 14, color: 'error.main' }} />
                  <Typography variant="caption" fontWeight={600} color="error.main">BLOCKERS</Typography>
                </Box>
                {current.blockers.map((b, i) => (
                  <Typography key={i} variant="body2" sx={{ mb: 0.25 }}>• {b}</Typography>
                ))}
              </Box>
            )}

            {/* Risks */}
            {current.risks.length > 0 && (
              <Box sx={{ flex: 1, minWidth: 180 }}>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5, mb: 0.75 }}>
                  <WarnIcon sx={{ fontSize: 14, color: 'warning.main' }} />
                  <Typography variant="caption" fontWeight={600} color="warning.main">RISKS</Typography>
                </Box>
                {current.risks.map((r, i) => (
                  <Typography key={i} variant="body2" sx={{ mb: 0.25 }}>• {r}</Typography>
                ))}
              </Box>
            )}
          </Box>

          {/* Action items */}
          {current.actionItems.length > 0 && (
            <Box>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5, mb: 0.75 }}>
                <ActionIcon sx={{ fontSize: 14, color: 'success.main' }} />
                <Typography variant="caption" fontWeight={600} color="success.main">ACTION ITEMS</Typography>
              </Box>
              {current.actionItems.map((a, i) => (
                <Box key={i} sx={{ display: 'flex', alignItems: 'flex-start', gap: 0.75, mb: 0.25 }}>
                  <CheckIcon sx={{ fontSize: 14, color: 'success.main', mt: '3px', flexShrink: 0 }} />
                  <Typography variant="body2">{a}</Typography>
                </Box>
              ))}
            </Box>
          )}

          {/* Key decisions */}
          {current.keyDecisions.length > 0 && (
            <Box>
              <Typography variant="caption" fontWeight={600} color="text.secondary" display="block" mb={0.75}>
                KEY DECISIONS
              </Typography>
              {current.keyDecisions.map((d, i) => (
                <Typography key={i} variant="body2" sx={{ mb: 0.25 }}>📌 {d}</Typography>
              ))}
            </Box>
          )}
        </Box>
      )}
    </Box>
  );
}
