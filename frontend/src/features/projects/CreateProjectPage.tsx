import {
  Box, Typography, Card, CardContent, TextField, Button,
  CircularProgress, Alert,
} from '@mui/material';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { projectsApi } from '@/api/projects';
import { useNotification } from '@/hooks/useNotification';

const schema = z.object({
  name: z.string().min(2, 'Name must be at least 2 characters').max(100),
  key: z.string().min(2).max(10).regex(/^[A-Z0-9]+$/, 'Key must be uppercase letters/numbers'),
  description: z.string().max(500).optional(),
});
type FormValues = z.infer<typeof schema>;

export default function CreateProjectPage() {
  const navigate = useNavigate();
  const qc = useQueryClient();
  const { success, error } = useNotification();

  const { register, handleSubmit, formState: { errors } } = useForm<FormValues>({
    resolver: zodResolver(schema),
  });

  const mutation = useMutation({
    mutationFn: projectsApi.create,
    onSuccess: (data) => {
      qc.invalidateQueries({ queryKey: ['projects'] });
      success('Project created successfully');
      navigate(`/projects/${data.projectId}`);
    },
    onError: (err: Error) => error(err.message),
  });

  const onSubmit = (values: FormValues) => mutation.mutate({ projectKey: values.key, name: values.name, description: values.description, leadUserId: '', departmentId: 0 });

  return (
    <Box sx={{ maxWidth: 600 }}>
      <Typography variant="h5" sx={{ fontWeight: 700, mb: 3 }}>Create Project</Typography>
      <Card>
        <CardContent sx={{ p: 3 }}>
          {mutation.isError && (
            <Alert severity="error" sx={{ mb: 2 }}>{(mutation.error as Error).message}</Alert>
          )}
          <Box component="form" onSubmit={handleSubmit(onSubmit)} noValidate>
            <TextField
              {...register('name')}
              label="Project Name"
              fullWidth
              margin="normal"
              error={!!errors.name}
              helperText={errors.name?.message}
            />
            <TextField
              {...register('key')}
              label="Project Key"
              fullWidth
              margin="normal"
              error={!!errors.key}
              helperText={errors.key?.message || 'Short uppercase identifier (e.g. ITS, PROJ)'}
              slotProps={{ htmlInput: { style: { textTransform: 'uppercase' } } }}
            />
            <TextField
              {...register('description')}
              label="Description"
              fullWidth
              multiline
              rows={3}
              margin="normal"
              error={!!errors.description}
              helperText={errors.description?.message}
            />
            <Box sx={{ display: 'flex', gap: 2, mt: 3 }}>
              <Button
                type="submit"
                variant="contained"
                disabled={mutation.isPending}
              >
                {mutation.isPending ? <CircularProgress size={20} color="inherit" /> : 'Create Project'}
              </Button>
              <Button variant="outlined" onClick={() => navigate('/projects')}>Cancel</Button>
            </Box>
          </Box>
        </CardContent>
      </Card>
    </Box>
  );
}
