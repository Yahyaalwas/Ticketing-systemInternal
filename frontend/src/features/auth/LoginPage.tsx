import {
  Box, Card, CardContent, TextField, Button, Typography,
  CircularProgress, Alert,
} from '@mui/material';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useMutation } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { login } from '@/api/auth';
import { useAuthStore } from '@/stores/authStore';

const schema = z.object({
  userPrincipalName: z.string().min(1, 'Required').email('Invalid email'),
  password: z.string().min(1, 'Required'),
});
type FormValues = z.infer<typeof schema>;

export default function LoginPage() {
  const navigate = useNavigate();
  const setAuth = useAuthStore((s) => s.setAuth);

  const { register, handleSubmit, formState: { errors } } = useForm<FormValues>({
    resolver: zodResolver(schema),
  });

  const mutation = useMutation({
    mutationFn: login,
    onSuccess: (data) => {
      setAuth(data.token, {
        id: data.userId,
        displayName: data.displayName,
        email: data.email,
        roles: data.roles,
      });
      navigate('/dashboard');
    },
  });

  const onSubmit = (values: FormValues) => mutation.mutate(values);

  return (
    <Box
      sx={{
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        background: 'linear-gradient(135deg, #0052CC 0%, #0747A6 50%, #172B4D 100%)',
      }}
    >
      <Card sx={{ width: '100%', maxWidth: 420, mx: 2 }}>
        <CardContent sx={{ p: 4 }}>
          <Box sx={{ textAlign: 'center', mb: 4 }}>
            <Typography variant="h4" fontWeight={700} color="primary">ITS</Typography>
            <Typography color="text.secondary" variant="body2" mt={0.5}>
              Internal Ticketing System
            </Typography>
          </Box>

          {mutation.isError && (
            <Alert severity="error" sx={{ mb: 2 }}>
              {(mutation.error as Error).message}
            </Alert>
          )}

          <Box component="form" onSubmit={handleSubmit(onSubmit)} noValidate>
            <TextField
              {...register('userPrincipalName')}
              label="Email / UPN"
              fullWidth
              margin="normal"
              error={!!errors.userPrincipalName}
              helperText={errors.userPrincipalName?.message}
              autoComplete="username"
            />
            <TextField
              {...register('password')}
              label="Password"
              type="password"
              fullWidth
              margin="normal"
              error={!!errors.password}
              helperText={errors.password?.message}
              autoComplete="current-password"
            />
            <Button
              type="submit"
              fullWidth
              variant="contained"
              size="large"
              disabled={mutation.isPending}
              sx={{ mt: 3, py: 1.5 }}
            >
              {mutation.isPending ? <CircularProgress size={22} color="inherit" /> : 'Sign In'}
            </Button>
          </Box>
        </CardContent>
      </Card>
    </Box>
  );
}
