import { useMemo } from 'react';
import { ThemeProvider as MuiThemeProvider, CssBaseline } from '@mui/material';
import { useUIStore } from '@/stores/uiStore';
import { getTheme } from '@/theme/theme';

export function ThemeProvider({ children }: { children: React.ReactNode }) {
  const colorMode = useUIStore((s) => s.colorMode);
  const theme = useMemo(() => getTheme(colorMode), [colorMode]);

  return (
    <MuiThemeProvider theme={theme}>
      <CssBaseline />
      {children}
    </MuiThemeProvider>
  );
}
