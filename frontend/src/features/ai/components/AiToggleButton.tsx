import { Fab, Tooltip, Badge } from '@mui/material';
import { AutoAwesome as AiIcon } from '@mui/icons-material';
import { useAiStore } from '@/stores/aiStore';

export function AiToggleButton() {
  const { isOpen, togglePanel } = useAiStore();

  return (
    <Tooltip title={isOpen ? 'Close AI Assistant' : 'Open AI Assistant'} placement="left">
      <Fab
        color="primary"
        size="medium"
        onClick={togglePanel}
        sx={{
          position: 'fixed',
          bottom: 24,
          right: 24,
          zIndex: (t) => t.zIndex.drawer + 2,
          boxShadow: 4,
          background: isOpen ? undefined : 'linear-gradient(135deg, #0052CC, #6554C0)',
        }}
      >
        <Badge badgeContent="AI" color="secondary" sx={{ '& .MuiBadge-badge': { fontSize: 9, height: 16, minWidth: 16, top: -4, right: -4 } }}>
          <AiIcon />
        </Badge>
      </Fab>
    </Tooltip>
  );
}
