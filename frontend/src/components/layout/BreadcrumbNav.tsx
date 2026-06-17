import { Breadcrumbs, Link as MuiLink, Typography } from '@mui/material';
import { Link, useLocation, useMatches } from 'react-router-dom';
import NavigateNextIcon from '@mui/icons-material/NavigateNext';

const LABELS: Record<string, string> = {
  dashboard: 'Dashboard',
  projects: 'Projects',
  new: 'New Project',
  board: 'Board',
  tickets: 'Tickets',
  'my-work': 'My Work',
  kanban: 'Kanban',
  reports: 'Reports',
  admin: 'Administration',
};

export function BreadcrumbNav() {
  const location = useLocation();
  const segments = location.pathname.split('/').filter(Boolean);

  if (segments.length === 0) return null;

  return (
    <Breadcrumbs
      separator={<NavigateNextIcon sx={{ fontSize: 14 }} />}
      aria-label="breadcrumb"
      sx={{ fontSize: '0.8rem' }}
    >
      {segments.map((seg, i) => {
        const path = '/' + segments.slice(0, i + 1).join('/');
        const label = LABELS[seg] ?? seg;
        const isLast = i === segments.length - 1;

        return isLast ? (
          <Typography key={path} sx={{ fontSize: '0.8rem', fontWeight: 500 }} color="text.primary">
            {label}
          </Typography>
        ) : (
          <MuiLink
            key={path}
            component={Link}
            to={path}
            underline="hover"
            color="text.secondary"
            sx={{ fontSize: '0.8rem' }}
          >
            {label}
          </MuiLink>
        );
      })}
    </Breadcrumbs>
  );
}
