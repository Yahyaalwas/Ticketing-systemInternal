import { Box } from '@mui/material';
import { Outlet } from 'react-router-dom';
import { Sidebar } from './Sidebar';
import { Navbar } from './Navbar';
import { useUiStore } from '@/stores/uiStore';

const SIDEBAR_WIDTH = 240;
const SIDEBAR_COLLAPSED_WIDTH = 64;
const NAVBAR_HEIGHT = 56;

export function AppLayout() {
  const sidebarOpen = useUiStore((s) => s.sidebarOpen);
  const width = sidebarOpen ? SIDEBAR_WIDTH : SIDEBAR_COLLAPSED_WIDTH;

  return (
    <Box sx={{ display: 'flex', height: '100vh', overflow: 'hidden' }}>
      <Sidebar width={width} collapsedWidth={SIDEBAR_COLLAPSED_WIDTH} navbarHeight={NAVBAR_HEIGHT} />
      <Box sx={{ display: 'flex', flexDirection: 'column', flex: 1, minWidth: 0, ml: `${width}px`, transition: 'margin-left 0.2s' }}>
        <Navbar height={NAVBAR_HEIGHT} />
        <Box
          component="main"
          sx={{
            flex: 1,
            overflow: 'auto',
            mt: `${NAVBAR_HEIGHT}px`,
            p: 3,
            bgcolor: 'background.default',
          }}
        >
          <Outlet />
        </Box>
      </Box>
    </Box>
  );
}
