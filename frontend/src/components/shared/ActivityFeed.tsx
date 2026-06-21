import {
  Box, Typography, Tooltip, Button, CircularProgress, Skeleton,
} from '@mui/material';
import {
  Add as AddIcon, Edit as EditIcon, SwapHoriz as TransitionIcon,
  Person as PersonIcon, Flag as FlagIcon, Comment as CommentIcon,
  AttachFile as AttachIcon, Schedule as ScheduleIcon, Label as LabelIcon,
  Visibility as WatchIcon,
} from '@mui/icons-material';
import { cloneElement, type ReactElement } from 'react';
import { useInfiniteQuery } from '@tanstack/react-query';
import { ticketsApi } from '@/api/tickets';
import { ActivityItemDto } from '@/types';
import { formatDistanceToNow, formatDateTime } from '@/utils/date';
import { UserAvatar } from '@/components/common/UserAvatar';

const TYPE_META: Record<string, { icon: ReactElement; color: string }> = {
  created: { icon: <AddIcon />, color: '#36B37E' },
  status_changed: { icon: <TransitionIcon />, color: '#0052CC' },
  transitioned: { icon: <TransitionIcon />, color: '#0052CC' },
  assigned: { icon: <PersonIcon />, color: '#6554C0' },
  priority_changed: { icon: <FlagIcon />, color: '#FF8B00' },
  comment_added: { icon: <CommentIcon />, color: '#00B8D9' },
  attachment_added: { icon: <AttachIcon />, color: '#36B37E' },
  due_date_changed: { icon: <ScheduleIcon />, color: '#FF8B00' },
  label_added: { icon: <LabelIcon />, color: '#6554C0' },
  label_removed: { icon: <LabelIcon />, color: '#97A0AF' },
  watcher_added: { icon: <WatchIcon />, color: '#00B8D9' },
};

function getDescription(item: ActivityItemDto): string {
  const { activityType, fieldName, oldValue, newValue } = item;
  const key = activityType.toLowerCase();
  if (key === 'created') return 'created this ticket';
  if (key === 'status_changed' || key === 'transitioned')
    return oldValue ? `changed status from "${oldValue}" → "${newValue}"` : `set status to "${newValue}"`;
  if (key === 'assigned') return newValue ? `assigned to ${newValue}` : 'unassigned';
  if (key === 'priority_changed')
    return oldValue ? `changed priority from ${oldValue} → ${newValue}` : `set priority to ${newValue}`;
  if (key === 'comment_added') return 'added a comment';
  if (key === 'attachment_added') return `uploaded "${newValue}"`;
  if (key === 'due_date_changed') return newValue ? `set due date to ${newValue}` : 'removed due date';
  if (key === 'label_added') return `added label "${newValue}"`;
  if (key === 'label_removed') return `removed label "${oldValue}"`;
  if (fieldName && newValue) return `updated ${fieldName} to "${newValue}"`;
  return activityType.replace(/_/g, ' ').toLowerCase();
}

interface ActivityFeedProps {
  ticketId: string;
}

export function ActivityFeed({ ticketId }: ActivityFeedProps) {
  const { data, fetchNextPage, hasNextPage, isFetchingNextPage, isLoading } = useInfiniteQuery({
    queryKey: ['activity', ticketId],
    queryFn: ({ pageParam }) => ticketsApi.getActivity(ticketId, pageParam as number, 20),
    initialPageParam: 1,
    getNextPageParam: (last) => last.pageNumber < last.totalPages ? last.pageNumber + 1 : undefined,
    staleTime: 30_000,
  });

  const items = data?.pages.flatMap(p => p.items) ?? [];

  if (isLoading) {
    return (
      <Box sx={{ py: 1 }}>
        {[1, 2, 3].map(i => (
          <Box key={i} sx={{ display: 'flex', gap: 1.5, mb: 2, alignItems: 'flex-start' }}>
            <Skeleton variant="circular" width={32} height={32} />
            <Box sx={{ flex: 1 }}>
              <Skeleton width="60%" height={18} />
              <Skeleton width="30%" height={14} />
            </Box>
          </Box>
        ))}
      </Box>
    );
  }

  if (items.length === 0) {
    return (
      <Box sx={{ textAlign: 'center', py: 3 }}>
        <Typography variant="body2" color="text.disabled">No activity yet.</Typography>
      </Box>
    );
  }

  return (
    <Box>
      {items.map((item, idx) => {
        const meta = TYPE_META[item.activityType.toLowerCase()] ?? { icon: <EditIcon />, color: '#97A0AF' };
        const isLast = idx === items.length - 1;

        return (
          <Box key={item.id} sx={{ display: 'flex', gap: 1.5, alignItems: 'flex-start', position: 'relative' }}>
            {!isLast && (
              <Box sx={{ position: 'absolute', left: 15, top: 32, bottom: -8, width: 2, bgcolor: 'divider', zIndex: 0 }} />
            )}
            <Box sx={{
              width: 32, height: 32, borderRadius: '50%', flexShrink: 0, zIndex: 1,
              bgcolor: meta.color + '20', color: meta.color,
              display: 'flex', alignItems: 'center', justifyContent: 'center',
              border: `2px solid ${meta.color}40`,
            }}>
              {cloneElement(meta.icon, { sx: { fontSize: 14 } } as object)}
            </Box>
            <Box sx={{ pb: 2.5, flex: 1, minWidth: 0 }}>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.75, flexWrap: 'wrap' }}>
                <UserAvatar name={item.actorName} avatarUrl={item.actorAvatarUrl} size={20} />
                <Typography variant="body2" sx={{ fontWeight: 600 }} component="span">{item.actorName}</Typography>
                <Typography variant="body2" color="text.secondary" component="span">{getDescription(item)}</Typography>
              </Box>
              <Tooltip title={formatDateTime(item.occurredAt)} placement="bottom-start">
                <Typography variant="caption" color="text.disabled" sx={{ cursor: 'default' }}>
                  {formatDistanceToNow(item.occurredAt)}
                </Typography>
              </Tooltip>
            </Box>
          </Box>
        );
      })}

      {hasNextPage && (
        <Box sx={{ textAlign: 'center', pt: 1 }}>
          <Button size="small" onClick={() => fetchNextPage()} disabled={isFetchingNextPage}>
            {isFetchingNextPage ? <CircularProgress size={14} sx={{ mr: 0.5 }} /> : null}
            Load more
          </Button>
        </Box>
      )}
    </Box>
  );
}
