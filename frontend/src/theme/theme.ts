import { createTheme, alpha } from '@mui/material/styles';

declare module '@mui/material/styles' {
  interface Palette {
    sidebar: { bg: string; text: string; hover: string; active: string; };
  }
  interface PaletteOptions {
    sidebar?: { bg: string; text: string; hover: string; active: string; };
  }
}

export const getTheme = (mode: 'light' | 'dark') =>
  createTheme({
    palette: {
      mode,
      primary: { main: '#0052CC', light: '#2684FF', dark: '#0747A6' },
      secondary: { main: '#6554C0', light: '#8777D9', dark: '#403294' },
      error: { main: '#DE350B' },
      warning: { main: '#FF8B00' },
      success: { main: '#006644' },
      info: { main: '#0065FF' },
      sidebar: mode === 'light'
        ? { bg: '#0052CC', text: '#DEEBFF', hover: 'rgba(255,255,255,0.08)', active: 'rgba(255,255,255,0.15)' }
        : { bg: '#1A1F36', text: '#B3BAC5', hover: 'rgba(255,255,255,0.06)', active: 'rgba(255,255,255,0.12)' },
      background: {
        default: mode === 'light' ? '#F4F5F7' : '#0D1117',
        paper: mode === 'light' ? '#FFFFFF' : '#161B22',
      },
    },
    typography: {
      fontFamily: '"Inter", "Roboto", "Helvetica", "Arial", sans-serif',
      h4: { fontWeight: 600 },
      h5: { fontWeight: 600 },
      h6: { fontWeight: 600 },
    },
    shape: { borderRadius: 6 },
    components: {
      MuiButton: {
        styleOverrides: {
          root: { textTransform: 'none', fontWeight: 500 },
        },
      },
      MuiChip: {
        styleOverrides: {
          root: { fontWeight: 500 },
        },
      },
      MuiCard: {
        styleOverrides: {
          root: { backgroundImage: 'none' },
        },
      },
      MuiTableHead: {
        styleOverrides: {
          root: ({ theme }) => ({
            '& .MuiTableCell-head': {
              backgroundColor: alpha(theme.palette.primary.main, 0.04),
              fontWeight: 600,
              fontSize: '0.75rem',
              textTransform: 'uppercase',
              letterSpacing: '0.05em',
            },
          }),
        },
      },
    },
  });
