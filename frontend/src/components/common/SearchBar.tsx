import { InputBase, Box, InputAdornment } from '@mui/material';
import { Search as SearchIcon } from '@mui/icons-material';
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';

export function SearchBar() {
  const [value, setValue] = useState('');
  const navigate = useNavigate();

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && value.trim()) {
      navigate(`/tickets?search=${encodeURIComponent(value.trim())}`);
      setValue('');
    }
  };

  return (
    <Box
      sx={{
        bgcolor: 'action.hover',
        borderRadius: 1,
        px: 1.5,
        py: 0.5,
        display: 'flex',
        alignItems: 'center',
        border: '1px solid',
        borderColor: 'divider',
        '&:focus-within': { borderColor: 'primary.main', bgcolor: 'background.paper' },
      }}
    >
      <InputBase
        value={value}
        onChange={(e) => setValue(e.target.value)}
        onKeyDown={handleKeyDown}
        placeholder="Search tickets... (Enter)"
        startAdornment={
          <InputAdornment position="start">
            <SearchIcon fontSize="small" sx={{ color: 'text.secondary' }} />
          </InputAdornment>
        }
        sx={{ fontSize: 14, width: '100%' }}
      />
    </Box>
  );
}
