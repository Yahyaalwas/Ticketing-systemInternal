import { Box, Pagination, Typography, Select, MenuItem, FormControl } from '@mui/material';

interface PaginationBarProps {
  page: number;
  totalPages: number;
  totalCount: number;
  pageSize: number;
  onPageChange: (page: number) => void;
  onPageSizeChange: (size: number) => void;
}

export function PaginationBar({
  page, totalPages, totalCount, pageSize, onPageChange, onPageSizeChange,
}: PaginationBarProps) {
  const start = (page - 1) * pageSize + 1;
  const end = Math.min(page * pageSize, totalCount);

  return (
    <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mt: 2, flexWrap: 'wrap', gap: 1 }}>
      <Typography variant="body2" color="text.secondary">
        {totalCount === 0 ? 'No results' : `${start}–${end} of ${totalCount}`}
      </Typography>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
        <FormControl size="small">
          <Select value={pageSize} onChange={(e) => onPageSizeChange(Number(e.target.value))}>
            {[10, 25, 50, 100].map((s) => <MenuItem key={s} value={s}>{s} / page</MenuItem>)}
          </Select>
        </FormControl>
        <Pagination
          count={totalPages}
          page={page}
          onChange={(_, p) => onPageChange(p)}
          size="small"
          shape="rounded"
        />
      </Box>
    </Box>
  );
}
