import { Box } from '@mui/material';
import { Outlet } from 'react-router-dom';
import { Sidebar } from './Sidebar';
import { Navbar } from './Navbar';
import { useUiStore } from '@/stores/uiStore';
import { useAiStore } from '@/stores/aiStore';
import { AiAssistantPanel } from '@/features/ai/AiAssistantPanel';
import { AiToggleButton } from '@/features/ai/components/AiToggleButton';

const SIDEBAR_WIDTH = 240;
const SIDEBAR_COLLAPSED_WIDTH = 64;
const NAVBAR_HEIGHT = 56;
const AI_PANEL_WIDTH = 480;

export function AppLayout() {
  const sidebarOpen = useUiStore((s) => s.sidebarOpen);
  const aiOpen = useAiStore((s) => s.isOpen);
  const sidebarWidth = sidebarOpen ? SIDEBAR_WIDTH : SIDEBAR_COLLAPSED_WIDTH;

  return (
    <Box sx={{ display: 'flex', height: '100vh', overflow: 'hidden' }}>
      <Sidebar width={sidebarWidth} collapsedWidth={SIDEBAR_COLLAPSED_WIDTH} navbarHeight={NAVBAR_HEIGHT} />
      <Box
        sx={{
          display: 'flex', flexDirection: 'column', flex: 1, minWidth: 0,
          ml: `${sidebarWidth}px`,
          mr: aiOpen ? `${AI_PANEL_WIDTH}px` : 0,
          transition: 'margin 0.2s',
        }}
      >
        <Navbar height={NAVBAR_HEIGHT} />
        <Box
          component="main"
          sx={{
            flex: 1, overflow: 'auto',
            mt: `${NAVBAR_HEIGHT}px`,
            p: 3,
            bgcolor: 'background.default',
          }}
        >
          <Outlet />
        </Box>
      </Box>
      <AiAssistantPanel />
      <AiToggleButton />
    </Box>
  );
}
