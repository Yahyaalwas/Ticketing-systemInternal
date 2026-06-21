import {
  AppBar, Toolbar, IconButton, Badge, Avatar, Box, Menu, MenuItem,
  Divider, Typography, Tooltip,
} from '@mui/material';
import {
  Notifications as NotificationsIcon,
  LightMode as LightIcon,
  DarkMode as DarkIcon,
  Logout as LogoutIcon,
  Person as PersonIcon,
} from '@mui/icons-material';
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuthStore } from '@/stores/authStore';
import { useUIStore } from '@/stores/uiStore';
import { NotificationPanel } from './NotificationPanel';
import { SearchBar } from '@/components/common/SearchBar';

interface NavbarProps {
  height: number;
}

export function Navbar({ height }: NavbarProps) {
  const navigate = useNavigate();
  const { user, logout } = useAuthStore();
  const { colorMode, toggleColorMode } = useUIStore();
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const [notifAnchor, setNotifAnchor] = useState<null | HTMLElement>(null);

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  const initials = user?.displayName
    ? user.displayName.split(' ').map((n) => n[0]).join('').slice(0, 2).toUpperCase()
    : '?';

  return (
    <AppBar
      position="fixed"
      elevation={0}
      sx={{
        bgcolor: 'background.paper',
        color: 'text.primary',
        borderBottom: '1px solid',
        borderColor: 'divider',
        height,
        zIndex: (t) => t.zIndex.drawer + 1,
      }}
    >
      <Toolbar sx={{ height, minHeight: `${height}px !important`, gap: 1 }}>
        <Box sx={{ flex: 1, maxWidth: 480 }}>
          <SearchBar />
        </Box>
        <Box sx={{ flexGrow: 1 }} />

        <Tooltip title={colorMode === 'light' ? 'Dark mode' : 'Light mode'}>
          <IconButton onClick={toggleColorMode} size="small">
            {colorMode === 'dark' ? <LightIcon /> : <DarkIcon />}
          </IconButton>
        </Tooltip>

        <Tooltip title="Notifications">
          <IconButton size="small" onClick={(e) => setNotifAnchor(e.currentTarget)}>
            <Badge badgeContent={3} color="error">
              <NotificationsIcon />
            </Badge>
          </IconButton>
        </Tooltip>

        <Tooltip title={user?.displayName ?? 'Account'}>
          <IconButton size="small" onClick={(e) => setAnchorEl(e.currentTarget)}>
            <Avatar sx={{ width: 32, height: 32, bgcolor: 'primary.main', fontSize: 13 }}>
              {initials}
            </Avatar>
          </IconButton>
        </Tooltip>

        <Menu anchorEl={anchorEl} open={Boolean(anchorEl)} onClose={() => setAnchorEl(null)}>
          <MenuItem disabled>
            <Box>
              <Typography variant="body2" sx={{ fontWeight: 600 }}>{user?.displayName}</Typography>
              <Typography variant="caption" color="text.secondary">{user?.email}</Typography>
            </Box>
          </MenuItem>
          <Divider />
          <MenuItem onClick={() => { setAnchorEl(null); navigate('/profile'); }}>
            <PersonIcon fontSize="small" sx={{ mr: 1 }} /> Profile
          </MenuItem>
          <MenuItem onClick={handleLogout}>
            <LogoutIcon fontSize="small" sx={{ mr: 1 }} /> Sign out
          </MenuItem>
        </Menu>

        <NotificationPanel anchor={notifAnchor} onClose={() => setNotifAnchor(null)} />
      </Toolbar>
    </AppBar>
  );
}
