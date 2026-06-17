import { Box, useTheme } from '@mui/material';

interface MarkdownRendererProps {
  html: string;
  compact?: boolean;
}

export function MarkdownRenderer({ html, compact = false }: MarkdownRendererProps) {
  const theme = useTheme();
  const dark = theme.palette.mode === 'dark';

  return (
    <Box
      sx={{
        lineHeight: 1.7,
        fontSize: compact ? '0.875rem' : '0.9375rem',
        color: 'text.primary',
        wordBreak: 'break-word',
        '& h1': { fontSize: '1.75rem', fontWeight: 700, mb: 1, mt: 2.5, borderBottom: '1px solid', borderColor: 'divider', pb: 0.5 },
        '& h2': { fontSize: '1.375rem', fontWeight: 700, mb: 1, mt: 2 },
        '& h3': { fontSize: '1.125rem', fontWeight: 600, mb: 0.5, mt: 1.5 },
        '& p': { mt: 0, mb: compact ? 0.75 : 1.25 },
        '& p:last-child': { mb: 0 },
        '& code': {
          bgcolor: dark ? 'rgba(255,255,255,0.1)' : 'rgba(0,0,0,0.06)',
          color: dark ? '#e06c75' : '#c7254e',
          px: 0.75, py: 0.25, borderRadius: 0.75,
          fontFamily: '"JetBrains Mono", "Fira Code", monospace',
          fontSize: '0.85em',
        },
        '& pre': {
          bgcolor: dark ? '#1e1e2e' : '#f6f8fa',
          border: '1px solid', borderColor: 'divider',
          p: 2, borderRadius: 1.5, overflow: 'auto', mb: 1.5,
          '& code': { bgcolor: 'transparent', color: 'inherit', p: 0 },
        },
        '& ul, & ol': { pl: 3, mb: compact ? 0.75 : 1.25, mt: 0 },
        '& li': { mb: 0.25 },
        '& blockquote': {
          borderLeft: `4px solid ${theme.palette.primary.main}`,
          bgcolor: dark ? 'rgba(255,255,255,0.04)' : 'rgba(0,82,204,0.04)',
          pl: 2, ml: 0, py: 0.5, mb: 1.25, borderRadius: '0 6px 6px 0',
          '& p': { mb: 0 },
        },
        '& a': { color: 'primary.main', textDecoration: 'none', '&:hover': { textDecoration: 'underline' } },
        '& table': { width: '100%', borderCollapse: 'collapse', mb: 1.25, fontSize: '0.875rem' },
        '& th': { bgcolor: 'action.hover', fontWeight: 600, textAlign: 'left', border: '1px solid', borderColor: 'divider', p: '6px 12px' },
        '& td': { border: '1px solid', borderColor: 'divider', p: '6px 12px' },
        '& img': { maxWidth: '100%', height: 'auto', borderRadius: 1 },
        '& hr': { border: 'none', borderTop: '1px solid', borderColor: 'divider', my: 2 },
      }}
      dangerouslySetInnerHTML={{ __html: html || '<p style="color: #97A0AF; font-style: italic">No description provided.</p>' }}
    />
  );
}
