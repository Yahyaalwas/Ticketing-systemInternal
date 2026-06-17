import {
  Box, Drawer, List, ListItem, ListItemButton, ListItemIcon,
  ListItemText, Tooltip, Typography, IconButton, Divider,
} from '@mui/material';
import {
  Dashboard as DashboardIcon,
  FolderOpen as ProjectsIcon,
  BugReport as TicketsIcon,
  ViewKanban as KanbanIcon,
  BarChart as ReportsIcon,
  AdminPanelSettings as AdminIcon,
  ChevronLeft as CollapseIcon,
  ChevronRight as ExpandIcon,
} from '@mui/icons-material';
import { NavLink, useLocation } from 'react-router-dom';
import { useTheme } from '@mui/material/styles';
import { useUiStore } from '@/stores/uiStore';

interface SidebarProps {
  width: number;
  collapsedWidth: number;
  navbarHeight: number;
}

const NAV_ITEMS = [
  { label: 'Dashboard', icon: <DashboardIcon />, to: '/dashboard' },
  { label: 'Projects', icon: <ProjectsIcon />, to: '/projects' },
  { label: 'Tickets', icon: <TicketsIcon />, to: '/tickets' },
  { label: 'Kanban', icon: <KanbanIcon />, to: '/kanban' },
  { label: 'Reports', icon: <ReportsIcon />, to: '/reports' },
  { label: 'Admin', icon: <AdminIcon />, to: '/admin' },
];

export function Sidebar({ width, collapsedWidth, navbarHeight }: SidebarProps) {
  const theme = useTheme();
  const { sidebarOpen, toggleSidebar } = useUiStore();
  const location = useLocation();
  const sb = (theme.palette as any).sidebar;

  return (
    <Drawer
      variant="permanent"
      sx={{
        width,
        flexShrink: 0,
        '& .MuiDrawer-paper': {
          width,
          boxSizing: 'border-box',
          bgcolor: sb?.bg ?? '#0052CC',
          color: sb?.text ?? '#FFFFFF',
          borderRight: 'none',
          overflowX: 'hidden',
          transition: 'width 0.2s',
          top: 0,
          height: '100vh',
        },
      }}
    >
      {/* Logo area */}
      <Box
        sx={{
          height: navbarHeight,
          display: 'flex',
          alignItems: 'center',
          px: sidebarOpen ? 2 : 1,
          justifyContent: sidebarOpen ? 'space-between' : 'center',
          borderBottom: '1px solid rgba(255,255,255,0.15)',
        }}
      >
        {sidebarOpen && (
          <Typography variant="h6" fontWeight={700} color="inherit" noWrap>
            ITS
          </Typography>
        )}
        <IconButton onClick={toggleSidebar} size="small" sx={{ color: 'inherit' }}>
          {sidebarOpen ? <CollapseIcon /> : <ExpandIcon />}
        </IconButton>
      </Box>

      <List sx={{ pt: 1 }}>
        {NAV_ITEMS.map(({ label, icon, to }) => {
          const active = location.pathname.startsWith(to);
          return (
            <ListItem key={to} disablePadding sx={{ display: 'block' }}>
              <Tooltip title={!sidebarOpen ? label : ''} placement="right">
                <ListItemButton
                  component={NavLink}
                  to={to}
                  sx={{
                    minHeight: 44,
                    justifyContent: sidebarOpen ? 'initial' : 'center',
                    px: 2,
                    mx: 1,
                    mb: 0.5,
                    borderRadius: 1,
                    bgcolor: active ? (sb?.activeBg ?? 'rgba(255,255,255,0.2)') : 'transparent',
                    color: active ? (sb?.activeText ?? '#FFFFFF') : (sb?.text ?? 'rgba(255,255,255,0.8)'),
                    '&:hover': {
                      bgcolor: sb?.hoverBg ?? 'rgba(255,255,255,0.12)',
                    },
                  }}
                >
                  <ListItemIcon
                    sx={{
                      minWidth: 0,
                      mr: sidebarOpen ? 2 : 'auto',
                      justifyContent: 'center',
                      color: 'inherit',
                    }}
                  >
                    {icon}
                  </ListItemIcon>
                  {sidebarOpen && <ListItemText primary={label} primaryTypographyProps={{ fontSize: 14 }} />}
                </ListItemButton>
              </Tooltip>
            </ListItem>
          );
        })}
      </List>
    </Drawer>
  );
}
