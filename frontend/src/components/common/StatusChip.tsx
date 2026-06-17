import { Chip, ChipProps } from '@mui/material';

interface StatusChipProps {
  label: string;
  category?: string;
  color?: string;
  size?: ChipProps['size'];
}

function categoryToColor(category?: string): ChipProps['color'] {
  switch (category?.toLowerCase()) {
    case 'todo': return 'default';
    case 'in progress': return 'primary';
    case 'done': return 'success';
    case 'blocked': return 'error';
    default: return 'default';
  }
}

export function StatusChip({ label, category, color, size = 'small' }: StatusChipProps) {
  if (color) {
    return (
      <Chip
        label={label}
        size={size}
        sx={{ bgcolor: color, color: '#fff', fontWeight: 500 }}
      />
    );
  }
  return <Chip label={label} size={size} color={categoryToColor(category)} variant="outlined" />;
}
