import { Chip } from '@mui/material';
import { getPriorityColor } from '@/utils/format';

interface PriorityChipProps {
  priority: string | null | undefined;
  size?: 'small' | 'medium';
}

export function PriorityChip({ priority, size = 'small' }: PriorityChipProps) {
  if (!priority) return null;
  const color = getPriorityColor(priority);
  return (
    <Chip
      label={priority}
      size={size}
      sx={{ bgcolor: color + '22', color, borderColor: color, fontWeight: 600, fontSize: 11 }}
      variant="outlined"
    />
  );
}
