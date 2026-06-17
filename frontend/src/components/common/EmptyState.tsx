import { Box, Typography, Button } from '@mui/material';
import { InboxOutlined as InboxIcon } from '@mui/icons-material';

interface EmptyStateProps {
  title?: string;
  message?: string;
  actionLabel?: string;
  onAction?: () => void;
  icon?: React.ReactNode;
}

export function EmptyState({
  title = 'Nothing here yet',
  message,
  actionLabel,
  onAction,
  icon,
}: EmptyStateProps) {
  return (
    <Box sx={{ textAlign: 'center', py: 8, px: 3 }}>
      <Box sx={{ color: 'text.disabled', mb: 2 }}>
        {icon ?? <InboxIcon sx={{ fontSize: 56 }} />}
      </Box>
      <Typography variant="h6" color="text.secondary" gutterBottom>{title}</Typography>
      {message && <Typography variant="body2" color="text.disabled" mb={3}>{message}</Typography>}
      {actionLabel && onAction && (
        <Button variant="contained" onClick={onAction}>{actionLabel}</Button>
      )}
    </Box>
  );
}
