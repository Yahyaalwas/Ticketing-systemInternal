import {
  Popover, Box, Typography, List, ListItem, ListItemText,
  Button, Chip, Divider, CircularProgress,
} from '@mui/material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { getNotifications, markAllRead, markRead } from '@/api/notifications';
import { formatDistanceToNow } from '@/utils/date';

interface NotificationPanelProps {
  anchor: HTMLElement | null;
  onClose: () => void;
}

export function NotificationPanel({ anchor, onClose }: NotificationPanelProps) {
  const qc = useQueryClient();
  const open = Boolean(anchor);

  const { data, isLoading } = useQuery({
    queryKey: ['notifications'],
    queryFn: getNotifications,
    enabled: open,
  });

  const markAllMutation = useMutation({
    mutationFn: markAllRead,
    onSuccess: () => qc.invalidateQueries({ queryKey: ['notifications'] }),
  });

  const markOneMutation = useMutation({
    mutationFn: markRead,
    onSuccess: () => qc.invalidateQueries({ queryKey: ['notifications'] }),
  });

  const notifications = data ?? [];
  const unreadCount = notifications.filter((n) => !n.isRead).length;

  return (
    <Popover
      open={open}
      anchorEl={anchor}
      onClose={onClose}
      anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
      transformOrigin={{ vertical: 'top', horizontal: 'right' }}
      PaperProps={{ sx: { width: 360, maxHeight: 480 } }}
    >
      <Box sx={{ p: 2, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <Typography variant="subtitle1" fontWeight={600}>
          Notifications {unreadCount > 0 && <Chip label={unreadCount} size="small" color="error" sx={{ ml: 1 }} />}
        </Typography>
        {unreadCount > 0 && (
          <Button size="small" onClick={() => markAllMutation.mutate()}>Mark all read</Button>
        )}
      </Box>
      <Divider />
      {isLoading ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', p: 3 }}>
          <CircularProgress size={24} />
        </Box>
      ) : notifications.length === 0 ? (
        <Box sx={{ p: 3, textAlign: 'center' }}>
          <Typography color="text.secondary" variant="body2">No notifications</Typography>
        </Box>
      ) : (
        <List disablePadding sx={{ overflow: 'auto', maxHeight: 380 }}>
          {notifications.map((n, i) => (
            <Box key={n.id}>
              <ListItem
                alignItems="flex-start"
                sx={{
                  bgcolor: n.isRead ? 'transparent' : 'action.hover',
                  cursor: 'pointer',
                  '&:hover': { bgcolor: 'action.selected' },
                }}
                onClick={() => !n.isRead && markOneMutation.mutate(n.id)}
              >
                <ListItemText
                  primary={<Typography variant="body2" fontWeight={n.isRead ? 400 : 600}>{n.title}</Typography>}
                  secondary={
                    <>
                      <Typography variant="caption" display="block">{n.message}</Typography>
                      <Typography variant="caption" color="text.secondary">{formatDistanceToNow(n.createdAt)}</Typography>
                    </>
                  }
                />
              </ListItem>
              {i < notifications.length - 1 && <Divider component="li" />}
            </Box>
          ))}
        </List>
      )}
    </Popover>
  );
}
