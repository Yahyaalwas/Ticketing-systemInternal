import { Box, Skeleton } from '@mui/material';

export function PageSkeleton() {
  return (
    <Box sx={{ p: 3 }}>
      <Skeleton variant="text" width={300} height={40} sx={{ mb: 2 }} />
      <Skeleton variant="rectangular" height={200} sx={{ mb: 2, borderRadius: 1 }} />
      <Skeleton variant="rectangular" height={200} sx={{ borderRadius: 1 }} />
    </Box>
  );
}
