import {
  Box, Typography, Button, Grid, Card, CardContent, CardActionArea,
  Chip, Skeleton, InputAdornment, TextField,
} from '@mui/material';
import { Add as AddIcon, Search as SearchIcon } from '@mui/icons-material';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { useState } from 'react';
import { listProjects } from '@/api/projects';
import { EmptyState } from '@/components/common/EmptyState';

export default function ProjectListPage() {
  const navigate = useNavigate();
  const [search, setSearch] = useState('');

  const { data, isLoading } = useQuery({
    queryKey: ['projects', search],
    queryFn: () => listProjects({ search: search || undefined }),
  });

  const projects = data?.items ?? [];

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h5" fontWeight={700}>Projects</Typography>
        <Button variant="contained" startIcon={<AddIcon />} onClick={() => navigate('/projects/new')}>
          New Project
        </Button>
      </Box>

      <TextField
        size="small"
        placeholder="Search projects..."
        value={search}
        onChange={(e) => setSearch(e.target.value)}
        sx={{ mb: 3, width: 320 }}
        InputProps={{
          startAdornment: <InputAdornment position="start"><SearchIcon fontSize="small" /></InputAdornment>,
        }}
      />

      {isLoading ? (
        <Grid container spacing={2}>
          {Array.from({ length: 6 }).map((_, i) => (
            <Grid key={i} size={{ xs: 12, sm: 6, md: 4 }}>
              <Skeleton variant="rectangular" height={140} sx={{ borderRadius: 1 }} />
            </Grid>
          ))}
        </Grid>
      ) : projects.length === 0 ? (
        <EmptyState
          title="No projects found"
          message="Create your first project to get started."
          actionLabel="New Project"
          onAction={() => navigate('/projects/new')}
        />
      ) : (
        <Grid container spacing={2}>
          {projects.map((p) => (
            <Grid key={p.id} size={{ xs: 12, sm: 6, md: 4 }}>
              <Card sx={{ height: '100%' }}>
                <CardActionArea onClick={() => navigate(`/projects/${p.id}`)} sx={{ height: '100%' }}>
                  <CardContent>
                    <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                      <Typography variant="caption" fontFamily="monospace" color="text.secondary">
                        {p.key}
                      </Typography>
                      <Chip
                        label={p.status}
                        size="small"
                        color={p.status === 'Active' ? 'success' : 'default'}
                      />
                    </Box>
                    <Typography variant="subtitle1" fontWeight={600} gutterBottom>{p.name}</Typography>
                    <Typography variant="body2" color="text.secondary" noWrap>
                      {p.description || 'No description'}
                    </Typography>
                    <Box sx={{ mt: 2, display: 'flex', gap: 2 }}>
                      <Typography variant="caption" color="text.secondary">
                        {p.memberCount} members
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        {p.ticketCount} tickets
                      </Typography>
                    </Box>
                  </CardContent>
                </CardActionArea>
              </Card>
            </Grid>
          ))}
        </Grid>
      )}
    </Box>
  );
}
